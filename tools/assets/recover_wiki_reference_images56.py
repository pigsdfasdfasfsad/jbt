#!/usr/bin/env python3
"""Pass56: deterministic, private, provenance-bound offline wiki gallery pack.

Pictures from the owner's images.zip are *rendered screenshots/reference art*
and must never be used as source Roblox UV/normal texture bytes. No download,
web request, original Luau execution, or overwrite of an existing owner pack.
"""
from __future__ import annotations

import argparse
import hashlib
from io import BytesIO
import json
import re
from pathlib import Path
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED
import gzip
from PIL import Image

IMAGES_SHA='b07b058367d115b9710feb25accf0ac5cc8a1de74b4b1769a890ff0c457115f0'
DOCS_SHA='e2ebf80af7b6591af0de885a9cee9e863da398407fdc741ac3f215077936984f'
FORMAT='twr-wiki-weapon-reference-v1'
MAX_FILE_BYTES=1_500_000
ALIASES={
 '2x4 w- Barbed Wire':'2x4BW','AA-12':'AA12',
 'Aero Survival Rifle':'ASR','Barrett M82A1':'M82A1','Baseball Bat':'Bat',
 'Benelli M4':'BM4','CMMG Mk47 Mutant':'Mk47','Colt Python':'Python',
 'CZ Scorpion EVO Micro K':'SEMK','Desert Eagle':'DEagle','Fire Axe':'Axe',
 'FN FAL':'FAL','Frying Pan':'Pan','Gepard PDW':'Gepard','Glock 17':'G17',
 'HK P30L':'P30L','Ingram MAC-10':'M10','KAC PDW':'KAC',
 'Kriss Vector':'Vector','LH9 MKII':'LH9','LWRC IC-PSD':'LWRC',
 'M1 Garand':'M1','M110 SASS':'M110','Mossberg 500':'M500',
 'MP-443 Grach':'MP443','Mosin Nagant':'Mosin','Obrez Mosin':'Obrez',
 'OTs-14 Groza':'OTs14','PB 6P9':'PB6P9','Pool Cue':'Cue',
 'PP-91 Kedr':'PP91','Ruger 10-22':'R1022',
 'Sawn Off Shotgun':'Shorty','Serbu Super Shorty':'SOS','SKO Shorty':'SKO',
 'Sledge Hammer':'SledgeHammer','Spiked Baseball Bat':'SBB',
 'Sten Mk V':'MkV','Taurus Judge':'Judge','Taurus Model 66':'M66',
 'Thompson M1':'Thompson','Walther P38':'P38',
 'Winchester Model 1892':'M92','Winchester Model 70':'M70','XD-9':'XD9',
}
SKIN_SUFFIXES={
 'AD':'Art Deco','BO':'Blackout','BS':'Brushed Silver','SC':'Sports Car',
 'Xmas19':'Christmas 2019','Xmas20':'Christmas 2020'
}
POSE_SUFFIXES={'ADS':'ADS','ADSEmpty':'ADS empty','Hip':'Hip',
               'HipEmpty':'Hip empty','UI':'UI'}


def canonical(name: str)->str:
    return re.sub('[^a-z0-9]','',name.lower())


def file_sha256(path: Path)->str:
    with path.open('rb') as f:
        return hashlib.file_digest(f,'sha256').hexdigest()


def source_docs(archive: ZipFile,stem: str)->tuple[str,str]:
    try: body=archive.read('image-docs/'+stem+'.md').decode('utf-8-sig')
    except KeyError: return '', ''
    def field(key):
        found=re.search(r'^\| '+re.escape(key)+r' \| ([^|]+) \|',body,re.M)
        return found.group(1).strip() if found else ''
    return field('Subject'),field('Subject type')


def image_entry(z: ZipFile, docs: ZipFile, name: str, content: dict)->str:
    if not re.fullmatch(r'[A-Za-z0-9_ .+()-]+\.(?:png|jpg|jpeg)',name,re.I):
        raise ValueError('Unrecognized image basename: '+name)
    key='reference-images/'+name
    if key not in content:
        raw=z.read('images/'+name)
        if not 0<len(raw)<=MAX_FILE_BYTES:
            raise ValueError('Image too large or empty')
        with Image.open(BytesIO(raw)) as img: img.verify()
        with Image.open(BytesIO(raw)) as img:
            width,height=img.size
            if min(width,height)<1 or max(width,height)>4096 or width*height>8_000_000:
                raise ValueError('Image dimensions out of bounds')
        subject,kind=source_docs(docs,name.rsplit('.',1)[0])
        content[key]={
            'bytes':raw,'sha256':hashlib.sha256(raw).hexdigest(),
            'length':len(raw),'width':width,'height':height,
            'subject':subject[:128],'subjectType':kind[:40]
        }
    return key


def build(images_zip:Path, docs_zip:Path, catalog_path:Path,
          source_skins:Path, output:Path)->dict:
    for path,sha in ((images_zip,IMAGES_SHA),(docs_zip,DOCS_SHA)):
        if file_sha256(path)!=sha:
            raise ValueError('Unrecognized user-supplied wiki image archive: '+path.name)
    weapons=json.loads(catalog_path.read_text(encoding='utf8'))['weapons']
    names=[row['name'] for row in weapons]
    if len(names)!=91 or len(set(names))!=91:
        raise ValueError('Expected 91 known offline catalog weapons')
    with gzip.open(source_skins,'rb') as f: skin_catalog=json.load(f)
    if len(skin_catalog['skins'])!=118:
        raise ValueError('Expected original 118 skins from Pass54')
    skin_ids=list(skin_catalog['skins'])
    by_name={canonical(i.split('/',1)[1]):i for i in skin_ids}
    output_images={}
    weapon_refs={}
    skin_icons={}
    defaults=variants=poses=0
    with ZipFile(images_zip) as iz,ZipFile(docs_zip) as dz:
        available={n.rsplit('/',1)[-1] for n in iz.namelist()
                   if n.startswith('images/') and '/' in n}
        for weapon in names:
            alias=ALIASES.get(weapon,canonical(weapon))
            candidates=sorted({n.split('-',1)[0] for n in available if '-' in n
                               and canonical(n.split('-',1)[0])==canonical(alias)})
            if not candidates:continue
            prefix=max(candidates,key=lambda c:sum(
                n.lower().startswith((c+'-').lower()) for n in available))
            found={'default':None,'poses':{},'skins':{}}
            for suffix in ('Default','UI','ADS','Hip','ADSEmpty','HipEmpty'):
                filename=next((n for n in (prefix+'-'+suffix+'.png',
                    prefix+'-'+suffix+'.jpg') if n in available),None)
                if filename:
                    path=image_entry(iz,dz,filename,output_images)
                    if suffix=='Default':found['default']=path;defaults+=1
                    elif suffix in POSE_SUFFIXES:
                        found['poses'][POSE_SUFFIXES[suffix]]=path;poses+=1
            for candidate in sorted(n for n in available
                                    if n.lower().startswith((prefix+'-').lower())):
                match=re.fullmatch(re.escape(prefix)+r'-([^/.]+)\.(png|jpg|jpeg)',
                                   candidate,re.I)
                if not match:continue
                label=SKIN_SUFFIXES.get(match.group(1),match.group(1))
                skin=by_name.get(canonical(label))
                if skin is None:continue
                subject,kind=source_docs(dz,candidate.rsplit('.',1)[0])
                if kind.lower()!='skin' or canonical(subject)!=canonical(skin.split('/',1)[1]):
                    continue
                found['skins'][skin]=image_entry(iz,dz,candidate,output_images)
                variants+=1
            if found['default'] or found['skins']:weapon_refs[weapon]=found
        for skin in skin_ids:
            title=skin.split('/',1)[1]
            options=(canonical(title)+'icon',
                     canonical(title.replace('2019','19'))+'icon',
                     canonical(title.replace('2020','20'))+'icon')
            matches=[]
            for image in available:
                match=re.fullmatch(r'([A-Za-z0-9_-]+?)[_-]?Icon\.(png|jpg)',
                                   image,re.I)
                if match and canonical(match.group(1)+'icon') in options:
                    matches.append(image)
            if len(matches)!=1:continue
            subject,kind=source_docs(dz,matches[0].rsplit('.',1)[0])
            if kind.lower() in ('skin','item','cosmetic'):
                skin_icons[skin]=image_entry(iz,dz,matches[0],output_images)
    if len(weapon_refs)!=91 or defaults!=90 or variants!=1563 or poses!=270:
        raise ValueError('Wiki source image coverage changed: '+
                         f'{len(weapon_refs)}/{defaults}/{variants}/{poses}')
    if len(output_images)!=2033 or len(skin_icons)!=110:
        raise ValueError('Wiki file or skin icon evidence count changed')
    files={path:{k:v for k,v in content.items() if k!='bytes'}
           for path,content in sorted(output_images.items())}
    stats={'catalogWeapons':91,'coveredWeapons':len(weapon_refs),
           'defaultWeaponReferences':defaults,'skinVariantReferences':variants,
           'poseReferences':poses,'skinIconReferences':len(skin_icons),
           'uniqueImageFiles':len(output_images)}
    manifest={'format':FORMAT,'sourceImageZipSha256':IMAGES_SHA,
              'sourceImageDocsZipSha256':DOCS_SHA,
              'provenance':'Owner wiki screenshots/icons, NOT Roblox UV textures',
              'stats':stats,'files':files,'weapons':weapon_refs,
              'skinIcons':skin_icons}
    data=json.dumps(manifest,sort_keys=True,separators=(',',':'),
                    ensure_ascii=True).encode()
    if len(data)>1_500_000:raise ValueError('Manifest exceeds size budget')
    output.parent.mkdir(parents=True,exist_ok=True)
    with ZipFile(output,'x',compression=ZIP_DEFLATED,compresslevel=7) as z:
        def write(path,content):
            info=ZipInfo(path,date_time=(2020,1,1,0,0,0))
            info.compress_type=ZIP_DEFLATED
            info.external_attr=0o600<<16
            z.writestr(info,content,compress_type=ZIP_DEFLATED,compresslevel=7)
        write('manifest.json',data)
        for path in sorted(output_images):write(path,output_images[path]['bytes'])
    result=dict(stats)
    result['zip_bytes']=output.stat().st_size
    result['zip_sha256']=file_sha256(output)
    print(json.dumps(result,sort_keys=True))
    return result


if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--images',required=True,type=Path)
    p.add_argument('--image-docs',required=True,type=Path)
    p.add_argument('--weapons',required=True,type=Path)
    p.add_argument('--skins-pack',required=True,type=Path)
    p.add_argument('--output',required=True,type=Path)
    opts=p.parse_args()
    build(opts.images,opts.image_docs,opts.weapons,opts.skins_pack,opts.output)

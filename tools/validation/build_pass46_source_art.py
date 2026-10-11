#!/usr/bin/env python3
"""Build owner-local, fully offline art from the user-provided images.zip.

The input is the 91-weapon source catalog (content/weapons/catalog.json).
The output is the existing Pass27 visuals.json format with SHA-256 integrity
and source-original image provenance. Does not download remote assets.
Missing standalone reference silhouettes remain text-only in the game.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import re
from pathlib import Path
from zipfile import ZipFile
from PIL import Image, ImageOps, ImageEnhance

MAPS=('Ranch','Mill','Bypass','Cabin','Cargo','District',
      'Expressway','Prison','Laboratory','Manor')
ALIASES={
 '2x4 w- Barbed Wire':'2x4BW','AA-12':'AA12','Aero Survival Rifle':'ASR',
 'AK-47':'AK47','AR-57':'AR57','AS VAL':'ASVAL','ASh-12':'ASh12',
 'Barrett M82A1':'M82A1','Baseball Bat':'Bat','Benelli M4':'BM4',
 'Butcher Knife':'ButcherKnife','CBJ-MS':'CBJMS','CMMG Mk47 Mutant':'Mk47',
 'Colt Python':'Python','CZ Scorpion EVO Micro K':'SEMK',
 'Desert Eagle':'DEagle','Fire Axe':'FireAxe','FN FAL':'FAL',
 'Frying Pan':'Pan','Gepard PDW':'Gepard','Glock 17':'G17',
 'HK P30L':'P30L','Ingram MAC-10':'M10','KAC PDW':'KAC',
 'Kriss Vector':'Vector','LH9 MKII':'LH9','LWRC IC-PSD':'LWRC',
 'M1 Garand':'M1','M110 SASS':'M110','MG 42':'MG42','Mosin Nagant':'Mosin',
 'Mossberg 500':'M500','MP-443 Grach':'MP443','Obrez Mosin':'Obrez',
 'OTs-14 Groza':'OTs14','PB 6P9':'PB6P9','Pool Cue':'Cue',
 'PP-91 Kedr':'PP91','PPSh-41':'PPSh41','RPG-7':'RPG7',
 'Ruger 10-22':'Ruger','Sawn Off Shotgun':'Sawnoff','SCAR-H':'SCARH',
 'Serbu Super Shorty':'SBB','SKO Shorty':'SKO','Sledge Hammer':'SledgeHammer',
 'SPAS-12':'SPAS12','Sten Mk V':'MkV','Taurus Judge':'Judge',
 'Taurus Model 66':'M66','TEC-9':'TEC9','Thompson M1':'Thompson',
 'UMP-45':'UMP45','Walther P38':'P38','Winchester Model 1892':'M92',
 'Winchester Model 70':'M70','XD-9':'XD9'
}
PREFERRED={'Benelli M4':'BM4-Hip.png','Fire Axe':'FireAxe-Hold.png'}

def normalize(name:str)->str:
    return ''.join(c.lower() for c in name if c.isascii() and c.isalnum())

def encoded(image:Image.Image)->bytes:
    out=io.BytesIO()
    image.save(out,format='PNG',optimize=True)
    raw=out.getvalue()
    if not 0<len(raw)<2_000_000:
        raise ValueError('Output exceeds 2MB verified offline art loader budget')
    return raw

def read_image(z:ZipFile,name:str)->Image.Image:
    path='images/'+name
    info=z.getinfo(path)
    if info.file_size>4_000_000:
        raise ValueError(f'Unexpected oversized source image: {name}')
    im=Image.open(io.BytesIO(z.read(info)))
    im.load()
    if not 8<=im.width<=4096 or not 8<=im.height<=4096:
        raise ValueError('Invalid source image dimensions')
    return im.convert('RGBA')

def weapon_art(im:Image.Image)->Image.Image:
    # Keep the owner image silhouette and all alpha, brighten only RGB so
    # dark original-source default images remain legible on the dark armory.
    im=im.copy()
    im.thumbnail((240,126),Image.Resampling.LANCZOS)
    alpha=im.getchannel('A')
    bright=ImageEnhance.Brightness(im.convert('RGB')).enhance(2.5)
    out=bright.convert('RGBA')
    out.putalpha(alpha)
    canvas=Image.new('RGBA',(256,144),(0,0,0,0))
    canvas.alpha_composite(out,((256-out.width)//2,(144-out.height)//2))
    return canvas

def map_art(im:Image.Image)->Image.Image:
    return ImageOps.fit(im.convert('RGB'),(512,288),
        method=Image.Resampling.LANCZOS,centering=(.5,.5)).convert('RGBA')

def build(images:Path,catalog:Path,output:Path):
    spec=json.loads(catalog.read_text(encoding='utf-8'))
    weapons=[item['name'] for item in spec['weapons']]
    if len(weapons)!=91 or len(set(weapons))!=91:
        raise ValueError('Expected 91 distinct recovered weapon names')
    if output.exists() and any(output.rglob('*')):
        raise FileExistsError('Pass46 output directory must be empty')
    manifest={'format':'twr-pass27-offline-visual-reference-v1',
              'map_cards':{},'weapon_icons':{}}
    original_names={}
    missing=[]
    with ZipFile(images) as archive:
        available=set(archive.namelist())
        for name in MAPS:
            sprite=name+'Icon.png'
            if 'images/'+sprite not in available:
                raise FileNotFoundError('Missing source map icon '+sprite)
            raw=encoded(map_art(read_image(archive,sprite)))
            target='MapCards/'+name+'.png'
            path=output/target
            path.parent.mkdir(parents=True,exist_ok=True)
            path.write_bytes(raw)
            manifest['map_cards'][name]={'file':target,
                'sha256':hashlib.sha256(raw).hexdigest()}
            original_names[target]=sprite
        for name in weapons:
            stem=ALIASES.get(name,re.sub(r'[^A-Za-z0-9]','',name))
            options=[stem+'-Default.png',stem+'-Default.jpg']
            if name in PREFERRED:
                options.append(PREFERRED[name])
            sprite=next((x for x in options if 'images/'+x in available),None)
            if sprite is None:
                # Intentionally exclude -UI screenshots: those include static
                # ammunition numbers that would contradict the real game.
                missing.append(name)
                continue
            raw=encoded(weapon_art(read_image(archive,sprite)))
            normalized=normalize(name)
            target='WeaponIcons/'+normalized+'.png'
            path=output/target
            path.parent.mkdir(parents=True,exist_ok=True)
            path.write_bytes(raw)
            manifest['weapon_icons'][normalized]={'file':target,
                'sha256':hashlib.sha256(raw).hexdigest()}
            original_names[target]=sprite
    if len(manifest['weapon_icons'])<87:
        raise ValueError('Source art catalog reference coverage unexpectedly low')
    (output/'visuals.json').write_text(
        json.dumps(manifest,sort_keys=True,indent=2)+'\n',encoding='utf8')
    report={'format':'twr-pass46-source-image-provenance-v1',
        'source':'user-provided images.zip (no network)',
        'image_archive_sha256':hashlib.sha256(images.read_bytes()).hexdigest(),
        'original_roblox_3d_meshes_restored':False,
        'original_hud_image_labels_restored':False,
        'map_cards':len(manifest['map_cards']),
        'weapon_artworks':len(manifest['weapon_icons']),
        'catalog_weapons':len(weapons),
        'missing_original_sprite_references':missing,
        'derived_file_to_source_image':original_names,
        'ui_only_no_gameplay_effect':True}
    (output/'SOURCE_IMAGE_PROVENANCE46.json').write_text(
        json.dumps(report,sort_keys=True,indent=2)+'\n',encoding='utf8')
    return report

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--images',required=True,type=Path)
    parser.add_argument('--catalog',required=True,type=Path)
    parser.add_argument('--output',required=True,type=Path)
    args=parser.parse_args()
    print(json.dumps(build(args.images,args.catalog,args.output),indent=2))

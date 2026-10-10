#!/usr/bin/env python3
"""Recover CMaps local/environment sound metadata, NOT original sound bytes.

Original CMaps SoundId and Volume are empty/zero in this TestPlace source.
SetVolume child NumberValue is the intended scripted target loudness.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
from zipfile import ZipFile
from lxml import etree

MAPS=('Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Laboratory','Manor')
MEMBER='TestPlace/TestPlace.rbxlx'

def prop(item,name):
    if item is None:return None
    props=item.find('Properties')
    return next((p for p in props if p.get('name')==name),None) if props is not None else None

def named(item):
    p=prop(item,'Name')
    return (p.text or '') if p is not None else ''

def ancestors(item):
    parts=[]
    while item is not None and item.tag=='Item':
        parts.append(item);item=item.getparent()
    return list(reversed(parts))

def bounded(item,key,default=0,minimum=-100000,maximum=100000):
    p=prop(item,key)
    value=float(p.text) if p is not None and p.text else float(default)
    if not math.isfinite(value) or value<minimum or value>maximum:
        raise ValueError('Invalid source sound scalar '+key)
    return round(value,7)

def enabled(item,key):
    p=prop(item,key)
    return p is not None and str(p.text).lower()=='true'

def position(part):
    if part is None:return None
    cf=prop(part,'CFrame')
    if cf is None:raise ValueError('Source sound anchor CFrame missing')
    coords=[]
    for axis in ('X','Y','Z'):
        node=cf.find(axis)
        if node is None or node.text is None:
            raise ValueError('Source sound anchor position incomplete')
        value=float(node.text)
        if not math.isfinite(value) or abs(value)>100000:
            raise ValueError('Original source sound location out of bounds')
        coords.append(round(value,7))
    return coords

def sound(item,chain,scope):
    anchor=next((a for a in chain[-2::-1] if a.get('class')=='Part'),None) if scope=='local' else None
    if scope=='local' and anchor is None:
        raise ValueError('Positioned sound has no original source Part')
    cue=named(anchor) if anchor is not None else named(item)
    slug=re.sub(r'[^a-z0-9_-]','',cue.lower())
    if not slug or len(slug)>80:raise ValueError('Unsafe sound cue name')
    soundid=prop(item,'SoundId')
    if soundid is not None and soundid.text and soundid.text.strip():
        raise ValueError('Audited original snapshot unexpectedly contains SoundId')
    vol=item.get('_pass38_setvolume')
    if vol is None:raise ValueError('Original SetVolume value missing')
    gain=float(vol)
    if not math.isfinite(gain) or gain<0 or gain>10:
        raise ValueError('Invalid source SetVolume target')
    return {
        'scope':scope,'cue':slug,'source_name':cue,
        'source_path':'/'.join(named(a) for a in chain[3:]),
        'position_studs':position(anchor),'source_id_available':False,
        'original_volume':bounded(item,'Volume',0,0,10),
        'script_target_volume':round(gain,7),
        'initial_playing':enabled(item,'Playing'),
        'looped':enabled(item,'Looped'),
        'rolloff_min_studs':bounded(item,'RollOffMinDistance',0,0,20000),
        'rolloff_max_studs':bounded(item,'RollOffMaxDistance',100,0,20000),
        'rolloff_mode':int(bounded(item,'RollOffMode',0,0,3)),
        'playback_speed':bounded(item,'PlaybackSpeed',1,.01,8)
    }

def recover(archive:Path,out:Path):
    out=Path(out);out.mkdir(parents=True,exist_ok=True)
    sources={m:[] for m in MAPS}
    with ZipFile(archive) as z:
        if z.getinfo(MEMBER).file_size>500_000_000:
            raise ValueError('Original RBXLX exceeds input budget')
        with z.open(MEMBER) as stream:
            source_sha=hashlib.file_digest(stream,'sha256').hexdigest()
        with z.open(MEMBER) as stream:
            for _,item in etree.iterparse(stream,events=('end',),tag='Item',
                                          huge_tree=True,resolve_entities=False,
                                          load_dtd=False,no_network=True):
                cls=item.get('class')
                if cls=='NumberValue' and named(item)=='SetVolume':
                    parent=item.getparent()
                    if parent is not None and parent.get('class')=='Sound':
                        parent.set('_pass38_setvolume',str(bounded(item,'Value',0,0,10)))
                elif cls=='Sound':
                    chain=ancestors(item)
                    if len(chain)>=5 and named(chain[0])=='ReplicatedStorage' and named(chain[1])=='CMaps':
                        map_name=named(chain[2])
                        group=named(chain[3])
                        if map_name in sources and group in ('Local Sounds','Environment Sounds'):
                            sources[map_name].append(sound(item,chain,
                                'local' if group=='Local Sounds' else 'environment'))
                parent=item.getparent();item.clear()
                while item.getprevious() is not None and item.getprevious().tag=='Item':
                    parent.remove(item.getprevious())
    report={'format':'twr-source-soundscape38-manifest-v1',
            'owner_rbxlx_sha256':source_sha,
            'source_sound_ids_are_empty':True,
            'source_sound_binary_bytes_available':False,
            'maps':{},'total_emitters':0,'total_positioned':0,'total_environment':0}
    for m in MAPS:
        rows=sources[m]
        local=sum(x['scope']=='local' for x in rows)
        environment=sum(x['scope']=='environment' for x in rows)
        payload={'format':'twr-source-soundscape38-v1','map':m,
                 'owner_rbxlx_sha256':source_sha,
                 'source_path':f'ReplicatedStorage/CMaps/{m}',
                 'source_ids_present':False,
                 'audio_binary_bytes_present':False,
                 'playback_is_approximate_with_external_wav':True,
                 'emitter_count':len(rows),'local_count':local,
                 'environment_count':environment,'emitters':rows}
        raw=(json.dumps(payload,sort_keys=True,separators=(',',':'))+'\n').encode('utf8')
        filename=m+'.soundscape38.json'
        (out/filename).write_bytes(raw)
        report['maps'][m]={'file':filename,'sha256':hashlib.sha256(raw).hexdigest(),
                           'emitters':len(rows),'positioned':local,
                           'environment':environment,
                           'unique_cues':sorted({x['cue'] for x in rows})}
        report['total_emitters']+=len(rows)
        report['total_positioned']+=local
        report['total_environment']+=environment
    (out/'SOURCE_SOUNDSCAPE_MANIFEST38.json').write_text(
        json.dumps(report,sort_keys=True,indent=2)+'\n',encoding='utf8')
    return report

if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--archive',type=Path,required=True)
    cli.add_argument('--out',type=Path,required=True)
    args=cli.parse_args()
    print(json.dumps(recover(args.archive,args.out),indent=2))

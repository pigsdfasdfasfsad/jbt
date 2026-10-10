#!/usr/bin/env python3
"""Offline recovery of original Roblox CMaps/*/Walls/Client Walls.

Not a complete retail map. The imported Client Walls collision-group matrix
is not present in the RBXLX: Godot layers are therefore opt-in only (F8).
No Lua executes; no external asset fetching occurs. Requires lxml only here.
"""
from __future__ import annotations
import argparse
from collections import Counter,defaultdict
import gzip
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import zipfile
from lxml import etree

MAPS=("Ranch","Mill","Bypass","Cabin","Cargo","District","Expressway","Prison","Laboratory","Manor")
MEMBER='TestPlace/TestPlace.rbxlx'
SHAPE_CLASSES={'Part','WedgePart','MeshPart','UnionOperation','TrussPart','CornerWedgePart'}

def prop(item,key):
    p=item.find('Properties')
    if p is None:return None
    for n in p:
        if n.get('name')==key:return n
    return None

def txt(item,key,default=''):
    n=prop(item,key)
    return n.text.strip() if n is not None and n.text else default

def vec(node,axes):
    if node is None: raise ValueError('Missing original CFrame or size')
    result=[]
    for a in axes:
        child=node.find(a)
        if child is None or child.text is None:raise ValueError('Missing vector value '+a)
        v=float(child.text)
        if not math.isfinite(v) or abs(v)>100_000:raise ValueError('Bad source number')
        result.append(round(v,7))
    return result

def name(item):return txt(item,'Name',item.get('class','Unknown'))

def ancestry(item):
    parts=[]
    while item is not None:
        if item.tag=='Item':parts.append(name(item))
        item=item.getparent()
    return tuple(reversed(parts))

def read_client_walls(z):
    rows=defaultdict(list);infected=Counter();sections=Counter()
    with z.open(MEMBER) as src:
        for _,item in etree.iterparse(src,events=('end',),tag='Item',resolve_entities=False,
                load_dtd=False,no_network=True,huge_tree=True):
            if item.get('class') in SHAPE_CLASSES:
                path=ancestry(item)
                if len(path)>=5 and path[:2]==('ReplicatedStorage','CMaps') and path[3]=='Walls':
                    map_name=path[2]
                    if path[4]=='Infected Walls': infected[map_name]+=1
                    if path[4]=='Client Walls':
                        sections[map_name]+=1
                        if map_name in MAPS:
                            cframe=prop(item,'CFrame')
                            if cframe is None:raise ValueError('Source wall missing CFrame: '+str(path))
                            size=prop(item,'Size') or prop(item,'size')
                            r=vec(cframe,('R00','R01','R02','R10','R11','R12','R20','R21','R22'))
                            t=vec(cframe,('X','Y','Z'))
                            s=vec(size,('X','Y','Z'))
                            if any(x<=.001 for x in s):raise ValueError('Source wall has degenerate dimensions')
                            if item.get('class') not in ('Part','WedgePart'):
                                raise ValueError('Non-native mesh in source client wall: '+str(path))
                            if txt(item,'CanCollide','true').lower()!='true':
                                continue
                            rows[map_name].append({'class':item.get('class'),'t':t,'r':r,'s':s})
            parent=item.getparent();item.clear()
            while item.getprevious() is not None and item.getprevious().tag=='Item':
                parent.remove(item.getprevious())
    return rows,infected,sections

def pack(header,records):
    data='\n'.join(json.dumps(x,sort_keys=True,separators=(',',':'),ensure_ascii=True) for x in [header,*records])+'\n'
    return gzip.compress(data.encode('utf8'),mtime=0,compresslevel=6)

def recover(input_archive, lab_scene, output_dir):
    input_archive=Path(input_archive);lab_scene=Path(lab_scene);output_dir=Path(output_dir)
    with zipfile.ZipFile(input_archive) as z:
        info=z.getinfo(MEMBER)
        if info.file_size>500_000_000:raise ValueError('Source XML above bounded size')
        with z.open(MEMBER) as f: source_hash=hashlib.file_digest(f,'sha256').hexdigest()
        rows,infected,sections=read_client_walls(z)
    if not lab_scene.exists():raise ValueError('Private exact Laboratory scene file is required')
    scene_hash=hashlib.sha256(lab_scene.read_bytes()).hexdigest()
    output_dir.mkdir(parents=True,exist_ok=True)
    report={'format':'twr-client-wall-source-audit-v1','owner_xml_source_sha256':source_hash,
       'laboratory_scene_sha256':scene_hash, 'source_module':'ModuleScript.Map.Source.txt',
       'source_collision_group_id':8, 'collision_group_matrix_known':False,
       'in_game_default':'OFF; F8 opt-in, only when source pack matches exact scene SHA',
       'release_maps':{}, 'extra_cmaps_client_walls':{k:v for k,v in sorted(sections.items()) if k not in MAPS}}
    for map_name in MAPS:
        entries=rows[map_name]
        if not entries:raise ValueError('No original client walls in '+map_name)
        header={'format':'twr-client-walls34-v1','map':map_name,
          'source_rbxlx_sha256':source_hash,'scene_sha256':scene_hash if map_name=='Laboratory' else None,
          'wall_count':len(entries),'collision_group_id':8,'default_enabled':False,
          'scope':'original invisible Client Walls source geometry; retail collision matrix unknown'}
        data=pack(header,entries)
        (output_dir/(map_name+'.clientwalls34.jsonl.gz')).write_bytes(data)
        report['release_maps'][map_name]={
          'client_walls':len(entries),'infected_walls_source_count':infected[map_name],
          'types':dict(Counter(x['class'] for x in entries)),
          'enabled_by_runtime':map_name=='Laboratory',
          'pack_sha256':hashlib.sha256(data).hexdigest(),
          'status':'complete CMaps wall source fragment, NOT proof of complete map'}
    (output_dir/'client-walls34-manifest.json').write_text(json.dumps(report,indent=2,sort_keys=True)+'\n')
    return report

if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--archive',type=Path,required=True)
    ap.add_argument('--lab-scene',type=Path,required=True)
    ap.add_argument('--out',type=Path,required=True)
    a=ap.parse_args()
    result=recover(a.archive,a.lab_scene,a.out)
    for m,v in result['release_maps'].items():print('TWR_PASS34_WALLS',m,v['client_walls'], 'infected',v['infected_walls_source_count'])
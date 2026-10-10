#!/usr/bin/env python3
"""Recover source-positioned nine-map CMaps/Server Walls and CMaps/Map/Objects.

Not full source maps. Only owner-supplied TestPlace.rbxlx is inspected; no
external Roblox asset IDs are downloaded or interpreted as original triangles.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import gzip
import hashlib
import json
import math
from pathlib import Path
import re
from zipfile import ZipFile
from lxml import etree

MAPS = ('Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Manor')
MEMBER = 'TestPlace/TestPlace.rbxlx'
PARTS = {'Part','WedgePart','MeshPart','UnionOperation','CornerWedgePart','TrussPart'}
MATRIX=('R00','R01','R02','R10','R11','R12','R20','R21','R22')


def pnode(item,key):
    props=item.find('Properties')
    if props is None: return None
    for node in props:
        if node.get('name')==key:return node
    return None


def pname(item):
    node=pnode(item,'Name')
    return node.text.strip() if node is not None and node.text else item.get('class','?')


def ancestors(item):
    names=[]
    while item is not None:
        if item.tag=='Item':names.append(pname(item))
        item=item.getparent()
    return tuple(reversed(names))


def floatlist(n,fields):
    if n is None: raise ValueError('Missing owner source transform vector')
    result=[]
    for label in fields:
        node=n.find(label)
        if node is None or node.text is None:raise ValueError('Missing source transform field '+label)
        value=float(node.text)
        if not math.isfinite(value) or abs(value)>100000:raise ValueError('Unbounded source float')
        result.append(round(value,7))
    return result


def part(item):
    pos=pnode(item,'CFrame')
    size=pnode(item,'Size')
    if size is None:size=pnode(item,'size')
    t=floatlist(pos,('X','Y','Z'))
    r=floatlist(pos,MATRIX)
    s=floatlist(size,('X','Y','Z'))
    if min(s)<.001 or max(s)>100000: raise ValueError('Invalid original bounding dimensions')
    return {'class':item.get('class'),'t':t,'r':r,'s':s}


def text_prop(item,key):
    n=pnode(item,key)
    return '' if n is None or n.text is None else n.text.strip()


def item_object(item):
    row=part(item)
    row['name']=pname(item)
    row['material']=text_prop(item,'Material') or '256'
    row['can_collide']=text_prop(item,'CanCollide').lower()=='true'
    row['transparency']=min(1,max(0,float(text_prop(item,'Transparency') or '0')))
    color=text_prop(item,'Color3uint8')
    try:
        v=int(color) & 0xffffff if color else 0xaaaaaa
    except ValueError:
        v=0xaaaaaa
    row['rgb']=[(v>>16)&255,(v>>8)&255,v&255]
    mesh=pnode(item,'MeshId')
    if mesh is not None:
        ids=re.findall(r'(?:rbxassetid://|[?&]id=)(\d+)', ' '.join(mesh.itertext()),flags=re.I)
        if ids: row['source_mesh_id']=ids[0]
    row['geometry_status']='native_primitive' if row['class'] in ('Part','WedgePart') else 'external_mesh_or_csg_unavailable'
    return row


def compressed(rows):
    data=('\n'.join(json.dumps(row,sort_keys=True,separators=(',',':'),ensure_ascii=True)
                    for row in rows)+'\n').encode('utf8')
    return gzip.compress(data,mtime=0,compresslevel=6)


def recover(archive:Path,out:Path):
    out=Path(out);out.mkdir(parents=True,exist_ok=True)
    walls=defaultdict(list)
    objects=defaultdict(list)
    with ZipFile(archive) as z:
        meta=z.getinfo(MEMBER)
        if meta.file_size>500_000_000:raise ValueError('Original XML oversized')
        with z.open(MEMBER) as inp: source_sha=hashlib.file_digest(inp,'sha256').hexdigest()
        with z.open(MEMBER) as inp:
            for _,item in etree.iterparse(inp,events=('end',),tag='Item',huge_tree=True,
                            resolve_entities=False,load_dtd=False,no_network=True):
                cls=item.get('class')
                if cls in PARTS:
                    path=ancestors(item)
                    if len(path)>=5 and path[:2]==('ReplicatedStorage','CMaps') and path[2] in MAPS:
                        m=path[2]
                        if path[3:5]==('Walls','Server Walls'):
                            row=part(item)
                            row['approximated_mesh']=cls not in ('Part','WedgePart')
                            row['can_collide']=text_prop(item,'CanCollide').lower()!='false'
                            walls[m].append(row)
                        elif path[3:5]==('Map','Objects'):
                            objects[m].append(item_object(item))
                parent=item.getparent();item.clear()
                while item.getprevious() is not None and item.getprevious().tag=='Item':parent.remove(item.getprevious())
    report={'format':'twr-pass36-cmaps-source-fragment-manifest-v1',
            'owner_rbxlx_sha256':source_sha,'not_full_maps':True,
            'source_server_walls_are_not_visual_map_geometry':True,
            'source_custom_mesh_triangle_data_unavailable':True,
            'source_maps':{},'total_wall_records':0,'total_object_records':0,
            'missing_custom_object_meshes':0}
    for m in MAPS:
        rows=walls[m];objs=objects[m]
        if not rows:raise ValueError('Missing original server walls for '+m)
        wc=Counter(r['class'] for r in rows); oc=Counter(r['class'] for r in objs)
        wr={'format':'twr-serverwalls36-v1','map':m,'owner_rbxlx_sha256':source_sha,
            'source_path':f'ReplicatedStorage/CMaps/{m}/Walls/Server Walls',
            'wall_count':len(rows),'native_primitives':wc['Part']+wc['WedgePart'],
            'approximated_collision_meshes':len(rows)-wc['Part']-wc['WedgePart'],
            'default_collision_enabled':False,'wall_layer':16}
        data=compressed([wr,*rows]); wpath=out/(m+'.serverwalls36.jsonl.gz');wpath.write_bytes(data)
        object_header={'format':'twr-source-objects36-v1','map':m,
            'owner_rbxlx_sha256':source_sha,
            'source_path':f'ReplicatedStorage/CMaps/{m}/Map/Objects',
            'object_count':len(objs),'native_primitives':oc['Part']+oc['WedgePart'],
            'missing_mesh_or_csg':len(objs)-oc['Part']-oc['WedgePart'],
            'visual_geometry_incomplete':True}
        obj_data=compressed([object_header,*objs]);opath=out/(m+'.objects36.jsonl.gz');opath.write_bytes(obj_data)
        record={'server_walls':len(rows),'server_class_counts':dict(sorted(wc.items())),
                'server_walls_file':wpath.name,'server_walls_sha256':hashlib.sha256(data).hexdigest(),
                'server_native':wc['Part']+wc['WedgePart'],
                'server_mesh_proxies':len(rows)-wc['Part']-wc['WedgePart'],
                'cmap_objects':len(objs),'object_class_counts':dict(sorted(oc.items())),
                'object_pack_file':opath.name,'object_pack_sha256':hashlib.sha256(obj_data).hexdigest(),
                'object_native':oc['Part']+oc['WedgePart'],
                'object_missing_custom_geometry':len(objs)-oc['Part']-oc['WedgePart']}
        report['source_maps'][m]=record
        report['total_wall_records']+=len(rows);report['total_object_records']+=len(objs)
        report['missing_custom_object_meshes']+=record['object_missing_custom_geometry']
        print('PASS36_RECOVERED',m,'source_barriers',len(rows),'source_objects',len(objs),'native_objects',record['object_native'])
    (out/'SOURCE_FRAGMENT_MANIFEST36.json').write_text(json.dumps(report,sort_keys=True,indent=2)+'\n',encoding='utf8')
    return report

if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--archive',type=Path,required=True)
    cli.add_argument('--out',type=Path,required=True)
    args=cli.parse_args();recover(args.archive,args.out)
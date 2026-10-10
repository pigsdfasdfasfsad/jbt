#!/usr/bin/env python3
"""Deterministically recover exact source CMaps/.../Walls/Infected Walls transforms.

NO source mesh binaries are supplied for MeshPart; marked oriented-box proxies.
Only local owner-supplied archives are read. No Roblox or network access.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import gzip
import hashlib
import json
import math
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

MAPS = ('Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Laboratory','Manor')
SCENE = 'TestPlace/TestPlace.rbxlx'
VALID = {'Part','WedgePart','MeshPart','UnionOperation','CornerWedgePart','TrussPart'}


def prop(item, key):
    p = item.find('Properties')
    if p is None: return None
    return next((n for n in p if n.get('name') == key), None)


def name(item):
    p = prop(item, 'Name')
    return p.text.strip() if p is not None and p.text else item.get('class', '')


def ancestral(item):
    path = []
    while item is not None:
        if item.tag == 'Item': path.append(name(item))
        item = item.getparent()
    return tuple(reversed(path))


def triple(p, fields):
    if p is None: raise ValueError('Original wall missing vector property')
    v = []
    for f in fields:
        el = p.find(f)
        if el is None or el.text is None: raise ValueError('Original wall missing '+f)
        x = float(el.text)
        if not math.isfinite(x) or abs(x) > 100000: raise ValueError('Nonfinite/unbounded original wall')
        v.append(round(x, 7))
    return v


def source_rows(z):
    rows = defaultdict(list)
    with z.open(SCENE) as f:
        for _, item in etree.iterparse(f, events=('end',), tag='Item', huge_tree=True,
                                       resolve_entities=False, load_dtd=False, no_network=True):
            if item.get('class') in VALID:
                ancestry = ancestral(item)
                if (len(ancestry) >= 5 and ancestry[:2] == ('ReplicatedStorage', 'CMaps')
                        and ancestry[3:5] == ('Walls','Infected Walls')
                        and ancestry[2] in MAPS):
                    cls = item.get('class')
                    cframe, size = prop(item,'CFrame'), prop(item,'Size')
                    if size is None: size = prop(item,'size')
                    if (p := prop(item,'CanCollide')) is not None and (p.text or '').strip().lower() == 'false':
                        raise ValueError('Unexpected noncollidable source infected wall')
                    t = triple(cframe, ('X','Y','Z'))
                    r = triple(cframe,('R00','R01','R02','R10','R11','R12','R20','R21','R22'))
                    s = triple(size,('X','Y','Z'))
                    if min(s) <= .001: raise ValueError('Degenerate original infected wall size')
                    if cls not in ('Part','WedgePart','MeshPart'):
                        raise ValueError('Unrecoverable original infected wall class '+cls)
                    rows[ancestry[2]].append({
                        'class':cls, 't':t, 'r':r, 's':s,
                        'approximated_mesh':cls=='MeshPart'
                    })
            parent = item.getparent(); item.clear()
            while item.getprevious() is not None and item.getprevious().tag=='Item':
                parent.remove(item.getprevious())
    return rows


def pack(rows):
    payload = ('\n'.join(json.dumps(x,sort_keys=True,separators=(',',':'))
                         for x in rows)+'\n').encode('utf-8')
    return gzip.compress(payload,mtime=0,compresslevel=6)


def recover(source,output):
    source=Path(source); output=Path(output)
    with ZipFile(source) as z:
        info = z.getinfo(SCENE)
        if info.file_size > 500_000_000: raise ValueError('Original RBXLX exceeds safety limit')
        with z.open(SCENE) as f: original_sha=hashlib.file_digest(f,'sha256').hexdigest()
        rows = source_rows(z)
    output.mkdir(parents=True,exist_ok=True)
    report={'format':'twr-pass35-original-infected-wall-manifest-v1',
            'original_rbxlx_sha256':original_sha,
            'source_path':'ReplicatedStorage/CMaps/{map}/Walls/Infected Walls',
            'source_collision_group_matrix_recovered':False,
            'runtime_compatibility':'F7 opt-in, infected-only Godot layer bit 8; other maps have blockout geometry',
            'maps':{}}
    for m in MAPS:
        items=rows[m]
        types=dict(sorted(Counter(item['class'] for item in items).items()))
        approximated=types.get('MeshPart',0)
        row={ 'map':m, 'wall_count':len(items), 'types':types,
            'native_primitives':len(items)-approximated,
            'approximated_meshparts':approximated, 'runtime_default_enabled':False,
            'note':'Original CMaps wall fragment; NOT an original complete map'}
        if items:
            header={'format':'twr-infected-walls35-v1','map':m,
                    'source_rbxlx_sha256':original_sha,'wall_count':len(items),
                    'source_class':'ReplicatedStorage.CMaps.'+m+'.Walls.Infected Walls',
                    'native_primitives':len(items)-approximated,
                    'approximated_meshparts':approximated,
                    'default_enabled':False,'collision_layer':8}
            data=pack([header,*items]);path=output/(m+'.infectedwalls35.jsonl.gz')
            path.write_bytes(data)
            row['sha256']=hashlib.sha256(data).hexdigest()
            row['file']=path.name
        report['maps'][m]=row
        print(f'TWR_PASS35_RECOVERED map={m} infected_walls={len(items)} native={len(items)-approximated} mesh_proxies={approximated}')
    manifest=output/'infected-walls35-manifest.json'
    manifest.write_text(json.dumps(report,sort_keys=True,indent=2)+'\n',encoding='utf8')
    return report


if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--archive',type=Path,required=True)
    ap.add_argument('--out',type=Path,required=True)
    args=ap.parse_args()
    recover(args.archive,args.out)
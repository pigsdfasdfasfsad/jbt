#!/usr/bin/env python3
"""Create/remove a fully fabricated owner-free Pass45 Laboratory test scene.

Adds one near and one far MeshPart proxy to the Pass44 native Part scene,
then rebinds the native28 cache to the new SYNTHETIC scene SHA. Neither the
source nor any gameplay runtime contains original Roblox mesh binaries.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from make_pass44_synthetic import create as create44,clean as clean44
from build_source_primitives44 import extract,read_pack

def create(root:Path):
    create44(root)
    scene=root/'Content'/'Maps'/'Laboratory.scene.jsonl.gz'
    rows=[json.loads(row) for row in gzip.decompress(scene.read_bytes()).splitlines()]
    originals=[row for row in rows[1:] if row.get('kind')=='geometry']
    if len(originals)!=20:raise ValueError('Unexpected original synthetic fixture count')
    identity=[1.,0.,0.,0.,1.,0.,0.,0.,1.]
    def meshpart(x):
        return {
            'kind':'geometry','class':'MeshPart','shape':'1','mat':'512',
            'r':identity,'t':[x,2.,0.],'s':[4.,3.,4.],
            'rgb':[120,127,140],'opacity':1.,'shadow':False,
            'collidable':False,'meshId':'','textureId':''
        }
    # Preserve source validity counts, add physical-unrelated visuals only.
    rows[0]['counts']['geometry']+=2
    rows.extend([meshpart(4.),meshpart(1800.)])
    encoded=('\n'.join(json.dumps(row,sort_keys=True,separators=(',',':'))
                       for row in rows)+'\n').encode('utf-8')
    scene.write_bytes(gzip.compress(encoded,mtime=0,compresslevel=6))
    pack=root/'Content'/'Geometry'/'Laboratory.native28.gz'
    report=extract(scene,pack,synthetic=True)
    counts=read_pack(pack)
    if report['native_instances']!=18 or counts['instances']!=18 or (
        report['original_geometry_count']!=22):
        raise ValueError('Pass45 source+native cache fixture regeneration failed')
    print('TWR_PASS45_SYNTHETIC_CREATED original_source=false '+
          'native=18 special_legacy=1 near_proxy=1 far_proxy=1')

def clean(root:Path):
    clean44(root)
    print('TWR_PASS45_SYNTHETIC_CLEANED')

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',required=True,type=Path)
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--create',action='store_true')
    mode.add_argument('--clean',action='store_true')
    args=parser.parse_args()
    (create if args.create else clean)(args.output)

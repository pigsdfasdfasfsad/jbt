#!/usr/bin/env python3
"""Fabricated Laboratory native instance stream + scene for Windows CI only."""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from make_pass39_synthetic import create as create39, clean as clean39
from build_source_primitives44 import extract,read_pack

def create(root:Path):
    create39(root)
    scene=root/'Content'/'Maps'/'Laboratory.scene.jsonl.gz'
    rows=[json.loads(line) for line in gzip.decompress(scene.read_bytes()).splitlines()]
    geometry=[row for row in rows[1:] if row.get('kind')=='geometry']
    if len(geometry)!=20:
        raise ValueError('Pass39 synthetic fixture geometry changed')
    geometry[0]['specialMeshType']='6'  # MUST stay in legacy visual renderer.
    geometry[1]['opacity']=0.0  # Not rendered, source collisions still count.
    geometry[-2]['t']=[1000.,0.,0.]
    geometry[-1]['t']=[1000.,0.,20.]
    # Support spatially separate, near (two tiles) and far (one tile)
    # visibility checks against actual Pass28 Godot MultiMesh instances.
    content=('\n'.join(json.dumps(row,sort_keys=True,separators=(',',':'))
                       for row in rows)+'\n').encode()
    scene.write_bytes(gzip.compress(content,mtime=0,compresslevel=6))
    pack=root/'Content'/'Geometry'/'Laboratory.native28.gz'
    manifest=extract(scene,pack,synthetic=True)
    decoded=read_pack(pack)
    if decoded['instances']!=18 or manifest['tiles']!=3 or manifest['batches']<3:
        raise ValueError('Synthetic native stream source count mismatch')
    print('TWR_PASS44_SYNTHETIC_CREATED instances=18 far_tiles=true '+str(manifest['batches']))

def clean(root:Path):
    for name in ('Laboratory.native28.gz','LABORATORY_NATIVE_RENDER_MANIFEST44.json'):
        (root/'Content'/'Geometry'/name).unlink(missing_ok=True)
    folder=root/'Content'/'Geometry'
    if folder.is_dir() and not any(folder.iterdir()):
        folder.rmdir()
    clean39(root)
    print('TWR_PASS44_SYNTHETIC_CLEANED')

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',required=True,type=Path)
    group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--create',action='store_true')
    group.add_argument('--clean',action='store_true')
    args=parser.parse_args()
    (create if args.create else clean)(args.output)

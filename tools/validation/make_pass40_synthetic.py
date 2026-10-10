#!/usr/bin/env python3
"""Build/delete synthetic Laboratory nav31 + physically walkable floor for CI.
All scene coordinates, PNGs and waypoints are FAKE fixtures, never Roblox art.
"""
from __future__ import annotations
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import struct
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from make_pass39_synthetic import create as create_pass39, clean as clean_pass39

def create(root:Path):
    create_pass39(root)
    scene=root/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    rows=[json.loads(s) for s in gzip.decompress(scene.read_bytes()).splitlines()]
    # First visual Part becomes a huge, collision-enabled sample floor.
    # All others are nonphysical so generated sound/spawn probes stay clear.
    for row in rows[1:]:
        if row.get('kind')=='geometry':
            row['collidable']=False
    first=next(row for row in rows[1:] if row.get('kind')=='geometry')
    first.update({'t':[0.,0.,0.],'s':[200.,1.,200.],'collidable':True})
    for row in rows[1:]:
        if row.get('kind')=='spawn':
            row['t']=[0.,3.,0.] if row['side']=='player' else [1000.,3.,1000.]
    encoded=("\n".join(json.dumps(r,sort_keys=True,separators=(',',':')) for r in rows)+"\n").encode()
    packed=gzip.compress(encoded,mtime=0,compresslevel=6)
    scene.write_bytes(packed)
    nav=root/"Content"/"Navigation"/"Laboratory.nav31.gz"
    nav.parent.mkdir(parents=True,exist_ok=True)
    nodes=[(3.5*i,.5,0.) for i in range(19)]
    edges=[(i,i+1) for i in range(len(nodes)-1)]
    data=bytearray(b"TWRNAV31")
    data+=struct.pack('<Iii',2,len(nodes),len(edges))
    data+=hashlib.sha256(packed).digest()
    for v in nodes:data+=struct.pack('<fff',*v)
    for edge in edges:data+=struct.pack('<ii',*edge)
    nav.write_bytes(gzip.compress(data,mtime=0,compresslevel=6))
    print("TWR_PASS40_SYNTHETIC_CREATED graph_nodes=19 edges=18 source_sha_bound=true")

def clean(root:Path):
    nav=root/"Content"/"Navigation"/"Laboratory.nav31.gz"
    nav.unlink(missing_ok=True)
    folder=nav.parent
    if folder.is_dir() and not any(folder.iterdir()):folder.rmdir()
    clean_pass39(root)
    print("TWR_PASS40_SYNTHETIC_CLEANED")

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',required=True,type=Path)
    group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--create',action='store_true')
    group.add_argument('--clean',action='store_true')
    args=parser.parse_args()
    (create if args.create else clean)(args.output)

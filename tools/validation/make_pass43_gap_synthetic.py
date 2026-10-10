#!/usr/bin/env python3
"""Pass43 deliberately disconnected, collision-real two-platform jump fixture.

ALL INPUT IS FABRICATED from the existing synthetic Pass42 fixture. Platform
gap is 4.2 source studs: walk edges cannot cross it, only a typed jump can.
No owner maps, source nav, or original Roblox art enter public CI.
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
from make_pass42_synthetic import create as create42, clean as clean42

def create(root:Path):
    create42(root)
    scene=root/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    records=[json.loads(x) for x in gzip.decompress(scene.read_bytes()).splitlines()]
    header=records[0]
    solid=next(x for x in records[1:] if x.get("kind")=="geometry" and x.get("collidable"))
    # Left side ends at source X=29.4, right begins at 33.6, giving
    # a 1.176-metre empty pit below the 7-stud leap from node 8 to node 10.
    solid.update({"t":[-35.3,0.,0.],"s":[129.4,1.,200.]})
    second=solid.copy()
    second.update({"t":[66.8,0.,0.],"s":[66.4,1.,200.]})
    records.insert(2,second)
    header["counts"]["geometry"]+=1
    for x in records:
        if x.get("kind")=="spawn" and x.get("side")=="infected":
            x["t"]=[28.,.5,0.]
    raw=("\n".join(json.dumps(x,sort_keys=True,separators=(",",":"))
                     for x in records)+"\n").encode()
    packed=gzip.compress(raw,mtime=0,compresslevel=6)
    scene.write_bytes(packed)
    digest=hashlib.sha256(packed).digest()
    navigation=root/"Content"/"Navigation"
    for filename in ("Laboratory.nav31.gz","Laboratory.nav41.gz","Laboratory.nav42.gz"):
        path=navigation/filename
        data=bytearray(gzip.decompress(path.read_bytes()))
        if data[:8] not in (b"TWRNAV31",b"TWRNAV41",b"TWRNAV42"):
            raise ValueError("Old synthetic navigation framing changed")
        data[20:52]=digest
        path.write_bytes(gzip.compress(bytes(data),mtime=0,compresslevel=6))
    print("TWR_PASS43_GAP_FIXTURE_CREATED gap_source_studs=4.2 "+
          "real_physics_two_platforms=true synthetic_source_only=true")

def clean(root:Path):
    clean42(root)
    print("TWR_PASS43_GAP_FIXTURE_CLEANED")

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True,type=Path)
    options=parser.add_mutually_exclusive_group(required=True)
    options.add_argument("--create",action="store_true")
    options.add_argument("--clean",action="store_true")
    args=parser.parse_args()
    (create if args.create else clean)(args.output)

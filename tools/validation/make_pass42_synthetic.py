#!/usr/bin/env python3
"""Synthetic, source-identity-bound TWRNAV42 jump fixture for exported Windows.

Reuses an entirely fabricated tiny Laboratory scene. One existing 7-stud
corridor is encoded as a *jump action*; no original Roblox data enters CI.
"""
from __future__ import annotations
import argparse
import gzip
from pathlib import Path
import struct
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from make_pass41_synthetic import create as create41, clean as clean41

def create(root:Path):
    create41(root)
    nav=root/"Content"/"Navigation"/"Laboratory.nav41.gz"
    raw=gzip.decompress(nav.read_bytes())
    if raw[:8]!=b"TWRNAV41":
        raise ValueError("Synthetic Pass41 source navigation missing")
    version,nodes,edge_count=struct.unpack_from("<Iii",raw,8)
    if (version,nodes,edge_count)!=(3,19,18):
        raise ValueError("Expected synthetic 19-node repaired graph")
    coords=raw[52:52+12*nodes]
    edges=[struct.unpack_from("<ii",raw,52+12*nodes+8*i)
           for i in range(edge_count)]
    if (8,10) not in edges:
        raise ValueError("Fabricated 7-stud bridge missing")
    payload=bytearray(b"TWRNAV42")
    payload+=struct.pack("<Iii",4,nodes,edge_count)
    payload+=raw[20:52]+coords
    for a,b in edges:
        payload+=struct.pack("<iiB",a,b,1 if (a,b)==(8,10) else 0)
    (nav.parent/"Laboratory.nav42.gz").write_bytes(
        gzip.compress(bytes(payload),mtime=0,compresslevel=6))
    plans=root/"Content"/"MapPlans"
    for level in range(3):
        (plans/f"Laboratory.navgraph42-{level}.png").write_bytes(
            (plans/f"Laboratory.sourceplan39-{level}.png").read_bytes())
    print("TWR_PASS42_FIXTURE_CREATED synthetic_jump_edges=1 original_data=false")

def clean(root:Path):
    (root/"Content"/"Navigation"/"Laboratory.nav42.gz").unlink(missing_ok=True)
    for i in range(3):
        (root/"Content"/"MapPlans"/f"Laboratory.navgraph42-{i}.png").unlink(missing_ok=True)
    clean41(root)
    print("TWR_PASS42_FIXTURE_CLEANED")

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True,type=Path)
    p=parser.add_mutually_exclusive_group(required=True)
    p.add_argument("--create",action="store_true")
    p.add_argument("--clean",action="store_true")
    args=parser.parse_args()
    (create if args.create else clean)(args.output)

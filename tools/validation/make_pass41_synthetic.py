#!/usr/bin/env python3
"""Tiny entirely FABRICATED Laboratory v3 bridge graph for Windows smoke.

The gap from node 8 to 10 is 7 studs on a synthetic solid floor. No
owner geometry, source nav, source PNG, custom mesh or original Roblox data.
"""
from __future__ import annotations
import argparse
import gzip
from pathlib import Path
import struct
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from make_pass40_synthetic import create as create40, clean as clean40

def create(root:Path):
    create40(root)
    nav=root/'Content'/'Navigation'/'Laboratory.nav31.gz'
    source=gzip.decompress(nav.read_bytes())
    if source[:8]!=b'TWRNAV31':raise ValueError('Synthetic Pass40 nav header missing')
    v,n,e=struct.unpack_from('<Iii',source,8)
    if (v,n,e)!=(2,19,18):
        raise ValueError('Synthetic base navigation graph changed')
    points=source[52:52+12*n]
    originals=[struct.unpack_from('<ii',source,52+12*n+8*i) for i in range(e)]
    if (8,9) not in originals or (9,10) not in originals:
        raise ValueError('Synthetic graph lacks target corridor')
    repaired=[pair for pair in originals if pair!=(8,9)]+[(8,10)]
    output=bytearray(b'TWRNAV41')
    output+=struct.pack('<Iii',3,n,len(repaired))
    output+=source[20:52]+points
    for a,b in repaired:output+=struct.pack('<ii',a,b)
    (nav.parent/'Laboratory.nav41.gz').write_bytes(
        gzip.compress(bytes(output),mtime=0,compresslevel=6))
    plans=root/'Content'/'MapPlans'
    for level in range(3):
        (plans/f'Laboratory.navgraph41-{level}.png').write_bytes(
            (plans/f'Laboratory.sourceplan39-{level}.png').read_bytes())
    print('TWR_PASS41_FIXTURE_CREATED fabricated_graph=19 native_bridge=1 no_source_data=true')

def clean(root:Path):
    (root/'Content'/'Navigation'/'Laboratory.nav41.gz').unlink(missing_ok=True)
    for level in range(3):
        (root/'Content'/'MapPlans'/f'Laboratory.navgraph41-{level}.png').unlink(missing_ok=True)
    clean40(root)
    print('TWR_PASS41_FIXTURE_CLEANED')

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output',required=True,type=Path)
    flags=p.add_mutually_exclusive_group(required=True)
    flags.add_argument('--create',action='store_true')
    flags.add_argument('--clean',action='store_true')
    arg=p.parse_args()
    (create if arg.create else clean)(arg.output)

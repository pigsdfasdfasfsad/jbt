#!/usr/bin/env python3
"""Tiny generated terrain fixture: one solid voxel cube and a water top."""
import argparse
import gzip
import struct
from pathlib import Path

def generate(target: Path):
    target.parent.mkdir(parents=True,exist_ok=True)
    palette=bytearray([105,110,98]*23)
    palette[6:9]=bytes((85,123,79))
    quads=[]
    for direction in range(6):
        quads.append((direction,3,3,4,1,1,2))
    quads.append((2,10,3,9,1,1,1))
    stream=bytearray(b"TWRTERR1")
    stream += struct.pack("<I",1)
    stream += bytes(palette)
    stream += struct.pack("<iiiI",0,0,0,len(quads))
    for face in quads:stream+=bytes(face)
    with target.open("wb") as out,gzip.GzipFile(
        filename="",fileobj=out,mode="wb",mtime=0,compresslevel=6
    ) as archive:
        archive.write(stream)
    print("TWR_SYNTHETIC_TERRAIN_OK chunks=1 faces=7")

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,required=True)
    generate(parser.parse_args().output)

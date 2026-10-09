#!/usr/bin/env python3
"""Generate harmless OBJ/PNG outside the PCK to test true post-export loading."""
import argparse
import struct
import zlib
from pathlib import Path

ID="99887766"

def chunk(name, body):
    return (struct.pack(">I",len(body))+name+body+
            struct.pack(">I",zlib.crc32(name+body)&0xffffffff))

def make(root):
    mesh = root/"Meshes"/(ID+".obj")
    image = root/"Textures"/(ID+".png")
    mesh.parent.mkdir(parents=True,exist_ok=True)
    image.parent.mkdir(parents=True,exist_ok=True)
    mesh.write_text(
        "# synthetic, unit-normalized mesh for Windows CI\n"
        "v -0.5 -0.5 0\nv 0.5 -0.5 0\n"
        "v 0.5 0.5 0\nv -0.5 0.5 0\n"
        "vt 0 0\nvt 1 0\nvt 1 1\nvt 0 1\n"
        "f 1/1 2/2 3/3 4/4\n",encoding="ascii")
    # A genuine image, not merely a file carrying a .png extension.
    pixel_data=(b"\x00"+bytes([225,42,55,255,50,200,70,255]))*2
    ihdr=struct.pack(">IIBBBBB",2,2,8,6,0,0,0)
    png=(b"\x89PNG\r\n\x1a\n"+chunk(b"IHDR",ihdr)+
         chunk(b"IDAT",zlib.compress(pixel_data))+
         chunk(b"IEND",b""))
    image.write_bytes(png)
    print(f"TWR_PRIVATE_ART_FIXTURE_OK id={ID}")

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",type=Path,required=True)
    make(parser.parse_args().output_dir)

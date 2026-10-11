#!/usr/bin/env python3
"""Pass55 synthetic-only source skin layer and PNG fixtures.

Any preexisting file is never overwritten. Cleanup verifies exact fake data
before deletion, preventing removal of owner-installed source art.
"""
from __future__ import annotations
import argparse
import gzip
import json
import struct
import zlib
from pathlib import Path

PACK_NAME="SourceSkinLayers55.json.gz"
TEXTURE_ID="5555500000001"
MISSING_ID="5555500000002"
FORMAT="twr-source-skin-texture-layers-v1"


def blank():
    return {"meshTextureId":"","layers":[]}


def fixture():
    skins={}
    for name in ("Low/Cocoa","High/Gold","Ethereal/Spirit"):
        skins[name]={"imageId":"","slots":[blank() for _ in range(5)]}
    skins["High/Gold"]["slots"][0]["layers"].append({
        "kind":"Texture","textureId":TEXTURE_ID,"rgb":[255,255,255],
        "transparency":0,"tileU":2,"tileV":3,
        "offsetU":.25,"offsetV":-.125,"zIndex":0})
    skins["Ethereal/Spirit"]["slots"][0]["meshTextureId"]=MISSING_ID
    return {"format":FORMAT,"synthetic":True,"skin_count":3,
            "layer_count":1,"mesh_texture_count":1,"skins":skins}


def png_chunk(tag,data):
    return (struct.pack(">I",len(data))+tag+data+
            struct.pack(">I",zlib.crc32(tag+data)&0xffffffff))


def png():
    # A clearly synthetic 4x4 opaque purple PNG, no image dependency.
    width=height=4
    row=b"\x00"+bytes([180,74,199,255])*width
    pixels=row*height
    return (b"\x89PNG\r\n\x1a\n"+
            png_chunk(b"IHDR",struct.pack(">IIBBBBB",width,height,8,6,0,0,0))+
            png_chunk(b"IDAT",zlib.compress(pixels,9))+
            png_chunk(b"IEND",b""))


def packed():
    payload=json.dumps(fixture(),sort_keys=True,separators=(",",":")).encode()
    return gzip.compress(payload,mtime=0,compresslevel=9)


def paths(args):
    return (args.output_dir/PACK_NAME,args.assets_dir/(TEXTURE_ID+".png"))


def main():
    p=argparse.ArgumentParser()
    p.add_argument("--output-dir",required=True,type=Path)
    p.add_argument("--assets-dir",required=True,type=Path)
    mode=p.add_mutually_exclusive_group(required=True)
    mode.add_argument("--create",action="store_true")
    mode.add_argument("--clean",action="store_true")
    args=p.parse_args()
    layer,texture=paths(args)
    expected_pack=packed()
    expected_png=png()
    if args.create:
        if layer.exists() or texture.exists():
            raise FileExistsError("Pass55 refuses to overwrite any source asset")
        layer.parent.mkdir(parents=True,exist_ok=True)
        texture.parent.mkdir(parents=True,exist_ok=True)
        with layer.open("xb") as f:f.write(expected_pack)
        try:
            with texture.open("xb") as f:f.write(expected_png)
        except Exception:
            if layer.read_bytes()==expected_pack:layer.unlink()
            raise
        print("TWR_PASS55_SYNTHETIC_LAYER_READY layers=1 installed_png=1 missing_png=1")
    else:
        if layer.exists() != texture.exists():
            raise ValueError("Partial fixture; refusing to delete uncertain contents")
        if not layer.exists():
            print("TWR_PASS55_SYNTHETIC_LAYER_ALREADY_CLEAN")
            return
        if layer.read_bytes()!=expected_pack or texture.read_bytes()!=expected_png:
            raise ValueError("Refusing cleanup: source pack is not the Pass55 fixture")
        layer.unlink()
        texture.unlink()
        print("TWR_PASS55_SYNTHETIC_LAYER_CLEANED")


if __name__=="__main__":main()

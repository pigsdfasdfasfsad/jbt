#!/usr/bin/env python3
"""Deterministic Pass56 wiki reference smoke ZIP. Never overwrites owner assets."""
from __future__ import annotations
import argparse
import hashlib
from io import BytesIO
import json
from pathlib import Path
import struct
import zlib
from zipfile import ZIP_DEFLATED,ZipFile,ZipInfo

PACK="SourceWikiReferenceImages56.zip"
SOURCE_FORMAT="twr-wiki-weapon-reference-v1"
PATHS=("reference-images/G17-Default.png",
       "reference-images/G17-Ego.png",
       "reference-images/EgoIcon.png")


def chunk(tag,data):
    return (struct.pack(">I",len(data))+tag+data+
            struct.pack(">I",zlib.crc32(tag+data)&0xffffffff))


def png(red,green,blue):
    row=b"\0"+bytes((red,green,blue,255))*4
    return (b"\x89PNG\r\n\x1a\n"+
            chunk(b"IHDR",struct.pack(">IIBBBBB",4,4,8,6,0,0,0))+
            chunk(b"IDAT",zlib.compress(row*4,9))+chunk(b"IEND",b""))


def content():
    images={
        PATHS[0]:png(100,100,100),
        PATHS[1]:png(82,33,180),
        PATHS[2]:png(30,70,150)
    }
    file_data={
        path:{"sha256":hashlib.sha256(raw).hexdigest(),
              "length":len(raw),"width":4,"height":4,
              "subject":"Ego" if "Ego" in path else "Glock 17",
              "subjectType":"skin" if "Ego" in path else "weapon"}
        for path,raw in images.items()
    }
    meta={
        "format":SOURCE_FORMAT,"synthetic":True,
        "files":file_data,
        "weapons":{"Glock 17":{
            "default":PATHS[0],"poses":{},"skins":{"Neon/Ego":PATHS[1]}
        }},
        "skinIcons":{"Neon/Ego":PATHS[2]},
        "stats":{"catalogWeapons":1,"coveredWeapons":1,
                 "defaultWeaponReferences":1,"skinVariantReferences":1,
                 "poseReferences":0,"skinIconReferences":1,
                 "uniqueImageFiles":3}
    }
    buffer=BytesIO()
    with ZipFile(buffer,"w",compression=ZIP_DEFLATED,compresslevel=7) as z:
        def write(path,data):
            info=ZipInfo(path,(2020,1,1,0,0,0))
            info.compress_type=ZIP_DEFLATED
            z.writestr(info,data,compress_type=ZIP_DEFLATED,compresslevel=7)
        write("manifest.json",json.dumps(meta,sort_keys=True,
            separators=(",",":")).encode())
        for path in sorted(images):write(path,images[path])
    return buffer.getvalue()


def main():
    p=argparse.ArgumentParser()
    p.add_argument("--output",required=True,type=Path)
    actions=p.add_mutually_exclusive_group(required=True)
    actions.add_argument("--create",action="store_true")
    actions.add_argument("--clean",action="store_true")
    a=p.parse_args()
    expected=content()
    if a.create:
        a.output.parent.mkdir(parents=True,exist_ok=True)
        with a.output.open("xb") as f:f.write(expected)
        print("TWR_PASS56_SYNTHETIC_REFERENCE_READY images=3 weapons=1")
    else:
        if not a.output.exists():
            raise FileNotFoundError("Synthetic gallery was not created")
        if a.output.read_bytes()!=expected:
            raise ValueError("Refusing to delete unfamiliar owner reference pack")
        a.output.unlink()
        print("TWR_PASS56_SYNTHETIC_REFERENCE_CLEANED")


if __name__=="__main__":main()

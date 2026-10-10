#!/usr/bin/env python3
"""Fabricated Lab scene and 3 floorplans for the exported Windows F4 smoke.
No original Roblox models, place XML, binary meshes or image assets.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
import struct
import zlib

BASE="Laboratory"
PLANS=3

def png(size=64):
    def chunk(typ,data):
        return struct.pack(">I",len(data))+typ+data+struct.pack(">I",zlib.crc32(typ+data)&0xffffffff)
    raw=b"".join(b"\x00"+b"".join(bytes(((x*3)%256,(y*3)%256,90,255))
               for x in range(size)) for y in range(size))
    return (b"\x89PNG\r\n\x1a\n"+chunk(b"IHDR",struct.pack(">IIBBBBB",size,size,8,6,0,0,0))+
            chunk(b"IDAT",zlib.compress(raw))+chunk(b"IEND",b""))

def create(root:Path):
    maps=root/"Content"/"Maps";maps.mkdir(parents=True,exist_ok=True)
    plans=root/"Content"/"MapPlans";plans.mkdir(parents=True,exist_ok=True)
    ident=[1.,0.,0.,0.,1.,0.,0.,0.,1.]
    geometry=[{"kind":"geometry","class":"Part","shape":"1","mat":"512",
               "r":ident,"t":[i*3-27,0,0],"s":[2,1,2],
               "opacity":1.,"rgb":[145,149,155],"shadow":True,"collidable":i<7}
              for i in range(20)]
    walls=[{"kind":"collision","class":"Part","r":ident,
            "t":[0,5,30],"s":[5,5,1]}]
    light=[{"kind":"light","class":"PointLight","r":ident,
            "t":[0,5,0],"rgb":[255,220,170],"energy":1.,"range":16.,"angle":90.,"face":5}]
    spawn=[{"kind":"spawn","side":"player","name":"1","t":[0,5,0]},
           {"kind":"spawn","side":"infected","name":"InfectedSpawn","t":[20,5,20]}]
    pickup=[{"kind":"pickup_spawn","group":"Item","t":[2,5,3]},
            {"kind":"pickup_spawn","group":"Fortification","t":[-2,5,3]}]
    rows=geometry+walls+light+spawn+pickup
    counts={"geometry":20,"collision":1,"lights":1,"player_spawns":1,
            "infected_spawns":1,"item_markers":1,"fortification_markers":1}
    head={"format":"twr-source-map-v2","map":BASE,"synthetic":True,
          "source_member":"CI SYNTHETIC PASS39 ONLY",
          "owner_rbxlx_sha256":"0"*64,"scale":.28,"counts":counts}
    data="\n".join(json.dumps(x,sort_keys=True,separators=(",",":")) for x in [head,*rows])+"\n"
    (maps/(BASE+".scene.jsonl.gz")).write_bytes(gzip.compress(data.encode(),mtime=0))
    for i in range(PLANS):
        (plans/(BASE+f".sourceplan39-{i}.png")).write_bytes(png())
    print("TWR_PASS39_SYNTHETIC_CREATED original_bytes=false scene=Laboratory plans=3")

def clean(root:Path):
    for p in [root/"Content"/"Maps"/(BASE+".scene.jsonl.gz"),
              *[root/"Content"/"MapPlans"/(BASE+f".sourceplan39-{i}.png")
                for i in range(PLANS)]]:
        p.unlink(missing_ok=True)
    for p in [root/"Content"/"Maps",root/"Content"/"MapPlans"]:
        if p.is_dir() and not any(p.iterdir()):p.rmdir()
    print("TWR_PASS39_SYNTHETIC_CLEANED")

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,required=True)
    group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--create",action="store_true")
    group.add_argument("--clean",action="store_true")
    args=parser.parse_args()
    (create if args.create else clean)(args.output)

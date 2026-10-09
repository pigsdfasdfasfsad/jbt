#!/usr/bin/env python3
"""Synthetic six-weapon offline source-assembly test pack, no Roblox assets."""
import argparse,gzip,json
from pathlib import Path

NAMES=("Glock 17","Sawn Off Shotgun","AK-47","RPG-7","2x4","Molotov")
I=[1,0,0,0,1,0,0,0,1]

def generate(output):
    models={}
    for name in NAMES:
        parts=[]
        for i in range(4):
            parts.append({"name":("Barrel","Receiver","Magazine","Slide")[i],"class":"Part",
                          "kind":"geometry","ref":"test"+str(i),
                          "t":[i*.13,0,(-2.2 if i==0 else -i*.07)],"r":I,
                          "s":[.3,.3,.8],"rgb":[65+i*12,80,85],
                          "opacity":1.0,"shape":"1","collidable":False,
                          "mat":"272","shadow":False})
        models[name]={"parts":parts,"original_mesh_ids":[]}
    data={"format":"twr-original-weapon-assemblies-v1",
          "synthetic":True,"models":models}
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open("wb") as f,gzip.GzipFile(
        filename="",mode="wb",fileobj=f,mtime=0,compresslevel=6
    ) as stream:
        stream.write(json.dumps(data,separators=(",",":"),sort_keys=True).encode())
    print("TWR_SYNTHETIC_ORIGINAL_WEAPONS_OK models="+str(len(models)))

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True,type=Path)
    generate(parser.parse_args().output)

#!/usr/bin/env python3
"""Synthetic R6 source-blueprint fixture, not original private zombie data."""
import argparse
import gzip
import json
from pathlib import Path

TYPES=("Civilian","Sprinter","Military","Hazmat","Burster","Bolter")
I=[1,0,0,0,1,0,0,0,1]
PARTS=[
 ("Torso","Torso",[0,0,0],[2,2,1],[120,130,110]),
 ("Head","Head",[0,1.52,0],[1,1,1],[145,120,105]),
 ("Left Arm","LeftArm",[-1.5,0,0],[1,2,1],[95,105,100]),
 ("Right Arm","RightArm",[1.5,0,0],[1,2,1],[95,105,100]),
 ("Left Leg","LeftLeg",[-0.5,-2,0],[1,2,1],[63,67,63]),
 ("Right Leg","RightLeg",[0.5,-2,0],[1,2,1],[63,67,63])
]

def generate(path):
    types={}
    for type_name in TYPES:
        parts=[{"name":name,"region":region,"class":"Part","t":t,
                "r":I,"s":size,"rgb":rgb,"shape":"1","opacity":1.0}
               for name,region,t,size,rgb in PARTS]
        types[type_name]=[{"kind":"original_infected_snapshot_variant",
                           "infected_type":type_name,
                           "variant_hash":"synthetic",
                           "parts":parts}]
    data={"format":"twr-source-infected-variants-v1","synthetic":True,
          "types":types}
    path.parent.mkdir(parents=True,exist_ok=True)
    with path.open("wb") as output, gzip.GzipFile(
        filename="",mode="wb",fileobj=output,compresslevel=6,mtime=0
    ) as gz:
        gz.write(json.dumps(data,sort_keys=True,separators=(",",":")).encode())
    print("TWR_SYNTHETIC_INFECTED_PACK_OK types="+str(len(types)))

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--output",type=Path,required=True)
    generate(p.parse_args().output)

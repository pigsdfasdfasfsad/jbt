#!/usr/bin/env python3
"""Pass62 CI fixture: fabricated tool groups, invisible Handle pivots and parts.
Never reproduces, overwrites, or publishes the original owner's source pack.
"""
import argparse
import gzip
import json
from pathlib import Path

IDENTITY=[1.,0.,0.,0.,1.,0.,0.,0.,1.]
YAW90=[0.,0.,1.,0.,1.,0.,-1.,0.,0.]

def part(name,position,rotation=IDENTITY,visible=True):
    return {"name":name,"class":"Part","t":list(position),"r":list(rotation),
            "s":[.24,.18,.32],"rgb":[105,111,117],
            "opacity":1.0 if visible else 0.0,
            "mat":"272","shape":"1","shadow":False}

def hierarchy(name,parts,groups):
    return {"part_count":len(parts),"groups":[
        {"path":[key],"part_indices":indices,
         "pivot_t":list(pivot),"pivot_r":list(rotation)}
        for key,indices,pivot,rotation in groups]}

def payloads():
    glock=[
        part("Handle",(0.,0.,0.),visible=False),
        part("Handle",(.4,0.,-.2),YAW90,False),
        part("Slide",(.4,.1,-.45),YAW90),
        part("HousingDetail",(.47,.1,-.50),YAW90),
        part("Handle",(-.1,-.2,.03),visible=False),
        part("Magazine",(-.1,-.3,.1)),
        part("Body",(0.,-.1,-.1)),
        part("Barrel",(0.,0.,-.75)),
        part("AimPart",(0.,0.,-.1),visible=False)]
    moss=[
        part("Handle",(0.,0.,0.),visible=False),
        part("Handle",(.3,0.,-.4),YAW90,False),
        part("Pump",(.33,-.08,-.53),YAW90),
        part("ForeRail",(.33,-.08,-.64),YAW90),
        part("Handle",(.1,.1,-.35),visible=False),
        part("Chamber",(.1,.1,-.35)),
        part("Barrel",(0.,.1,-.7)),
        part("Body",(0.,0.,0.)),
        part("Pos",(0.,.1,-.01),visible=False),
        part("AimPart",(0.,.1,-.01),visible=False)]
    assembly={"format":"twr-original-weapon-assemblies-v1",
              "synthetic":True,"models":{
                  "Glock 17":{"parts":glock,"original_mesh_ids":[]},
                  "Mossberg 500":{"parts":moss,"original_mesh_ids":[]}}}
    sidecar={"format":"twr-source-weapon-hierarchy62-v1",
             "synthetic":True,
             "source":"synthetic-standin-only",
             "original_rbxlx_sha256":"synthetic",
             "source_assembly_sha256":"synthetic",
             "source_model_count":2,"original_part_count":19,
             "models":{
                 "Glock 17":hierarchy("Glock 17",glock,[
                     ("Slide",[1,2,3],(.4,0.,-.2),YAW90),
                     ("Mag",[4,5],(-.1,-.2,.03),IDENTITY)]),
                 "Mossberg 500":hierarchy("Mossberg 500",moss,[
                     ("Pump",[1,2,3],(.3,0.,-.4),YAW90),
                     ("Slide",[4,5],(.1,.1,-.35),IDENTITY)])}}
    return [gzip.compress(json.dumps(data,sort_keys=True,separators=(",",":")
                ).encode("utf-8"),mtime=0,compresslevel=6)
            for data in (assembly,sidecar)]

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--source",required=True,type=Path)
    p.add_argument("--hierarchy",required=True,type=Path)
    operation=p.add_mutually_exclusive_group(required=True)
    operation.add_argument("--create",action="store_true")
    operation.add_argument("--clean",action="store_true")
    args=p.parse_args()
    source,sidecar=payloads()
    names=[args.source,args.hierarchy]
    blobs=[source,sidecar]
    if args.create:
        if any(path.exists() for path in names):
            raise FileExistsError("Pass62 refuses to overwrite original source packs")
        for path,blob in zip(names,blobs):
            path.parent.mkdir(parents=True,exist_ok=True)
            try:
                with path.open("xb") as handle:handle.write(blob)
            except Exception:
                for prior,expected in zip(names,blobs):
                    if prior.exists() and prior.read_bytes()==expected:prior.unlink()
                raise
        print("TWR_PASS62_SYNTHETIC_CREATED models=2 "+
              "slide_group_parts=2 pump_group_parts=2")
    else:
        for path,expected in zip(names,blobs):
            if not path.exists() or path.read_bytes()!=expected:
                raise ValueError("Pass62 refuses to remove an unfamiliar owner pack")
        for path in names:path.unlink()
        print("TWR_PASS62_SYNTHETIC_CLEANED")

if __name__=="__main__":
    main()

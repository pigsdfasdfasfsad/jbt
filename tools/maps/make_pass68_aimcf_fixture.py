#!/usr/bin/env python3
"""Pass68 fake source parts for inverse/special AimCF algebra smokes.

Creates entirely fabricated helper geometry; NEVER overwrites owner packs,
never includes game meshes, Roblox scripts or original weapon transforms.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
from make_pass64_bolt_shell_fixture import blobs as prior
from make_pass62_motor_hierarchy_fixture import part

def blobs():
    source,sidecar=prior()
    a=json.loads(gzip.decompress(source))
    h=json.loads(gzip.decompress(sidecar))
    for name,aim in (
        ("MP7",(0.,.36,.85)),
        ("SPAS-12",(-.1,.17,1.42)),
        ("Winchester Model 70",(0.,.26,.88)),
    ):
        pieces=[
            part("Handle",(0.,0.,0.),visible=False),
            part("AimPart",aim,visible=False),
            part("Barrel",(0.,.1,-.85)),
            part("Receiver",(0.,0.,-.20)),
        ]
        a["models"][name]={"parts":pieces,"original_mesh_ids":[]}
        h["models"][name]={"part_count":len(pieces),"groups":[]}
    h["source_model_count"]=len(a["models"])
    h["original_part_count"]=sum(
        len(model["parts"]) for model in a["models"].values())
    return [gzip.compress(json.dumps(data,sort_keys=True,
                    separators=(",",":")).encode("utf-8"),
                    compresslevel=6,mtime=0)
            for data in (a,h)]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--hierarchy",type=Path,required=True)
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--create",action="store_true")
    mode.add_argument("--clean",action="store_true")
    args=parser.parse_args()
    paths=(args.source,args.hierarchy)
    payloads=blobs()
    if args.create:
        if any(path.exists() for path in paths):
            raise FileExistsError("Pass68 refuses to overwrite owner source")
        for path,packed in zip(paths,payloads):
            path.parent.mkdir(parents=True,exist_ok=True)
            try:
                with path.open("xb") as handle:handle.write(packed)
            except Exception:
                for previous,expected in zip(paths,payloads):
                    if previous.exists() and previous.read_bytes()==expected:
                        previous.unlink()
                raise
        print("TWR_PASS68_SYNTHETIC_CREATED models=6 "+
              "glock_mossberg_mp7_spas_winchester_aimparts=true")
    else:
        if any(not path.exists() or path.read_bytes()!=packed
               for path,packed in zip(paths,payloads)):
            raise ValueError("Pass68 refuses deletion of unknown owner pack")
        for path in paths:path.unlink()
        print("TWR_PASS68_SYNTHETIC_CLEANED")

if __name__=="__main__":
    main()

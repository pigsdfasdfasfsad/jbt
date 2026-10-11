#!/usr/bin/env python3
"""Pass64 synthetic source pack: nested bolt/bolt-handle and shell loading.

Uses fabricated local geometry only; cannot replace existing owner packs.
Previous Pass62/63 synthetic fixtures are left unchanged.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

from make_pass63_magazine_fixture import blobs as previous_blobs
from make_pass62_motor_hierarchy_fixture import part,IDENTITY

def blobs():
    source,groups=previous_blobs()
    model=json.loads(gzip.decompress(source))
    hierarchy=json.loads(gzip.decompress(groups))
    mosin=[
        part("Handle",(0,0,0),visible=False),
        part("Handle",(-.10,.025,-.10),visible=False),
        part("BoltBody",(-.10,.025,-.15)),
        part("Handle",(.065,.04,-.10),visible=False),
        part("BoltKnob",(.14,.09,-.12)),
        part("BoltLever",(.09,.05,-.16)),
        part("Barrel",(0,0,-.65)),
        part("Stock",(0,-.07,.20)),
    ]
    model["models"]["Mosin Nagant"]={
        "parts":mosin,"original_mesh_ids":[]}
    hierarchy["models"]["Mosin Nagant"]={
        "part_count":len(mosin),
        "groups":[
            {"path":["Bolt"],"part_indices":[1,2,3,4,5],
             "pivot_t":[-.10,.025,-.10],"pivot_r":IDENTITY},
            {"path":["Bolt","Bolt Handle"],"part_indices":[3,4,5],
             "pivot_t":[.065,.04,-.10],"pivot_r":IDENTITY}
        ]}
    hierarchy["source_model_count"]=3
    hierarchy["original_part_count"]=sum(
        len(entry["parts"]) for entry in model["models"].values())
    return [gzip.compress(json.dumps(value,sort_keys=True,
                    separators=(",",":")).encode("utf-8"),
                    compresslevel=6,mtime=0)
            for value in (model,hierarchy)]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--hierarchy",type=Path,required=True)
    act=parser.add_mutually_exclusive_group(required=True)
    act.add_argument("--create",action="store_true")
    act.add_argument("--clean",action="store_true")
    opts=parser.parse_args()
    paths=[opts.source,opts.hierarchy]
    data=blobs()
    if opts.create:
        if any(path.exists() for path in paths):
            raise FileExistsError(
                "Pass64 refuses to overwrite owner or previous synthetic pack")
        for path,packed in zip(paths,data):
            path.parent.mkdir(parents=True,exist_ok=True)
            try:
                with path.open("xb") as f:
                    f.write(packed)
            except Exception:
                for prior,expected in zip(paths,data):
                    if prior.exists() and prior.read_bytes()==expected:
                        prior.unlink()
                raise
        print("TWR_PASS64_SYNTHETIC_CREATED models=3 "+
              "mosin_bolt_group_parts=3 nested_handle_parts=2")
    else:
        if any(not path.exists() or path.read_bytes()!=packed
               for path,packed in zip(paths,data)):
            raise ValueError(
                "Pass64 refuses to clean unknown/owner original packs")
        for path in paths:path.unlink()
        print("TWR_PASS64_SYNTHETIC_CLEANED")

if __name__=="__main__":
    main()

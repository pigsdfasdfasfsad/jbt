#!/usr/bin/env python3
"""Pass69 synthetic lens families — never touches the owner source files.

Creates fake original-shaped 12-weapon assemblies for native testing.
The true ScopeDiameter values remain in original inert weapon JSON.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
from make_pass68_aimcf_fixture import blobs as previous
from make_pass62_motor_hierarchy_fixture import part

LENSES={
    "AUG":"ReticleGlass",
    "Barrett M82A1":"ScopeReticle",
    "M110 SASS":"ReticleGlass",
    "M14":"ScopeGlass",
    "SVD":"ReticleGlass",
    "VSS":"ReticleGlass",
    "Winchester Model 70":"ScopeGlass",
}

def blobs():
    original,hierarchy=previous()
    a=json.loads(gzip.decompress(original))
    h=json.loads(gzip.decompress(hierarchy))
    for name,glass in LENSES.items():
        if name not in a["models"]:
            pieces=[
                part("Handle",(0,0,0),visible=False),
                part("AimPart",(0,.25,.70),visible=False),
                part("Barrel",(0,.02,-.80)),
                part("Receiver",(0,0,-.25)),
            ]
            a["models"][name]={"parts":pieces,"original_mesh_ids":[]}
            h["models"][name]={"part_count":len(pieces),"groups":[]}
        parts=a["models"][name]["parts"]
        parts.append(part(glass,(0,.25,.20),visible=True))
        h["models"][name]["part_count"]=len(parts)
    h["source_model_count"]=len(a["models"])
    h["original_part_count"]=sum(
        len(model["parts"]) for model in a["models"].values())
    return [gzip.compress(json.dumps(item,sort_keys=True,
        separators=(",",":")).encode("utf-8"),compresslevel=6,mtime=0)
        for item in (a,h)]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--hierarchy",type=Path,required=True)
    act=parser.add_mutually_exclusive_group(required=True)
    act.add_argument("--create",action="store_true")
    act.add_argument("--clean",action="store_true")
    options=parser.parse_args()
    paths=(options.source,options.hierarchy)
    payload=blobs()
    if options.create:
        if any(path.exists() for path in paths):
            raise FileExistsError("Pass69 never overwrites existing source packs")
        for path,data in zip(paths,payload):
            path.parent.mkdir(parents=True,exist_ok=True)
            try:
                with path.open("xb") as f:f.write(data)
            except Exception:
                for prev,expected in zip(paths,payload):
                    if prev.exists() and prev.read_bytes()==expected:
                        prev.unlink()
                raise
        print("TWR_PASS69_SYNTHETIC_CREATED models=12 "+
              "seven_original_scope_lens_classes=true")
    else:
        if any(not path.exists() or path.read_bytes()!=data
               for path,data in zip(paths,payload)):
            raise ValueError("Pass69 will not delete unfamiliar owner packs")
        for path in paths:path.unlink()
        print("TWR_PASS69_SYNTHETIC_CLEANED")

if __name__=="__main__":
    main()

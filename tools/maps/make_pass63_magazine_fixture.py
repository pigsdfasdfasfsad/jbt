#!/usr/bin/env python3
"""Pass63 fake two-part magazine source geometry; never owner original assets."""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
from make_pass62_motor_hierarchy_fixture import payloads,part,IDENTITY

def blobs():
    source,group=payloads()
    a=json.loads(gzip.decompress(source))
    h=json.loads(gzip.decompress(group))
    glock=a["models"]["Glock 17"]["parts"]
    glock_index=len(glock)
    glock.append(part("MagazineSleeve",(-.13,-.36,.07)))
    glock_group=h["models"]["Glock 17"]
    mag=next(g for g in glock_group["groups"] if g["path"]==["Mag"])
    mag["part_indices"].append(glock_index)
    glock_group["part_count"]=len(glock)

    # Pass62's synthetic Mossberg has only Pump and Slide, so create
    # a fresh invented Mag group here. Do not modify the Pass62 fixture:
    # older native regression counts must remain byte-identical.
    moss=a["models"]["Mossberg 500"]["parts"]
    index=len(moss)
    moss.extend([
        part("Handle",(0.,-.4,.04),visible=False),
        part("Magazine",(0.,-.4,.12)),
        part("MagazineSleeve",(-.13,-.36,.07))
    ])
    moss_group=h["models"]["Mossberg 500"]
    moss_group["groups"].append({
        "path":["Mag"],"part_indices":[index,index+1,index+2],
        "pivot_t":[0.,-.4,.04],"pivot_r":IDENTITY})
    moss_group["part_count"]=len(moss)
    h["original_part_count"]=sum(len(m["parts"]) for m in a["models"].values())
    return [gzip.compress(json.dumps(x,sort_keys=True,separators=(",",":")
                          ).encode(),mtime=0,compresslevel=6)
            for x in (a,h)]

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--source",type=Path,required=True)
    p.add_argument("--hierarchy",type=Path,required=True)
    g=p.add_mutually_exclusive_group(required=True)
    g.add_argument("--create",action="store_true")
    g.add_argument("--clean",action="store_true")
    a=p.parse_args()
    files=(a.source,a.hierarchy)
    raw=blobs()
    if a.create:
        if any(x.exists() for x in files):
            raise FileExistsError("Pass63 will not overwrite an existing source pack")
        for path,data in zip(files,raw):
            path.parent.mkdir(parents=True,exist_ok=True)
            try:
                with path.open("xb") as f:f.write(data)
            except Exception:
                for prior,expected in zip(files,raw):
                    if prior.exists() and prior.read_bytes()==expected:
                        prior.unlink()
                raise
        print("TWR_PASS63_SYNTHETIC_CREATED guns=2 "+
              "glock_mag_visible_children=2 mossberg_mag_visible_children=2")
    else:
        if any(not path.exists() or path.read_bytes()!=data
               for path,data in zip(files,raw)):
            raise ValueError("Pass63 will not delete unfamiliar or owner source packs")
        for path in files:path.unlink()
        print("TWR_PASS63_SYNTHETIC_CLEANED")

if __name__=="__main__":
    main()

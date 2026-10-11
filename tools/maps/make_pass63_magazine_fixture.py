#!/usr/bin/env python3
"""Pass63 fake two-part magazine source geometry; never owner original assets."""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path
from make_pass62_motor_hierarchy_fixture import payloads,part

def blobs():
    source,group=payloads()
    a=json.loads(gzip.decompress(source))
    h=json.loads(gzip.decompress(group))
    for gun in ("Glock 17","Mossberg 500"):
        parts=a["models"][gun]["parts"]
        index=len(parts)
        parts.append(part("MagazineSleeve",(-.13,-.36,.07)))
        entry=h["models"][gun]
        mag=next(g for g in entry["groups"] if g["path"]==["Mag"])
        mag["part_indices"].append(index)
        entry["part_count"]=len(parts)
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

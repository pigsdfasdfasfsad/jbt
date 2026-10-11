#!/usr/bin/env python3
"""Pass57 isolated native source-geometry fixture with one REAL local OBJ.

This fixture is entirely fabricated, not a recovered Roblox triangle mesh.
The verified owner source pack, owner's local .obj files, and ordinary
offline profile are never overwritten or deleted.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

SOURCE_MESH_ID="555570001"
I=[1.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,1.0]
OBJ="""# SYNTHETIC TEST MESH - not a recovered Roblox MeshPart
v -0.5 -0.5 -0.5
v 0.5 -0.5 -0.5
v 0.0 0.5 -0.5
v 0.0 0.0 0.5
f 1 2 3
f 1 4 2
f 2 4 3
f 3 4 1
"""


def data()->bytes:
    entries=[
        ("Bolt","MeshPart",".6,.3,.9","rbxassetid://555570001","1",1.0),
        ("Barrel","MeshPart",".13,.12,1.15","rbxassetid://555570002","1",1.0),
        ("Receiver","MeshPart",".50,.36,.75","rbxassetid://555570003","1",1.0),
        ("Ball","Part",".25,.25,.25","","0",1.0),
        ("Cylinder","Part",".60,.18,.18","","2",1.0),
        ("Block","Part",".35,.35,.5","","1",1.0),
        ("StockWedge","WedgePart",".45,.25,.52","","1",1.0),
        ("Guard","UnionOperation",".41,.21,.60","","1",1.0),
        ("Handle","Part",".15,.15,.25","","1",0.0),
    ]
    parts=[]
    for i,(name,category,size,mesh_id,shape,opacity) in enumerate(entries):
        part={"name":name,"class":category,"t":[round(.04*i,5),0.0,-.45-round(.10*i,5)],
              "r":I,"s":[float(v) for v in size.split(",")],
              "rgb":[105,120,130],"opacity":opacity,"shape":shape,
              "mat":"272","shadow":False}
        if mesh_id:part["meshId"]=mesh_id
        parts.append(part)
    payload={
        "format":"twr-original-weapon-assemblies-v1","synthetic":True,
        "models":{"Glock 17":{"parts":parts,
                   "original_mesh_ids":["rbxassetid://555570001",
                                        "rbxassetid://555570002",
                                        "rbxassetid://555570003"]}}
    }
    return gzip.compress(json.dumps(payload,sort_keys=True,
        separators=(",",":")).encode(),mtime=0,compresslevel=6)


def files(pack:Path,mesh_dir:Path):
    return pack,mesh_dir/(SOURCE_MESH_ID+".obj")


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,required=True)
    parser.add_argument("--mesh-dir",type=Path,required=True)
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--create",action="store_true")
    mode.add_argument("--clean",action="store_true")
    args=parser.parse_args()
    pack,mesh=files(args.output,args.mesh_dir)
    expected_pack=data()
    expected_obj=OBJ.encode()
    if args.create:
        if pack.exists() or mesh.exists():
            raise FileExistsError("Refusing to overwrite owner/private geometry")
        pack.parent.mkdir(parents=True,exist_ok=True)
        mesh.parent.mkdir(parents=True,exist_ok=True)
        with pack.open("xb") as f:f.write(expected_pack)
        try:
            with mesh.open("xb") as f:f.write(expected_obj)
        except Exception:
            if pack.read_bytes()==expected_pack:pack.unlink()
            raise
        print("TWR_PASS57_SYNTHETIC_GEOMETRY_READY source_parts=9 "+
              "visible_parts=8 local_obj=1 missing_mesh_ids=2")
    else:
        if not pack.exists() or not mesh.exists() or (
            pack.read_bytes()!=expected_pack or mesh.read_bytes()!=expected_obj
        ):
            raise ValueError("Refusing to delete nonmatching owner geometry files")
        pack.unlink()
        mesh.unlink()
        print("TWR_PASS57_SYNTHETIC_GEOMETRY_CLEANED")


if __name__=="__main__":
    main()

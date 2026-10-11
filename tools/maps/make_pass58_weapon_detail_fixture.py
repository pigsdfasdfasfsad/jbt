#!/usr/bin/env python3
"""Pass58 native Windows fixture for source-guided 3D geometry.

Entirely synthetic original-style records, one fabricated OBJ. Never
overwrites or deletes user content unless it exactly matches this fixture.
"""
from __future__ import annotations

import argparse
import gzip
import json
from pathlib import Path

MESH_ID = "555580001"
ROTATION = [1.,0.,0.,0.,1.,0.,0.,0.,1.]
OBJ = """# PASS58_SYNTHETIC_TETRAHEDRON, NOT ORIGINAL ROBLOX GEOMETRY
v -0.5 -0.5 -0.5
v 0.5 -0.5 -0.5
v 0.0 0.5 -0.5
v 0.0 0.0 0.5
f 1 2 3
f 1 4 2
f 2 4 3
f 3 4 1
"""
COMPONENTS = [
    ("Bolt","MeshPart",(.22,.31,.68),"rbxassetid://555580001","1",1.),
    ("Barrel","MeshPart",(.12,.12,.96),"rbxassetid://555580002","1",1.),
    ("Receiver","MeshPart",(.21,.39,.79),"rbxassetid://555580003","1",1.),
    ("Magazine","MeshPart",(.13,.57,.34),"rbxassetid://555580004","1",1.),
    ("Stock","MeshPart",(.16,.51,.91),"rbxassetid://555580005","1",1.),
    ("PistolGrip","MeshPart",(.11,.46,.39),"rbxassetid://555580006","1",1.),
    ("TriggerGuard","MeshPart",(.09,.43,.41),"rbxassetid://555580007","1",1.),
    ("ReticleGlass","MeshPart",(.16,.16,.04),"rbxassetid://555580008","1",.4),
    ("UnknownCasing","MeshPart",(.34,.35,.45),"rbxassetid://555580009","1",1.),
    ("Ball","Part",(.29,.29,.29),"","0",1.),
    ("Cylinder","Part",(.61,.18,.18),"","2",1.),
    ("StockWedge","WedgePart",(.41,.27,.49),"","1",1.),
    ("Handle","Part",(.12,.12,.13),"","1",0.),
]


def build_pack() -> bytes:
    parts=[]
    for i,(name,kind,size,mesh_id,shape,opacity) in enumerate(COMPONENTS):
        record={
            "name":name,"class":kind,"s":list(size),
            "t":[round(.06*i,5),0.0,round(-.6-.04*i,5)],
            "r":ROTATION,"rgb":[95,107,117],"opacity":opacity,
            "shape":shape,"mat":"272","shadow":False
        }
        if mesh_id:record["meshId"]=mesh_id
        parts.append(record)
    payload={
        "format":"twr-original-weapon-assemblies-v1","synthetic":True,
        "models":{"Glock 17":{"parts":parts,
                  "original_mesh_ids":[part["meshId"] for part in parts
                         if "meshId" in part]}}
    }
    body=json.dumps(payload,sort_keys=True,separators=(",",":")).encode()
    return gzip.compress(body,mtime=0,compresslevel=6)


def main() -> None:
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,required=True)
    parser.add_argument("--mesh-dir",type=Path,required=True)
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--create",action="store_true")
    mode.add_argument("--clean",action="store_true")
    args=parser.parse_args()
    pack=args.output
    mesh=args.mesh_dir/(MESH_ID+".obj")
    original_pack=build_pack()
    original_mesh=OBJ.encode("ascii")
    if args.create:
        if pack.exists() or mesh.exists():
            raise FileExistsError("Pass58 refuses to overwrite original source geometry")
        pack.parent.mkdir(parents=True,exist_ok=True)
        mesh.parent.mkdir(parents=True,exist_ok=True)
        with pack.open("xb") as f:f.write(original_pack)
        try:
            with mesh.open("xb") as f:f.write(original_mesh)
        except Exception:
            if pack.read_bytes()==original_pack:pack.unlink()
            raise
        print("TWR_PASS58_FAKE_SOURCE_CREATED parts=13 visible=12 "+
              "mesh_ids=9 local_obj=1 profile_components=5 lens=1")
    else:
        if not pack.exists() or not mesh.exists() or (
            pack.read_bytes()!=original_pack or mesh.read_bytes()!=original_mesh
        ):
            raise ValueError("Pass58 refuses to remove unfamiliar owner assets")
        pack.unlink()
        mesh.unlink()
        print("TWR_PASS58_FAKE_SOURCE_CLEANED")


if __name__=="__main__":
    main()

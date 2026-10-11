#!/usr/bin/env python3
"""Extract original weapon Model ancestry and Handle pivots, without Luau execution.

Produces an owner-only sidecar keyed by the *existing* Pass47 source part
indices, leaving the SHA-pinned Pass47 original pack unchanged. Never
contains original MeshPart triangle bytes or arbitrary executable scripts.
"""
from __future__ import annotations
import argparse
from collections import defaultdict
import gzip
import hashlib
import json
import math
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

PREFIX = ("ReplicatedStorage", "Models", "Tools")
MEMBER = "TestPlace/TestPlace.rbxlx"
SOURCE_SHA = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"
ASSEMBLY_SHA = "fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7"
PARTS = {"Part","MeshPart","WedgePart","CornerWedgePart","UnionOperation"}
ALIASES = {"2x4 w/ Barbed Wire":"2x4 w- Barbed Wire",
           "Ruger 10/22":"Ruger 10-22"}

def finite(props,key):
    n=float(props.get(key,""))
    if not math.isfinite(n) or abs(n)>100000:
        raise ValueError("Invalid source CFrame element")
    return round(n,7)

def source_cframe(props):
    cf=props.get("CFrame") or {}
    if not cf:raise ValueError("Source part lacks CFrame")
    pos=[finite(cf,key) for key in ("X","Y","Z")]
    rotation=[finite(cf,f"R{i}{j}") for i in range(3) for j in range(3)]
    return pos,rotation

def read_original(archive:Path,member=MEMBER):
    parts=defaultdict(list)
    groups=defaultdict(dict)
    stack=[]
    with ZipFile(archive) as z,z.open(member) as stream:
        for event,el in etree.iterparse(stream,events=("start","end"),
                                     tag=("Item","Properties"),huge_tree=True):
            if event=="start" and el.tag=="Item":
                stack.append({"type":el.get("class"),"name":"","props":{}})
            elif event=="end" and el.tag=="Properties" and stack:
                cur=stack[-1]
                name=el.find('./string[@name="Name"]')
                cur["name"]=name.text if name is not None and name.text else ""
                targeted=len(stack)>=5 and tuple(
                    item["name"] for item in stack[:3])==PREFIX
                if targeted and cur["type"] in PARTS:
                    cf=el.find('./CoordinateFrame[@name="CFrame"]')
                    if cf is not None:
                        cur["props"]["CFrame"]={
                            child.tag:(child.text or "").strip() for child in cf}
                el.clear()
            elif event=="end" and el.tag=="Item":
                targeted=len(stack)>=5 and tuple(
                    item["name"] for item in stack[:3])==PREFIX
                cur=stack[-1]
                if targeted and cur["type"] in PARTS:
                    weapon=stack[3]["name"]
                    pos,rotation=source_cframe(cur["props"])
                    index=len(parts[weapon])
                    ancestry=tuple(x["name"] for x in stack[4:-1]
                                   if x["type"]=="Model")
                    parts[weapon].append((cur["name"],pos,rotation,ancestry))
                    if ancestry:
                        for depth in range(1,len(ancestry)+1):
                            key=ancestry[:depth]
                            group=groups[weapon].setdefault(key,
                                {"indices":[],"handle":None})
                            group["indices"].append(index)
                        if cur["name"]=="Handle":
                            group=groups[weapon][ancestry]
                            if group["handle"] is not None:
                                group["ambiguous"]=True
                            else:group["handle"]=(pos,rotation)
                stack.pop()
                el.clear()
                while el.getprevious() is not None:
                    del el.getparent()[0]
    return parts,groups

def create(archive:Path,out:Path,synthetic=False,member=MEMBER):
    allparts,allgroups=read_original(archive,member)
    model_count=len(allparts)
    total=sum(map(len,allparts.values()))
    if synthetic:
        if not 1<=model_count<=5 or total>60:
            raise ValueError("Unexpected synthetic tool inventory")
    elif model_count!=98 or total!=1484:
        raise ValueError(f"Original inventory mismatch {model_count}/{total}")
    models={}
    source_groups=0
    for weapon,records in sorted(allparts.items()):
        # Match Pass47's first *any-depth* Handle, not a newly guessed pivot.
        grip=next((p[1] for p in records if p[0]=="Handle"),None)
        if grip is None:
            grip=[sum(p[1][i] for p in records)/len(records)
                  for i in range(3)]
        found=[]
        for path,group in sorted(allgroups[weapon].items()):
            # Missing/duplicate Handles cannot prove a reliable pivot.
            if group["handle"] is None or group.get("ambiguous"):
                continue
            world_pos,rotation=group["handle"]
            indices=group["indices"]
            if len(indices)>200 or not indices:
                raise ValueError("Oversized source group")
            translation=[round(world_pos[i]-grip[i],7) for i in range(3)]
            if any(not math.isfinite(v) or abs(v)>100
                   for v in translation):
                raise ValueError("Unsafe authored group transform")
            found.append({"path":list(path),"part_indices":indices,
                          "pivot_t":translation,"pivot_r":rotation})
            source_groups+=1
        if found:
            models[weapon]={"part_count":len(records),"groups":found}
    if not synthetic:
        for old,new in ALIASES.items():
            if old not in models:continue
            if new in models:raise ValueError("Duplicate source alias")
            models[new]=models[old]
    digest=hashlib.sha256()
    with ZipFile(archive) as z,z.open(member) as stream:
        for block in iter(lambda:stream.read(1<<20),b""):
            digest.update(block)
    original_hash=digest.hexdigest()
    if not synthetic and original_hash!=SOURCE_SHA:
        raise ValueError("Unknown original source rbxlx SHA")
    payload={"format":"twr-source-weapon-hierarchy62-v1",
             "synthetic":synthetic,
             "source":"ReplicatedStorage/Models/Tools",
             "original_rbxlx_sha256":original_hash,
             "source_assembly_sha256":ASSEMBLY_SHA if not synthetic else "synthetic",
             "source_model_count":model_count,"original_part_count":total,
             "models":models}
    encoded=json.dumps(payload,sort_keys=True,separators=(",",":"),
        ensure_ascii=False).encode()
    packed=gzip.compress(encoded,mtime=0,compresslevel=6)
    if len(packed)>512*1024:
        raise ValueError("Hierarchy pack exceeds runtime size limit")
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_bytes(packed)
    manifest={"format":payload["format"],"synthetic":synthetic,
              "source_file_sha256":original_hash,
              "assembly_pack_sha256":payload["source_assembly_sha256"],
              "hierarchy_pack_sha256":hashlib.sha256(packed).hexdigest(),
              "source_models":model_count,"source_parts":total,
              "models_with_moving_groups":len(models),
              "original_nested_groups":source_groups,
              "source_mesh_triangle_bytes_recovered":False,
              "motor6d_joint_c0c1_data_recovered":False,
              "group_handle_cframes_recovered":True}
    (out.parent/"SOURCE_WEAPON_HIERARCHY_MANIFEST62.json").write_text(
        json.dumps(manifest,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    return manifest

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--archive",type=Path,required=True)
    p.add_argument("--out",type=Path,required=True)
    p.add_argument("--member",default=MEMBER)
    p.add_argument("--synthetic",action="store_true")
    args=p.parse_args()
    print(json.dumps(create(args.archive,args.out,args.synthetic,args.member),
                     indent=2,sort_keys=True))

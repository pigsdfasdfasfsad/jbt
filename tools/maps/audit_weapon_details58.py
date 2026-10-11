#!/usr/bin/env python3
"""Pass58 private owner tool-geometry audit. Outputs metadata only, not meshes.

Reads previously SHA-verified compressed original source tool assemblies.
Counts recognizable silhouette-reconstruction opportunities; these are NOT
recovered original MeshPart triangle meshes. No network and no Luau execution.
"""
from __future__ import annotations

import argparse
import collections
import gzip
import hashlib
import json
import math
from pathlib import Path

EXPECTED_SHA="fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7"
TOOL_SOURCE_SHA="272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"
MAGAZINES={"magazine","mag","pistolmagazine"}
STOCKS={"stock","buttstock","stockpad","buttpad"}
GRIPS={"pistolgrip","gripmodel","foregrip","handgrip"}
GUARDS={"triggerguard","guardtrigger"}
CHASSIS={"body","receiver","upperreceiver","lowerreceiver","slide",
         "bolt","handguard","handguardupper","handguardlower","pump",
         "rails","picatinnyrail"}
LENSES={"reticleglass","opticglass","reflectiveglass","glass","scopelens","opticallens"}
TUBES={"barrel","forebarrel","midbarrel","barrelextra","muzzle",
       "nozzle","tube","magtube","gastube","airtube","fueltube",
       "cleaningrod","suppressor","silencer","pipe","rod","cylinder",
       "scopetube"}


def normalized_name(s):
    return "".join(c.lower() for c in s if c.isascii() and c.isalnum())


def recognized(size,name,kind):
    if kind!="MeshPart" or len(size)!=3 or not all(
        isinstance(v,(float,int)) and math.isfinite(v) and 0<v<1000
        for v in size
    ):
        return "unclassified_bounds"
    n=normalized_name(name)
    x,y,z=size
    dims=[x,y,z]
    thin=min(dims)
    a,b=sorted(dims)[1:]
    if n in LENSES and thin<=a*.7 and a>=b*.60:
        return "optic_lens_proxy"
    thin_x=x<=max(y,z)*.72
    if thin_x and n in MAGAZINES:return "magazine_profile"
    if thin_x and n in STOCKS:return "stock_profile"
    if thin_x and n in GRIPS:return "grip_profile"
    if thin_x and n in GUARDS:return "trigger_guard_profile"
    if z>=max(x,y)*.75 and n in CHASSIS:return "chassis_profile"
    major=max(dims)
    cross=sorted(dims)[:2]
    if n in TUBES and major>=max(cross)*1.6 and (
        min(cross)>=max(cross)*.58
    ):
        return "circular_tube"
    return "unclassified_bounds"


def run(pack_path:Path,out_path:Path):
    compressed=pack_path.read_bytes()
    sha=hashlib.sha256(compressed).hexdigest()
    if sha!=EXPECTED_SHA:
        raise ValueError("Owner tool pack digest does not match verified source")
    if len(compressed)>1024*1024:
        raise ValueError("Source tool pack too large")
    raw=gzip.decompress(compressed)
    if len(raw)>8*1024*1024:raise ValueError("Unbounded uncompressed source pack")
    data=json.loads(raw)
    if(data.get("synthetic") is not False or
       data.get("format")!="twr-original-weapon-assemblies-v1" or
       data.get("original_rbxlx_sha256")!=TOOL_SOURCE_SHA or
       data.get("source_model_count")!=98):
        raise ValueError("Source pack is not the expected authenticated owner metadata")
    aliases=data.get("aliases",{})
    if len(aliases)!=2 or len(data["models"])!=100:
        raise ValueError("Expected 98 original tool models plus two alias names")
    names=sorted(set(data["models"])-set(aliases))
    if len(names)!=98:
        raise ValueError("Alias/source tool names unexpectedly changed")
    totals=collections.Counter()
    mesh_ids=set()
    detail_by_name=collections.Counter()
    per_weapon=[]
    for name in names:
        rec=collections.Counter()
        parts=data["models"][name]["parts"]
        for part in parts:
            rec["total_parts"]+=1
            kind=part.get("class","")
            rec[kind]+=1
            mesh_id=part.get("meshId","")
            if mesh_id:
                mesh_ids.add(mesh_id)
                rec["referenced_mesh_parts"]+=1
            alpha=part.get("opacity",1)
            if not isinstance(alpha,(float,int)) or not math.isfinite(alpha):
                raise ValueError("Invalid source part opacity")
            if alpha<=.001:
                rec["invisible_helpers"]+=1
                continue
            rec["visible_parts"]+=1
            if kind=="MeshPart":
                classification=recognized(part.get("s"),part.get("name",""),kind)
                rec[classification]+=1
                if classification!="unclassified_bounds":
                    detail_by_name[part.get("name","")]+=1
            else:
                rec["native_or_union_parts"]+=1
        totals.update(rec)
        per_weapon.append({"name":name,"visible_parts":rec["visible_parts"],
            "original_mesh_id_parts":rec["referenced_mesh_parts"],
            "source_guided_profiles":sum(rec[k] for k in (
                "magazine_profile","stock_profile","grip_profile",
                "trigger_guard_profile","chassis_profile","optic_lens_proxy",
                "circular_tube")),
            "remaining_box_meshparts":rec["unclassified_bounds"],
            "original_triangle_meshes_recovered":0})
    if totals["total_parts"]!=1484 or len(mesh_ids)!=1137:
        raise ValueError("Owner geometry source counts differ from frozen audit")
    output={
        "format":"twr-pass58-source-geometry-opportunity-v1",
        "source_zip_rbxlx_sha256":TOOL_SOURCE_SHA,
        "authenticated_assembly_pack_sha256":EXPECTED_SHA,
        "original_models":98,
        "distinct_mesh_asset_ids":len(mesh_ids),
        "original_mesh_triangle_bytes_recovered":0,
        "source_counts":dict(sorted(totals.items())),
        "most_common_reconstructed_part_names":detail_by_name.most_common(35),
        "weapons":sorted(per_weapon,key=lambda p:(
            -p["remaining_box_meshparts"],p["name"])),
        "interpretation":"Source-guided procedural projections only. Recovered author position, "
          "orientation and bounds; NO original remote triangles; no visual 1:1 certificate."
    }
    out_path.parent.mkdir(parents=True,exist_ok=True)
    with out_path.open("x",encoding="utf8") as f:
        json.dump(output,f,indent=2,sort_keys=True)
        f.write("\n")
    print(json.dumps({"source_models":98,"source_parts":totals["total_parts"],
        "unique_mesh_ids":len(mesh_ids),
        "reconstruction_opportunities":sum(totals[k] for k in (
            "magazine_profile","stock_profile","grip_profile",
            "trigger_guard_profile","chassis_profile","optic_lens_proxy",
            "circular_tube")),
        "source_report":str(out_path)},sort_keys=True))


if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--source-pack",type=Path,required=True)
    p.add_argument("--out",type=Path,required=True)
    args=p.parse_args()
    run(args.source_pack,args.out)

#!/usr/bin/env python3
"""Validate private v2 original map packs without publishing their contents."""
from __future__ import annotations
import argparse
import collections
import gzip
import json
import math
from pathlib import Path

MAPS=("Ranch","Mill","Bypass","Cabin","Cargo","District",
      "Expressway","Prison","Laboratory","Manor")


def verify_scene(file: Path, expected_map: str) -> dict:
    counts=collections.Counter()
    spawns=collections.Counter()
    pickups=collections.Counter()
    source_collidable=0
    with gzip.open(file,"rt",encoding="utf8") as stream:
        header=json.loads(next(stream))
        if header.get("format")!="twr-source-map-v2" or header.get("map")!=expected_map:
            raise ValueError(f"wrong source format/map for {file}")
        if header.get("synthetic"):
            raise ValueError(f"synthetic fixture cannot be used as private original map: {file}")
        if abs(float(header.get("scale",-1))-0.28)>0.00001:
            raise ValueError(f"wrong source units: {file}")
        for index,line in enumerate(stream,2):
            row=json.loads(line)
            kind=row.get("kind")
            if kind not in ("geometry","collision","light","spawn","pickup_spawn"):
                raise ValueError(f"unknown source record {kind!r} at line {index}")
            counts[kind]+=1
            if kind in ("geometry","collision","light","spawn","pickup_spawn"):
                position=row.get("t")
                if not isinstance(position,list) or len(position)!=3 or not all(
                    isinstance(v,(int,float)) and math.isfinite(v) for v in position
                ):
                    raise ValueError(f"bad transform at line {index}")
            if kind in ("geometry","collision"):
                dims=row.get("s")
                rot=row.get("r")
                if not isinstance(dims,list) or len(dims)!=3 or not all(
                    isinstance(v,(int,float)) and .001<=v<1e7 for v in dims
                ):
                    raise ValueError(f"bad size at line {index}")
                if not isinstance(rot,list) or len(rot)!=9 or not all(
                    isinstance(v,(int,float)) and math.isfinite(v) for v in rot
                ):
                    raise ValueError(f"bad rotation at line {index}")
                if kind=="geometry":
                    source_collidable+=bool(row.get("collidable"))
                    rgb=row.get("rgb")
                    if not isinstance(rgb,list) or len(rgb)!=3 or not all(
                        isinstance(v,int) and 0<=v<=255 for v in rgb
                    ):
                        raise ValueError(f"bad color at line {index}")
            if kind=="spawn":spawns[row.get("side")]+=1
            if kind=="pickup_spawn":pickups[row.get("group")]+=1

    expected=header.get("counts",{})
    for key,actual in (
        ("geometry",counts["geometry"]),
        ("collision",counts["collision"]),
        ("lights",counts["light"]),
        ("infected_spawns",spawns["infected"]),
        ("player_spawns",spawns["player"]),
        ("item_markers",pickups["Item"]),
        ("fortification_markers",pickups["Fortification"])
    ):
        if expected.get(key)!=actual:raise ValueError(f"{file}: {key} {actual} != {expected.get(key)}")
    if counts["geometry"]<10 or spawns["player"]<1 or spawns["infected"]<1:
        raise ValueError(f"missing original scene or spawn group: {file}")
    return {
        "map":expected_map,"original_parts":counts["geometry"],
        "collidable_render_parts":source_collidable,
        "server_collision_shapes":counts["collision"],
        "original_lights":counts["light"],
        "original_spawn_groups":dict(spawns),
        "original_pickup_groups":dict(pickups),
        "has_source_lighting":"lighting" in header,
        "requires_external_original_meshes":True
    }


def verify_folder(folder: Path) -> list:
    return [verify_scene(folder/(name+".scene.jsonl.gz"),name) for name in MAPS]


def main():
    cli=argparse.ArgumentParser()
    cli.add_argument("--directory",required=True,type=Path)
    cli.add_argument("--report",type=Path)
    args=cli.parse_args()
    report=verify_folder(args.directory)
    for r in report:
        print("TWR_SOURCE_MAP_VERIFIED "+r["map"]+" parts="+str(r["original_parts"]) +
              " solid="+str(r["collidable_render_parts"]))
    if args.report:
        args.report.parent.mkdir(parents=True,exist_ok=True)
        args.report.write_text(json.dumps(report,indent=2)+"\n",encoding="utf8")
    print("TWR_SOURCE_MAPS_ALL_VALID maps="+str(len(report)))


if __name__=="__main__":
    main()

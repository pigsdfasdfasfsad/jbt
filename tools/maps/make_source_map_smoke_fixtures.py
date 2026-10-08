#!/usr/bin/env python3
"""Generate minimal, safe 10-map fixtures for exported-Windows source-loader tests.

No original Roblox positions, IDs, meshes or restricted geometry are included.
Each fixture uses the same v2 schema as the private source-positioned exports.
"""
import argparse
import gzip
import json
from pathlib import Path

MAPS = ("Ranch", "Mill", "Bypass", "Cabin", "Cargo", "District",
        "Expressway", "Prison", "Laboratory", "Manor")
I = [1,0,0,0,1,0,0,0,1]

def create(folder):
    folder.mkdir(parents=True, exist_ok=True)
    for name in MAPS:
        path = folder / (name + ".scene.jsonl.gz")
        with path.open("wb") as output, gzip.GzipFile(
            filename="", mode="wb", fileobj=output, mtime=0, compresslevel=6
        ) as zipped:
            def write(rec):
                zipped.write((json.dumps(rec,sort_keys=True,separators=(",",":"))+"\n").encode())
            counts={"geometry":60,"collision":3,"lights":2,
                    "infected_spawns":2,"player_spawns":2,
                    "item_markers":5,"fortification_markers":3}
            write({"kind":"header","format":"twr-source-map-v2","map":name,
                   "scale":0.28,"synthetic":True,"source_member":"CI SYNTHETIC - NOT ROBLOX",
                   "counts":counts})
            for i in range(60):
                write({"kind":"geometry","ref":f"testgeo{i}","name":"Synthetic",
                       "class":"WedgePart" if i == 1 else "Part",
                       "t":[i % 10 * 4,0,i // 10 * 4],"r":I,"s":[3,1,3],
                       "shape":"1","rgb":[145,135,110],"mat":"272",
                       "opacity":1,"shadow":False,"collidable":i<10})
            for i in range(3):
                write({"kind":"collision","ref":f"wall{i}","class":"Part",
                       "t":[i*4,0,25],"r":I,"s":[2,2,2]})
            for i in range(2):
                write({"kind":"light","class":"SpotLight" if i else "PointLight",
                       "ref":f"light{i}","t":[i*4,6,0],"r":I,
                       "rgb":[230,220,190],"energy":1,"range":16,
                       "angle":70,"face":5})
            for side in ("infected","player"):
                for i in range(2):
                    write({"kind":"spawn","side":side,"name":side+str(i),
                           "t":[i*5,2,12 if side == "infected" else 0]})
            for group,n in (("Item",5),("Fortification",3)):
                for i in range(n):
                    write({"kind":"pickup_spawn","group":group,"ref":group+str(i),
                           "t":[i*4,1,8 if group == "Item" else 4]})
    print("TWR_SYNTHETIC_SOURCE_MAPS_OK maps=10")

if __name__ == "__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--output-dir",type=Path,required=True)
    create(p.parse_args().output_dir)

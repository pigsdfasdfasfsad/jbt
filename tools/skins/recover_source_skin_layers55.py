#!/usr/bin/env python3
"""Build owner-private inert source skin texture-reference metadata for Pass55.

No network access, executable Luau, texture downloads, or overwrites. Source
archive SHA is pinned; output is a bounded, deterministic gzip sidecar.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import math
import re
from pathlib import Path
from zipfile import ZipFile

SOURCE_MEMBER = "scripts/ModuleScript.Skins.Source.txt"
SOURCE_SHA256 = "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015"
FORMAT = "twr-source-skin-texture-layers-v1"
TIERS = re.compile(r"^\t\t([A-Za-z]+) = \{$", re.M)
SKINS = re.compile(r'^\t\t\t(?:\["([^"]+)"\]|([A-Za-z][\w]*)) = \{$', re.M)
VALUES = re.compile(r"^\t{4}Value([1-5]) = \{$", re.M)
LAYERS = re.compile(r"^\t{5}(Texture1?) = \{\n(.*?)^\t{5}\};", re.M | re.S)
ASSET = re.compile(r'^\s*"rbxassetid://(\d{1,20})"\s*$')
NUMBER = r"([-+]?(?:\d+(?:\.\d+)?|\.\d+)(?:[eE][-+]?\d+)?)"


def source_field(chunk: str, key: str, indent: int = 5) -> str | None:
    found = re.search(r"^\t{" + str(indent) + r"}" + re.escape(key)
                      + r"\s*=\s*([^;]+);", chunk, re.M)
    return found.group(1).strip() if found else None


def asset_id(value: str | None) -> str:
    if value is None: return ""
    found = ASSET.fullmatch(value)
    if not found: raise ValueError("Unrecognized source asset reference syntax")
    return found.group(1)


def number(value: str | None, default: float, lo: float, hi: float) -> float:
    if value is None: return default
    if re.fullmatch(NUMBER, value) is None:
        raise ValueError("Source parameter is not a bounded numeric literal")
    parsed = float(value)
    if not math.isfinite(parsed) or not lo <= parsed <= hi:
        raise ValueError("Unbounded original source layer scalar")
    return round(parsed, 7)


def rgb(value: str | None) -> list[int]:
    if value is None: return [255,255,255]
    hit = re.fullmatch(r"Color3\.(new|fromRGB)\(([^)]+)\)",value)
    if not hit: raise ValueError("Unexpected nested Color3 syntax")
    numbers = [number(x.strip(), 0, 0, 255) for x in hit.group(2).split(",")]
    if len(numbers) != 3: raise ValueError("Source overlay Color3 requires RGB")
    result = [round(x if hit.group(1) == "fromRGB" else x * 255) for x in numbers]
    if any(not 0 <= x <= 255 for x in result):
        raise ValueError("Source overlay color outside RGB")
    return result


def make_layer(body: str) -> dict:
    texture=asset_id(source_field(body,"Texture",6))
    if not texture: raise ValueError("Source texture object has no asset ID")
    out={"textureId":texture,
         "rgb":rgb(source_field(body,"Color3",6)),
         "transparency":number(source_field(body,"Transparency",6),0,0,1),
         "tileU":number(source_field(body,"StudsPerTileU",6),1,.05,64),
         "tileV":number(source_field(body,"StudsPerTileV",6),1,.05,64),
         "offsetU":number(source_field(body,"OffsetStudsU",6),0,-32,32),
         "offsetV":number(source_field(body,"OffsetStudsV",6),0,-32,32),
         "zIndex":int(number(source_field(body,"ZIndex",6),0,-100,100))}
    return out


def extract(archive: Path) -> dict:
    with ZipFile(archive) as z: raw=z.read(SOURCE_MEMBER)
    if hashlib.sha256(raw).hexdigest()!=SOURCE_SHA256:
        raise ValueError("Original source skin SHA-256 mismatch")
    text=raw.decode("utf-8-sig").replace("\r","")
    parsed={}
    for tier in TIERS.finditer(text):
        next_tier=TIERS.search(text,tier.end())
        tier_source=text[tier.end():next_tier.start() if next_tier else len(text)]
        for skin in SKINS.finditer(tier_source):
            name=skin.group(1) or skin.group(2)
            next_skin=SKINS.search(tier_source,skin.end())
            chunk=tier_source[skin.end():next_skin.start() if next_skin else len(tier_source)]
            image=asset_id(source_field(chunk,"Image",4))
            slots={}
            for value in VALUES.finditer(chunk):
                next_value=VALUES.search(chunk,value.end())
                body=chunk[value.end():next_value.start() if next_value else len(chunk)]
                layers=[]
                for layer in LAYERS.finditer(body):
                    entry=make_layer(layer.group(2));entry["kind"]=layer.group(1)
                    layers.append(entry)
                if len(layers)>2 or len({x["kind"] for x in layers})!=len(layers):
                    raise ValueError("Unexpected source layer multiplicity")
                slots[int(value.group(1))]={
                    "meshTextureId":asset_id(source_field(body,"TextureID",5)),
                    "layers":layers
                }
            if set(slots)!={1,2,3,4,5}:
                raise ValueError("Original skin missing a Value1-Value5 slot")
            key=tier.group(1)+"/"+name
            if key in parsed:raise ValueError("Duplicate source skin "+key)
            parsed[key]={"imageId":image,"slots":[slots[i] for i in range(1,6)]}
    if len(parsed)!=118: raise ValueError("Unexpected source skin count")
    num_layers=sum(len(slot["layers"]) for skin in parsed.values() for slot in skin["slots"])
    num_mesh=sum(bool(slot["meshTextureId"]) for skin in parsed.values() for slot in skin["slots"])
    if num_layers!=94 or num_mesh!=33:
        raise ValueError(f"Source texture reference count changed: {num_layers}/{num_mesh}")
    return {"format":FORMAT,"synthetic":False,"source_sha256":SOURCE_SHA256,
            "skin_count":118,"layer_count":num_layers,"mesh_texture_count":num_mesh,
            "skins":parsed}


def main() -> None:
    p=argparse.ArgumentParser()
    p.add_argument("--scripts-archive",required=True,type=Path)
    p.add_argument("--output",required=True,type=Path)
    args=p.parse_args()
    pack=extract(args.scripts_archive)
    data=json.dumps(pack,sort_keys=True,separators=(",",":"),ensure_ascii=False).encode()
    payload=gzip.compress(data,mtime=0,compresslevel=9)
    if len(data)>256*1024 or len(payload)>96*1024:
        raise ValueError("Original texture reference catalog exceeds size budget")
    args.output.parent.mkdir(parents=True,exist_ok=True)
    with args.output.open("xb") as f: f.write(payload)
    print(json.dumps({"skins":118,"texture_layers":94,"mesh_texture_ids":33,
                      "compressed_sha256":hashlib.sha256(payload).hexdigest(),
                      "size":len(payload)},sort_keys=True))

if __name__=="__main__":main()

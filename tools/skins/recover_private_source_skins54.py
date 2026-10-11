#!/usr/bin/env python3
"""Pass54: build PRIVATE source skin palette and source weapon-part slot maps.

Reads inert bytes from the user's scripts.zip and TestPlace.zip, does not
execute Luau or Roblox. The generated .json.gz packs are owner-side assets:
DO NOT commit the generated packs, private archives or full source text.
"""
from __future__ import annotations

import argparse
from collections import Counter
import gzip
import hashlib
import json
from pathlib import Path
import re
from zipfile import ZipFile

from lxml import etree

SKINS_MEMBER = "scripts/ModuleScript.Skins.Source.txt"
PLACE_MEMBER = "TestPlace/TestPlace.rbxlx"
SKINS_SHA = "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015"
PLACE_SHA = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"
PARTS = {"Part", "MeshPart", "WedgePart", "CornerWedgePart", "UnionOperation"}
SOURCE = ("ReplicatedStorage", "Models", "Tools")
TIER = re.compile(r"^\t\t([A-Za-z]+) = \{$", re.M)
SKIN = re.compile(r'^\t\t\t(?:\["([^"]+)"\]|([A-Za-z][A-Za-z0-9_]*)) = \{$', re.M)
VALUE = re.compile(r"^\t\t\t\tValue([1-5]) = \{$", re.M)


def compressed_json(value: dict, path: Path) -> str:
    data = json.dumps(value, separators=(",", ":"), ensure_ascii=False,
                      sort_keys=True).encode("utf-8")
    packed = gzip.compress(data, compresslevel=9, mtime=0)
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        raise FileExistsError("Refusing to overwrite existing private pack: " + str(path))
    path.write_bytes(packed)
    return hashlib.sha256(packed).hexdigest()


def palette(archive: Path, target: Path) -> tuple[str,int,int]:
    with ZipFile(archive) as z:
        raw = z.read(SKINS_MEMBER)
    if hashlib.sha256(raw).hexdigest() != SKINS_SHA:
        raise ValueError("Unrecognized source skins archive; refusing to infer colors")
    source = raw.decode("utf-8-sig").replace("\r", "")
    parsed = {}
    for tier in TIER.finditer(source):
        next_tier = TIER.search(source, tier.end())
        block = source[tier.end():next_tier.start() if next_tier else len(source)]
        for skin in SKIN.finditer(block):
            name = skin.group(1) or skin.group(2)
            nxt = SKIN.search(block, skin.end())
            text = block[skin.end():nxt.start() if nxt else len(block)]
            slots = {}
            for value in VALUE.finditer(text):
                next_value = VALUE.search(text, value.end())
                body = text[value.end():next_value.start() if next_value else len(text)]
                color = re.search(
                    r'^\t{5}Color\s*=\s*Color3\.(new|fromRGB)\(([^)]+)\)\s*;',
                    body, re.M)
                material = re.search(
                    r'^\t{5}Material\s*=\s*(?:Enum\.Material\.)?"?([A-Za-z0-9_]+)"?\s*;',
                    body, re.M)
                if color is None: raise ValueError("Missing source color: " + tier[1] + "/" + name)
                nums = [float(x.strip()) for x in color[2].split(",")]
                if len(nums)!=3 or not all(0<=x<256 for x in nums):
                    raise ValueError("Unbounded source color")
                rgb = [int(round(x if color[1]=="fromRGB" else x*255)) for x in nums]
                if not all(0<=x<=255 for x in rgb):
                    raise ValueError("Source RGB outside 0-255")
                def field(key):
                    return re.search(r'^\t{5}'+key+r'\s*=\s*([^;]+);', body, re.M)
                alpha, reflect = field("Transparency"), field("Reflectance")
                variant, surface = field("MaterialVariant"), field("SurfaceAppearance")
                decal = bool(field("(?:Texture|Texture1|TextureID)"))
                properties = {"rgb":rgb, "material":material[1] if material else ""}
                if alpha: properties["transparency"]=float(alpha[1])
                if reflect: properties["reflectance"]=float(reflect[1])
                if variant: properties["variant"]=variant[1].strip('"')
                if surface: properties["surface"]=surface[1].strip('"')
                if decal: properties["textureUnavailable"]=True
                slots[int(value[1])]=properties
            if set(slots)!={1,2,3,4,5}:
                raise ValueError("Unexpected Value1-Value5 skin assignment")
            key=tier[1]+"/"+name
            if key in parsed: raise ValueError("Duplicated source skin")
            parsed[key]=[slots[i] for i in range(1,6)]
    if len(parsed)!=118: raise ValueError("Unexpected source skin count")
    output={"format":"twr-source-skin-material-v1","source_sha256":SKINS_SHA,
            "source_entries":118,"values_per_skin":5,"skins":parsed}
    return compressed_json(output,target),len(parsed),sum(len(v) for v in parsed.values())


def slot_map(archive: Path, target: Path) -> tuple[str,int,int]:
    # Validate original content SHA by streaming before parsing untrusted XML.
    with ZipFile(archive) as z, z.open(PLACE_MEMBER) as stream:
        digest=hashlib.sha256()
        while part:=stream.read(1<<20):digest.update(part)
    if digest.hexdigest()!=PLACE_SHA: raise ValueError("Unexpected original source place hash")
    stack=[]; models={}; parts=Counter()
    with ZipFile(archive) as z, z.open(PLACE_MEMBER) as stream:
        for event,el in etree.iterparse(stream,events=("start","end"),
                                        tag=("Item","Properties"),huge_tree=True):
            if event=="start" and el.tag=="Item":
                stack.append({"class":el.get("class"),"name":"","tags":[]})
            elif event=="end" and el.tag=="Properties" and stack:
                found=el.find('./string[@name="Name"]')
                stack[-1]["name"]=found.text if found is not None and found.text else ""
                el.clear()
            elif event=="end" and el.tag=="Item":
                item=stack[-1]
                target_model=(len(stack)>=4 and
                    tuple(x["name"] for x in stack[:3])==SOURCE)
                if (target_model and item["class"]=="BoolValue"
                    and re.fullmatch("Value[1-5]",item["name"])):
                    for parent in reversed(stack[:-1]):
                        if parent["class"] in PARTS:
                            parent["tags"].append(int(item["name"][-1]))
                            break
                if target_model and item["class"] in PARTS:
                    weapon=stack[3]["name"]
                    position=parts[weapon]
                    parts[weapon]+=1
                    # Repeated equal slot tags occur in two M110 SASS scopes.
                    if len(set(item["tags"]))>1:
                        raise ValueError("Conflicting skin slot tags on a source part")
                    if item["tags"]:
                        models.setdefault(weapon,{})[str(position)]=item["tags"][0]
                    else: models.setdefault(weapon,{})
                if target_model and len(stack)==4 and item["class"]=="Model":
                    models.setdefault(item["name"],{})
                stack.pop()
                el.clear()
                while el.getprevious() is not None:del el.getparent()[0]
    tagged=sum(len(m) for m in models.values())
    if len(models)!=98 or sum(parts.values())!=1484 or tagged!=728:
        raise ValueError("Original tool part/skin tag counts changed")
    output={"format":"twr-source-skin-slot-map-v1","synthetic":False,
            "source_rbxlx_sha256":PLACE_SHA,"original_models":98,
            "original_parts":1484,"tagged_parts":tagged,"models":models}
    return compressed_json(output,target),len(models),tagged


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--skins-archive",required=True,type=Path)
    ap.add_argument("--place-archive",required=True,type=Path)
    ap.add_argument("--out",required=True,type=Path)
    opts=ap.parse_args()
    a=palette(opts.skins_archive,opts.out/"SourceSkinMaterials54.json.gz")
    b=slot_map(opts.place_archive,opts.out/"SourceWeaponSkinSlots54.json.gz")
    print(json.dumps({"palette_pack_sha256":a[0],"skins":a[1],"material_values":a[2],
                      "skin_slot_pack_sha256":b[0],"source_tool_models":b[1],
                      "tagged_tool_parts":b[2]},sort_keys=True))


if __name__=="__main__":
    main()

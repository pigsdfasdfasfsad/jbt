#!/usr/bin/env python3
"""Export owner-authored CMaps lighting effects as deterministic, private sidecars.
No skybox image bytes are included. Effect data is not a complete Roblox renderer.
"""
import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

MAPS = ("Ranch","Mill","Bypass","Cabin","Cargo","District","Expressway","Prison","Laboratory","Manor")
KINDS = {"Sky":"sky","Atmosphere":"atmosphere","BloomEffect":"bloom",
         "ColorCorrectionEffect":"color_correction","SunRaysEffect":"sun_rays"}
FIELDS = {
    "sky":("StarCount","SunAngularSize"),
    "atmosphere":("Density","Offset","Glare","Haze","Color","Decay"),
    "bloom":("Intensity","Size","Threshold","Enabled"),
    "color_correction":("Brightness","Contrast","Saturation","TintColor","Enabled"),
    "sun_rays":("Intensity","Spread","Enabled")
}

def name(item):
    props=item.find("Properties")
    return next((x.text for x in props if x.get("name")=="Name"),"?") if props is not None else "?"

def path(item):
    parts=[]
    while item is not None:
        if item.tag=="Item": parts.append(name(item))
        item=item.getparent()
    return list(reversed(parts))

def effect(item,kind):
    properties={p.get("name"):p for p in item.find("Properties")}
    result={"kind":kind}
    for field in FIELDS[kind]:
        p=properties[field]
        if p.tag=="Color3":
            result[field.lower()]=[float(p.find(c).text) for c in ("R","G","B")]
        elif p.tag=="bool":
            result[field.lower()]=p.text.lower()=="true"
        else:
            result[field.lower()]=float(p.text)
    return result

def recover(archive,out):
    out=Path(out);out.mkdir(parents=True,exist_ok=True)
    rows={m:[] for m in MAPS}
    with ZipFile(archive) as z:
        member="TestPlace/TestPlace.rbxlx"
        if z.getinfo(member).file_size>500_000_000:
            raise ValueError("Source XML exceeds bounded input size")
        with z.open(member) as stream:
            owner_sha=hashlib.file_digest(stream,"sha256").hexdigest()
        with z.open(member) as stream:
            for _,item in etree.iterparse(stream,events=("end",),tag="Item",huge_tree=True,
                                            resolve_entities=False,load_dtd=False,no_network=True):
                kind=KINDS.get(item.get("class"))
                if kind:
                    segments=path(item)
                    if len(segments)>=5 and segments[:2]==["ReplicatedStorage","CMaps"] and segments[2] in MAPS and segments[3]=="Lighting Effects":
                        rows[segments[2]].append(effect(item,kind))
                item.clear()
                parent=item.getparent()
                while item.getprevious() is not None and item.getprevious().tag=="Item":
                    parent.remove(item.getprevious())
    manifest={"format":"twr-source-lighting37-manifest-v1","owner_rbxlx_sha256":owner_sha,"maps":{}}
    for map_name in MAPS:
        effects=rows[map_name]
        if sum(e["kind"]=="sky" for e in effects)!=1 or not any(e["kind"]=="color_correction" for e in effects):
            raise ValueError("Missing original source lighting essentials for "+map_name)
        data={"format":"twr-source-lighting37-v1","map":map_name,
              "owner_rbxlx_sha256":owner_sha,
              "source_path":f"ReplicatedStorage/CMaps/{map_name}/Lighting Effects",
              "skybox_images_present":False,"effects":effects}
        serialized=(json.dumps(data,sort_keys=True,separators=(",",":"))+"\n").encode()
        filename=map_name+".lighting37.json"
        (out/filename).write_bytes(serialized)
        manifest["maps"][map_name]={"effects":len(effects),"file":filename,
                                     "sha256":hashlib.sha256(serialized).hexdigest()}
    (out/"SOURCE_LIGHTING_MANIFEST37.json").write_text(
        json.dumps(manifest,sort_keys=True,indent=2)+"\n",encoding="utf8")
    return manifest

if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("--archive",required=True,type=Path)
    cli.add_argument("--out",required=True,type=Path)
    args=cli.parse_args()
    print(json.dumps(recover(args.archive,args.out),indent=2))

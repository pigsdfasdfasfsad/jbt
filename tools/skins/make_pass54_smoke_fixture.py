#!/usr/bin/env python3
"""Pass54 smoke-only synthetic skin material/weapon tag packs; never touch owner files."""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

FORMAT_PALETTE = "twr-source-skin-material-v1"
FORMAT_TAGS = "twr-source-skin-slot-map-v1"
COLORS = {
    "Low/Cocoa": ([40,17,17],[61,26,26],[40,17,17],[40,17,17],[61,26,26]),
    "High/Gold": ([188,155,93],[188,155,93],[159,131,78],[188,155,93],[188,155,93]),
    "Ethereal/Spirit": tuple([255,255,255] for _ in range(5)),
}


def paths(directory: Path) -> tuple[Path, Path]:
    return directory/"SourceSkinMaterials54.json.gz",directory/"SourceWeaponSkinSlots54.json.gz"


def write(path: Path, payload: dict) -> None:
    if path.exists():
        raise FileExistsError("Refusing to overwrite existing private or test pack: "+str(path))
    path.parent.mkdir(parents=True,exist_ok=True)
    raw=json.dumps(payload,separators=(",",":"),sort_keys=True).encode()
    with path.open("wb") as f,gzip.GzipFile(filename="",fileobj=f,mode="wb",
                                            mtime=0,compresslevel=9) as zipped:
        zipped.write(raw)


def check_fixture(path: Path, fmt: str) -> None:
    if not path.exists():raise FileNotFoundError(path)
    with gzip.open(path,"rb") as f:
        data=f.read(128*1024+1)
    if len(data)>128*1024:raise ValueError("Fixture exceeds safe size")
    info=json.loads(data)
    if info.get("synthetic") is not True or info.get("format")!=fmt:
        raise ValueError("Refusing to delete non-synthetic owner source pack")
    expected=set(COLORS) if fmt==FORMAT_PALETTE else {"Glock 17"}
    key="skins" if fmt==FORMAT_PALETTE else "models"
    if set(info.get(key,{}))!=expected:
        raise ValueError("Fixture has unexpected source skin keys")


def main() -> None:
    p=argparse.ArgumentParser()
    p.add_argument("--output-dir",required=True,type=Path)
    action=p.add_mutually_exclusive_group(required=True)
    action.add_argument("--create",action="store_true")
    action.add_argument("--clean",action="store_true")
    args=p.parse_args()
    palette,tags=paths(args.output_dir)
    if args.create:
        if palette.exists() or tags.exists():
            raise FileExistsError("Pass54 refuses to overwrite installed skin packs")
        skins={}
        for skin,colors in COLORS.items():
            material=("ForceField" if skin=="Ethereal/Spirit" else
                      "Marble" if skin=="High/Gold" else "SmoothPlastic")
            skins[skin]=[{"rgb":color,"material":material} for color in colors]
        payload={"format":FORMAT_PALETTE,"synthetic":True,
                 "source_entries":len(skins),"values_per_skin":5,"skins":skins}
        slots={"format":FORMAT_TAGS,"synthetic":True,
               "models":{"Glock 17":{"0":1,"1":2,"2":3,"3":4}}}
        write(palette,payload)
        try:write(tags,slots)
        except Exception:
            check_fixture(palette,FORMAT_PALETTE)
            palette.unlink()
            raise
        print("TWR_PASS54_SYNTHETIC_SKINS_READY palettes=3 material_values=15 part_tags=4")
    else:
        check_fixture(palette,FORMAT_PALETTE)
        check_fixture(tags,FORMAT_TAGS)
        palette.unlink()
        tags.unlink()
        print("TWR_PASS54_SYNTHETIC_SKINS_CLEANED")


if __name__=="__main__":
    main()

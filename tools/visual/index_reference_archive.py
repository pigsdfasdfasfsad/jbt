#!/usr/bin/env python3
"""Index original user-supplied map screenshots without publishing image bytes.

This is reference evidence for future matched-camera comparisons, not a claim
that any image is a render of the standalone Godot game.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import zipfile
from collections import defaultdict
from pathlib import Path, PurePosixPath

MAPS = ("Ranch", "Mill", "Bypass", "Cabin", "Cargo", "District",
        "Expressway", "Prison", "Laboratory", "Manor")
EXTENSIONS = {".png", ".jpg", ".jpeg", ".gif"}

def map_from_filename(path: str) -> str | None:
    stem = PurePosixPath(path).stem.casefold()
    for name in MAPS:
        lower = name.casefold()
        if (stem.startswith(lower) or stem.startswith("card-" + lower)
                or stem.startswith("vote-" + lower)):
            return name
    return None

def index(archive: Path) -> dict:
    grouped = defaultdict(list)
    with zipfile.ZipFile(archive) as source:
        for info in sorted(source.infolist(), key=lambda x: x.filename.casefold()):
            if info.is_dir() or PurePosixPath(info.filename).suffix.casefold() not in EXTENSIONS:
                continue
            map_name = map_from_filename(info.filename)
            if map_name is None:
                continue
            original_bytes = source.read(info)
            grouped[map_name].append({
                "source_member": info.filename,
                "sha256": hashlib.sha256(original_bytes).hexdigest(),
                "bytes": len(original_bytes),
            })
    return {
        "format": "twr-map-image-reference-index-v1",
        "source_archive": archive.name,
        "only_filenames_and_hashes": True,
        "not_gameplay_validation": True,
        "maps": {name: {"count": len(grouped[name]),
                        "images": grouped[name]} for name in MAPS},
    }

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    data = index(args.archive)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    print("TWR_REFERENCE_INDEX_OK " + " ".join(
        f"{name}={data['maps'][name]['count']}" for name in MAPS))

if __name__ == "__main__":
    main()

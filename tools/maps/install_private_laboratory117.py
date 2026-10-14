#!/usr/bin/env python3
"""Install a verified owner-private Laboratory source and collision cache.

No Roblox/Studio/internet required. Never creates source-data files in Git.
Keep the original scene ZIP/coordinates private to the owner's computer.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import json
import pathlib
import struct
import zipfile
from collections import Counter

SCENE_MEMBER = "Content/Maps/Laboratory.scene.jsonl.gz"
COLLISION_MEMBER = "Content/Collision/Laboratory.col26.gz"
NATIVE_MEMBER = "Content/Geometry/Laboratory.native28.gz"
SCENE_SHA256 = "b92efe792c93062a0741724f252949ec13cc828d601e277e21a1b13b1d02bb8c"
COLLISION_SHA256 = "a3067649db39d5f07be6600b642763fd846050c33b913f0d03dbfbed16468727"
NATIVE_SHA256 = "7a7600e8e4bc796faf2d6a178870f695960e346acad153b0d7139c0cc0fa8c01"
XML_SHA256 = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"
EXPECTED = {
    "geometry": 34268, "collision": 1517, "lights": 836,
    "infected_spawns": 15, "player_spawns": 8,
    "item_markers": 127, "fortification_markers": 47,
}


def validate(scene: bytes, collision: bytes, native: bytes | None = None) -> dict:
    if hashlib.sha256(scene).hexdigest() != SCENE_SHA256:
        raise ValueError("Unexpected Laboratory scene: exact verified digest required")
    if hashlib.sha256(collision).hexdigest() != COLLISION_SHA256:
        raise ValueError("Unexpected Laboratory collision cache: exact verified digest required")
    with gzip.open(io.BytesIO(scene), "rt", encoding="utf8") as stream:
        header = json.loads(next(stream))
        if (header.get("format") != "twr-source-map-v2" or
            header.get("map") != "Laboratory" or
            header.get("synthetic", True) or
            header.get("original_xml_sha256") != XML_SHA256 or
            header.get("counts") != EXPECTED):
            raise ValueError("Owner-authenticated original Laboratory source header mismatch")
        records, spawns, pickups = Counter(), Counter(), Counter()
        for line in stream:
            item = json.loads(line)
            records[item["kind"]] += 1
            if item["kind"] == "spawn":
                spawns[item["side"]] += 1
            if item["kind"] == "pickup_spawn":
                pickups[item["group"]] += 1
        if (records["geometry"] != 34268 or records["collision"] != 1517 or
            records["light"] != 836 or spawns["infected"] != 15 or
            spawns["player"] != 8 or pickups["Item"] != 127 or
            pickups["Fortification"] != 47):
            raise ValueError("Original source geometry or marker records incomplete")

    blob = gzip.decompress(collision)
    if len(blob) < 56 or blob[:8] != b"TWRCOL26":
        raise ValueError("Invalid private collision cache")
    version, tiles, shapes, proxies = struct.unpack_from("<IIII", blob, 8)
    if (version, tiles, shapes, proxies) != (1, 180, 18130, 4501):
        raise ValueError("Source collision cache shape counts do not match")
    if blob[24:56] != hashlib.sha256(scene).digest():
        raise ValueError("Collision cache does not match the exact compressed source map")

    offset, triangles, seen = 56, 0, set()
    for _ in range(tiles):
        if offset + 12 > len(blob):
            raise ValueError("Truncated original collision tile")
        x, z, faces = struct.unpack_from("<iiI", blob, offset)
        offset += 12
        if faces < 1 or (x, z) in seen:
            raise ValueError("Bad original collision tile")
        seen.add((x, z))
        triangles += faces
        offset += faces * 36
    if offset != len(blob) or triangles != 217204:
        raise ValueError("Invalid original collision byte length/triangle count")
    # Pass28 streaming native draw batches are already authored in the
    # existing Godot game. The private pack contains 18,212 source Parts /
    # Wedges in 47 tiles / 786 batches, never recovered MeshPart triangles.
    native_instances = 0
    if native is not None:
        if hashlib.sha256(native).hexdigest() != NATIVE_SHA256:
            raise ValueError("Unexpected original Part/Wedge native render pack")
        packed = gzip.decompress(native)
        if packed[:8] != b"TWRINS28" or len(packed) < 56:
            raise ValueError("Invalid TWRINS28 native renderer header")
        native_version, tile_studs, declared = struct.unpack_from("<III", packed, 8)
        draw_tiles = struct.unpack_from("<I", packed, 52)[0]
        if ((native_version, tile_studs, declared, draw_tiles) !=
            (1, 96, 18212, 47) or
            packed[20:52] != hashlib.sha256(scene).digest()):
            raise ValueError("Native source renderer not bound to exact Laboratory scene")
        pos, group_count, used, seen = 56, 0, 0, set()
        for _ in range(draw_tiles):
            if pos + 12 > len(packed):
                raise ValueError("Truncated original native render tile")
            x, z, groups = struct.unpack_from("<iiI", packed, pos)
            pos += 12
            if groups < 1 or (x, z) in seen:
                raise ValueError("Bad original native draw tile")
            seen.add((x, z))
            for _ in range(groups):
                if pos + 12 > len(packed):
                    raise ValueError("Truncated source rendering group")
                kind, material, alpha, r, g, b, shadow, count = struct.unpack_from(
                    "<BHB3BBI", packed, pos
                )
                pos += 12
                if kind > 1 or alpha < 1 or shadow > 1 or count < 1 or count > 20000:
                    raise ValueError("Invalid native draw batch")
                used += count
                group_count += 1
                pos += count * 48
        if pos != len(packed) or used != 18212 or group_count != 786:
            raise ValueError("Native original draw batch/instance count mismatch")
        native_instances = used
    return {
        "native_original_part_instances": native_instances,
        "original_geometry": 34268,
        "collision_shapes": shapes,
        "tiles": tiles,
        "original_enabled_lights": 836,
        "triangles": triangles,
        "approximate_mesh_bounds": proxies,
        "source_texture_pixels_embedded": False,
    }


def install(archive: pathlib.Path, game_folder: pathlib.Path,
            verify_executable: bool = True) -> dict:
    root = game_folder.resolve()
    if verify_executable and not (root / "ThoseWhoRemainOffline.exe").is_file():
        raise ValueError("Destination must contain the compiled Windows executable")
    with zipfile.ZipFile(archive) as zipped:
        available = set(zipped.namelist())
        if (SCENE_MEMBER not in available or COLLISION_MEMBER not in available or
            NATIVE_MEMBER not in available):
            raise ValueError("Private ZIP lacks the original Laboratory scene/collision/native rendering cache")
        scene = zipped.read(SCENE_MEMBER)
        collision = zipped.read(COLLISION_MEMBER)
        native = zipped.read(NATIVE_MEMBER)
    report = validate(scene, collision, native)
    for name, data in ((SCENE_MEMBER, scene), (COLLISION_MEMBER, collision),
                       (NATIVE_MEMBER, native)):
        target = root / pathlib.Path(name)
        target.parent.mkdir(parents=True, exist_ok=True)
        pending = target.with_name(target.name + ".pass117.tmp")
        try:
            pending.write_bytes(data)
            if target.exists():
                backup = target.with_name(target.name + ".before-pass117.bak")
                if backup.exists():
                    backup.unlink()
                target.replace(backup)
            pending.replace(target)
        finally:
            if pending.exists():
                pending.unlink()
    return report


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--private-pack", required=True, type=pathlib.Path)
    parser.add_argument("--game-folder", required=True, type=pathlib.Path)
    arguments = parser.parse_args()
    report = install(arguments.private_pack, arguments.game_folder)
    print("TWR_PASS117_OWNER_SOURCE_INSTALLED " +
          json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()

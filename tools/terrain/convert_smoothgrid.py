#!/usr/bin/env python3
"""Decode original Roblox SmoothGrid v1 and emit compact offline terrain meshes.

Original source positions and material IDs are retained, while adjoining
exposed faces are merged into rectangles. This first renderer is intentionally
blocky, not a recreation of Roblox's interpolated smooth-surface terrain.
Requires numpy for OFFLINE CONVERSION ONLY; the Godot executable does not.
"""
from __future__ import annotations
import argparse
import gzip
import io
import json
import struct
import zipfile
from pathlib import Path

import numpy as np

MAGIC = b"TWRTERR1"
SIZE = 32
CELLS = SIZE ** 3
MAPS = ("Ranch", "Mill", "Bypass", "Cabin", "Cargo", "District",
        "Expressway", "Prison", "Laboratory", "Manor")


def decode_smoothgrid(blob: bytes):
    if len(blob) < 2 or blob[:2] != b"\x01\x05":
        raise ValueError("Only 32-cell SmoothGrid version 1 is supported")
    position = 2
    previous = [0, 0, 0]
    chunks = {}
    while position < len(blob):
        if position + 12 > len(blob):
            raise ValueError("Truncated SmoothGrid chunk coordinates")
        encoded = blob[position:position + 12]
        position += 12
        delta = [int.from_bytes(encoded[a::3], "big", signed=True)
                 for a in range(3)]
        previous = [p + d for p, d in zip(previous, delta)]
        coordinate = tuple(previous)
        if coordinate in chunks or len(chunks) >= 4000:
            raise ValueError("Duplicate/excessive SmoothGrid chunks")
        material = np.empty(CELLS, dtype=np.uint8)
        occupancy = np.empty(CELLS, dtype=np.uint8)
        written = 0
        while written < CELLS:
            if position >= len(blob):
                raise ValueError("Truncated SmoothGrid cell run")
            control = blob[position]
            position += 1
            material_id = control & 63
            if control & 64:
                if position >= len(blob):
                    raise ValueError("Truncated occupancy byte")
                level = blob[position]
                position += 1
            else:
                level = 255 if material_id else 0
            length = 1
            if control & 128:
                if position >= len(blob):
                    raise ValueError("Truncated run-count byte")
                extra = blob[position]
                position += 1
                if extra:
                    length += extra
                else:
                    if position >= len(blob):
                        raise ValueError("Truncated liquid byte")
                    position += 1
            if length > CELLS - written:
                raise ValueError("Terrain chunk cell count overflow")
            material[written:written + length] = material_id
            occupancy[written:written + length] = level
            written += length
        # Source cell ordering has X fastest, then Z, then Y.
        chunks[coordinate] = (material.reshape((SIZE, SIZE, SIZE)),
                              occupancy.reshape((SIZE, SIZE, SIZE)))
    return chunks


def layer_with_neighbors(chunks, coordinate, threshold=128):
    solid = np.zeros((34, 34, 34), dtype=bool)
    water = np.zeros_like(solid)
    material, occupancy = chunks[coordinate]
    solid[1:33, 1:33, 1:33] = (material >= 2) & (occupancy >= threshold)
    water[1:33, 1:33, 1:33] = (material == 1) & (occupancy > 0)
    boundaries = (
        ((1, 0, 0), (slice(1, 33), slice(1, 33), 33),
         (slice(None), slice(None), 0)),
        ((-1, 0, 0), (slice(1, 33), slice(1, 33), 0),
         (slice(None), slice(None), 31)),
        ((0, 1, 0), (33, slice(1, 33), slice(1, 33)),
         (0, slice(None), slice(None))),
        ((0, -1, 0), (0, slice(1, 33), slice(1, 33)),
         (31, slice(None), slice(None))),
        ((0, 0, 1), (slice(1, 33), 33, slice(1, 33)),
         (slice(None), 0, slice(None))),
        ((0, 0, -1), (slice(1, 33), 0, slice(1, 33)),
         (slice(None), 31, slice(None))),
    )
    for step, destination, side in boundaries:
        other = chunks.get(tuple(coordinate[i] + step[i] for i in range(3)))
        if other is None:
            continue
        m, o = other
        solid[destination] = ((m >= 2) & (o >= threshold))[side]
        water[destination] = ((m == 1) & (o > 0))[side]
    return solid, water


def merge_rectangles(surface):
    scratch = surface.copy()
    for u in range(32):
        for v in range(32):
            palette = int(scratch[u, v])
            if palette == 0:
                continue
            height = 1
            while v + height < 32 and scratch[u, v + height] == palette:
                height += 1
            width = 1
            while u + width < 32 and np.all(
                scratch[u + width, v:v + height] == palette
            ):
                width += 1
            scratch[u:u + width, v:v + height] = 0
            yield u, v, width, height, palette


def extract_faces(chunks, coordinate):
    mat, _ = chunks[coordinate]
    pad_solid, pad_water = layer_with_neighbors(chunks, coordinate)
    inside_solid = pad_solid[1:33, 1:33, 1:33]
    inside_water = pad_water[1:33, 1:33, 1:33]
    if not inside_solid.any() and not inside_water.any():
        return []
    faces = []
    # Direction order: +X, -X, +Y, -Y, +Z, -Z.
    # Numpy dimensions follow source ordering Y, Z, X.
    offsets = ((0, 0, 1), (0, 0, -1), (1, 0, 0),
               (-1, 0, 0), (0, 1, 0), (0, -1, 0))
    for direction, (dy, dz, dx) in enumerate(offsets):
        neighbor_solid = pad_solid[
            1 + dy:33 + dy, 1 + dz:33 + dz, 1 + dx:33 + dx]
        neighbor_water = pad_water[
            1 + dy:33 + dy, 1 + dz:33 + dz, 1 + dx:33 + dx]
        masks = ((inside_solid & ~neighbor_solid),
                 (inside_water & ~neighbor_water & ~neighbor_solid))
        for visible in masks:
            for index in range(32):
                if direction < 2:
                    enabled = visible[:, :, index]
                    materials = mat[:, :, index]
                elif direction < 4:
                    enabled = visible[index, :, :].T
                    materials = mat[index, :, :].T
                else:
                    enabled = visible[:, index, :].T
                    materials = mat[:, index, :].T
                if not np.any(enabled):
                    continue
                for u, v, width, height, palette in merge_rectangles(
                    np.where(enabled, materials, 0)
                ):
                    if direction < 2:
                        x, y, z = index, u, v
                    elif direction < 4:
                        x, y, z = u, index, v
                    else:
                        x, y, z = u, v, index
                    if palette > 22:
                        raise ValueError("Unexpected original terrain material ID")
                    faces.append((direction, x, y, z, width, height, palette))
    return faces


def convert_smoothgrid(blob: bytes, palette: bytes):
    if len(palette) != 69:
        raise ValueError("Expected 23 original terrain material colors")
    chunks = decode_smoothgrid(blob)
    output = io.BytesIO()
    output.write(MAGIC)
    output.write(struct.pack("<I", 0))
    output.write(palette)
    total_faces = 0
    written_chunks = 0
    solid_voxels = 0
    water_voxels = 0
    for coord in sorted(chunks):
        faces = extract_faces(chunks, coord)
        if not faces:
            continue
        total_faces += len(faces)
        if total_faces > 500000:
            raise ValueError("Excessive terrain faces")
        m, o = chunks[coord]
        solid_voxels += int(np.count_nonzero((m >= 2) & (o >= 128)))
        water_voxels += int(np.count_nonzero((m == 1) & (o > 0)))
        output.write(struct.pack("<iiiI", *coord, len(faces)))
        for face in faces:
            output.write(bytes(face))
        written_chunks += 1
    output.seek(8)
    output.write(struct.pack("<I", written_chunks))
    return output.getvalue(), {
        "source_chunks": len(chunks),
        "render_chunks": written_chunks,
        "terrain_quads": total_faces,
        "solid_voxels": solid_voxels,
        "water_voxels": water_voxels
    }


def convert_archive(source_zip: Path, destination: Path, names=MAPS):
    destination.mkdir(parents=True, exist_ok=True)
    result = {"format": "twr-original-terrain-face-mesh-v1",
              "grid_studs": 4,
              "surface_style": "block-faced greedy rectangles; not original smooth interpolation",
              "maps": {}}
    with zipfile.ZipFile(source_zip) as archive:
        for name in names:
            if name not in MAPS:
                raise ValueError("Unknown source map: " + name)
            terrain = archive.read("Content/Terrain/" + name + ".SmoothGrid.bin")
            colors = archive.read("Content/Terrain/" + name + ".MaterialColors.bin")
            mesh, info = convert_smoothgrid(terrain, colors)
            path = destination / (name + ".terrainmesh.gz")
            with path.open("wb") as file, gzip.GzipFile(
                filename="", fileobj=file, mode="wb", mtime=0,
                compresslevel=6
            ) as zipped:
                zipped.write(mesh)
            info["compressed_bytes"] = path.stat().st_size
            result["maps"][name] = info
            print("TWR_SOURCE_TERRAIN_CONVERTED map=" + name +
                  " chunks=" + str(info["render_chunks"]) +
                  " faces=" + str(info["terrain_quads"]))
    (destination / "terrainmesh-manifest.json").write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-zip", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--maps", nargs="*", default=list(MAPS))
    args = parser.parse_args()
    convert_archive(args.source_zip, args.output_dir, args.maps)

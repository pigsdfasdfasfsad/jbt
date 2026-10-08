#!/usr/bin/env python3
"""Generate deterministic synthetic Laboratory source-pack CI test data.

No Roblox geometry, private source bytes, or mesh binaries are included.
The fixture exists only to exercise source-pack loading in the exported .exe.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

IDENTITY = [1, 0, 0, 0, 1, 0, 0, 0, 1]

def generate(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    header = {
        'kind': 'header', 'format': 'twr-laboratory-scene-v1',
        'map': 'Laboratory', 'scale': 0.28, 'synthetic': True,
        'source_member': 'CI GENERATED (NO ROBLOX ASSETS)',
        'counts': {'geometry': 30000, 'collision': 1000,
                   'lights': 801, 'infected_spawns': 15, 'player_spawns': 8,
                   'item_markers': 127, 'fortification_markers': 47},
    }
    def write(stream, record):
        stream.write((json.dumps(record, separators=(',', ':'), sort_keys=True) + '\n').encode())
    with path.open('wb') as output:
        with gzip.GzipFile(filename='', mode='wb', fileobj=output,
                           compresslevel=6, mtime=0) as stream:
            write(stream, header)
            for i in range(30000):
                write(stream, {
                    'kind': 'geometry', 'ref': f'testgeo{i}',
                    'name': 'SyntheticWedge' if i == 1 else 'SyntheticFloor',
                    'class': 'WedgePart' if i == 1 else 'Part',
                    't': [i % 200, 0, i // 200], 'r': IDENTITY,
                    's': [2.0, 0.5, 2.0], 'shape': '1',
                    **({'specialMeshScale': [480.0, 40.0, 480.0]} if i == 2 else {}),
                    'rgb': [100, 110, 130], 'opacity': (0.0 if i == 0 else 1.0),
                    'mat': '272', 'shadow': False, 'collidable': (i < 16600),
                })
            for i in range(1000):
                write(stream, {
                    'kind': 'collision', 'ref': f'testwall{i}',
                    'name': 'SyntheticWall', 'class': 'Part',
                    't': [i % 100, 0, i // 100 + 220],
                    'r': IDENTITY, 's': [2, 2, 2], 'shape': '1',
                })
            for i in range(801):
                write(stream, {
                    'kind': 'light', 'ref': f'testlight{i}',
                    'class': 'SpotLight' if i == 0 else 'PointLight',
                    't': [i % 30, 6, i // 30], 'r': IDENTITY,
                    'rgb': [255, 230, 180], 'energy': 1.0,
                    'range': 16, 'angle': 70, 'face': 5,
                })
            for side, count in [('infected', 15), ('player', 8)]:
                for i in range(count):
                    write(stream, {
                        'kind': 'spawn', 'side': side,
                        'name': f'{side}{i:02}',
                        't': [i * 8, 4, 10 if side == 'infected' else 0],
                    })
            for group, total in [('Item', 127), ('Fortification', 47)]:
                for i in range(total):
                    write(stream, {
                        'kind': 'pickup_spawn', 'group': group,
                        'ref': f'test{group}{i:03}',
                        't': [i * 2, 4, 8 if group == 'Item' else 16],
                    })

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    generate(args.output)
    print(f'TWR_SYNTHETIC_LAB_PACK_OK bytes={args.output.stat().st_size}')

if __name__ == '__main__':
    main()

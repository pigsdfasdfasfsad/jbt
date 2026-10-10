#!/usr/bin/env python3
"""Create/dispose *synthetic* offline source-map sidecars for native Windows CI.

The files contain no owner-held Roblox scene, audio, image or mesh binaries.
They test that Pass25 navigation, Pass26 collision, Pass27 artwork and
Pass28 geometry are all read by the exported Godot executable, not just
checked by Python text tests. Never publish generated fixtures as source art.
"""
from __future__ import annotations

import argparse
import gzip
from hashlib import sha256
import json
from pathlib import Path
import struct
import zlib

MAPS = ('Ranch', 'Mill', 'Bypass', 'Cabin', 'Cargo', 'District', 'Expressway', 'Prison', 'Laboratory', 'Manor')
R = (1., 0., 0., 0., 1., 0., 0., 0., 1.)


def compressed(raw: bytes) -> bytes:
    # Gzip mtime and filename deliberately fixed so fixture hashes are stable.
    return gzip.compress(raw, mtime=0, compresslevel=6)


def png2x2() -> bytes:
    # Pure Python standard library 2x2 opaque synthetic graphic.
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind+data) & 0xffffffff)
    pixels = bytes((0,)) + bytes((40, 80, 95, 255)) * 2
    raw = pixels + pixels
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 2, 2, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b'')


def source_rows():
    rows = []
    for i in range(16):
        rows.append({'kind': 'geometry', 'class': 'Part', 'shape': '1',
                     'r': list(R), 't': [i * 2.5 - 10, 0, 0],
                     's': [2., 1., 2.], 'mat': '512', 'opacity': 1.,
                     'rgb': [140, 141, 147], 'shadow': True,
                     'collidable': i < 4})
    # Deliberately reflect the old server-only wall and item anchor split.
    rows.append({'kind': 'collision', 'class': 'Part', 'r': list(R),
                 't': [0., 1., 3.], 's': [1., 2., 1.]})
    for i in range(2):
        rows.append({'kind': 'light', 'class': 'PointLight', 'r': list(R),
                     't': [i * 2., 4., 0.], 'rgb': [255, 235, 188],
                     'energy': 1., 'range': 16., 'angle': 90., 'face': 5})
    rows.append({'kind': 'spawn', 'side': 'player', 'name': '1', 't': [0., 5., 0.]})
    rows.append({'kind': 'spawn', 'side': 'infected', 'name': 'Enemy', 't': [25., 5., 20.]})
    for i in range(2):
        rows.append({'kind': 'pickup_spawn', 'group': 'Item', 't': [i * 3., 4.5, 3.]})
    rows.append({'kind': 'pickup_spawn', 'group': 'Fortification', 't': [1., 4.5, 1.]})
    return rows


def source_scene():
    rows = source_rows()
    kinds = {'geometry': 0, 'collision': 0, 'lights': 0, 'player_spawns': 0,
             'infected_spawns': 0, 'item_markers': 0, 'fortification_markers': 0}
    for r in rows:
        kind = r['kind']
        if kind in ('geometry', 'collision'): kinds[kind] += 1
        elif kind == 'light': kinds['lights'] += 1
        elif kind == 'spawn': kinds[r['side']+'_spawns'] += 1
        elif kind == 'pickup_spawn': kinds[r['group'].lower()+'_markers'] += 1
    header = {'format': 'twr-source-map-v2', 'map': 'Laboratory', 'synthetic': True,
              'source_member': 'CI PASS30 SYNTHETIC ONLY - NOT ORIGINAL GAME',
              'scale': .28, 'counts': kinds}
    payload = '\n'.join(json.dumps(x, sort_keys=True, separators=(',', ':')) for x in [header, *rows]) + '\n'
    return compressed(payload.encode('utf-8'))


def nav25(digest):
    # Three source-stud nodes near the source player spawn; distances <1.5m.
    pts = [(0., 5., 0.), (2., 5., 0.), (4., 5., 0.)]
    b = bytearray(b'TWRNAV25' + struct.pack('<Iii', 1, len(pts), len(pts)-1) + digest)
    for p in pts: b.extend(struct.pack('<fff', *p))
    for i in range(len(pts)-1): b.extend(struct.pack('<ii', i, i+1))
    return compressed(b)



def nav31(digest):
    # Synthetic F9 assisted-entry test: fifty-one mutually accessible
    # waypoints extend 28 metres from the player, plus one disconnected
    # exterior spawn waypoint to require opt-in recovery. NO owner asset data.
    points = [(i * 2., 5., 0.) for i in range(51)] + [(300., 5., 40.)]
    edges = [(i, i+1) for i in range(50)]
    raw = bytearray(b'TWRNAV31' + struct.pack('<Iii', 2, len(points), len(edges)) + digest)
    for p in points: raw.extend(struct.pack('<fff', *p))
    for a,b in edges: raw.extend(struct.pack('<ii',a,b))
    return compressed(raw)



def clientwalls34(digest):
    # Fabricated one-piece invisible wall at (0,5,20) Roblox studs.
    # In Godot metres its centre is (0,1.4,-5.6), with player-only
    # collision toggle OFF until F8 is pressed. No owner-held geometry.
    header = {'format': 'twr-client-walls34-v1',
              'map': 'Laboratory',
              'scene_sha256': digest.hex(),
              'source_rbxlx_sha256': '0'*64,
              'wall_count': 1, 'collision_group_id': 8,
              'default_enabled': False}
    wall = {'class': 'Part', 't': [0., 5., 20.],
            'r': list(R), 's': [2., 6., 2.]}
    contents = '\\n'.join(json.dumps(x, sort_keys=True, separators=(',',':'))
                          for x in [header, wall]) + '\\n'
    return compressed(contents.encode('utf8'))


def col26(digest):
    # Synthetic walkable floor under original player + all distant nav31
    # F9 candidates, with upward-facing triangle normals for a ground ray.
    # No original asset bytes and no fabricated level content in final export.
    b = bytearray(b'TWRCOL26' + struct.pack('<IIII', 1, 1, 1, 0) + digest)
    b.extend(struct.pack('<iiI', 0, 0, 2))
    vertices = [(-4., 1.4, -4.), (36., 1.4, 4.), (36., 1.4, -4.),
                (-4., 1.4, -4.), (-4., 1.4, 4.), (36., 1.4, 4.)]
    for p in vertices: b.extend(struct.pack('<fff', *p))
    return compressed(b)


def native28(digest):
    b = bytearray(struct.pack('<8sIII32sI', b'TWRINS28', 1, 96, 1, digest, 1))
    b.extend(struct.pack('<iiI', 0, 0, 1))
    b.extend(struct.pack('<BHB3sBI', 0, 512, 255, bytes((140,141,147)), 0, 1))
    b.extend(struct.pack('<12f', .7, 0, 0, 0, .28, 0, 0, 0, .7, 0, 1, 0))
    return compressed(b)


def staged_files(outdir):
    scene = source_scene()
    digest = sha256(scene).digest()
    png = png2x2()
    sha = sha256(png).hexdigest()
    images = {'format': 'twr-pass27-offline-visual-reference-v1',
              'map_cards': {m: {'file': f'MapCards/{m}.png', 'sha256':sha} for m in MAPS},
              'weapon_icons': {'glock17':{'file':'WeaponIcons/G17-UI.png','sha256':sha}}}
    base = Path(outdir)/'Content'
    files = {base/'Maps'/'Laboratory.scene.jsonl.gz':scene,
             base/'Navigation'/'Laboratory.nav25.gz':nav25(digest),
             base/'Navigation'/'Laboratory.nav31.gz':nav31(digest),
             base/'Collision'/'Laboratory.col26.gz':col26(digest),
             base/'Walls'/'Laboratory.clientwalls34.jsonl.gz':clientwalls34(digest),
             base/'Geometry'/'Laboratory.native28.gz':native28(digest),
             base/'Art'/'visuals.json': (json.dumps(images,sort_keys=True)+'\n').encode('utf8'),
             base/'Art'/'WeaponIcons'/'G17-UI.png': png}
    for m in MAPS: files[base/'Art'/'MapCards'/f'{m}.png'] = png
    return files


def create(outdir):
    entries=staged_files(outdir)
    existing=[str(p) for p in entries if p.exists()]
    if existing: raise FileExistsError('Synthetic smoke fixture would overwrite existing files: '+str(existing))
    for p,contents in entries.items():
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(contents)
    print(f'TWR_PASS30_SYNTHETIC_SIDECARS_OK files={len(entries)} maps=10')


def clean(outdir):
    # Only files created by this script, never directories or unrelated private bytes.
    entries=staged_files(outdir)
    for p in entries:
        p.unlink(missing_ok=True)
    print(f'TWR_PASS30_SYNTHETIC_CLEAN_OK files={len(entries)}')


if __name__=='__main__':
    a=argparse.ArgumentParser()
    a.add_argument('--output', type=Path, required=True)
    g=a.add_mutually_exclusive_group(required=True)
    g.add_argument('--create',action='store_true')
    g.add_argument('--clean',action='store_true')
    args=a.parse_args()
    (create if args.create else clean)(args.output)

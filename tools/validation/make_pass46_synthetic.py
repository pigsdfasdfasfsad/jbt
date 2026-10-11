#!/usr/bin/env python3
"""Owner-free fabricated Pass46 map/weapon art for the exported Windows CI.

Writes ten dummy map thumbnails and three dummy source-weapon silhouettes.
Never includes user images, private assets or the actual Roblox game art.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
from make_pass30_synthetic_sidecars import MAPS, png2x2

WEAPONS={'glock17':'glock17.png','aa12':'aa12.png','m4a1':'m4a1.png'}
MARKER='PASS46_CI_SYNTHETIC_ONLY.marker'

def paths(root:Path):
    art=root/'Content'/'Art'
    originals=[art/'MapCards'/f'{name}.png' for name in MAPS]
    originals += [art/'WeaponIcons'/stem for stem in WEAPONS.values()]
    originals += [art/'visuals.json',art/MARKER]
    return art,originals

def create(root:Path):
    art,files=paths(root)
    if any(path.exists() for path in files) or (art.exists() and any(art.rglob('*'))):
        raise FileExistsError('Refusing to replace owner offline art with synthetic fixture')
    png=png2x2()
    digest=hashlib.sha256(png).hexdigest()
    manifest={'format':'twr-pass27-offline-visual-reference-v1',
        'map_cards':{name:{'file':f'MapCards/{name}.png','sha256':digest}
                     for name in MAPS},
        'weapon_icons':{stem:{'file':f'WeaponIcons/{file}','sha256':digest}
                        for stem,file in WEAPONS.items()}}
    for path in files:
        path.parent.mkdir(parents=True,exist_ok=True)
        if path.name==MARKER:
            path.write_text('PASS46_CI_SYNTHETIC_ONLY\n',encoding='ascii')
        elif path.name=='visuals.json':
            path.write_text(json.dumps(manifest,sort_keys=True)+'\n',encoding='utf8')
        else:
            path.write_bytes(png)
    print('TWR_PASS46_SYNTHETIC_ART_CREATED maps=10 weapon_icons=3 original=false')

def clean(root:Path):
    art,files=paths(root)
    marker=art/MARKER
    if not marker.is_file() or marker.read_text()!='PASS46_CI_SYNTHETIC_ONLY\n':
        raise FileNotFoundError('Synthetic fixture marker missing; refusing to touch user art')
    for path in files:
        path.unlink(missing_ok=True)
    for folder in (art/'MapCards',art/'WeaponIcons',art):
        if folder.exists() and not any(folder.iterdir()):
            folder.rmdir()
    print('TWR_PASS46_SYNTHETIC_ART_CLEANED maps=10 weapon_icons=3')

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output',required=True,type=Path)
    group=p.add_mutually_exclusive_group(required=True)
    group.add_argument('--create',action='store_true')
    group.add_argument('--clean',action='store_true')
    a=p.parse_args()
    (create if a.create else clean)(a.output)

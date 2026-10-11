#!/usr/bin/env python3
"""Entirely fabricated source-lobby scene, never original owner Roblox data."""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

I=[1,0,0,0,1,0,0,0,1]
CAMS=('Start','Shop','Loadout','Perks','Options','Gifts',
      'Leaderboards','WeaponNode')
SLOTS=('Primary','Secondary','Melee','Utility','View')

def part(cls,name,t,size,alpha=1,shape='1'):
    return {'class':cls,'name':name,'source_path':'synthetic/'+name,
        't':t,'r':I,'s':size,'rgb':[126,148,172],
        'opacity':alpha,'mat':'272','shape':shape,'shadow':False}

def create(path:Path):
    if path.exists():raise FileExistsError('Refusing overwrite of existing lobby pack')
    geometry=[
        part('Part','Floor',[0,-2,-8],[18,1,18]),
        part('Part','Ball',[0,0,-7],[1,1,1],shape='0'),
        part('WedgePart','Ramp',[2,-1,-8],[3,1,4]),
        part('MeshPart','MissingSourceSofa',[-3,-1,-9],[3,2,2]),
        part('MeshPart','MissingSourceLamp',[4,0,-11],[1,5,1]),
        part('UnionOperation','MissingSourceCSG',[1,0,-15],[4,3,2]),
        part('Part','DisplayBoard',[-2,2,-15],[6,3,.4]),
        part('Part','InvisibleHelper',[0,0,0],[1,1,1],alpha=0)
    ]
    cameras={name:{'t':[index*1.4,0,0],'r':I}
             for index,name in enumerate(CAMS)}
    loadout={name:{'t':[0,-.5,-5-index],'r':I}
             for index,name in enumerate(SLOTS)}
    lights=[
        {'name':'FakePoint','class':'PointLight','source_path':'synthetic',
          't':[0,5,-8],'r':I,'rgb':[255,205,185],'energy':1,
          'range':16,'angle':90,'face':5,'enabled':True},
        {'name':'FakeSpot','class':'SpotLight','source_path':'synthetic',
          't':[2,5,-12],'r':I,'rgb':[200,220,255],'energy':1,
          'range':12,'angle':55,'face':5,'enabled':True},
    ]
    doc={'format':'twr-pass49-original-lobby-v1','synthetic':True,
         'owner_place_sha256':'synthetic-only','source_path':'Workspace/Lobby',
         'geometry':geometry,'lights':lights,'cameras':cameras,
         'loadout_points':loadout,'original_mesh_triangles_embedded':False}
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_bytes(gzip.compress(json.dumps(
        doc,sort_keys=True,separators=(',',':')).encode(),
        mtime=0,compresslevel=6))
    print('TWR_PASS49_SYNTHETIC_CREATED source_geometry=8 visible=7 proxies=3 cameras=8 loadouts=5 lights=2')

def clean(path:Path):
    if not path.exists():return
    d=json.loads(gzip.decompress(path.read_bytes()))
    if (d.get('synthetic') is not True or d.get('format')!='twr-pass49-original-lobby-v1'
        or len(d.get('geometry',[]))!=8 or d.get('owner_place_sha256')!='synthetic-only'):
        raise ValueError('Refusing to delete owner original-source lobby')
    path.unlink()
    print('TWR_PASS49_SYNTHETIC_CLEANED')

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output',type=Path,required=True)
    group=p.add_mutually_exclusive_group(required=True)
    group.add_argument('--create',action='store_true')
    group.add_argument('--clean',action='store_true')
    args=p.parse_args()
    (create if args.create else clean)(args.output)

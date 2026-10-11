#!/usr/bin/env python3
"""Entirely fabricated 15-variant 8-type infected asset pack for public CI."""
from __future__ import annotations
import argparse,gzip,json
from pathlib import Path

VARIANTS={'Civilian':4,'Sprinter':2,'Bolter':1,'Military':1,
          'Riot':1,'Hazmat':4,'Bloater':1,'Burster':1}
I=[1,0,0,0,1,0,0,0,1]
BODY=[('Torso','Torso',[0,0,0],[2,2,1]),('Head','Head',[0,1.5,0],[1,1,1]),
      ('Left Arm','LeftArm',[-1.5,0,0],[1,2,1]),('Right Arm','RightArm',[1.5,0,0],[1,2,1]),
      ('Left Leg','LeftLeg',[-.5,-2,0],[1,2,1]),('Right Leg','RightLeg',[.5,-2,0],[1,2,1])]

def payload():
    types={}
    for typ,count in VARIANTS.items():
        variants=[]
        for n in range(count):
            parts=[{'name':name,'region':region,'class':'Part','t':xyz,'r':I,
              's':size,'rgb':[110,130,90],'opacity':1.,
              'shape':'0' if region=='Head' else '1',
              'reconstructed_r6_proxy':True,'source_authored':False}
              for name,region,xyz,size in BODY]
            parts += [
              {'name':'OriginalEyepiece','region':'Head','class':'MeshPart',
               't':[0,1.52,-.33],'r':I,'s':[.35,.18,.22],
               'rgb':[23,43,55],'opacity':1.,'shape':'1',
               'source_authored':True,'source_group':'fabricated/smoke',
               'meshId':'rbxassetid://999999999'},
              {'name':'SourceHair','region':'Head','class':'Part',
               't':[0,1.89,0],'r':I,'s':[.71,.18,.6],
               'rgb':[40,36,31],'opacity':1.,'shape':'1',
               'source_authored':True,'source_group':'fabricated/smoke'}
            ]
            variants.append({'kind':'source_ai_kit_plus_r6_proxy',
                'infected_type':typ,'variant_id':typ+'-'+str(n+1),
                'parts':parts,'original_groups':['fabricated/smoke']})
        types[typ]=variants
    return {'format':'twr-source-infected-variants-v1','synthetic':True,
        'generation':'pass48_verified_source_ai_kit_plus_explicit_r6_proxy',
        'types':types,'original_animation_tracks_restored':False,
        'original_cloud_mesh_triangles_restored':False}

def create(target:Path):
    if target.exists():raise FileExistsError('Refusing to replace owner infected pack')
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(gzip.compress(json.dumps(
        payload(),sort_keys=True,separators=(',',':')).encode(),
        mtime=0,compresslevel=6))
    print('TWR_PASS48_SYNTHETIC_CREATED types=8 variants=15 per_type_source_accessories=2 body_proxies=6')

def clean(target:Path):
    if not target.exists():return
    d=json.loads(gzip.decompress(target.read_bytes()))
    if d.get('synthetic') is not True or set(d.get('types',{}))!=set(VARIANTS) or d.get('generation')!='pass48_verified_source_ai_kit_plus_explicit_r6_proxy':
        raise ValueError('Refusing to delete non-synthetic infected source pack')
    target.unlink()
    print('TWR_PASS48_SYNTHETIC_CLEANED')

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output',type=Path,required=True)
    m=p.add_mutually_exclusive_group(required=True)
    m.add_argument('--create',action='store_true')
    m.add_argument('--clean',action='store_true')
    a=p.parse_args()
    (create if a.create else clean)(a.output)

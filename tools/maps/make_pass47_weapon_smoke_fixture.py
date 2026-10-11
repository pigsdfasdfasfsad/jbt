#!/usr/bin/env python3
"""Fabricated Pass47 source 3D weapon models for exported Windows CI.

Creates six invented five-part tools, including one originally invisible
marker, one absent cloud MeshPart, a native ball and a native gun receiver.
Never embeds owner model transforms or original Roblox mesh bytes.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

NAMES=('Glock 17','Sawn Off Shotgun','AK-47','RPG-7','2x4','Molotov')
I=[1.,0.,0.,0.,1.,0.,0.,0.,1.]

def create(out:Path):
    if out.exists():raise FileExistsError("Refusing overwrite of original tool pack")
    models={}
    for name in NAMES:
        parts=[]
        for i,part_name in enumerate(('Barrel','Receiver','Magazine','Slide','Pos')):
            part={
                'name':part_name,'class':'MeshPart' if i==0 else 'Part',
                't':[round(i*.13,5),0.,round(-.9-i*.08,5)],
                'r':I,'s':[.45,.45,.8], 'rgb':[65+i*11,80,85],
                'opacity':0. if i==4 else 1.,
                'shape':'0' if i==1 else '1',
                'mat':'272','shadow':False
            }
            if i==0:part['meshId']='rbxassetid://999999999'
            parts.append(part)
        models[name]={'parts':parts,'original_mesh_ids':['rbxassetid://999999999']}
    payload={'format':'twr-original-weapon-assemblies-v1',
             'synthetic':True, 'models':models}
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_bytes(gzip.compress(json.dumps(
        payload,sort_keys=True,separators=(',',':')).encode(),
        mtime=0,compresslevel=6))
    print('TWR_PASS47_SYNTHETIC_CREATED models=6 source_visible_per_tool=4 cloud_mesh_missing=true')

def clean(out:Path):
    # Always use only the original known synthetic fixture shape.
    if not out.exists():return
    data=json.loads(gzip.decompress(out.read_bytes()))
    if (data.get('synthetic') is not True or
        data.get('format')!='twr-original-weapon-assemblies-v1' or
        set(data.get('models',{}))!=set(NAMES)):
        raise ValueError('Refusing to delete non-synthetic tool source pack')
    out.unlink()
    print('TWR_PASS47_SYNTHETIC_CLEANED')

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output',type=Path,required=True)
    group=p.add_mutually_exclusive_group(required=True)
    group.add_argument('--create',action='store_true')
    group.add_argument('--clean',action='store_true')
    a=p.parse_args()
    (create if a.create else clean)(a.output)

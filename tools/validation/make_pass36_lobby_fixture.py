#!/usr/bin/env python3
"""Disposable non-original Lobby smoke fixture for Windows CI.

Do not confuse these fabricated cubes/cameras with owner-held Roblox data.
Refuses overwrite, and clean only deletes an exact synthetic marker.
"""
from __future__ import annotations
import argparse
import gzip
import json
from pathlib import Path

CAMERAS=('Start','Shop','WeaponNode','Loadout','Perks','Options','Leaderboards','Gifts')
IDENTITY=[1.,0.,0.,0.,1.,0.,0.,0.,1.]


def pack():
    geo=[]
    for i in range(16):
        geo.append({'kind':'geometry','class':'MeshPart' if i % 4 == 0 else 'Part',
                    'name':'SyntheticLobbyCube'+str(i),'group':'Objects',
                    't':[280+i*0.4,-2945,20+i*0.4],'r':IDENTITY,
                    's':[1.,1.,1.], 'rgb':[90+i,140,165],
                    'opacity':1.,'material':'512','shape':'1','shadow':False,
                    'meshId':'','textureId':''})
    cameras=[{'kind':'camera','name':k,'t':[290,-2944,15+i], 'r':IDENTITY}
             for i,k in enumerate(CAMERAS)]
    lights=[{'kind':'light','class':'PointLight','name':'SyntheticLight',
             't':[290.,-2945.,25.], 'r':IDENTITY, 'rgb':[255,210,168],
             'range':16., 'brightness':1., 'enabled':True}]
    header={'format':'twr-pass36-source-lobby-v1',
            'source_path':'Workspace.Lobby','source_rbxlx_sha256':'0'*64,
            'synthetic':True,'origin_studs':[290,-2945,0],
            'counts':{'geometry':len(geo),'cameras':len(cameras),'lights':len(lights)}}
    raw=('\n'.join(json.dumps(x,sort_keys=True,separators=(',',':'))
                 for x in (header,*geo,*cameras,*lights))+'\n').encode()
    return gzip.compress(raw,mtime=0,compresslevel=6)


def main():
    p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);
    grp=p.add_mutually_exclusive_group(required=True);grp.add_argument('--create',action='store_true');grp.add_argument('--clean',action='store_true')
    a=p.parse_args();path=a.output/'Content'/'Lobby'/'OriginalLobby.lobby36.jsonl.gz';data=pack()
    if a.create:
        if path.exists():raise FileExistsError('Refuse to overwrite Lobby owner source')
        path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
        print('TWR_PASS36_CI_FIXTURE_CREATED synthetic=true bytes='+str(len(data)))
    elif path.exists():
        if path.read_bytes()!=data:raise ValueError('Refuse to remove non-synthetic owner data')
        path.unlink();print('TWR_PASS36_CI_FIXTURE_REMOVED')

if __name__=='__main__': main()
#!/usr/bin/env python3
"""Recover original Workspace.Lobby Part transforms and UI camera points.

Meshes/CSG triangles and original materials/decals ARE NOT embedded. The pack
preserves exact source placements but substitutes geometry when those binaries
are missing. Never executes Roblox source or downloads network assets.
"""
from __future__ import annotations
import argparse
from collections import Counter
import gzip
import hashlib
import json
import math
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

MEMBER='TestPlace/TestPlace.rbxlx'
SHAPES={'Part','WedgePart','MeshPart','UnionOperation','CornerWedgePart','TrussPart','Seat','VehicleSeat'}
LIGHTS={'PointLight','SpotLight','SurfaceLight'}
CAMERAS={'Start','Shop','WeaponNode','Loadout','Perks','Options','Leaderboards','Gifts'}
OMIT={'CamPoints','LoadoutPoints','MidPoints'}
MAX_SOURCE_SIZE=500_000_000

def prop(item,key):
    properties=item.find('Properties')
    return None if properties is None else next((x for x in properties if x.get('name')==key),None)

def val(item,key,default=''):
    p=prop(item,key)
    return default if p is None or p.text is None else p.text.strip()

def name(item):return val(item,'Name',item.get('class','Unknown'))

def ancestry(item):
    parts=[]
    while item is not None:
        if item.tag=='Item':parts.append(name(item))
        item=item.getparent()
    return tuple(reversed(parts))

def vec(p,keys):
    if p is None: raise ValueError('Missing original source vector')
    values=[]
    for key in keys:
        v=p.find(key)
        if v is None or v.text is None:raise ValueError('Missing source vector coordinate: '+key)
        num=float(v.text)
        if not math.isfinite(num) or abs(num)>100000:raise ValueError('Nonfinite or unbounded source value')
        values.append(round(num,7))
    return values

def rgb(item):
    source=val(item,'Color3uint8')
    if source:
        n=int(source)
        return [(n>>16)&255,(n>>8)&255,n&255]
    p=prop(item,'Color3')
    if p is not None:
        return [round(max(0,min(1,c))*255) for c in vec(p,('R','G','B'))]
    return [166,166,166]

def floating(item,key,default):
    text=val(item,key)
    if not text:return default
    n=float(text)
    if not math.isfinite(n):raise ValueError('Nonfinite '+key)
    return n

def extract(z):
    shapes=[]; cameras={};lights=[];counts=Counter();omitted=Counter()
    with z.open(MEMBER) as f:
        for _,item in etree.iterparse(f,events=('end',),tag='Item',huge_tree=True,
                                        resolve_entities=False,load_dtd=False,no_network=True):
            cls=item.get('class')
            if cls in SHAPES|LIGHTS:
                path=ancestry(item)
                if path[:2]==('Workspace','Lobby') and len(path)>2:
                    group=path[2]
                    if cls in SHAPES:
                        cf=prop(item,'CFrame')
                        size=prop(item,'size')
                        if size is None:size=prop(item,'Size')
                        if cf is not None and size is not None:
                            t=vec(cf,('X','Y','Z'))
                            r=vec(cf,('R00','R01','R02','R10','R11','R12','R20','R21','R22'))
                            s=vec(size,('X','Y','Z'))
                            if min(s)<=0.00001:raise ValueError('Nonpositive source lobby Part size')
                            if group=='CamPoints':
                                if len(path)!=4 or path[3] not in CAMERAS:
                                    raise ValueError('Unknown source lobby UI viewpoint '+str(path))
                                if path[3] in cameras:raise ValueError('Duplicated original Lobby CamPoint')
                                cameras[path[3]]={'kind':'camera','name':path[3],'t':t,'r':r}
                            elif group in OMIT: omitted[group]+=1
                            else:
                                alpha=max(0.0,min(1.0,1-floating(item,'Transparency',0.0)))
                                shapes.append({'kind':'geometry','class':cls,'name':name(item)[:96],
                                               'group':group,'t':t,'r':r,'s':s,
                                               'rgb':rgb(item),'opacity':round(alpha,5),
                                               'material':val(item,'Material','256'),
                                               'shape':val(item,'shape','1'),
                                               'shadow':val(item,'CastShadow','true').lower()!='false',
                                               'meshId':val(item,'MeshId') or val(item,'MeshID'),
                                               'textureId':val(item,'TextureID') or val(item,'TextureId')})
                                counts[cls]+=1
                    elif cls in LIGHTS:
                        par=item.getparent()
                        while par is not None and par.tag!='Item':par=par.getparent()
                        # Roblox point lights are children of Parts; use the
                        # source fixture's CFrame, not a synthetic world offset.
                        cf=prop(par,'CFrame') if par is not None else None
                        if cf is not None:
                            lights.append({'kind':'light','class':cls,'name':name(item)[:72],
                                't':vec(cf,('X','Y','Z')),'r':vec(cf,('R00','R01','R02','R10','R11','R12','R20','R21','R22')),
                                'range':round(max(0.,min(200.,floating(item,'Range',16.))),5),
                                'brightness':round(max(0.,min(10.,floating(item,'Brightness',1.))),5),
                                'rgb':rgb(item),'enabled':val(item,'Enabled','true').lower()!='false'})
            parent=item.getparent();item.clear()
            while item.getprevious() is not None and item.getprevious().tag=='Item':parent.remove(item.getprevious())
    if set(cameras)!=CAMERAS:raise ValueError(f'Unexpected source lobby camera coverage {set(cameras)}')
    if not 700 <= len(shapes) <= 950: raise ValueError(f'Unexpected lobby geometry count {len(shapes)}')
    return shapes,cameras,lights,counts,omitted

def extract_to_file(archive, out):
    archive=Path(archive);out=Path(out)
    with ZipFile(archive) as z:
        info=z.getinfo(MEMBER)
        if info.file_size>MAX_SOURCE_SIZE:raise ValueError('Owner source above safety bound')
        with z.open(MEMBER) as f:sha=hashlib.file_digest(f,'sha256').hexdigest()
        shapes,cameras,lights,counts,omitted=extract(z)
    header={'format':'twr-pass36-source-lobby-v1','source_rbxlx_sha256':sha,
            'source_path':'Workspace.Lobby',
            'origin_studs':[290,-2945,0],
            'counts':{'geometry':len(shapes),'cameras':len(cameras),'lights':len(lights)},
            'missing_mesh_representations':sum(1 for s in shapes if s['class'] in ('MeshPart','UnionOperation')),
            'note':'Original source transforms; absent mesh and UnionOperation binaries render as clearly marked proxies'}
    rows=[header,*shapes,*(cameras[k] for k in sorted(cameras)),*lights]
    data= ('\n'.join(json.dumps(row,sort_keys=True,separators=(',',':')) for row in rows)+'\n').encode('utf8')
    packed=gzip.compress(data,mtime=0,compresslevel=6)
    out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(packed)
    manifest={'format':'twr-pass36-source-lobby-provenance-v1','source_member':MEMBER,
        'source_rbxlx_sha256':sha,'pack_sha256':hashlib.sha256(packed).hexdigest(),
        'geometry_by_class':dict(sorted(counts.items())),'omitted_markers':dict(sorted(omitted.items())),
        'cameras':sorted(cameras),'source_lights':len(lights),'lobby_pack':out.name,
        'geometry_count':len(shapes),'missing_custom_meshes':header['missing_mesh_representations'],
        'camera_transform_scope':'Workspace.Lobby.CamPoints - exact rotation/origin translated by 290,-2945,0 studs',
        'runtime':'Godot offline optional original-transform 3D menu, legacy flat UI fallback'}
    out.with_name('lobby36-manifest.json').write_text(json.dumps(manifest,indent=2,sort_keys=True)+'\n',encoding='utf8')
    print('TWR_PASS36_LOBBY recovered_objects='+str(len(shapes))+' cameras='+str(len(cameras))+' lights='+str(len(lights))+' missing_meshes='+str(header['missing_mesh_representations']))
    return manifest

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--archive',required=True);ap.add_argument('--output',required=True);a=ap.parse_args()
    extract_to_file(a.archive,a.output)
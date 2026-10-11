#!/usr/bin/env python3
"""Extract the owner's original Workspace/Lobby 3D scene from inert RBXLX XML.

The missing Roblox external MeshPart / CSG triangles are NOT fabricated.
Produces SHA-bound original source geometry, 8 cameras, 5 loadout points
and 18 light emitters; source Camera Start is the relative geometry origin.
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

SOURCE_XML='272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2'
MEMBER='TestPlace/TestPlace.rbxlx'
CLASSES={'Part','MeshPart','UnionOperation','WedgePart','CornerWedgePart'}
CAMERAS={'Start','Shop','Loadout','Perks','Options','Gifts','Leaderboards','WeaponNode'}
LOADOUTS={'View','Primary','Secondary','Melee','Utility'}

def num(x):
    n=float(x)
    if not math.isfinite(n) or abs(n)>100000:raise ValueError('Unbounded source number')
    return round(n,7)

def props_map(props):
    interested={'Name','CFrame','size','Color3uint8','Transparency','Material',
      'shape','CastShadow','MeshId','TextureID','TextureId','Brightness','Range',
      'Color','Angle','Face','Enabled','MeshType','Scale','Offset'}
    d={}
    for c in props:
        k=c.get('name')
        if k not in interested:continue
        if c.tag in ('CoordinateFrame','Vector3','Color3'):
            d[k]={v.tag:(v.text or '').strip() for v in c}
        elif c.tag=='Content':d[k]=''.join(c.itertext()).strip()
        else:d[k]=(c.text or '').strip()
    return d

def vector(p,keys=('X','Y','Z')):
    return [num(p[k]) for k in keys]

def cframe(d):
    c=d['CFrame']
    t=vector(c)
    r=[num(c.get('R'+str(i)+str(j),str(int(i==j))))
       for i in range(3) for j in range(3)]
    if max(abs(x) for x in r)>1.02:raise ValueError('Original CFrame invalid')
    return t,r

def white(d):
    if d.get('Color3uint8'):
        packed=int(d['Color3uint8'])
        return [(packed>>i)&255 for i in (16,8,0)]
    if 'Color' in d:
        p=d['Color']
        return [max(0,min(255,round(float(p[k])*255))) for k in ('R','G','B')]
    return [192,192,192]

def source_part(item,path):
    d=item['props'];t,r=cframe(d);s=vector(d['size'])
    if min(s)<.001 or max(s)>5000:raise ValueError('Source Part dimension invalid')
    rgb=white(d)
    alpha=num(max(0,min(1,1-float(d.get('Transparency','0')))))
    row={'name':item['name'],'class':item['class'],
      'source_path':'/'.join(path[2:]),'t':t,'r':r,'s':s,
      'rgb':rgb,'opacity':alpha,'mat':str(d.get('Material','272')),
      'shape':str(d.get('shape','1')),
      'shadow':d.get('CastShadow','false').lower()=='true'}
    for raw,key in [('MeshId','meshId'),('TextureID','textureId'),('TextureId','textureId')]:
        if d.get(raw):row[key]=d[raw]
    special=item.get('special') or {}
    if special:
        if special.get('MeshId'):row['specialMeshId']=special['MeshId']
        if special.get('MeshType'):row['specialMeshType']=str(special['MeshType'])
        for raw,key in [('Scale','specialMeshScale'),('Offset','specialMeshOffset')]:
            if raw in special:row[key]=vector(special[raw])
    return row

def source_light(item,path,stack):
    for parent in reversed(stack[:-1]):
        if parent['class'] in CLASSES and parent.get('props',{}).get('CFrame'):
            pos,rotation=cframe(parent['props'])
            break
    else:raise ValueError('Source Light missing positioned parent')
    d=item['props']
    return {'name':item['name'],'class':item['class'],
       'source_path':'/'.join(path[2:]),
       't':pos,'r':rotation,'rgb':white(d),
       'energy':num(d.get('Brightness','1')),
       'range':num(d.get('Range','12')),
       'angle':num(d.get('Angle','90')),
       'face':int(d.get('Face','5')),
       'enabled':d.get('Enabled','true').lower()=='true'}

def original_xml_sha(z,member):
    with z.open(member) as source:
        digest=hashlib.sha256()
        while chunk:=source.read(1<<20):digest.update(chunk)
        return digest.hexdigest()

def extract(archive:Path,output:Path,synthetic=False,member=MEMBER):
    with ZipFile(archive) as z:
        source_sha=original_xml_sha(z,member)
        if not synthetic and source_sha!=SOURCE_XML:
            raise ValueError('Original TestPlace.rbxlx SHA mismatch')
        stack=[];geometry=[];lights=[];cameras={};loadout={};ui=Counter()
        with z.open(member) as fd:
            for event,el in etree.iterparse(fd,events=('start','end'),
                                              tag=('Item','Properties'),huge_tree=True):
                if event=='start' and el.tag=='Item':
                    stack.append({'class':el.get('class'),'name':'','props':{},'special':{}})
                elif event=='end' and el.tag=='Properties' and stack:
                    stack[-1]['props']=props_map(el)
                    stack[-1]['name']=stack[-1]['props'].get('Name','')
                    el.clear()
                elif event=='end' and el.tag=='Item':
                    item=stack[-1];path=tuple(x['name'] for x in stack)
                    if path[:2]==('Workspace','Lobby'):
                        if item['class']=='SpecialMesh' and len(stack)>3 and stack[-2]['class'] in CLASSES:
                            stack[-2]['special']=item['props']
                        if item['class'] in CLASSES:
                            data=source_part(item,path)
                            if len(path)>=4 and path[2]=='CamPoints' and path[3] in CAMERAS:
                                cameras[path[3]]={'t':data['t'],'r':data['r']}
                            elif len(path)>=4 and path[2]=='LoadoutPoints' and path[3] in LOADOUTS:
                                loadout[path[3]]={'t':data['t'],'r':data['r']}
                            else:
                                geometry.append(data)
                        elif item['class'] in ('PointLight','SpotLight'):
                            lights.append(source_light(item,path,stack))
                        elif item['class'] in ('TextLabel','ImageLabel','SurfaceGui','Decal'):
                            ui[item['class']]+=1
                    stack.pop();el.clear()
                    while el.getprevious() is not None:del el.getparent()[0]
    if not synthetic and (len(geometry)!=818 or len(lights)!=18 or
                          set(cameras)!=CAMERAS or set(loadout)!=LOADOUTS):
        raise ValueError('Original lobby class/camera counts changed')
    if synthetic and (len(geometry)<2 or not cameras or not lights):
        raise ValueError('Invalid fabricated lobby')
    origin=cameras['Start']['t']
    for collection in (geometry,lights):
        for row in collection:
            row['t']=[num(a-b) for a,b in zip(row['t'],origin)]
    for mapping in (cameras,loadout):
        for row in mapping.values():
            row['t']=[num(a-b) for a,b in zip(row['t'],origin)]
    classes=Counter(x['class'] for x in geometry)
    data={
      'owner_place_sha256':source_sha,
      'source_path':'Workspace/Lobby',
      'generation':'exact_source_transform_geometry_with_missing_mesh_bounds',
      'original_mesh_triangles_embedded':False,
      'original_3d_ui_surfacegui_restored':False,
      'source_camera_cframe_preserved':True,
      'source_lighting_approximation':True,
      'source_offsets_relative_to_original_start_camera':True,
      'geometry':geometry,'lights':lights,'cameras':cameras,
      'loadout_points':loadout,'format':'twr-pass49-original-lobby-v1',
      'synthetic':bool(synthetic)
    }
    raw=json.dumps(data,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()
    if len(raw)>10_000_000:raise ValueError('Lobby data size exceeded')
    packed=gzip.compress(raw,mtime=0,compresslevel=6)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_bytes(packed)
    visible=sum(p['opacity']>.001 for p in geometry)
    missing=sum(p['class'] in ('MeshPart','UnionOperation') and p['opacity']>.001
                for p in geometry)
    manifest={
      'format':'twr-pass49-original-lobby-evidence-v1',
      'source_xml_sha256':source_sha,
      'source':'Workspace/Lobby','source_parts':len(geometry),
      'source_classes':dict(classes),'visible_geometry_parts':visible,
      'mesh_or_csg_proxy_parts_visible':missing,
      'original_camera_points':len(cameras),'camera_names':sorted(cameras),
      'original_loadout_points':len(loadout),'light_emitters':len(lights),
      'original_2d_ui_objects_not_converted':dict(ui),
      'source_relative_origin_studs':origin,
      'original_mesh_triangle_bytes_recovered':False,
      'source_lobby_3d_enabled':True,'original_rbx_3d_gui_restored':False,
      'compressed_sha256':hashlib.sha256(packed).hexdigest(),
      'source_lobby_model_contains_approximated_mesh_proxies':True,
      'compressed_size':len(packed)
    }
    (output.parent/'SOURCE_LOBBY_MANIFEST49.json').write_text(
        json.dumps(manifest,sort_keys=True,indent=2)+'\n')
    return manifest

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--archive',type=Path,required=True)
    p.add_argument('--out',type=Path,required=True)
    p.add_argument('--synthetic',action='store_true')
    p.add_argument('--member',default=MEMBER)
    a=p.parse_args()
    print(json.dumps(extract(a.archive,a.out,a.synthetic,a.member),indent=2))

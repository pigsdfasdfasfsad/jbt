#!/usr/bin/env python3
"""Extract ONLY source 3D weapon assembly transforms from owner TestPlace.rbxlx.

Reads inert XML under ReplicatedStorage/Models/Tools; never runs Roblox/Luau.
The place lacks original remote MeshPart triangle bytes: preserve MeshIds as
provenance while Godot uses its existing honest source-bounds fallback.
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

MEMBER = 'TestPlace/TestPlace.rbxlx'
REAL_SOURCE_SHA = '272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2'
PART_CLASSES = {'Part','MeshPart','WedgePart','CornerWedgePart','UnionOperation'}
ALIASES = {'2x4 w/ Barbed Wire':'2x4 w- Barbed Wire', 'Ruger 10/22':'Ruger 10-22'}
SOURCE_PREFIX = ('ReplicatedStorage','Models','Tools')

def fields(properties):
    names={'Name','CFrame','size','Color3uint8','Color3','Transparency','Material',
           'MeshId','TextureID','TextureId','shape','CastShadow','MeshType','Offset','Scale'}
    result={}
    for p in properties:
        key=p.get('name')
        if key not in names: continue
        if key in ('CFrame','size','Color3','Offset','Scale'):
            result[key]={c.tag: (c.text or '').strip() for c in p}
        elif p.tag=='Content':
            result[key]=''.join(p.itertext()).strip()
        else:
            result[key]=(p.text or '').strip()
    return result

def triple(d, keys):
    return [float(d[k]) for k in keys]

def rounded(v):
    if not math.isfinite(v) or abs(v)>100000:
        raise ValueError('Nonfinite/over-budget source transform')
    return round(v,7)

def make_part(item):
    p=item['properties']; cf=p.get('CFrame')
    if not cf or not p.get('size'):
        raise ValueError('Original weapon part lacks CFrame/size')
    size=[rounded(x) for x in triple(p['size'],('X','Y','Z'))]
    if min(size)<=0 or max(size)>1000:
        raise ValueError('Invalid original weapon part size')
    position=[rounded(x) for x in triple(cf,('X','Y','Z'))]
    rotation=[rounded(float(cf.get(f'R{i}{j}', int(i==j))))
              for i in range(3) for j in range(3)]
    if max(abs(x) for x in rotation)>2:
        raise ValueError('Invalid original weapon rotation')
    try:
        packed=int(p.get('Color3uint8','4294967295'))
        rgb=[(packed>>s)&255 for s in (16,8,0)]
    except (ValueError,TypeError):
        c=p.get('Color3',{});rgb=[max(0,min(255,round(v*255)))
            for v in triple(c,('R','G','B'))]
    transparency=float(p.get('Transparency',0))
    if not math.isfinite(transparency) or abs(transparency)>2:
        raise ValueError('Invalid source transparency')
    result={'name':p.get('Name') or item['class'], 'class':item['class'],
        't':position, 'r':rotation,'s':size, 'rgb':rgb,
        'opacity':rounded(max(0.,min(1.,1.-transparency))),
        'mat':str(p.get('Material','272')),
        'shape':str(p.get('shape','1')),
        'shadow':p.get('CastShadow','false').lower()=='true'}
    for src,dst in (('MeshId','meshId'),('TextureID','textureId'),('TextureId','textureId')):
        if p.get(src): result[dst]=p[src]
    mesh=item.get('special') or {}
    if mesh:
        if mesh.get('MeshId'): result['specialMeshId']=mesh['MeshId']
        if mesh.get('MeshType'): result['specialMeshType']=mesh['MeshType']
        for src,dst in (('Scale','specialMeshScale'),('Offset','specialMeshOffset')):
            if mesh.get(src):result[dst]=[rounded(x) for x in triple(mesh[src],('X','Y','Z'))]
    return result

def source_models(archive:Path, member=MEMBER, synthetic=False):
    stack=[]; models={}; totals=Counter()
    with ZipFile(archive) as z, z.open(member) as stream:
        for event, el in etree.iterparse(stream,events=('start','end'),
                                         tag=('Item','Properties'),huge_tree=True):
            if event=='start' and el.tag=='Item':
                stack.append({'class':el.get('class'), 'name':'',
                              'properties':{},'special':None})
            elif event=='end' and el.tag=='Properties' and stack:
                current=stack[-1]
                name=el.find('./string[@name="Name"]')
                current['name']=name.text if name is not None and name.text else ''
                if (len(stack)>=4 and
                    tuple(s['name'] for s in stack[:3])==SOURCE_PREFIX):
                    current['properties']=fields(el)
                el.clear()
            elif event=='end' and el.tag=='Item':
                item=stack[-1]
                targeted=(len(stack)>=4 and
                    tuple(x['name'] for x in stack[:3])==SOURCE_PREFIX)
                if targeted and item['class']=='SpecialMesh':
                    for parent in reversed(stack[:-1]):
                        if parent['class'] in PART_CLASSES:
                            parent['special']=item['properties'];break
                if targeted and item['class'] in PART_CLASSES:
                    weapon=stack[3]['name']
                    models.setdefault(weapon,[]).append(make_part(item))
                    totals[item['class']]+=1
                if targeted and len(stack)==4 and item['class']=='Model':
                    models.setdefault(item['name'],[])
                stack.pop()
                el.clear()
                while el.getprevious() is not None:
                    del el.getparent()[0]
    if (not synthetic and (len(models)!=98 or not 1400<=sum(map(len,models.values()))<=1800)) or (synthetic and not models):
        raise ValueError('Tool hierarchy count changed; refusing to guess')
    return models,totals

def build(archive:Path,catalog:Path,output:Path,synthetic=False,member=MEMBER):
    catalog_data=json.loads(catalog.read_text(encoding='utf8'))
    active={x['name'] for x in catalog_data['weapons']}
    if len(active)!=(len(active) if synthetic else 91) or not active:
        raise ValueError('Unexpected source catalog weapon count')
    models,classes=source_models(archive,member,synthetic)
    pack={};source_model_count=len(models)
    per_model={};visible=0; hidden=0;original_assets=set(); all_parts=0
    for name in sorted(models):
        records=models[name]
        if not records:raise ValueError('Source tool has no renderable part: '+name)
        # The original models stand in a developer display area at unrelated
        # global positions. Use first source Handle as local grip origin.
        # With no Handle, use mean part location. Preserve ALL relative CFrames.
        grip=next((x['t'] for x in records if x['name']=='Handle'),None)
        if grip is None:
            grip=[sum(p['t'][i] for p in records)/len(records) for i in range(3)]
        transformed=[]
        for p in records:
            o=dict(p);o['t']=[rounded(v - base) for v,base in zip(p['t'],grip)]
            if max(abs(x) for x in o['t'])>100:
                raise ValueError('Weapon relative source position invalid '+name)
            if p['opacity']<=.001:hidden+=1
            else:visible+=1
            for k in ('meshId','specialMeshId'):
                if p.get(k):original_assets.add(p[k])
            transformed.append(o)
        per_model[name]={'parts':len(records), 'visible':sum(x['opacity']>.001 for x in records),
                         'mesh_reference_parts':sum(x['class']=='MeshPart' for x in records),
                         'grip_source':grip}
        pack[name]={'parts':transformed,
                    'original_mesh_ids':sorted({x.get('meshId') for x in records
                                               if x.get('meshId')})}
        all_parts+=len(records)
    added_aliases={}
    for source,alias in ALIASES.items():
        if source not in pack:
            if synthetic:continue
            raise ValueError('Missing exact documented model alias: '+source)
        if alias in pack:raise ValueError('Duplicate original source alias: '+alias)
        pack[alias]={'parts':pack[source]['parts'],
                     'original_mesh_ids':pack[source]['original_mesh_ids']}
        added_aliases[source]=alias
    uncovered=sorted(active-set(pack))
    if uncovered:raise ValueError('Active weapons lacking original tool assemblies: '+str(uncovered))
    with ZipFile(archive) as z,z.open(member) as source:
        sha=hashlib.sha256()
        while chunk:=source.read(1<<20):sha.update(chunk)
    if not synthetic and sha.hexdigest()!=REAL_SOURCE_SHA:
        raise ValueError('Refusing unexpected owner TestPlace source SHA-256')
    payload={
        'format':'twr-original-weapon-assemblies-v1',
        'synthetic':synthetic,
        'source':'ReplicatedStorage/Models/Tools',
        'original_rbxlx_sha256':sha.hexdigest(),
        'source_model_count':source_model_count,
        'aliases':added_aliases,
        'models':pack
    }
    raw=json.dumps(payload,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()
    output.parent.mkdir(parents=True,exist_ok=True)
    compressed=gzip.compress(raw,mtime=0,compresslevel=6)
    output.write_bytes(compressed)
    manifest={
        'format':'twr-pass47-owner-source-weapon-assemblies-v1',
        'original_rbxlx_sha256':sha.hexdigest(),
        'source_archive_sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),
        'source_model_count':source_model_count,
        'pack_model_name_count':len(pack),
        'original_source_parts':all_parts,
        'visible_source_parts':visible,
        'invisible_source_marker_parts':hidden,
        'original_parts_by_class':dict(classes),
        'catalog_weapon_count':len(active),
        'catalog_covered':len(active&set(pack)),
        'source_mesh_reference_count':len(original_assets),
        'source_mesh_triangle_bytes_recovered':False,
        'source_relative_geometry_preserved':True,
        'unmodified_original_source':True,
        'tool_pack_sha256':hashlib.sha256(compressed).hexdigest(),
        'tool_pack_uncompressed_size':len(raw),
        'per_original_model':per_model,
        'extra_source_models_not_active':sorted(set(models)-active-set(added_aliases))
    }
    (output.parent/'SOURCE_WEAPON_MODELS_MANIFEST47.json').write_text(
        json.dumps(manifest,indent=2,sort_keys=True)+'\n')
    return manifest

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--archive',type=Path,required=True)
    p.add_argument('--catalog',type=Path,required=True)
    p.add_argument('--out',type=Path,required=True)
    p.add_argument('--member',default=MEMBER)
    p.add_argument('--synthetic',action='store_true')
    a=p.parse_args()
    print(json.dumps(build(a.archive,a.catalog,a.out,a.synthetic,a.member),indent=2,sort_keys=True))

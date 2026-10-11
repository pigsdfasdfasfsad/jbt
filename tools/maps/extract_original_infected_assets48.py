#!/usr/bin/env python3
"""Owner-only original AI infected accessory extraction from inert TestPlace XML.

Reconstructs standard six-part R6 proxy bodies to mount exact source-held
accessory transforms. It DOES NOT recover authored active R6 snapshots,
external cloud mesh triangles, clothing or original animation tracks.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import gzip, hashlib, json, math
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

MEMBER='TestPlace/TestPlace.rbxlx'
SOURCE_SHA='272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2'
ROOT=('ReplicatedStorage','Assets','AI','Infected')
PARTS={'Part','MeshPart','UnionOperation','WedgePart','CornerWedgePart'}
BASE_CENTRES={'Torso':(0,0,0),'Head':(0,1.5,0),'LeftArm':(-1.5,0,0),
    'RightArm':(1.5,0,0),'LeftLeg':(-.5,-2,0),'RightLeg':(.5,-2,0)}
BASE_PARTS=[('Torso','Torso',(2,2,1)),('Head','Head',(1,1,1)),
 ('Left Arm','LeftArm',(1,2,1)),('Right Arm','RightArm',(1,2,1)),
 ('Left Leg','LeftLeg',(1,2,1)),('Right Leg','RightLeg',(1,2,1))]
I=[1,0,0,0,1,0,0,0,1]
HEAD='Generic/Models/Head Model'
HAIR=['Generic/Hair/Male/BlackHair','Generic/Hair/Male/BrownHair',
      'Generic/Hair/Female/BlackPonyTail','Generic/Hair/Female/LongBlondeHair',
      'Generic/Hair/Male/BlackMessyHair','Generic/Hair/Female/BrownAnimeHair',
      'Generic/Hair/Male/ManBunHair']
BLOATER=[('Bloater/Models/Head Model','Head'),
         ('Bloater/Models/Torso Model','Torso'),
         ('Bloater/Models/Left Arm Model','LeftArm'),
         ('Bloater/Models/Right Arm Model','RightArm')]
BURSTER=[('Burster/Models/Head Model','Head'),
         ('Burster/Models/Torso Model','Torso'),
         ('Burster/Models/Left Arm Model','LeftArm'),
         ('Burster/Models/Right Arm Model','RightArm')]
# Group combinations reflect recovered asset availability, not retail spawn odds.
PLANS={
 'Civilian':[[(HEAD,'Head'),(hair,'Head')] for hair in HAIR[:4]],
 'Sprinter':[[(HEAD,'Head'),(hair,'Head')] for hair in HAIR[4:6]],
 'Bolter':[[(HEAD,'Head'),(HAIR[6],'Head')]],
 'Military':[[(HEAD,'Head'),('Military/Military Helmet','Head')]],
 'Riot':[[(HEAD,'Head'),('Riot/Riot Helmet','Head')]],
 'Hazmat':[[('Hazmat/'+colour,'Head')]
           for colour in ('White','Yellow','Orange','Military')],
 'Bloater':[BLOATER],
 'Burster':[BURSTER],
}

def fin(n):
    n=float(n)
    if not math.isfinite(n) or abs(n)>100000:raise ValueError('Nonfinite original coordinate')
    return round(n,7)

def properties(props):
    keys={'Name','CFrame','size','Color3uint8','Color3','Transparency','Material',
          'MeshId','TextureID','TextureId','shape','CastShadow','MeshType','Offset','Scale'}
    out={}
    for e in props:
        key=e.get('name')
        if key not in keys:continue
        if key in ('CFrame','size','Color3','Offset','Scale'):
            out[key]={child.tag:(child.text or '').strip() for child in e}
        elif e.tag=='Content':out[key]=''.join(e.itertext()).strip()
        else:out[key]=(e.text or '').strip()
    return out

def vec(obj,keys=('X','Y','Z')):return [fin(obj[k]) for k in keys]

def parse_part(item):
    d=item['props']; cf=d.get('CFrame');size=d.get('size')
    if not cf or not size:raise ValueError('Original asset part missing CFrame/size')
    rotation=[fin(cf.get('R'+str(i)+str(j),1 if i==j else 0))
              for i in range(3) for j in range(3)]
    dimensions=vec(size)
    if min(dimensions)<=0 or max(dimensions)>1000:
        raise ValueError('Original source part dimension outside budget')
    packed=d.get('Color3uint8')
    if packed is not None:
        packed=int(packed);rgb=[(packed>>bits)&255 for bits in (16,8,0)]
    elif 'Color3' in d:
        rgb=[int(max(0,min(255,round(x*255))))
             for x in vec(d['Color3'],('R','G','B'))]
    else:rgb=[135,135,135]
    alpha=max(0,min(1,1-fin(d.get('Transparency',0))))
    part={'name':d.get('Name') or item['class'],'class':item['class'],
          'pos':vec(cf),'r':rotation,'s':dimensions,'rgb':rgb,
          'opacity':alpha,'mat':str(d.get('Material','272')),
          'shape':str(d.get('shape','1'))}
    if d.get('MeshId'):part['meshId']=d['MeshId']
    texture=d.get('TextureID') or d.get('TextureId')
    if texture:part['textureId']=texture
    special=item.get('special') or {}
    if special.get('MeshId'):part['specialMeshId']=special['MeshId']
    if special.get('MeshType'):part['specialMeshType']=str(special['MeshType'])
    for src,dst in (('Scale','specialMeshScale'),('Offset','specialMeshOffset')):
        if src in special:part[dst]=vec(special[src])
    return part

def extract_groups(archive:Path,member:str=MEMBER):
    result=defaultdict(list);stack=[]
    with ZipFile(archive) as z,z.open(member) as fd:
        for event,el in etree.iterparse(fd,events=('start','end'),
                                          tag=('Item','Properties'),huge_tree=True):
            if event=='start' and el.tag=='Item':
                stack.append({'class':el.get('class'),'name':'','props':{},'special':{}})
            elif event=='end' and el.tag=='Properties' and stack:
                d=properties(el)
                stack[-1]['props']=d;stack[-1]['name']=d.get('Name','')
                el.clear()
            elif event=='end' and el.tag=='Item':
                item=stack[-1];names=[x['name'] for x in stack]
                if tuple(names[:4])==ROOT:
                    if item['class']=='SpecialMesh' and len(stack)>5 and stack[-2]['class'] in PARTS:
                        stack[-2]['special']=item['props']
                    if item['class'] in PARTS:
                        result['/'.join(names[4:-1])].append(parse_part(item))
                stack.pop();el.clear()
                while el.getprevious() is not None:del el.getparent()[0]
    return result

def transpose(m):return [[m[i*3+j] for i in range(3)] for j in range(3)]
def mult_vec(m,v):return [sum(m[i][j]*v[j] for j in range(3)) for i in range(3)]
def mult(a,b):return [[sum(a[i][k]*b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]

def addon_parts(group,region,groups):
    data=groups.get(group)
    if not data:raise ValueError('Missing original infected asset group: '+group)
    anchor=next((p for p in data if p['name'] in ('Handle','Head')
                 and p['class']=='Part'),data[0])
    inverse=transpose(anchor['r']);origin=anchor['pos']
    elements=[]
    for p in data:
        if p is anchor or p['opacity']<=.001:continue
        relative=mult_vec(inverse,[p['pos'][j]-origin[j] for j in range(3)])
        rotation=mult(inverse,[p['r'][j:j+3] for j in (0,3,6)])
        baseline=BASE_CENTRES[region]
        local=[fin(baseline[j]+relative[j]) for j in range(3)]
        if any(abs(local[i]-baseline[i])>8 for i in range(3)):
            raise ValueError('Source asset too far from its original anchor')
        item={k:v for k,v in p.items() if k!='pos'}
        item.update({'region':region,'t':local,
                     'r':[fin(x) for row in rotation for x in row],
                     'source_authored':True,'source_group':group})
        elements.append(item)
    if not elements:raise ValueError('No visible components: '+group)
    return elements

def base_parts(type_name):
    colours={
        'Hazmat':([225,205,75],[190,179,55]),
        'Military':([75,99,65],[50,64,50]),
        'Riot':([45,56,69],[35,42,52]),
        'Bloater':([75,60,66],[60,48,54]),
        'Burster':([80,110,48],[65,82,39]),
        'Sprinter':([117,58,46],[64,65,63]),
        'Bolter':([107,40,33],[65,48,45])}
    shirt,pants=colours.get(type_name,([91,96,86],[59,62,57]))
    result=[]
    for name,region,dimensions in BASE_PARTS:
        rgb=([119,106,91] if region=='Head' else pants
             if region.endswith('Leg') else shirt)
        result.append({'name':name,'region':region,'class':'Part',
          't':list(BASE_CENTRES[region]),'r':I,'s':list(dimensions),
          'rgb':rgb,'opacity':1.,'mat':'272',
          'shape':'0' if region=='Head' else '1',
          'reconstructed_r6_proxy':True,'source_authored':False})
    return result

def source_sha(archive:Path,member:str):
    with ZipFile(archive) as z,z.open(member) as fd:
        h=hashlib.sha256()
        while data:=fd.read(1<<20):h.update(data)
        return h.hexdigest()

def build(archive:Path,out:Path,synthetic=False,member=MEMBER):
    digest=source_sha(archive,member)
    if not synthetic and digest!=SOURCE_SHA:
        raise ValueError('Original Roblox infected kit source SHA mismatch')
    groups=extract_groups(archive,member)
    types={};unique={};per_type=Counter()
    for type_name,plans in PLANS.items():
        variants=[]
        for group_specs in plans:
            elements=base_parts(type_name)
            for group,region in group_specs:
                for p in addon_parts(group,region,groups):
                    unique[(group,p['name'],tuple(p['t']),tuple(p['s']))]=p
                    elements.append(p)
            variants.append({'kind':'source_ai_kit_plus_r6_proxy',
                'infected_type':type_name,
                'variant_id':type_name+'-'+str(len(variants)+1),
                'original_groups':[group for group,_ in group_specs],
                'parts':elements})
            per_type[type_name]+=len(elements)
        types[type_name]=variants
    if len(types)!=8:raise ValueError('Unsupported original type coverage')
    payload={'format':'twr-source-infected-variants-v1',
             'generation':'pass48_verified_source_ai_kit_plus_explicit_r6_proxy',
             'original_rbxlx_sha256':digest,'synthetic':bool(synthetic),
             'types':types,'original_animation_tracks_restored':False,
             'original_cloud_mesh_triangles_restored':False}
    raw=json.dumps(payload,sort_keys=True,separators=(',',':')).encode()
    packed=gzip.compress(raw,mtime=0,compresslevel=6)
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_bytes(packed)
    identifiers=set()
    for p in unique.values():
        for key in ('meshId','specialMeshId'):
            if p.get(key):identifiers.add(p[key])
    manifest={
      'format':'twr-pass48-source-infected-asset-kit-v1',
      'original_rbxlx_sha256':digest,
      'source':'ReplicatedStorage/Assets/AI/Infected',
      'original_source_snapshot_rigs_restored':False,
      'standard_R6_body_positions_reconstructed_as_proxies':True,
      'original_animation_tracks_restored':False,
      'original_cloud_mesh_triangles_restored':False,
      'types_with_sourced_accessories':len(types),
      'variants_per_type':{key:len(v) for key,v in types.items()},
      'total_variants':sum(map(len,types.values())),
      'unique_original_visible_accessory_parts':len(unique),
      'unique_missing_original_mesh_ids':len(identifiers),
      'source_group_count':len({g for p in PLANS.values() for v in p for g,_ in v}),
      'source_combination_is_a_reconstruction':True,
      'compressed_pack_sha256':hashlib.sha256(packed).hexdigest(),
      'total_source_plus_proxy_part_records':sum(per_type.values()),
      'per_type_total_part_records':dict(per_type)}
    (out.parent/'SOURCE_INFECTED_ASSET_KIT_MANIFEST48.json').write_text(
        json.dumps(manifest,sort_keys=True,indent=2)+'\n')
    return manifest

if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--archive',type=Path,required=True)
    ap.add_argument('--out',type=Path,required=True)
    ap.add_argument('--member',default=MEMBER)
    ap.add_argument('--synthetic',action='store_true')
    args=ap.parse_args()
    print(json.dumps(build(args.archive,args.out,args.synthetic,args.member),indent=2))

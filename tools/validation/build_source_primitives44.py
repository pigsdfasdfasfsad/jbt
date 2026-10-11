#!/usr/bin/env python3
"""Offline source-SHA-bound native Part/Wedge renderer for original Laboratory.

Writes TWRINS28 consumed by Pass28PrimitiveStreamer. It NEVER fabricates
MeshPart/UnionOperation triangles or replaces SpecialMesh shape renderers.
--synthetic is permitted only for fabricated CI scenes.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct

SCENE_SHA = '35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74'
FORMAT = b'TWRINS28'
STUD = .28
TILE = 96

def _triplet(v, positive=False):
    if not isinstance(v,list) or len(v)!=3: raise ValueError('Invalid source vector')
    result=tuple(float(x) for x in v)
    if any(not math.isfinite(x) or abs(x)>100000 or (positive and x<.001)
           for x in result):
        raise ValueError('Nonfinite or out-of-budget source dimension')
    return result

def to_godot_transform(rec):
    pos=_triplet(rec['t'])
    size=_triplet(rec['s'],True)
    r=rec['r']
    if not isinstance(r,list) or len(r)!=9:raise ValueError('Bad source rotation')
    r=tuple(float(x) for x in r)
    if any(not math.isfinite(v) or abs(v)>10000 for v in r):
        raise ValueError('Invalid source orientation')
    sx,sy,sz=(v*STUD for v in size)
    # Identical handedness and CFrame columns as LaboratorySourceLoader:
    transform=(r[0]*sx,r[3]*sx,-r[6]*sx,
               r[1]*sy,r[4]*sy,-r[7]*sy,
               -r[2]*sz,-r[5]*sz,r[8]*sz,
               pos[0]*STUD,pos[1]*STUD,-pos[2]*STUD)
    if not all(math.isfinite(v) and abs(v)<=100000 for v in transform):
        raise ValueError('Native Godot transform outside runtime budget')
    return transform

def classify(rec):
    cls=rec.get('class')
    if cls not in ('Part','WedgePart'):return None,'custom-or-nonnative'
    if rec.get('shape','1')!='1':return None,'non-brick-native-shape'
    # SphereMesh/SpecialMesh/file-mesh shapes stay in the older renderer.
    if rec.get('specialMeshType') not in (None,'') or 'specialMeshOffset' in rec:
        return None,'special-mesh-preserved-in-fallback'
    opacity=float(rec.get('opacity',1))
    if not math.isfinite(opacity) or not 0<=opacity<=1:
        raise ValueError('Source transparency outside [0,1]')
    if opacity<=.001:return None,'invisible'
    alpha=max(0,min(255,round(opacity*255)))
    if not alpha:return None,'alpha-quantizes-to-zero'
    material=int(rec.get('mat','0'))
    if not 0<=material<=65535:raise ValueError('Bad source material')
    rgb=rec.get('rgb')
    if not isinstance(rgb,list) or len(rgb)!=3 or any(
        not isinstance(c,int) or isinstance(c,bool) or not 0<=c<=255 for c in rgb):
        raise ValueError('Bad source RGB')
    return (int(cls=='WedgePart'),material,alpha,*rgb,
            int(bool(rec.get('shadow',False)))),None

def extract(scene:Path,out:Path,synthetic=False):
    scene_bytes=Path(scene).read_bytes()
    source_sha=hashlib.sha256(scene_bytes).hexdigest()
    if not synthetic and source_sha!=SCENE_SHA:
        raise ValueError('Exact original Laboratory scene SHA mismatch')
    if len(scene_bytes)>32*1024*1024:raise ValueError('Compressed scene too large')
    original=gzip.decompress(scene_bytes)
    if len(original)>64*1024*1024:raise ValueError('Uncompressed scene too large')
    lines=original.splitlines()
    header=json.loads(lines[0])
    if (header.get('format')!='twr-source-map-v2' or
        header.get('map')!='Laboratory' or
        bool(header.get('synthetic',False))!=synthetic):
        raise ValueError('Source identity mismatch')
    tiles=defaultdict(lambda:defaultdict(list))
    counts=Counter();rejected=Counter()
    for line in lines[1:]:
        record=json.loads(line)
        if record.get('kind')!='geometry':continue
        counts['source_geometry_records']+=1
        key,reason=classify(record)
        if key is None:
            rejected[reason]+=1
            continue
        matrix=to_godot_transform(record)
        pos=_triplet(record['t'])
        tile=(math.floor(pos[0]/TILE),math.floor(pos[2]/TILE))
        if max(abs(tile[0]),abs(tile[1]))>16000:
            raise ValueError('Source tile coordinate too large')
        tiles[tile][key].append(matrix)
        counts['rendered_native_instances']+=1
        counts['native_wedges' if key[0] else 'native_parts']+=1
    counts['tiles']=len(tiles)
    counts['batches']=sum(len(v) for v in tiles.values())
    if not 1<=counts['tiles']<=512 or not 1<=counts['rendered_native_instances']<=100000 or counts['batches']>8000:
        raise ValueError('Original source exceeds native renderer budgets')
    raw=bytearray(FORMAT)
    raw+=struct.pack('<III',1,TILE,counts['rendered_native_instances'])
    raw+=bytes.fromhex(source_sha)+struct.pack('<I',counts['tiles'])
    for (tx,tz),batches in sorted(tiles.items()):
        raw+=struct.pack('<iiI',tx,tz,len(batches))
        for group,instances in sorted(batches.items()):
            if len(instances)>20000:raise ValueError('Excessive source batch')
            kind,mat,alpha,r,g,b,shadow=group
            raw+=struct.pack('<BHB3BBI',kind,mat,alpha,r,g,b,shadow,len(instances))
            for transform in instances:raw+=struct.pack('<12f',*transform)
    out=Path(out);out.parent.mkdir(parents=True,exist_ok=True)
    zipped=gzip.compress(bytes(raw),compresslevel=6,mtime=0)
    out.write_bytes(zipped)
    manifest={
        'format':'twr-pass44-native-primitive-cache-manifest-v1',
        'map':'Laboratory',
        'original_source_scene_sha256':source_sha,
        'native28_sha256':hashlib.sha256(zipped).hexdigest(),
        'synthetic_fixture':synthetic,
        'original_custom_mesh_triangles_recovered':False,
        'original_special_meshes_preserved_in_legacy_proxy':True,
        'source_collision_cache_unchanged':True,
        'original_geometry_count':counts['source_geometry_records'],
        'native_instances':counts['rendered_native_instances'],
        'native_parts':counts['native_parts'],
        'native_wedges':counts['native_wedges'],
        'tiles':counts['tiles'],
        'batches':counts['batches'],
        'excluded_original_geometry':dict(sorted(rejected.items()))
    }
    (out.parent/'LABORATORY_NATIVE_RENDER_MANIFEST44.json').write_text(
        json.dumps(manifest,indent=2,sort_keys=True)+'\n')
    return manifest

def read_pack(path:Path):
    """Independent strict decoder of existing Godot TWRINS28 v1 layout."""
    packed=gzip.decompress(Path(path).read_bytes())
    if packed[:8]!=FORMAT or len(packed)<56:
        raise ValueError('Invalid TWRINS28 header')
    version,tile,declared=struct.unpack_from('<III',packed,8)
    if (version,tile)!=(1,96) or not 1<=declared<=100000:
        raise ValueError('Invalid source primitive header')
    digest=packed[20:52].hex()
    tile_count=struct.unpack_from('<I',packed,52)[0]
    if not 1<=tile_count<=512:raise ValueError('Invalid source tile count')
    offset=56
    batches=instances=0
    for _ in range(tile_count):
        tx,tz,count=struct.unpack_from('<iiI',packed,offset);offset+=12
        if max(abs(tx),abs(tz))>16000 or count>8000:
            raise ValueError('Invalid source tile')
        for _ in range(count):
            kind,mat,alpha,r,g,b,shadow,total=struct.unpack_from('<BHB3BBI',packed,offset)
            offset+=12
            if kind>1 or alpha==0 or shadow>1 or not 1<=total<=20000:
                raise ValueError('Invalid source draw batch')
            for _ in range(total):
                matrix=struct.unpack_from('<12f',packed,offset);offset+=48
                if not all(math.isfinite(v) and abs(v)<=100000 for v in matrix):
                    raise ValueError('Invalid source draw transform')
            batches+=1
            instances+=total
    if offset!=len(packed) or instances!=declared:
        raise ValueError('Corrupt or trailing source primitive bytes')
    return {'source_sha256':digest,'instances':instances,
            'batches':batches,'tiles':tile_count}

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--scene',required=True,type=Path)
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--synthetic',action='store_true')
    args=parser.parse_args()
    manifest=extract(args.scene,args.out,args.synthetic)
    checked=read_pack(args.out)
    assert checked['instances']==manifest['native_instances']
    print(json.dumps(manifest,sort_keys=True,indent=2))

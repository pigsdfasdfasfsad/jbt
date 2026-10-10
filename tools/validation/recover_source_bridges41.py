#!/usr/bin/env python3
"""Add only short native-Part-supported source bridges to Pass40 Laboratory.

Uses original owner scene + SHA-bound Pass40 TWRNAV31, does NOT recover the
retail Roblox navmesh, custom triangle geometry or missing SmoothGrid terrain.
Every new short edge has 9 source-native floor and body-clearance checks.
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
import numpy as np
from shapely.geometry import Point
from shapely.strtree import STRtree
import recover_source_navigation40 as source40

SCENE_SHA='35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74'
BASE_SHA='b021b905a0a87619997a0e2da08a4c7b2db18ecd2a5250f93dfd84d08cb26bdc'
MAX_BRIDGE_STUDS=8.0
MAX_VERTICAL_STUDS=2.2
MAX_NODES=125000
MAX_EDGES=350000

def read_graph(scene:Path,nav:Path,allow_synthetic=False):
    scene_bytes=scene.read_bytes()
    scene_digest=hashlib.sha256(scene_bytes).hexdigest()
    nav_bytes=nav.read_bytes()
    nav_digest=hashlib.sha256(nav_bytes).hexdigest()
    if not allow_synthetic and (scene_digest!=SCENE_SHA or nav_digest!=BASE_SHA):
        raise ValueError('Pass41 source scene or Pass40 base navigation SHA mismatch')
    decoded=gzip.decompress(nav_bytes)
    if decoded[:8]!=b'TWRNAV31':
        raise ValueError('Unsupported base navigation magic')
    version,node_count,edge_count=struct.unpack_from('<Iii',decoded,8)
    if version!=2 or not 1<=node_count<=MAX_NODES or not 0<=edge_count<=MAX_EDGES:
        raise ValueError('Unsupported base graph dimensions')
    if decoded[20:52]!=bytes.fromhex(scene_digest):
        raise ValueError('Base nav graph not bound to owner scene bytes')
    if len(decoded)!=52+12*node_count+8*edge_count:
        raise ValueError('Base navigation framing corrupt')
    points=np.frombuffer(decoded,dtype='<f4',count=3*node_count,offset=52).reshape((-1,3)).copy()
    edges=np.frombuffer(decoded,dtype='<i4',count=2*edge_count,
                        offset=52+12*node_count).reshape((-1,2)).copy()
    if not np.isfinite(points).all() or np.max(np.abs(points))>=100000:
        raise ValueError('Invalid source waypoint positions')
    if ((edges<0).any() or (edges>=node_count).any() or
            (edges[:,0]==edges[:,1]).any()):
        raise ValueError('Invalid original source graph connection')
    return scene_digest,nav_digest,points,edges

def components(count,edges):
    parent=list(range(count))
    def find(k):
        while parent[k]!=k:
            parent[k]=parent[parent[k]]
            k=parent[k]
        return k
    for a,b in edges:
        ra,rb=find(int(a)),find(int(b))
        if ra!=rb:parent[rb]=ra
    return [find(i) for i in range(count)]

def native_floor_support(x,z,feet,tree,surfaces):
    point=Point(float(x),float(z))
    return any(not surfaces[int(idx)].approximate and
        abs(surfaces[int(idx)].height-feet)<=1.3 and
        surfaces[int(idx)].polygon.covers(point)
        for idx in tree.query(point))

def bridge_clear(a,b,floor_tree,surfaces,collision_tree,blockers):
    if abs(float(a[1]-b[1]))>MAX_VERTICAL_STUDS:return False
    if np.linalg.norm(a-b)>MAX_BRIDGE_STUDS+0.0001:return False
    for k in range(1,10):
        x,y,z=a+(b-a)*(k/10)
        if not native_floor_support(x,z,y,floor_tree,surfaces):
            return False
        if source40.blocked(x,z,y,collision_tree,blockers):
            return False
    return True

def bridge_candidates(points,labels,floor_tree,surfaces,collision_tree,blockers):
    # Spatial grid avoids the scipy dependency and O(N^2) neighbor searches.
    buckets=defaultdict(list)
    for i,point in enumerate(points):
        buckets[(round(float(point[0])/3.5),
                 round(float(point[2])/3.5))].append(i)
    best_by_original_component_pair={}
    examined=set()
    counts=Counter()
    for cell,ids in sorted(buckets.items()):
        for dx in (-2,-1,0,1,2):
            for dz in (-2,-1,0,1,2):
                for j in buckets.get((cell[0]+dx,cell[1]+dz),()):
                    for i in ids:
                        if j<=i or labels[i]==labels[j]:
                            continue
                        edge=(i,j)
                        if edge in examined:
                            continue
                        examined.add(edge)
                        if abs(float(points[i,1]-points[j,1]))>MAX_VERTICAL_STUDS:
                            continue
                        distance=float(np.linalg.norm(points[i]-points[j]))
                        if distance>MAX_BRIDGE_STUDS+0.0001:
                            continue
                        counts['distance_bounded_cross_component_pairs']+=1
                        if not bridge_clear(points[i],points[j],floor_tree,surfaces,
                                            collision_tree,blockers):
                            counts['rejected_unsupported_or_obstructed']+=1
                            continue
                        counts['validated_native_floor_pairs']+=1
                        pair=tuple(sorted((labels[i],labels[j])))
                        score=(round(distance,6),i,j)
                        if (pair not in best_by_original_component_pair or
                                score<best_by_original_component_pair[pair]):
                            best_by_original_component_pair[pair]=score
    bridges=sorted([(a,b) for _,a,b in best_by_original_component_pair.values()])
    counts['distinct_component_links']=len(bridges)
    return bridges,counts

def generate(scene:Path,base_nav:Path,output:Path,allow_synthetic=False):
    scene_digest,base_digest,points,edges=read_graph(scene,base_nav,allow_synthetic)
    # Never execute or interpret Luau; only offline source geometry records.
    with gzip.open(scene,'rt',encoding='utf-8') as handle:
        header=json.loads(next(handle))
        if header.get('format')!='twr-source-map-v2' or header.get('map')!='Laboratory':
            raise ValueError('Unexpected scene map identity')
        if bool(header.get('synthetic',False))!=allow_synthetic:
            raise ValueError('Synthetic fixture/source policy mismatch')
        rows=[json.loads(line) for line in handle]
    surfaces,blockers,_=source40.source_geometry(rows)
    labels=components(len(points),edges)
    surface_tree=STRtree([s.polygon for s in surfaces])
    collider_tree=STRtree([c.polygon for c in blockers])
    bridges,counts=bridge_candidates(points,labels,surface_tree,surfaces,
                                     collider_tree,blockers)
    if not allow_synthetic and (len(bridges)<1 or len(bridges)>100):
        raise ValueError('Unexpected source-native bridge count')
    joined=np.asarray(bridges,dtype=np.int32).reshape((-1,2))
    combined=np.concatenate([edges,joined],axis=0)
    if len(combined)>MAX_EDGES:
        raise ValueError('Bridge repairs exceed nav loader budget')
    previous_components=len(set(labels))
    next_components=len(set(components(len(points),combined)))
    if next_components>previous_components or next_components<previous_components-len(bridges):
        raise ValueError('Invalid graph repair connectivity')
    data=bytearray(b'TWRNAV41')
    data+=struct.pack('<Iii',3,len(points),len(combined))
    data+=bytes.fromhex(scene_digest)
    for xyz in points:
        data+=struct.pack('<fff',*xyz)
    for a,b in combined:
        data+=struct.pack('<ii',int(a),int(b))
    zipped=gzip.compress(bytes(data),mtime=0,compresslevel=6)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_bytes(zipped)
    report={'format':'twr-pass41-source-native-bridge-repairs-v1',
        'source_scene_sha256':scene_digest,
        'pass40_nav_sha256':base_digest,
        'pass41_nav_sha256':hashlib.sha256(zipped).hexdigest(),
        'authoritative_roblox_navmesh':False,
        'missing_meshes_and_terrain_still_unresolved':True,
        'original_waypoints_unchanged':True,
        'all_original_edges_preserved':True,
        'sampled_floor_support':'original Part planes only; no custom mesh/CSG proxies',
        'maximum_bridge_studs':MAX_BRIDGE_STUDS,
        'bridge_interior_physics_samples':9,
        'points':len(points),
        'original_edges':len(edges),
        'additional_bridges':len(bridges),
        'new_edges':len(combined),
        'previous_components':previous_components,
        'remaining_components':next_components,
        'counts':dict(counts),
        'additional_edge_indices':bridges}
    manifest=output.parent/'LABORATORY_NATIVE_BRIDGES_MANIFEST41.json'
    manifest.write_text(json.dumps(report,sort_keys=True,indent=2)+'\n',
                        encoding='utf8')
    return report

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--scene',required=True,type=Path)
    p.add_argument('--base',required=True,type=Path)
    p.add_argument('--out',required=True,type=Path)
    p.add_argument('--synthetic',action='store_true')
    a=p.parse_args()
    print(json.dumps(generate(a.scene,a.base,a.out,
                              allow_synthetic=a.synthetic),indent=2))

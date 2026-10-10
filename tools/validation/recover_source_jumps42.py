#!/usr/bin/env python3
"""Source-bound, conservative jump edges for the owner's Laboratory scene.

Original Pathfind ModuleScript declares AgentCanJump=true, AgentCanClimb=false
and WaypointSpacing=4. Source data lacks the retail navmesh and original
custom triangle meshes/terrain. Test collision using original-positioned
native Part floor faces and nine intermediate 3D body-clearance samples.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import gzip
import hashlib
import json
from pathlib import Path
import struct
import numpy as np
from shapely.geometry import Point
from shapely.strtree import STRtree
import recover_source_navigation40 as source40
import recover_source_bridges41 as source41

SCENE_SHA='35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74'
BASE_SHA='12ac9bae602cbdea3ae6a6789289d1068f1c8efd5d603de34d6186698c375dd0'
MAX_JUMP_LENGTH_STUDS=8.0
MAX_JUMP_VERTICAL_STUDS=3.5
MIN_HORIZONTAL_STUDS=2.4

def read_graph(scene,base,allow_synthetic=False):
    compressed=scene.read_bytes();scene_sha=hashlib.sha256(compressed).hexdigest()
    original=base.read_bytes();base_sha=hashlib.sha256(original).hexdigest()
    if not allow_synthetic and (scene_sha!=SCENE_SHA or base_sha!=BASE_SHA):
        raise ValueError('Source Laboratory scene or Pass41 bridge SHA mismatch')
    unpacked=gzip.decompress(original)
    if len(unpacked)<52 or unpacked[:8]!=b'TWRNAV41':
        raise ValueError('Unexpected predecessor navigation format')
    version,n,e=struct.unpack_from('<Iii',unpacked,8)
    if version!=3 or n<1 or n>125000 or e<1 or e>350000 or len(unpacked)!=52+n*12+e*8:
        raise ValueError('Bad source predecessor navigation shape')
    if unpacked[20:52]!=bytes.fromhex(scene_sha):
        raise ValueError('Predecessor navigation not bound to original source scene')
    pts=np.frombuffer(unpacked,dtype='<f4',offset=52,count=n*3).reshape((-1,3)).copy()
    edges=np.frombuffer(unpacked,dtype='<i4',offset=52+n*12,count=e*2).reshape((-1,2)).copy()
    if not np.isfinite(pts).all() or (np.abs(pts)>100000).any():
        raise ValueError('Invalid original waypoint')
    if (edges<0).any() or (edges>=n).any() or (edges[:,0]==edges[:,1]).any():
        raise ValueError('Invalid source predecessor edge')
    return scene_sha,base_sha,pts,edges

def native_support(x,y,z,tree,surfaces):
    point=Point(float(x),float(z))
    return any(not surfaces[int(k)].approximate and
               abs(surfaces[int(k)].height-y)<=1.3 and
               surfaces[int(k)].polygon.covers(point)
               for k in tree.query(point))

def arc_clear(a,b,collision_tree,blockers):
    displacement=b-a
    horizontal=float(np.linalg.norm(displacement[[0,2]]))
    if not MIN_HORIZONTAL_STUDS<=horizontal<=MAX_JUMP_LENGTH_STUDS+1e-5:
        return False
    if abs(float(displacement[1]))>MAX_JUMP_VERTICAL_STUDS:
        return False
    if float(np.linalg.norm(displacement))>MAX_JUMP_LENGTH_STUDS+1e-5:
        return False
    rise=max(3.8,abs(float(displacement[1]))+2.0)
    for t in (.1,.2,.3,.4,.5,.6,.7,.8,.9):
        x,y,z=a+displacement*t
        y+=rise*4*t*(1-t)
        if source40.blocked(x,z,y,collision_tree,blockers):
            return False
    return True

def validated_jump_pairs(points,labels,floors,blockers):
    native_tree=STRtree([s.polygon for s in floors])
    collision_tree=STRtree([b.polygon for b in blockers])
    is_native=[native_support(x,y,z,native_tree,floors) for x,y,z in points]
    cells=defaultdict(list)
    for i,(x,y,z) in enumerate(points):
        if is_native[i]:
            cells[(round(float(x)/3.5),round(float(z)/3.5))].append(i)
    best={};counts=Counter()
    for (bx,bz),ids in sorted(cells.items()):
        for dx in range(-3,4):
            for dz in range(-3,4):
                for j in cells.get((bx+dx,bz+dz),()):
                    for i in ids:
                        if j<=i or labels[i]==labels[j]:
                            continue
                        a,b=points[i],points[j]
                        if abs(float(a[1]-b[1]))>MAX_JUMP_VERTICAL_STUDS:
                            continue
                        distance=float(np.linalg.norm(a-b))
                        if distance>MAX_JUMP_LENGTH_STUDS+1e-5 or distance<MIN_HORIZONTAL_STUDS:
                            continue
                        counts['cross_component_near_pairs']+=1
                        if not arc_clear(a,b,collision_tree,blockers):
                            counts['rejected_by_swept_body']+=1
                            continue
                        counts['physically_bounded_jump_arc_pairs']+=1
                        key=tuple(sorted((labels[i],labels[j])))
                        score=(round(distance,6),i,j)
                        if key not in best or score<best[key]:
                            best[key]=score
    links=sorted((i,j) for _,i,j in best.values())
    counts['distinct_connected_component_pairs']=len(links)
    counts['native_supported_waypoints']=sum(is_native)
    return links,counts

def build(scene:Path,base:Path,out:Path,synthetic=False):
    scene_sha,base_sha,points,walk_edges=read_graph(scene,base,synthetic)
    with gzip.open(scene,'rt',encoding='utf8') as fd:
        head=json.loads(next(fd))
        if (head.get('format')!='twr-source-map-v2' or
            head.get('map')!='Laboratory' or
            bool(head.get('synthetic'))!=synthetic):
            raise ValueError('Source/synthetic identity rejected')
        rows=[json.loads(line) for line in fd]
    surfaces,blockers,_=source40.source_geometry(rows)
    labels=source41.components(len(points),walk_edges)
    jumps,stats=validated_jump_pairs(points,labels,surfaces,blockers)
    if not synthetic and not 1<=len(jumps)<=64:
        raise ValueError('Wrong number of source-validated jump links')
    original_set={tuple(sorted(map(int,edge))) for edge in walk_edges}
    if any(e in original_set for e in jumps):
        raise ValueError('Jump overlaps original reconstructed walk edge')
    all_edges=[(int(a),int(b),0) for a,b in walk_edges]
    all_edges.extend((int(a),int(b),1) for a,b in jumps)
    if len(all_edges)>350000:
        raise ValueError('Total original navigation edge budget exceeded')
    before=len(set(labels))
    joined=np.asarray([(a,b) for a,b,_ in all_edges],dtype=np.int32).reshape((-1,2))
    after=len(set(source41.components(len(points),joined)))
    if before-after>len(jumps) or after>before:
        raise ValueError('Source jump graph produced invalid components')
    blob=bytearray(b'TWRNAV42')
    blob+=struct.pack('<Iii',4,len(points),len(all_edges))
    blob+=bytes.fromhex(scene_sha)
    for x,y,z in points:
        blob+=struct.pack('<fff',float(x),float(y),float(z))
    for a,b,action in all_edges:
        blob+=struct.pack('<iiB',a,b,action)
    out.parent.mkdir(parents=True,exist_ok=True)
    packed=gzip.compress(bytes(blob),mtime=0,compresslevel=6)
    out.write_bytes(packed)
    spawns=[r['t'] for r in rows if r.get('kind')=='spawn' and r.get('side')=='player']
    before_size=after_size=0
    if spawns:
        x,y,z=spawns[0]
        target=np.asarray([x,y,z])
        index=int(np.argmin(np.sum((points-target)**2,axis=1)))
        previous=labels[index]
        before_size=int(sum(component==previous for component in labels))
        newer=source41.components(len(points),joined)
        after_size=int(sum(component==newer[index] for component in newer))
    report={
        'format':'twr-pass42-owner-source-jump-links-v1',
        'owner_scene_sha256':scene_sha,
        'pass41_base_sha256':base_sha,
        'pass42_nav_sha256':hashlib.sha256(packed).hexdigest(),
        'recovered_original_roblox_navmesh':False,
        'original_jump_capability_source_verified':True,
        'original_climb_capability':False,
        'original_waypoint_spacing_studs':4,
        'original_waypoints_unchanged':True,
        'original_walk_edges_unchanged':True,
        'jump_links_approximate':True,
        'missing_meshes_or_terrain_restored':False,
        'max_jump_link_studs':MAX_JUMP_LENGTH_STUDS,
        'max_height_difference_studs':MAX_JUMP_VERTICAL_STUDS,
        'arc_intermediate_checks':9,
        'walk_waypoints':len(points),
        'previous_walk_edges':len(walk_edges),
        'new_jump_edges':len(jumps),
        'all_edges':len(all_edges),
        'components_before':before,
        'components_after':after,
        'player_start_component_size_before':before_size,
        'player_start_component_size_after':after_size,
        'metrics':dict(stats),
        'new_jump_edge_indices':jumps}
    (out.parent/'LABORATORY_JUMP_LINKS_MANIFEST42.json').write_text(
        json.dumps(report,indent=2,sort_keys=True)+'\n')
    return report

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--scene',required=True,type=Path)
    p.add_argument('--base',required=True,type=Path)
    p.add_argument('--out',required=True,type=Path)
    p.add_argument('--synthetic',action='store_true')
    args=p.parse_args()
    print(json.dumps(build(args.scene,args.base,args.out,args.synthetic),indent=2))

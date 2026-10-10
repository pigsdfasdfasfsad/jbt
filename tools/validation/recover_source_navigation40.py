#!/usr/bin/env python3
"""Build conservative, offline, source-bound Laboratory navigation for TWR.

Only authored, walkable horizontal Part/CSG bounding surfaces and authored
collision bounds are available. MeshPart/UnionOperation triangles are missing;
this graph is approximated and must NOT be called original Roblox navmesh.

Output is existing TWRNAV31 v2 bytes, accepted by the current Godot runtime.
Nodes are emitted in Roblox studs, with SHA-256 of the *compressed* source
scene. No Roblox networking, asset downloads or Luau execution.
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
from typing import NamedTuple
import numpy as np
from shapely.geometry import Point, Polygon, box
from shapely.strtree import STRtree

STUD=0.28
GRID=3.5
BODY_RADIUS=0.95
BODY_LOW=0.35
BODY_HIGH=5.6
FLOOR_STEP=2.20
LIMIT_NODES=125000
LIMIT_EDGES=350000
SOURCE_SHA='35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74'

class Surface(NamedTuple):
    polygon:Polygon
    height:float
    source_index:int
    approximate:bool

class Collider(NamedTuple):
    polygon:Polygon
    min_y:float
    max_y:float
    source_index:int


def bounds(row):
    r=np.asarray(row['r'],dtype=np.float64).reshape(3,3)
    center=np.asarray(row['t'],dtype=np.float64)
    size=np.asarray(row['s'],dtype=np.float64)
    if not (np.isfinite(r).all() and np.isfinite(center).all() and np.isfinite(size).all()) or np.min(size)<.001:
        raise ValueError('Invalid source geometry')
    offsets=np.array([[x,y,z] for x in (-.5,.5) for y in (-.5,.5) for z in (-.5,.5)])*size
    corners=offsets@r.T+center
    outline=Polygon(corners[:,[0,2]]).convex_hull
    if not outline.is_valid or outline.is_empty:
        raise ValueError('Invalid source oriented bounding rectangle')
    return outline, float(corners[:,1].min()),float(corners[:,1].max())


def top_surface(row,index):
    # Horizontally walkable upper side of original collision volume. Sloped
    # wedges and custom mesh boundaries cannot be validated with certainty;
    # restrict to approximately horizontal, reasonably wide, thin slabs.
    r=np.asarray(row['r'],dtype=np.float64).reshape(3,3)
    sx,sy,sz=row['s']
    if abs(r[1,1])<.965 or min(sx,sz)<2.5 or sy>max(3.,.22*min(sx,sz)):
        return None
    if row['class']=='WedgePart' or str(row.get('shape','1'))!='1':
        return None
    surface_half=np.array([[-sx/2,0,-sz/2],[-sx/2,0,sz/2],
                           [sx/2,0,sz/2],[sx/2,0,-sz/2]],dtype=np.float64)
    surface_half[:,1]=sy/2 if r[1,1]>0 else -sy/2
    corners=surface_half@r.T+np.asarray(row['t'],dtype=np.float64)
    polygon=Polygon(corners[:,[0,2]]).convex_hull
    if polygon.is_empty or polygon.area < 6: return None
    topy=float(np.mean(corners[:,1]))
    if float(np.ptp(corners[:,1]))>.8:return None
    return Surface(polygon,topy,index,row['class'] in ('MeshPart','UnionOperation'))


def load_scene(path:Path,allow_synthetic=False):
    packed=path.read_bytes()
    scene_sha=hashlib.sha256(packed).hexdigest()
    if not allow_synthetic and scene_sha!=SOURCE_SHA:
        raise ValueError('Original source Laboratory scene SHA mismatch')
    with gzip.open(path,'rt',encoding='utf8') as f:
        header=json.loads(next(f))
        if header.get('format')!='twr-source-map-v2' or header.get('map')!='Laboratory' or (header.get('synthetic') is not bool(allow_synthetic)):
            raise ValueError('Unexpected original Laboratory scene header')
        rows=[json.loads(line) for line in f]
    return scene_sha,rows


def source_geometry(rows):
    surfaces=[];blockers=[];stats=Counter()
    for idx,row in enumerate(rows):
        if row.get('kind')=='geometry':
            if not row.get('collidable'):continue
        elif row.get('kind')!='collision':continue
        outline,ymin,ymax=bounds(row)
        blockers.append(Collider(outline,ymin,ymax,idx))
        stats['collision_bounds']+=1
        if row['kind']=='geometry':
            surface=top_surface(row,idx)
            if surface:
                surfaces.append(surface)
                stats['walkable_surface_patches']+=1
                if surface.approximate:stats['proxy_surface_patches']+=1
    return surfaces,blockers,stats


def grid_samples(surfaces):
    # Quantize only horizontal X/Z. Preserve separate floor bands in each
    # cell; do not collapse a second floor over the same x/z coordinates.
    samples=defaultdict(list)
    added=0
    for surface in surfaces:
        minx,minz,maxx,maxz=surface.polygon.bounds
        safe_footprint=surface.polygon.buffer(-.55)
        for i in range(math.ceil(minx/GRID),math.floor(maxx/GRID)+1):
            x=i*GRID
            for j in range(math.ceil(minz/GRID),math.floor(maxz/GRID)+1):
                z=j*GRID
                point=Point(x,z)
                if not safe_footprint.covers(point):continue
                heights=samples[(i,j)]
                # Overlapping thin floor coverings: keep the upper walkable
                # surface instead of spawning two nearly coincident floors.
                if any(abs(entry[0]-surface.height)<1.1 for entry in heights):
                    for k,(h,origin,proxy) in enumerate(heights):
                        if abs(h-surface.height)<1.1 and h<surface.height:
                            heights[k]=(surface.height,surface.source_index,surface.approximate)
                    continue
                heights.append((surface.height,surface.source_index,surface.approximate))
                added+=1
                if added>LIMIT_NODES*3:
                    raise ValueError('Source navigation grid sample budget exceeded')
    return samples


def blocked(x,z,feet,tree,colliders,exclude=None):
    point=Point(x,z);query=box(x-BODY_RADIUS,z-BODY_RADIUS,x+BODY_RADIUS,z+BODY_RADIUS)
    for c_index in tree.query(query):
        part=colliders[int(c_index)]
        if part.source_index==exclude:continue
        if part.max_y<=feet+BODY_LOW or part.min_y>=feet+BODY_HIGH:continue
        if part.polygon.distance(point)<BODY_RADIUS:
            return True
    return False


def supported(x,z,height,floor_tree,surfaces):
    point=Point(x,z)
    for idx in floor_tree.query(point):
        surface=surfaces[int(idx)]
        if abs(surface.height-height)<=1.3 and surface.polygon.covers(point):
            return True
    return False


def generate(scene:Path,output:Path,allow_synthetic=False,grid_size=GRID):
    if abs(grid_size-GRID)>1e-7:raise ValueError('TWRNAV31 graph spacing is fixed at 3.5 studs')
    digest,rows=load_scene(scene,allow_synthetic)
    surfaces,colliders,stats=source_geometry(rows)
    if not surfaces:raise ValueError('No verifiable walkable source planes')
    tree=STRtree([b.polygon for b in colliders]);floor_tree=STRtree([f.polygon for f in surfaces])
    raw_samples=grid_samples(surfaces)
    stats['sample_candidates']=sum(map(len,raw_samples.values()))
    # Actual node format Roblox [x,y,z] in source studs; coordinates are
    # converted to metres and Z-reflected once by Pass25SourceNavigationRuntime.
    nodes=[];cell_indices=defaultdict(list)
    for (i,j),heights in sorted(raw_samples.items()):
        for top_y,source_index,approx in sorted(heights):
            x,z=i*GRID,j*GRID
            if blocked(x,z,top_y,tree,colliders,source_index):
                stats['blocked_samples']+=1
                continue
            cell_indices[(i,j)].append(len(nodes))
            nodes.append((x,round(top_y,5),z,source_index,approx,i,j))
            if len(nodes)>LIMIT_NODES:raise ValueError('Source nav node budget exceeded')
    if len(nodes)<16:raise ValueError('Too few physically supported Lab cells')
    stats['accepted_nodes']=len(nodes)
    edges=set();tested=0
    # Check the height and colliders between nodes. The graph has NO
    # unsupported long-range teleports, no invented ladder/door traversal.
    for index,node in enumerate(nodes):
        x,y,z,source,approx,i,j=node
        for dx,dz in [(1,0),(0,1),(1,1),(1,-1)]:
            for other in cell_indices.get((i+dx,j+dz),[]):
                nx,ny,nz,_,_,_,_=nodes[other]
                if abs(y-ny)>FLOOR_STEP:continue
                valid=True
                for fraction in (.25,.5,.75):
                    xx=x+(nx-x)*fraction;zz=z+(nz-z)*fraction
                    height=y+(ny-y)*fraction
                    if not supported(xx,zz,height,floor_tree,surfaces) or blocked(xx,zz,height,tree,colliders):
                        valid=False;break
                tested+=1
                if not valid:
                    stats['rejected_edge_samples']+=1
                    continue
                edges.add((index,other))
                if len(edges)>LIMIT_EDGES:raise ValueError('Source graph edge budget exceeded')
    stats['accepted_edges']=len(edges)
    stats['tested_edges']=tested
    stats['proxy_supported_nodes']=sum(n[4] for n in nodes)
    stats['isolated_nodes']=len(nodes)-len({v for e in edges for v in e})
    if len(edges)<12:raise ValueError('Too few safe Laboratory graph edges')
    parents=list(range(len(nodes)))
    def root(n):
        while parents[n]!=n:
            parents[n]=parents[parents[n]];n=parents[n]
        return n
    for i,j in edges:
        a,b=root(i),root(j)
        if a!=b:parents[b]=a
    components=Counter(root(i) for i in range(len(nodes)))
    stats['connected_components']=len(components)
    stats['largest_component']=max(components.values())
    # Never assume a source player/infected marker can stand on recovered
    # geometry; report 3D distance and reachability rather than inventing links.
    world_nodes=np.asarray([(x*STUD,y*STUD,-z*STUD) for x,y,z,*_ in nodes],dtype=np.float64)
    spawn_records=[]
    for r in rows:
        if r.get('kind')!='spawn':continue
        pos=r['t']
        delta=world_nodes-np.asarray((pos[0]*STUD,pos[1]*STUD,-pos[2]*STUD))
        distances=np.linalg.norm(delta,axis=1)
        node_id=int(np.argmin(distances));dist=float(distances[node_id])
        spawn_records.append({'side':r['side'],'name':r.get('name',''),
                'distance_to_nearest_waypoint_m':round(float(dist),3),
                'connected_component_size':components[root(int(node_id))] if dist<=5 else 0,
                'source_anchor_within_5m':bool(dist<=5)})
    stats['original_player_markers_near_graph']=sum(x['source_anchor_within_5m'] for x in spawn_records if x['side']=='player')
    stats['original_infected_markers_near_graph']=sum(x['source_anchor_within_5m'] for x in spawn_records if x['side']=='infected')
    blob=bytearray(b'TWRNAV31')
    blob.extend(struct.pack('<Iii',2,len(nodes),len(edges)))
    blob.extend(bytes.fromhex(digest))
    for x,y,z,*_ in nodes:blob.extend(struct.pack('<fff',x,y,z))
    for i,j in sorted(edges):blob.extend(struct.pack('<ii',i,j))
    output.parent.mkdir(parents=True,exist_ok=True)
    packed=gzip.compress(bytes(blob),mtime=0,compresslevel=6)
    output.write_bytes(packed)
    report={'format':'twr-pass40-approximated-laboratory-navigation-v1',
            'scene_sha256':digest,'nav_sha256':hashlib.sha256(packed).hexdigest(),
            'real_source_geometry':not allow_synthetic,
            'walkable_surfaces_approximate':True,
            'original_roblox_navmesh_recovered':False,
            'terrain_navigation_missing':True,
            'meshpart_csg_geometry_missing':True,
            'no_invented_portals_or_links':True,
            'graph_version':'TWRNAV31-v2',
            'statistics':dict(stats), 'source_spawn_reachability':spawn_records}
    (output.parent/'LABORATORY_NAVIGATION_MANIFEST40.json').write_text(json.dumps(report,sort_keys=True,indent=2)+'\n')
    return report

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--scene',required=True,type=Path)
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--synthetic',action='store_true')
    args=parser.parse_args()
    print(json.dumps(generate(args.scene,args.out,allow_synthetic=args.synthetic),indent=2))

#!/usr/bin/env python3
"""Render a deterministic top-down diagnostic PNG from static map IR.
Requires Pillow only when invoked; checked-in recovered report previews do not
need regeneration during ordinary validation.
"""
from __future__ import annotations
import argparse,gzip,json,math
from pathlib import Path

def xyz(props):
    cf=props.get('CFrame') or props.get('CoordinateFrame') or {}
    try:return float(cf.get('X')),float(cf.get('Y')),float(cf.get('Z'))
    except:return None

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('ir',type=Path); ap.add_argument('output',type=Path); ap.add_argument('--size',type=int,default=1600); ns=ap.parse_args()
    try: from PIL import Image,ImageDraw
    except ImportError: raise SystemExit('Pillow is required only for preview rendering')
    points=[]
    with gzip.open(ns.ir,'rt',encoding='utf-8') as f:
        header=json.loads(next(f))
        for line in f:
            r=json.loads(line); p=xyz(r.get('properties',{}))
            if p: points.append((r,p))
    if not points: raise SystemExit('IR has no positioned instances')
    xs=[p[0] for _,p in points]; zs=[p[2] for _,p in points]; pad=30
    xmin,xmax=min(xs),max(xs); zmin,zmax=min(zs),max(zs); span=max(xmax-xmin,zmax-zmin,1)
    im=Image.new('RGB',(ns.size,ns.size),'white'); d=ImageDraw.Draw(im)
    def xy(x,z): return (pad+(x-xmin)/span*(ns.size-2*pad), pad+(z-zmin)/span*(ns.size-2*pad))
    for r,p in points:
        props=r.get('properties',{}); name=r.get('name',''); cls=r.get('class','')
        x,y=xy(p[0],p[2]); radius=1
        if cls in {'Part','MeshPart','UnionOperation','WedgePart','TrussPart'}:
            d.ellipse((x-radius,y-radius,x+radius,y+radius),fill='black')
        lname=name.lower()
        if 'spawn' in lname or 'objective' in lname or 'fort' in lname or 'item' in lname:
            d.rectangle((x-3,y-3,x+3,y+3),outline='black',width=1)
    ns.output.parent.mkdir(parents=True,exist_ok=True); im.save(ns.output,optimize=True)
    print(f'rendered {len(points)} positioned records from {header.get("source_name")}')
if __name__=='__main__':main()

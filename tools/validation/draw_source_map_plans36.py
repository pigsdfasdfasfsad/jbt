#!/usr/bin/env python3
"""Top-down outlines of original server-wall footprints; NOT authentic map art."""
from __future__ import annotations
from pathlib import Path
import argparse,gzip,json,math
from PIL import Image,ImageDraw,ImageFont
MAPS=('Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Manor')


def decode(filename):
    with gzip.open(filename,'rt',encoding='utf8') as f:return [json.loads(line) for line in f]


def footprint(row):
    x,_,z=row['t']; sx,_,sz=row['s'];r=row['r']; pts=[]
    for u,v in [(-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5)]:
        px=x+r[0]*u*sx+r[2]*v*sz
        pz=-(z+r[6]*u*sx+r[8]*v*sz)
        pts.append((px,pz))
    return pts


def make(path,objects,dest):
    rows=decode(path);header=rows[0];parts=rows[1:]
    objs=decode(objects);native=[o for o in objs[1:] if o.get('geometry_status')=='native_primitive']
    allp=[p for part in parts for p in footprint(part)]
    xmin=min(x for x,y in allp); xmax=max(x for x,y in allp)
    ymin=min(y for x,y in allp); ymax=max(y for x,y in allp)
    w,h=1400,1080;padx,pady=100,190;aw=w-2*padx; ah=h-pady-85
    spanx=max(1,xmax-xmin);spany=max(1,ymax-ymin)
    scale=min(aw/spanx,ah/spany)
    mx=(xmin+xmax)/2;my=(ymin+ymax)/2
    def pixel(point):return (round(w/2+(point[0]-mx)*scale),round(pady+ah/2+(point[1]-my)*scale))
    img=Image.new('RGB',(w,h),(17,25,37));d=ImageDraw.Draw(img)
    for i in range(0,11):
        x=padx+i*aw/10;y=pady+i*ah/10
        d.line([(int(x),pady),(int(x),pady+ah)],fill=(37,48,62),width=1)
        d.line([(padx,int(y)),(padx+aw,int(y))],fill=(37,48,62),width=1)
    for row in parts:
        points=[pixel(p) for p in footprint(row)]
        col=(115,176,196) if row['class'] in ('Part','WedgePart') else (225,143,81)
        d.polygon(points,outline=col)
    for row in native:
        x,_,z=row['t'];a,b=pixel((x,-z)); d.ellipse((a-2,b-2,a+2,b+2),fill=(235,212,112))
    try:
        f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',23)
        f2=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',38)
    except OSError:f=ImageFont.load_default();f2=f
    m=header['map']
    d.text((padx,43),m.upper()+'  /  ORIGINAL SERVER WALL FOOTPRINTS',font=f2,fill=(239,245,248))
    d.text((padx,103),'Extracted from owner TestPlace.rbxlx | CMaps source fragment, NOT a complete visual map',font=f,fill=(179,193,207))
    d.text((padx,147),f"{len(parts):,} source wall parts  |  {header['approximated_collision_meshes']} approximate MeshPart box proxies  |  {len(native)} native source props",font=f,fill=(156,196,213))
    d.text((padx,h-56),f"X span: {spanx:,.1f} Roblox studs     Z span: {spany:,.1f} studs",font=f,fill=(168,184,197))
    d.text((padx+840,h-56),'walls  /  mesh proxies  /  native props',font=f,fill=(222,219,176))
    dest.parent.mkdir(parents=True,exist_ok=True);img.save(dest,optimize=True)
    return dest

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--source',type=Path,required=True);ap.add_argument('--out',type=Path,required=True)
    args=ap.parse_args()
    for m in MAPS:
        file=args.out/(m+'.sourcefragment36.png')
        make(args.source/(m+'.serverwalls36.jsonl.gz'),args.source/(m+'.objects36.jsonl.gz'),file)
        print(m,file.stat().st_size)
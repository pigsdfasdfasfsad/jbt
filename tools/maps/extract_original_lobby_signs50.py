#!/usr/bin/env python3
"""Extract static original lobby SurfaceGui headings, never old player ranks.

Exact authored TextLabel contents, nested UDim2 positions, source Adornee
CFrames and source panel dimensions. Original stale player names/scores and
external image textures remain excluded from the owner-private source pack.
"""
from __future__ import annotations
import argparse
from collections import Counter,defaultdict
import gzip,hashlib,json,math
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

MEMBER='TestPlace/TestPlace.rbxlx'
SOURCE_SHA='272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2'
LOBBY49_SHA='98a8bf71c0561be814b2de01831b48e413e349ed4925fab3c0b8a403d7845b49'
ALLOWED_PAGES={'level','kills','wavesSurvived','donatedRobux'}
FOCUS='BulletinBoard/SurfaceGuis/'
SELECTED_COUNT=34
PANEL_COUNT=9

def propdata(el):
    allowed={'Name','Text','TextSize','TextScaled','TextColor3','TextTransparency',
       'Position','Size','AnchorPoint','Visible','CanvasSize','Face','Adornee',
       'CFrame','size','Transparency','TextXAlignment','TextYAlignment',
       'Enabled','BackgroundTransparency','TextStrokeTransparency'}
    result={}
    for sub in el:
        name=sub.get('name')
        if name not in allowed:continue
        result[name]=(sub.text.strip() if sub.text and sub.text.strip() else
            {child.tag:(child.text or '').strip() for child in sub} if len(sub) else '')
    return result

def number(value,fallback=0):
    v=float(value if value not in ('',None) else fallback)
    if not math.isfinite(v) or abs(v)>100000:
        raise ValueError('Nonfinite/unbounded source GUI value')
    return v

def v3(cf,keys=('X','Y','Z')):
    return [number(cf[k]) for k in keys]

def frame(properties):
    cf=properties['CFrame']
    return {'t':v3(cf),'r':[number(cf.get(f'R{i}{j}',int(i==j)))
            for i in range(3) for j in range(3)],'s':v3(properties['size'])}

def udim2(props,key):
    value=props.get(key)
    if not isinstance(value,dict):return (0.,0.,0.,0.)
    return tuple(number(value.get(k,0)) for k in ('XS','XO','YS','YO'))

def scaled_rect(props,parent):
    x,y,w,h=parent
    xs,xo,ys,yo=udim2(props,'Size')
    sx,ox,sy,oy=udim2(props,'Position')
    width=w*xs+xo
    height=h*ys+yo
    if width<=0 or height<=0:return None
    anchor=props.get('AnchorPoint',{})
    ax=number(anchor.get('X','0')) if isinstance(anchor,dict) else 0
    ay=number(anchor.get('Y','0')) if isinstance(anchor,dict) else 0
    return (x+w*sx+ox-ax*width,y+h*sy+oy-ay*height,width,height)

def wanted(text,name,path):
    if name=='Title':return True
    return (path.startswith(FOCUS+'Personal/Main/') and name not in ('Value','Title')
        and text.upper()==text and any(c.isalpha() for c in text) and len(text)<=30)

def summarize(source:Path,member=MEMBER):
    stack=[];refs={};surface={};candidates=[];counts=Counter();start=None
    with ZipFile(source) as archive,archive.open(member) as stream:
        for ev,el in etree.iterparse(stream,events=('start','end'),
                                      tag=('Item','Properties'),huge_tree=True):
            if ev=='start' and el.tag=='Item':
                stack.append({'cls':el.get('class'),'name':'',
                              'props':{},'ref':el.get('referent')})
            elif ev=='end' and el.tag=='Properties' and stack:
                d=propdata(el)
                stack[-1]['props']=d
                stack[-1]['name']=d.get('Name') if isinstance(d.get('Name'),str) else ''
                el.clear()
            elif ev=='end' and el.tag=='Item':
                item=stack[-1];names=[x['name'] for x in stack]
                if names[:2]==['Workspace','Lobby']:
                    cls=item['cls'];path='/'.join(names[2:]);p=item['props']
                    if(cls in ('Part','MeshPart','UnionOperation','WedgePart','CornerWedgePart')
                       and item['ref'] and 'CFrame' in p and 'size' in p):
                        refs[item['ref']]=(path,frame(p))
                        if path=='CamPoints/Start':start=v3(p['CFrame'])
                    if cls=='SurfaceGui':
                        counts['original_gui_panels']+=1
                        surface[item['ref']]={'path':path,'props':p}
                    if cls=='TextLabel':
                        counts['original_text_labels']+=1
                        index=next((i for i in range(len(stack)-2,-1,-1)
                                    if stack[i]['cls']=='SurfaceGui'),None)
                        if index is not None:
                            text=p.get('Text','')
                            if isinstance(text,str) and text.strip() and wanted(
                                    text,item['name'],path):
                                candidates.append({
                                    'path':path,'name':item['name'],
                                    'text':text.strip(),
                                    'gui_ref':stack[index]['ref'],
                                    'props':p,
                                    'parents':[x['props'] for x in stack[index+1:-1]
                                               if x['cls'] in ('Frame','ScrollingFrame',
                                                               'TextButton','TextLabel')]
                                })
                stack.pop();el.clear()
                while el.getprevious() is not None:del el.getparent()[0]
    if start is None:raise ValueError('Original Lobby Start anchor unavailable')
    panels=defaultdict(lambda:{'headings':[]})
    rejected=[]
    for item in candidates:
        gui=surface.get(item['gui_ref'])
        if gui is None or not gui['path'].startswith(FOCUS):continue
        anchor=refs.get(gui['props'].get('Adornee',''))
        if anchor is None:raise ValueError('Original SurfaceGui Adornee unresolved')
        if str(gui['props'].get('Face',''))!='5':
            raise ValueError('Source panel has unsupported Face')
        c=gui['props'].get('CanvasSize')
        if not isinstance(c,dict):raise ValueError('Missing original CanvasSize')
        cw,ch=(number(c[k]) for k in ('X','Y'))
        if cw<=0 or ch<=0 or max(cw,ch)>2000:
            raise ValueError('Invalid source SurfaceGui dimensions')
        rect=(0.,0.,cw,ch)
        for ancestor in item['parents']:
            next_rect=scaled_rect(ancestor,rect)
            if next_rect is not None:rect=next_rect
        textrect=scaled_rect(item['props'],rect)
        if textrect is None:
            rejected.append(item['path']);continue
        px,py,pw,ph=textrect
        ux=(px+pw*.5)/cw-.5
        uy=.5-(py+ph*.5)/ch
        if not -2<ux<2 or not -2<uy<2:
            raise ValueError('Original heading outside credible source panel')
        properties=item['props']
        rgb=properties.get('TextColor3',{})
        if not isinstance(rgb,dict):rgb={}
        color=[max(0,min(255,round(number(rgb.get(k,1))*255)))
               for k in ('R','G','B')]
        label={'text':item['text'],'source_path':item['path'],
               'uv':[round(ux,6),round(uy,6)],
               'font_px':round(max(12,min(90,number(properties.get('TextSize',36))))),
               'rgb':color,
               'transparency':round(max(0,min(1,number(
                   properties.get('TextTransparency',0)))),5)}
        panel_id=gui['path']
        panel=panels[panel_id]
        panel.update({
          'source_gui_path':panel_id,
          'original_adornee_path':anchor[0],
          'source_part_frame':anchor[1],
          'canvas':[int(cw),int(ch)],
          'page':next((p for p in ALLOWED_PAGES
                       if f'Global/Slides/{p}' in panel_id),'always')
        })
        panel['headings'].append(label)
    if rejected:raise ValueError(f'Unsupported original headings: {rejected}')
    entries=[]
    for name in sorted(panels):
        panel=panels[name]
        panel['source_part_frame']['t']=[
            round(x-y,7) for x,y in zip(panel['source_part_frame']['t'],start)]
        entries.append(panel)
    return counts,entries,start

def build(archive:Path,output:Path,synthetic=False,member=MEMBER):
    h=hashlib.sha256()
    with ZipFile(archive) as z,z.open(member) as f:
        while chunk:=f.read(1<<20):h.update(chunk)
    source_sha=h.hexdigest()
    if not synthetic and source_sha!=SOURCE_SHA:
        raise ValueError('Original source XML SHA mismatch')
    counts,panels,origin=summarize(archive,member)
    total=sum(len(p['headings']) for p in panels)
    if not synthetic:
        if(len(panels)!=PANEL_COUNT or total!=SELECTED_COUNT or
           counts['original_gui_panels']!=26 or counts['original_text_labels']!=872):
            raise ValueError('Recovered bulletin heading counts changed')
    elif not panels or not total:
        raise ValueError('Synthetic bulletin headings missing')
    payload={
        'format':'twr-pass50-source-lobby-signs-v1',
        'synthetic':bool(synthetic),'original_rbxlx_sha256':source_sha,
        'lobby49_sha256':LOBBY49_SHA if not synthetic else 'synthetic',
        'source_surfacegui_count':counts['original_gui_panels'],
        'source_textlabel_count':counts['original_text_labels'],
        'historic_snapshots_intentionally_excluded':True,
        'panels':panels
    }
    raw=json.dumps(payload,sort_keys=True,separators=(',',':')).encode()
    if len(raw)>512*1024:raise ValueError('Lobby sign payload too large')
    compressed=gzip.compress(raw,mtime=0,compresslevel=6)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_bytes(compressed)
    manifest={
        'format':'twr-pass50-private-source-signs-manifest-v1',
        'original_rbxlx_sha256':source_sha,
        'pass49_source_pack_sha256':LOBBY49_SHA,
        'compressed_pack_sha256':hashlib.sha256(compressed).hexdigest(),
        'panel_count':len(panels),'static_source_headings':total,
        'original_surfaceguis_total':counts['original_gui_panels'],
        'original_textlabels_total':counts['original_text_labels'],
        'original_textlabels_excluded':counts['original_text_labels']-total,
        'original_text_snapshot_scores_preserved_as_live':False,
        'source_parent_adornee_transforms_restored':True,
        'board_slide_pages':sorted(ALLOWED_PAGES),
        'original_picture_texture_bytes_recovered':False,
        'original_roblox_gui_rendering_system_restored':False,
        'source_origin_studs':origin,'compressed_size':len(compressed)
    }
    (output.parent/'SOURCE_LOBBY_SIGNS_MANIFEST50.json').write_text(
        json.dumps(manifest,indent=2,sort_keys=True)+'\n')
    return manifest

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--archive',required=True,type=Path)
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--member',default=MEMBER)
    parser.add_argument('--synthetic',action='store_true')
    args=parser.parse_args()
    print(json.dumps(build(args.archive,args.out,args.synthetic,args.member),indent=2))

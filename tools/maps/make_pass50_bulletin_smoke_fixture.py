#!/usr/bin/env python3
"""Wholly fabricated 9-panel 34-heading bulletin pack for Windows CI.

Does not contain saved user names, past leaderboard rankings or owner XML
artifacts. Install together with the synthetic Pass49 3D lobby fixture.
"""
from __future__ import annotations
import argparse,gzip,json
from pathlib import Path

PAGES=('level','kills','wavesSurvived','donatedRobux')
I=[1.,0.,0.,0.,1.,0.,0.,0.,1.]
PERSONAL=['PERSONAL STATS:','INFECTED KILLED:','CIVILIAN','RIOT','MILITARY',
 'BLOATER','BOLTER','SPRINTER','BURSTER','HAZMAT','JUGGERNAUT','LEVEL:',
 'WAVES SURVIVED:','HEADSHOT','LONG RANGE','EXPLOSIVE','FIRE','BARBED WIRE',
 'DECAPITATION','SPECIAL KILLS','REVIVES:','ASSISTS:']
PAGE_HEADS=['HIGHEST LEVELS','MOST KILLS','WAVES SURVIVED','AMOUNT DONATED']
FIELDS=['Time Left:','Challenge:','Prize:']
PERSONAL_WEEK=['Ranking:','Prize:','Kills:']

def make_panel(name,page,labels,index):
    pos=[index*.6,1.,-7.]
    size=[3.,2.,.2] if page=='always' else [.8,1.2,.2]
    return {
      'source_gui_path':'fabricated/'+name,
      'original_adornee_path':'fabricated/FakeBoard/'+name,
      'source_part_frame':{'t':pos,'r':I,'s':size},
      'canvas':[1000,650] if page=='always' else [240,360],
      'page':page,
      'headings':[{
          'source_path':'fabricated/'+name+'/'+str(i),
          'text':text,
          'uv':[0.,round(.43-(i+.5)/max(1,len(labels))*.7,6)],
          'font_px':28 if i else 36,
          'rgb':[32,31,29],
          'transparency':0
      } for i,text in enumerate(labels)]
    }

def fixture():
    panels=[make_panel('Personal','always',PERSONAL,0)]
    for i,(page,title) in enumerate(zip(PAGES,PAGE_HEADS)):
        panels.append(make_panel('Global'+page,page,[title],1))
    panels += [
        make_panel('WeeklyInfo','always',FIELDS,2),
        make_panel('WeeklyMain','always',['WEEKLY LEADERBOARD:'],3),
        make_panel('WeeklyPersonal','always',PERSONAL_WEEK,4),
        make_panel('Update','always',['LAST UPDATED'],5)
    ]
    assert len(panels)==9
    assert sum(len(p['headings']) for p in panels)==34
    return {
        'format':'twr-pass50-source-lobby-signs-v1',
        'synthetic':True,
        'original_rbxlx_sha256':'synthetic-ci-no-owner-snapshot',
        'lobby49_sha256':'synthetic',
        'source_surfacegui_count':26,
        'source_textlabel_count':872,
        'historic_snapshots_intentionally_excluded':True,
        'panels':panels
    }

def create(path:Path):
    if path.exists():raise FileExistsError('Refusing to overwrite original sign pack')
    path.parent.mkdir(parents=True,exist_ok=True)
    encoded=json.dumps(fixture(),sort_keys=True,separators=(',',':')).encode()
    path.write_bytes(gzip.compress(encoded,mtime=0,compresslevel=6))
    print('TWR_PASS50_SYNTHETIC_CREATED source_boards=9 static_heading_count=34 old_users_excluded=838')

def clean(path:Path):
    if not path.exists():return
    data=json.loads(gzip.decompress(path.read_bytes()))
    if (data.get('format')!='twr-pass50-source-lobby-signs-v1' or
        data.get('synthetic') is not True or
        data.get('original_rbxlx_sha256')!='synthetic-ci-no-owner-snapshot' or
        len(data.get('panels',[]))!=9):
        raise ValueError('Refusing to delete non-synthetic source bulletin')
    path.unlink()
    print('TWR_PASS50_SYNTHETIC_CLEANED')

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output',type=Path,required=True)
    m=p.add_mutually_exclusive_group(required=True)
    m.add_argument('--create',action='store_true')
    m.add_argument('--clean',action='store_true')
    args=p.parse_args()
    (create if args.create else clean)(args.output)

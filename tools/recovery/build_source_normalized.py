#!/usr/bin/env python3
"""Statically normalize a small set of recovered TWR Luau data modules.

This parser never executes Luau. It only reads literal source text and extracts
known literal tables required by the standalone reconstruction.
"""
from __future__ import annotations
import argparse, hashlib, json, re
from pathlib import Path

TYPE_NAMES = ["Bolter","Civilian","Sprinter","Military","Hazmat","Riot","Burster","Bloater","Juggernaut"]

def sha256(path: Path) -> str:
    h=hashlib.sha256();
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024), b''): h.update(b)
    return h.hexdigest()

def brace_block(text: str, token: str, start: int=0) -> str:
    i=text.find(token,start)
    if i < 0: raise ValueError(f'missing token {token!r}')
    o=text.find('{',i)
    if o < 0: raise ValueError(f'missing opening brace after {token!r}')
    depth=0; quote=None; esc=False
    for j in range(o,len(text)):
        c=text[j]
        if quote:
            if esc: esc=False
            elif c=='\\': esc=True
            elif c==quote: quote=None
            continue
        if c in "\"'": quote=c; continue
        if c=='{': depth+=1
        elif c=='}':
            depth-=1
            if depth==0: return text[o:j+1]
    raise ValueError(f'unclosed block {token!r}')

def num(block: str, key: str):
    m=re.search(rf'\b{re.escape(key)}\s*=\s*(-?\d+(?:\.\d+)?)\s*;',block)
    return float(m.group(1)) if m and '.' in m.group(1) else (int(m.group(1)) if m else None)

def infected(src: Path) -> dict:
    text=src.read_text(encoding='utf-8-sig',errors='strict')
    stats=brace_block(text,'Stats =')
    out=[]
    for name in TYPE_NAMES:
        b=brace_block(stats, f'{name} =')
        hum=brace_block(b,'Humanoid =')
        manip=brace_block(b,'Manipulators =')
        entry={
            'name':name,
            'credits':num(b,'Credits'),'xp':num(b,'EXP'),'damage':num(b,'Damage'),
            'walk_speed':num(hum,'WalkSpeed'),'jump_power':num(hum,'JumpPower'),
            'health':num(hum,'Health'),'max_health':num(hum,'MaxHealth'),
            'manipulators':{k:num(manip,k) for k in ('Fire','Melee','Smoke')},
            'release_spawn_allowed': name != 'Juggernaut',
        }
        for k in ('BurstRadius','BurstDamage','BurstAreaDamage','SporeRadius','SporeDamage'):
            v=num(b,k)
            if v is not None: entry[k[0].lower()+k[1:]]=v
        out.append(entry)
    return {
        'format':'twr-infected-source-normalization-v1',
        'source_file':src.name,'source_sha256':sha256(src),
        'authority':'recovered original TWR script','static_only':True,
        'release_policy_note':'Juggernaut source data is retained for provenance but release_spawn_allowed is false.',
        'infected':out,
    }

def fortifications(src: Path) -> dict:
    text=src.read_text(encoding='utf-8-sig',errors='strict')
    result=[]
    for name, token in [('50 Cal','["50 Cal"] ='),('Barbed Wire','["Barbed Wire"] ='),('Clap Bomb','["Clap Bomb"] ='),('Jack','Jack =')]:
        b=brace_block(text, token)
        e={'name':name}
        for k in ('Count','Swings','Damage','Radius','MaxPen'):
            v=num(b,k)
            if v is not None: e[k[0].lower()+k[1:]]=v
        for k in ('Icon','Sound','FireSound'):
            m=re.search(rf'\b{re.escape(k)}\s*=\s*"([^"]+)"\s*;',b)
            if m: e[k[0].lower()+k[1:]]=m.group(1)
        result.append(e)
    return {'format':'twr-fortification-source-normalization-v1','source_file':src.name,'source_sha256':sha256(src),'authority':'recovered original TWR script','static_only':True,'fortifications':result}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--scripts-dir',type=Path,required=True)
    ap.add_argument('--out-dir',type=Path,required=True)
    ns=ap.parse_args(); ns.out_dir.mkdir(parents=True,exist_ok=True)
    inf=ns.scripts_dir/'ModuleScript.Infected Info.Source.txt'
    fort=ns.scripts_dir/'ModuleScript.FortificationsInfo.Source.txt'
    (ns.out_dir/'infected_info.json').write_text(json.dumps(infected(inf),indent=2,sort_keys=True)+'\n',encoding='utf-8')
    (ns.out_dir/'fortifications_info.json').write_text(json.dumps(fortifications(fort),indent=2,sort_keys=True)+'\n',encoding='utf-8')
    print('normalized recovered script tables')
if __name__=='__main__': main()

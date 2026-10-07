#!/usr/bin/env python3
"""Static Roblox XML place parser.

The parser treats Script/LocalScript/ModuleScript Source as inert text. It does
not import, evaluate, execute, or interpret Luau. Input may be a .rbxlx file or
a ZIP containing .rbxlx members. Output is deterministic JSONL.GZ IR.
"""
from __future__ import annotations
import argparse, gzip, hashlib, io, json, zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

SCRIPT_CLASSES={"Script","LocalScript","ModuleScript"}

def scalar(el:ET.Element):
    tag=el.tag.rsplit('}',1)[-1]
    txt=el.text or ''
    if tag=='bool': return txt.strip().lower()=='true'
    if tag in {'int','int64'}:
        try:return int(txt.strip())
        except:return txt
    if tag in {'float','double'}:
        try:return float(txt.strip())
        except:return txt
    if tag in {'string','ProtectedString','BinaryString','SharedString','token','Ref'}: return txt
    if tag=='Content':
        child=next(iter(el),None); return (child.text if child is not None else txt) or ''
    if tag in {'Vector3','Vector2','Color3','CoordinateFrame','CFrame','UDim','UDim2','Rect2D','PhysicalProperties'}:
        out={}
        for c in el:
            key=c.tag.rsplit('}',1)[-1]
            out[key]=scalar(c)
        return out
    if len(el):
        out={}
        for c in el:
            key=c.attrib.get('name') or c.tag.rsplit('}',1)[-1]
            val=scalar(c)
            if key in out:
                if not isinstance(out[key],list): out[key]=[out[key]]
                out[key].append(val)
            else: out[key]=val
        return out
    stripped=txt.strip()
    try:
        if stripped and any(ch in stripped for ch in '.eE'): return float(stripped)
        if stripped and stripped.lstrip('-').isdigit(): return float(stripped)
    except ValueError:
        pass
    return txt

def item_properties(item:ET.Element):
    p=item.find('./Properties')
    if p is None:
        # tolerate namespaces
        p=next((c for c in item if c.tag.rsplit('}',1)[-1]=='Properties'),None)
    out={}
    if p is not None:
        for el in p:
            name=el.attrib.get('name') or el.tag.rsplit('}',1)[-1]
            out[name]=scalar(el)
    return out

def parse_bytes(data:bytes, source_name:str):
    root=ET.fromstring(data)
    records=[]
    def walk(item:ET.Element,parent_ref:str|None,parent_path:str):
        cls=item.attrib.get('class','Unknown'); ref=item.attrib.get('referent','')
        props=item_properties(item); name=str(props.get('Name') or cls)
        path=parent_path+'/'+name.replace('/','_')
        record={'class':cls,'ref':ref,'parent_ref':parent_ref,'name':name,'path':path,'properties':props}
        if cls in SCRIPT_CLASSES and 'Source' in props:
            record['script_source_inert']=True
        records.append(record)
        for child in item:
            if child.tag.rsplit('}',1)[-1]=='Item': walk(child,ref,path)
    for child in root:
        if child.tag.rsplit('}',1)[-1]=='Item': walk(child,None,'')
    return records

def input_members(path:Path, member:str|None):
    if zipfile.is_zipfile(path):
        with zipfile.ZipFile(path) as z:
            names=[member] if member else sorted(n for n in z.namelist() if n.lower().endswith('.rbxlx'))
            for n in names:
                if not n or n not in z.namelist(): raise SystemExit(f'ZIP member not found: {n}')
                yield n,z.read(n)
    else: yield path.name,path.read_bytes()

def write_ir(records,out:Path,source_name:str,source_sha:str):
    out.parent.mkdir(parents=True,exist_ok=True)
    with gzip.GzipFile(filename=str(out),mode='wb',mtime=0) as raw:
        with io.TextIOWrapper(raw,encoding='utf-8',newline='\n') as f:
            f.write(json.dumps({'record_type':'header','format':'twr-static-rbxlx-ir-v1','source_name':source_name,'source_sha256':source_sha,'instance_count':len(records),'scripts_executed':False},sort_keys=True)+'\n')
            for r in records: f.write(json.dumps(r,sort_keys=True,ensure_ascii=False,separators=(',',':'))+'\n')

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('input',type=Path); ap.add_argument('--member'); ap.add_argument('--output',type=Path,required=True); ns=ap.parse_args()
    members=list(input_members(ns.input,ns.member))
    if len(members)!=1 and ns.output.suffixes[-2:] == ['.jsonl','.gz']:
        raise SystemExit('Multiple .rbxlx members require --member or an output directory')
    for name,data in members:
        rec=parse_bytes(data,name); sha=hashlib.sha256(data).hexdigest()
        out=ns.output if len(members)==1 else ns.output/(Path(name).stem+'.jsonl.gz')
        write_ir(rec,out,name,sha); print(f'{name}: {len(rec)} instances -> {out}')
if __name__=='__main__': main()

#!/usr/bin/env python3
"""Normalize exported RemoteSpy capture text/JSON into transport-neutral records.

No Roblox networking is recreated. The output is evidence used to design typed
local commands/events.
"""
from __future__ import annotations
import argparse,json,re,hashlib
from pathlib import Path
CALL=re.compile(r'(?P<method>FireServer|InvokeServer|Fire|Invoke)\s*\(\s*["\'](?P<action>[^"\']+)["\']')

def flatten_json(obj,src,out):
    if isinstance(obj,dict):
        method=str(obj.get('method') or obj.get('Method') or obj.get('call') or '')
        action=obj.get('action') or obj.get('Action') or obj.get('name') or obj.get('Name')
        if method and action: out.append({'source':src,'method':method,'action':str(action),'raw_kind':'json'})
        for v in obj.values(): flatten_json(v,src,out)
    elif isinstance(obj,list):
        for v in obj: flatten_json(v,src,out)

def parse(path:Path):
    text=path.read_text(encoding='utf-8',errors='replace'); out=[]
    try: flatten_json(json.loads(text),path.name,out)
    except Exception: pass
    for m in CALL.finditer(text): out.append({'source':path.name,'method':m.group('method'),'action':m.group('action'),'raw_kind':'literal_text'})
    return out

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('inputs',nargs='+',type=Path); ap.add_argument('--output',type=Path,required=True); ns=ap.parse_args()
    calls=[]
    for p in ns.inputs:
        if p.is_dir():
            for q in sorted(x for x in p.rglob('*') if x.is_file()): calls.extend(parse(q))
        else: calls.extend(parse(p))
    for i,c in enumerate(calls): c['ordinal']=i
    summary={}
    for c in calls:
        k=(c['method'],c['action']); summary[k]=summary.get(k,0)+1
    result={'format':'twr-remotes-normalized-v1','transport':'evidence only; local standalone uses typed in-process commands/events','call_count':len(calls),'actions':[{'method':k[0],'action':k[1],'count':v} for k,v in sorted(summary.items())],'calls':calls}
    ns.output.parent.mkdir(parents=True,exist_ok=True); ns.output.write_text(json.dumps(result,indent=2,sort_keys=True)+'\n',encoding='utf-8')
    print(f'normalized {len(calls)} calls across {len(summary)} method/action pairs')
if __name__=='__main__':main()

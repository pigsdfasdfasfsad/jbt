#!/usr/bin/env python3
"""Compile deterministic public runtime/reconstruction content from normalized evidence.

This tool only consumes checked-in normalized evidence/documentation. It never
executes recovered Luau and never mutates immutable source archives.
"""
from __future__ import annotations
import argparse, gzip, hashlib, json, re, shutil
from pathlib import Path

MAPS=("Ranch","Mill","Bypass","Cabin","Cargo","District","Expressway","Prison","Laboratory","Manor")
WEAPON_SLOTS={"Primary","Secondary","Melee"}

def slug(s:str)->str:
    s=re.sub(r'[^a-z0-9]+','-',s.lower()).strip('-')
    return s or 'unnamed'

def load(p:Path):
    if p.suffix == '.gz':
        with gzip.open(p, 'rt', encoding='utf-8') as f: return json.load(f)
    return json.loads(p.read_text(encoding='utf-8'))
def dump(p:Path,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(obj,indent=2,sort_keys=True,ensure_ascii=False)+'\n',encoding='utf-8')

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--repo-root',type=Path,default=Path(__file__).resolve().parents[2])
    ns=ap.parse_args(); root=ns.repo_root.resolve(); evidence=root/'evidence'; out=root/'content'

    wc=load(evidence/'catalog/weapon_catalog.json.gz')
    active=[]; equipment=[]
    entries=out/'weapons/entries'; entries.mkdir(parents=True,exist_ok=True)
    expected=set()
    for rec in sorted(wc,key=lambda x:x['name'].casefold()):
        slot=(rec.get('stats') or {}).get('Slot')
        kind='weapon' if slot in WEAPON_SLOTS else 'equipment'
        normalized={'format':'twr-weapon-normalized-v1','kind':kind,'source_file':rec.get('file'),'name':rec['name'],'stats':rec.get('stats',{}),'animation_durations':rec.get('animation_durations',{}),'animations':rec.get('animations',{})}
        path=entries/(slug(rec['name'])+'.json'); dump(path,normalized); expected.add(path.name)
        (active if kind=='weapon' else equipment).append({'name':rec['name'],'slot':slot,'path':f'entries/{path.name}'})
    for p in entries.glob('*.json'):
        if p.name not in expected: p.unlink()
    dump(out/'weapons/catalog.json',{'format':'twr-weapon-catalog-v1','source':'evidence/catalog/weapon_catalog.json.gz','active_weapon_count':len(active),'equipment_count':len(equipment),'weapons':active,'equipment':equipment})

    expanded=load(evidence/'catalog/expanded_shared_tables.json')
    perk_table=expanded['Perks']['tables']['returned_table']['value']
    perks=[]
    for category,vals in perk_table.items():
        for name,data in vals.items(): perks.append({'category':category,'name':name,**data})
    dump(out/'perks/perks.json',{'format':'twr-perks-v1','source':expanded['Perks']['source'],'source_sha256':expanded['Perks']['sha256'],'count':len(perks),'perks':perks})

    infected=load(evidence/'recovery/infected_info.json')
    active_infected=[x for x in infected['infected'] if x['release_spawn_allowed']]
    excluded=[x for x in infected['infected'] if not x['release_spawn_allowed']]
    dump(out/'infected/infected.json',{'format':'twr-infected-runtime-v1','source':infected['source_file'],'source_sha256':infected['source_sha256'],'active_count':len(active_infected),'active':active_infected,'source_only_excluded':excluded,'release_rule':'Enemy Juggernaut spawning is disabled.'})

    forts=load(evidence/'recovery/fortifications_info.json')
    dump(out/'fortifications/fortifications.json',forts)

    inv=load(root/'docs/maps/MAP_INVENTORY.json')
    maps=[]
    for name in MAPS:
        d=inv[name]
        maps.append({'name':name,'location':d.get('location'),'skybox':d.get('skybox'),'release_mode':'Regular','max_waves':15,'objectives':d.get('objectives',[]),'preview':f'docs/maps/previews/{name.lower()}.png','dossier':f'docs/maps/{name.upper()}.md','static_ir':{'historical_checkpoint':'present','current_recovery':'bytes_not_recovered','source_required_for_regeneration':'original twr places.zip .rbxlx member'}})
        dump(out/f'maps/recovery/{name}.json',{'format':'twr-map-recovery-status-v1','map':name,'release_included':True,'preview':f'docs/maps/previews/{name.lower()}.png','outline':f'docs/assets/maps/{name.lower()}-outline.png','historical_ir_reported':True,'historical_ir_bytes_recovered':False,'regeneration_blocker':'Original twr places.zip raw bytes are present in Project storage but could not be materialized into this recovery runtime. Do not substitute TestPlace geometry.','authority_order':['original .rbxlx','embedded script/data','RemoteSpy','recovered original scripts','recordings','images','compiled docs','TestPlace','inference','approximation']})
    dump(out/'maps/maps.json',{'format':'twr-map-catalog-v1','count':len(maps),'excluded':['Stadium','Carnival'],'maps':maps})

    reward=load(evidence/'catalog/reward_analysis.json')
    dump(out/'rewards/rewards.json',{'format':'twr-reward-evidence-v1','source_confirmed_wave_reward_analysis':reward,'map_completion':{'status':'Proposed','MapCompletionXpBonus':300,'interpretation':'XP per player level; completion XP = max(1, level) * MapCompletionXpBonus','historical_engineering_report_prior_value':'player level × 300','historical_retail_proven':False,'one_time_receipt':'MapCompletion','save_immediately_after_reward':True}})

    remote_scan=load(evidence/'recovery/script_remote_actions.json')
    historical=load(evidence/'recovery/historical_checkpoint.json')
    dump(out/'remotes/recovery_status.json',{'format':'twr-remotes-recovery-v1','transport_policy':'Standalone uses typed in-process commands/events, not Roblox networking.','script_static_scan':remote_scan,'historical_runtime_normalization':{'reported_calls':historical['historical_validation']['remotespy_calls_normalized'],'database_bytes_recovered':False,'current_runtime_call_count':None,'note':'RemoteSpy ZIP raw bytes could not be materialized into the current recovery runtime, so 14,363 is retained only as a historical report count.'}})

    scripts=load(evidence/'catalog/script_index.json')
    idxdir=out/'source_index/scripts'; idxdir.mkdir(parents=True,exist_ok=True); exp=set()
    for rec in sorted(scripts,key=lambda x:x['file'].casefold()):
        fn=slug(rec['file'].removesuffix('.Source.txt'))+'.json'; exp.add(fn)
        dump(idxdir/fn,{'format':'twr-script-metadata-v1',**rec})
    for p in idxdir.glob('*.json'):
        if p.name not in exp: p.unlink()
    dump(out/'source_index/summary.json',{'format':'twr-source-index-v1','script_count':len(scripts),'note':'Metadata only; recovered raw Luau source is intentionally excluded from public Git history.'})

    av=load(evidence/'catalog/artifact_validation.json')['checks']['evidence']
    dump(out/'recovery/source_counts.json',{'format':'twr-recovery-source-counts-v1','source_archives_verified':av['source_archives_verified'],'source_files_verified':av['source_files_verified'],'distinct_hashes':av['distinct_hashes'],'weapon_and_equipment_definitions':av['weapon_and_equipment_definitions'],'feature_traces':av['feature_traces'],'conflict_records':av['conflict_records'],'asset_ids':av['asset_ids']})
    print(f'content compiled: {len(active)} weapons, {len(equipment)} equipment, {len(perks)} perks, {len(active_infected)} active infected, {len(MAPS)} maps, {len(scripts)} script metadata records')

if __name__=='__main__': main()

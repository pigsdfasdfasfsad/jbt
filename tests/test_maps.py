import pytest
from conftest import ROOT,load
MAPS=['Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Laboratory','Manor']
@pytest.mark.parametrize('name',MAPS)
def test_clean_preview_png(name):
 p=ROOT/f'docs/maps/previews/{name.lower()}.png'; b=p.read_bytes(); assert b[:8]==b'\x89PNG\r\n\x1a\n' and len(b)>10000
@pytest.mark.parametrize('name',MAPS)
def test_map_dossier_and_recovery_record(name):
 assert (ROOT/f'docs/maps/{name.upper()}.md').is_file(); d=load(f'content/maps/recovery/{name}.json'); assert d['map']==name and d['historical_ir_reported'] is True and d['historical_ir_bytes_recovered'] is False
def test_map_count_and_exclusions():
 d=load('content/maps/maps.json'); assert d['count']==10 and d['excluded']==['Stadium','Carnival']
def test_preview_provenance_not_live_screenshot():
 s=load('evidence/recovery/historical_checkpoint.json')['map_preview_provenance'].lower(); assert 'not live game screenshots' in s

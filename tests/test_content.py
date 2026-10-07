import json,pytest
from conftest import ROOT,load
W=load('content/weapons/catalog.json')
P=load('content/perks/perks.json')
I=load('content/infected/infected.json')
@pytest.mark.parametrize('entry',W['weapons'][:12])
def test_weapon_entry_resolves(entry):
 d=load('content/weapons/'+entry['path']); assert d['name']==entry['name'] and d['kind']=='weapon'
def test_weapon_and_equipment_counts(): assert W['active_weapon_count']==91 and W['equipment_count']==7
def test_weapon_slots_total(): assert sum(1 for x in W['weapons'] if x['slot']=='Primary')==49 and sum(1 for x in W['weapons'] if x['slot']=='Secondary')==25 and sum(1 for x in W['weapons'] if x['slot']=='Melee')==17
@pytest.mark.parametrize('perk',P['perks'])
def test_perk_record(perk): assert perk['name'] and perk['category'] and int(perk['Level'])>0
def test_perk_count(): assert P['count']==21
@pytest.mark.parametrize('infected',I['active'])
def test_active_infected_not_juggernaut(infected): assert infected['name']!='Juggernaut' and infected['release_spawn_allowed'] is True
def test_infected_counts_and_exclusion(): assert I['active_count']==8 and [x['name'] for x in I['source_only_excluded']]==['Juggernaut']
def test_fortifications_source_normalized(): assert {x['name'] for x in load('content/fortifications/fortifications.json')['fortifications']}=={'50 Cal','Barbed Wire','Clap Bomb','Jack'}

from pathlib import Path
import json,re,pytest
from conftest import ROOT,load
MAPS=['Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Laboratory','Manor']
@pytest.mark.parametrize('name',MAPS)
def test_release_map_present(name): assert name in [x['name'] for x in load('content/maps/maps.json')['maps']]
@pytest.mark.parametrize('name',['Stadium','Carnival'])
def test_excluded_map_absent(name): assert name not in [x['name'] for x in load('content/maps/maps.json')['maps']]
@pytest.mark.parametrize('mode',['Hardcore','Classic','Endless'])
def test_excluded_modes_documented(mode): assert mode in (ROOT/'docs/PRODUCT_PROFILE.md').read_text()
def test_regular_only_and_fifteen_waves():
 s=(ROOT/'src/Twr.Domain/Model/ReleaseRules.cs').read_text(); assert 'Mode = "Regular"' in s; assert 'MaxWaves = 15' in s
def test_no_juggernaut_release_spawns(): assert 'EnemyJuggernautAllowed = false' in (ROOT/'src/Twr.Domain/Model/ReleaseRules.cs').read_text()
def test_no_normal_hit_movement_stun(): assert 'NormalMeleeMovementStun = false' in (ROOT/'src/Twr.Domain/Model/ReleaseRules.cs').read_text()
def test_completion_reward_is_marked_proposed():
 d=load('content/rewards/rewards.json')['map_completion']; assert d['status']=='Proposed' and d['historical_retail_proven'] is False and d['MapCompletionXpBonus']==300
def test_no_wave_sixteen_literal(): assert 'Wave=16' not in ''.join(p.read_text() for p in (ROOT/'src/Twr.Domain').rglob('*.cs'))

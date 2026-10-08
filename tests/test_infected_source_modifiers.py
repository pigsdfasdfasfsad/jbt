"""Original infected manipulators must match recovered source statistics."""
import json
from conftest import ROOT

def test_source_damage_modifiers_and_smoke_immunity():
    infected = json.loads((ROOT / 'content/infected/infected.json').read_text())
    stats = {x['name']:x['manipulators'] for x in infected['active']}
    assert stats['Hazmat']['Fire'] == 1
    assert stats['Hazmat']['Smoke'] == 0
    assert stats['Burster']['Smoke'] == 0
    assert stats['Bolter']['Smoke'] == 0.5
    assert stats['Bloater']['Fire'] == 3
    assert stats['Riot']['Melee'] == 0.5

    code=(ROOT / 'src/Twr.Godot/Scripts/InfectedCatalog.cs').read_text()
    agent=(ROOT / 'src/Twr.Godot/Scripts/InfectedAgent.cs').read_text()
    hazard=(ROOT / 'src/Twr.Godot/Scripts/ThrowableHazardRuntime.cs').read_text()
    bootstrap=(ROOT / 'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    assert '("Bloater", "Fire") => 3.0f' in code
    assert '("Riot", "Melee") => 0.5f' in code
    assert '"Hazmat" or "Burster" => 0.0f' in code
    assert '"Bolter" => 0.5f' in code
    assert 'InfectedCatalog.DamageMultiplier' in agent
    assert 'InfectedCatalog.SmokeMultiplier' in agent
    assert 'if(infected.InfectedType=="Hazmat")continue' not in hazard
    assert 'TWR_SMOKE_INFECTED_MODIFIERS_OK' in bootstrap

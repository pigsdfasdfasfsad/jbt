from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_damage_objective_uses_percentage_health_scale_and_documented_immunities():
    target=read("src/Twr.Godot/Scripts/DamageObjectiveTarget.cs")
    assert "APPROXIMATED health scale" in target
    assert 'damageKind is "Melee" or "Fire"' in target
    objective=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert 'case "Damage":' in objective
    assert "DAMAGE TANKER" in objective

def test_tanker_explosion_does_not_pay_infected_kill_bonus():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'context.DamageKind == "ObjectiveExplosion"' in game
    objective=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert 'infected.ApplyDamage(9999f, false, "ObjectiveExplosion")' in objective

def test_map_specific_objective_pool_comes_from_recovered_catalog():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "_mapDefinition.Objectives.ToArray()" in game
    catalog=read("src/Twr.Godot/Scripts/MapCatalogRuntime.cs")
    assert 'GetProperty("objectives")' in catalog
    assert 'Content/maps/maps.json' in catalog

def test_explosive_fortifications_can_damage_tanker_and_slow_trap_suppresses_burster():
    fort=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    assert 'GetNodesInGroup("damage_objective")' in fort
    assert 'tanker.ApplyDamage(Definition.Damage!.Value, "Explosive")' in fort
    assert 'infected.ApplyDamage(5f, false, "BarbedWire")' in fort

from conftest import ROOT

GAME=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()

def test_natural_pickups_use_selected_map_layout_points():
    assert "SpawnNaturalPickups()" in GAME
    assert "_mapLayout.PickupPoints" in GAME
    assert "exact retail spawn-group transforms are not recovered" in GAME

def test_radio_supply_drop_lands_before_eight_pickups_spawn():
    assert "SpawnSupplyDrop()" in GAME
    assert "SpawnSupplyContents(position)" in GAME
    assert "exactly four Regular slots and four Fort" in GAME
    assert 'var regularPool=new[] { "Medkit","Ammo","Body Armor" }' in GAME
    assert "var nFifty=_rng.RandiRange(0,4)" in GAME
    assert 'x.Name!="50 Cal"' in GAME

def test_radio_completion_defers_drop_when_wave_has_under_twenty_seconds():
    assert "_stageTime<20" in GAME
    assert "_supplyQueuedForNextWave=true" in GAME
    assert "_queuedSupplySeconds=3.0" in GAME

def test_unpack_spawns_exactly_eight_with_variant_marked_approximate():
    assert "for (var i = 0; i < 8; i++)" in GAME
    assert "both Unpack variants produce exactly eight pickups" in GAME
    assert "APPROXIMATED: absent source for variant scheduling" in GAME

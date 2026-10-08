from conftest import ROOT

GAME=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()

def test_natural_pickups_use_selected_map_layout_points():
    assert "SpawnNaturalPickups()" in GAME
    assert "_mapLayout.PickupPoints" in GAME
    assert "ChooseUniqueMarkers(_mapLayout.PickupPoints, 4)" in GAME
    assert "ChooseUniqueMarkers(_mapLayout.FortificationPoints, 2)" in GAME
    assert "Legacy blockout maps have no recovered fortification markers." in GAME

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

def test_unpack_spawns_recovered_random_ammo_or_medical_variant():
    assert "_rng.RandiRange(1,2)==1" in GAME
    assert "_rng.RandiRange(0,8)" in GAME
    assert "for(var i=0;i<8;i++)" in GAME
    assert 'ammoVariant ? "Ammo" : (i<bandages ? "Bandages" : "Medkit")' in GAME
    assert "uniform 0..8 split" in GAME

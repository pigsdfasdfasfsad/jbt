from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_natural_spawn_groups_are_separate_and_50cal_is_supply_only():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'ChooseUniqueMarkers(_mapLayout.PickupPoints, 4)' in s
    assert 'SpawnNaturalAt(position, "Item")' in s
    assert 'ChooseUniqueMarkers(_mapLayout.FortificationPoints, 2)' in s
    assert 'SpawnNaturalAt(position, "Fortification")' in s
    assert 'SpawnNaturalAt(basePoint+offset,"Fortification")' in s
    assert 'new[]{"Barbed Wire","Clap Bomb","Jack"}' in s
    natural=s[s.index('if(group=="Fortification")'):s.index('var items=',s.index('if(group=="Fortification")'))]
    assert "50 Cal" not in natural

def test_natural_item_pool_includes_items_and_all_three_grenades():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in ['"Bandages"','"Ammo"','"Body Armor"','"Medkit"','"Energy Drink"','"Gas Mask"','"Frag"','"Molotov"','"Nerve Gas"']:
        assert token in s

def test_collected_natural_pickups_respawn_with_recovered_regular_cadence():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "NaturalRespawnMinSeconds=20.0" in s
    assert "NaturalRespawnMaxSeconds=20.0" in s
    assert "FITTED in the recovered TestPlace directive" in s
    assert "ChooseUniqueMarkers(_mapLayout.PickupPoints, 4)" in s
    assert "var fortCount=Math.Min(2,_mapLayout.PickupPoints.Count)" in s
    assert "ScheduleNaturalRespawn(position,naturalGroup)" in s
    assert "TickNaturalRespawns(delta)" in s

from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_natural_spawn_groups_are_separate_and_50cal_is_supply_only():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    # Pass120 inspects original marker pools until it places 4 items/2 forts.
    # The source marker selection still does not invent new map positions.
    assert '_mapLayout.PickupPoints.Count' in s
    assert '++placedOriginalItems >= 4' in s
    assert 'SpawnNaturalAt(position,"Item")' in s
    assert '_mapLayout.FortificationPoints.Count' in s
    assert '++placedOriginalForts >= 2' in s
    assert 'SpawnNaturalAt(position,"Fortification")' in s
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
    assert "_mapLayout.PickupPoints.Count" in s
    assert "++placedOriginalItems >= 4" in s
    assert "var fortCount=Math.Min(2,_mapLayout.PickupPoints.Count)" in s
    assert "ScheduleNaturalRespawn(authoredNaturalMarker,naturalGroup)" in s
    assert "var authoredNaturalMarker = naturalMarker ?? position;" in s
    assert "if (_isFallbackScene86) TickNaturalRespawns(delta);" in s
    assert "TickNaturalRespawns(delta)" in s

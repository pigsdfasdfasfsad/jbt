from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_natural_spawn_groups_are_separate_and_50cal_is_supply_only():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'SpawnNaturalAt(point,"Item")' in s
    assert 'SpawnNaturalAt(basePoint+offset,"Fortification")' in s
    assert 'new[]{"Barbed Wire","Clap Bomb","Jack"}' in s
    natural=s[s.index('if(group=="Fortification")'):s.index('var items=',s.index('if(group=="Fortification")'))]
    assert "50 Cal" not in natural

def test_natural_item_pool_includes_items_and_all_three_grenades():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in ['"Bandages"','"Ammo"','"Body Armor"','"Medkit"','"Energy Drink"','"Gas Mask"','"Frag"','"Molotov"','"Nerve Gas"']:
        assert token in s

def test_collected_natural_pickups_respawn_with_explicit_fitted_interval():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "NaturalRespawnMinSeconds=35.0" in s
    assert "NaturalRespawnMaxSeconds=55.0" in s
    assert "FITTED reconstruction" in s
    assert "ScheduleNaturalRespawn(position,naturalGroup)" in s
    assert "TickNaturalRespawns(delta)" in s

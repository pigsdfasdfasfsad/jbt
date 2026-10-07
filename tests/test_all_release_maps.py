from conftest import ROOT

def read(path):
    return (ROOT/path).read_text()

def test_all_ten_release_maps_have_runtime_blockouts():
    s=read("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    for name in ["Ranch","Mill","Bypass","Cabin","Cargo","District","Expressway","Prison","Laboratory","Manor"]:
        assert f'"{name}" =>' in s or f'Build{name}' in s

def test_menu_lists_map_catalog_instead_of_manor_only():
    s=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "MapCatalogRuntime.All()" in s
    assert "button.Pressed += () => StartGame(selected)" in s
    assert 'StartGame("Manor")' in s  # smoke path remains deterministic

def test_gameplay_uses_map_layout_for_player_enemy_pickup_and_objective_positions():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "MapBlockoutBuilder.Build(this, _mapDefinition)" in s
    assert "Position = _mapLayout.PlayerSpawn" in s
    assert "_mapLayout.InfectedSpawns" in s
    assert "_mapLayout.PickupPoints" in s
    assert "_mapLayout.ObjectivePoints" in s

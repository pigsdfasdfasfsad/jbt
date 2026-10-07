from conftest import ROOT

def text(path):
    return (ROOT / path).read_text()

def test_main_scene_is_not_a_title_card_shell():
    scene = text("src/Twr.Godot/Scenes/Main.tscn")
    bootstrap = text("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "OFFLINE RECONSTRUCTION" not in scene
    assert "GameplayRoot" in bootstrap
    assert 'StartGame("Manor")' in bootstrap

def test_first_person_combat_uses_recovered_glock_values():
    player = text("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "MagazineCapacity = 18" in player
    assert "WeaponDamage = 15f" in player
    assert "60.0 / 400.0" in player
    assert "ReloadSeconds = 1.85" in player
    assert "Range = 1000f" in player
    assert "SpendAmmo" in player

def test_wave_loop_is_timed_and_continuously_spawns():
    game = text("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "RegularWaveRules.WaveDurationSeconds" in game
    assert "RegularWaveRules.SpawnIntervalSeconds" in game
    assert "RegularWaveRules.MaxAlive" in game
    assert "AdvanceWave()" in game

def test_manor_geometry_is_not_misrepresented_as_verified():
    game = text("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "APPROXIMATED geometry" in game
    assert "measurements are not source-surveyed" in game

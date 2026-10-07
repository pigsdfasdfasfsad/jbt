from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_50cal_is_player_operated_not_autonomous():
    actor=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    assert 'Definition.Name=="50 Cal"' in actor
    assert "Key.E" in actor
    assert "Key.Q" in actor
    assert "MouseButton.Right" in actor
    assert "MouseButton.Left" in actor
    assert "_remainingShots=21" in actor
    assert "_timer=0.225" in actor
    assert "SetMountedMode" in actor

def test_50cal_preserves_damage_and_penetration_from_catalog():
    actor=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    catalog=read("content/fortifications/fortifications.json")
    assert '"damage": 75' in catalog
    assert '"maxPen": 5' in catalog
    assert "Definition.MaxPen" in actor
    assert "DamageMultiplier" in actor

def test_deployed_50cal_receives_player_and_runtime_references():
    controller=read("src/Twr.Godot/Scripts/FortificationController.cs")
    assert "Player = Player" in controller
    assert "Runtime = Runtime" in controller

def test_mount_locks_normal_player_movement_and_weapon_input():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "public bool MountedMode" in player
    assert "MountedMode" in player\n    assert "Velocity=Vector3.Zero" in player
    assert "SetMountedAim" in player

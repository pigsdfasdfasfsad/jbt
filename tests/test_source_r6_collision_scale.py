"""Roblox-scale player/zombie hitboxes must fit normal 2-stud-wide passages."""
from conftest import ROOT

def test_source_r6_collision_size_supports_indoor_navigation():
    infected=(ROOT/"src/Twr.Godot/Scripts/InfectedAgent.cs").read_text()
    player=(ROOT/"src/Twr.Godot/Scripts/FirstPersonPlayer.cs").read_text()
    gameplay=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    assert 'Radius = 0.30f, Height = 1.6f' in infected
    assert 'Radius = 0.29f, Height = 1.8f' in player
    assert 'basePoint + Vector3.Up * 0.8f' in gameplay
    assert 'HasClearAttackPath()' in infected

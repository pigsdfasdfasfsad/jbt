"""Pass93 denies unsupported legacy upper-floor Escort completion."""
from conftest import ROOT


def test_manor_upper_escort_requires_real_physics_before_rewards():
    objective=(ROOT/"src/Twr.Godot/Scripts/ObjectiveRuntime.cs").read_text(encoding="utf8")
    game=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text(encoding="utf8")
    assert 'if (RequireSameFloorSecure84)' in objective
    assert 'Pass90EscortArrivalSafety.CanComplete(' in objective
    assert '_escort.IsOnFloor()' in objective
    assert 'StopEscortHorizontal89(delta)' in objective
    assert 'Player.GlobalPosition.DistanceTo(_escort.GlobalPosition)' in objective
    assert 'RequireSameFloorSecure84 = MapName == "Manor"' in game
    assert 'EscortGroundNavigator89 = escortNavigator92' in game
    # Only reconstructed Manor upper rooms take this legacy safety path.
    assert '(MapName != "Manor" || ObjectivePosition(i).Y < 2.3f)' in game


def test_no_direct_recovered_original_map_rewrites():
    objective=(ROOT/"src/Twr.Godot/Scripts/ObjectiveRuntime.cs").read_text(encoding="utf8")
    assert 'else Complete();' in objective
    assert 'if (UseFallbackEscortNavigation89)' in objective

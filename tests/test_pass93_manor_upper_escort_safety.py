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
    assert 'objective.EscortGroundNavigator89 =' in game
    assert '_expresswayEscortNavigation92;' in game
    # Pass94 redirects reconstructed elevated Manor to the same
    # verified-floor completion guard used by Pass89's routed Escorts.
    assert 'ObjectivePosition(i).Y < 2.3f || manorUpperEscort94' in game
    assert 'UseManorElevatedEscort94 = manorUpperEscort94' in game


def test_no_direct_recovered_original_map_rewrites():
    objective=(ROOT/"src/Twr.Godot/Scripts/ObjectiveRuntime.cs").read_text(encoding="utf8")
    assert 'else Complete();' in objective
    assert 'if (UseFallbackEscortNavigation89)' in objective

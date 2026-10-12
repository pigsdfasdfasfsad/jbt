"""Pass94 uses real physics for every approximate Manor Escort route leg."""
from conftest import ROOT


def src(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_only_reconstructed_upper_manor_gets_own_floor_route():
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    objective=src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    planner=src("src/Twr.Godot/Scripts/Pass94ManorEscortRoute.cs")
    assert 'var manorUpperEscort94 = family == "Escort"' in game
    assert '_isFallbackScene86 && MapName == "Manor"' in game
    assert 'position.Y >= 2.3f' in game
    assert 'UseManorElevatedEscort94 = manorUpperEscort94' in game
    assert 'Pass94ManorEscortRoute.TryGroundUpperSpawn(' in game
    assert 'Pass94ManorEscortRoute.TryGroundGoal(' in objective
    assert 'Pass94ManorEscortRoute.GetVerifiedRoute(' in objective
    assert 'nav.MapName!="Manor"' in planner
    assert 'nav.Layout.ApproximateStairs77.Count!=3' in planner
    assert 'nav.Layout.ApproximateCrossings80.Count!=3' in planner


def test_upper_crosswalk_overlaps_real_ramp_without_step_lip():
    m=src("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    assert 'name+"_UpperCrossing80"' in m
    assert 'new Vector3(9.0f,.16f,1.20f)' in m
    assert 'name+"_UpperStairConnector94"' not in m
    assert 'the full-size Escort now approaches the stair' in m


def test_stairs_and_catwalk_reject_unsupported_capsule_paths():
    planner=src("src/Twr.Godot/Scripts/Pass94ManorEscortRoute.cs")
    objective=src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    for token in (
        "Pass90EscortArrivalSafety.TryGroundDestination(",
        "Pass89EscortSurfaceSafety.CanTravel(",
        "nav.GetVerifiedEscortRoute89(",
        "crossing.CentreFeet",
        "stair.LowerFeet.Lerp(",
        "RampSections = 12",
        "pass-through-retail-navmesh",
    ):
        if token=="pass-through-retail-navmesh":
            assert token not in planner
        else: assert token in planner
    assert "survivor.GlobalPosition =" not in planner
    assert "actor.GlobalPosition=" not in planner
    assert "StopEscortHorizontal89(delta)" in objective
    assert "EscortRouteInvalidations91++" in objective
    assert "probeFeet.Y=from.Y+" in objective
    assert "actor.MoveAndSlide()" in objective
    assert "CompleteObjective(ObjectiveId, Family)" in objective


def test_previous_nine_maps_keep_their_original_escort_navigation():
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "var groundEscort91 =" in game
    assert "var expresswayEscort92 =" in game
    assert "if (groundEscort91 || expresswayEscort92 || manorUpperEscort94)" in game
    assert "Pass91EscortSpawnSafety.TryFind(" in game
    assert "objective.EscortGroundNavigator89 =" in game
    assert "HighwayNavigation" in game

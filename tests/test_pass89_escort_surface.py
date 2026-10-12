"""Pass89: an Escort survivor follows actual supported physical routes."""
from conftest import ROOT


def read(p):
    return (ROOT/p).read_text(encoding="utf-8")


def test_escort_uses_fallback_world_graph_not_wall_clipping():
    obj=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    nav=read("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "TickFallbackEscort89(delta)" in obj
    assert "EscortGroundNavigator89" in obj
    assert "GetVerifiedEscortRoute89(" in obj
    assert "Pass89EscortSurfaceSafety.CanTravel(" in obj
    assert "actor.MoveAndSlide()" in obj
    assert "StopEscortHorizontal89(delta)" in obj
    assert "var newRoute=UseManorElevatedEscort94" in obj
    assert "nav.GetVerifiedEscortRoute89(" in obj
    assert "if (_escortRouteIndex89 >= _escortRoute89.Length)" in obj
    assert "EscortBlockedSteps89++" in obj
    assert "EscortSafeSteps89++" in obj
    assert "_escortRepathSeconds89=.80" in obj
    assert "EscortDestinationAdjusted89 = option!=target" in obj
    assert "GetVerifiedEscortRoute89(" in nav
    assert "Pass89EscortSurfaceSafety.CanTravel(" in nav
    assert "private static bool EscortAnchorMatches89" in nav
    assert 'ObjectivePosition(i).Y < 2.3f || manorUpperEscort94' in game
    assert "_groundNavigation73 is not null" in game
    assert "EscortGroundNavigator89 = _groundNavigation73" in game


def test_surface_audits_floor_and_side_and_wall_samples():
    src=read("src/Twr.Godot/Scripts/Pass89EscortSurfaceSafety.cs")
    assert "SampleSpacingMetres=.60f" in src
    assert "SideOffsets=[0f,-.22f,.22f]" in src
    assert "MaximumSegmentMetres=55f" in src
    assert "MaximumHeightDeviation=.47f" in src
    assert "new[]{.48f,.96f}" in src
    assert "space.IntersectRay(wall).Count>0" in src
    assert "space.IntersectRay(floorRay)" in src
    assert "wall.CollisionMask=1u" in src
    assert "floorRay.Exclude=exclude" in src
    assert "GlobalPosition=" not in src

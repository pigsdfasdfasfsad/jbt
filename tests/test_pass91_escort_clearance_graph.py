"""Pass91: Escort and infected graphs have independently audited edges."""
from conftest import ROOT

def test_escort_graph_uses_real_capsule_sweeps_and_does_not_change_zombie_graph():
    s=(ROOT/"src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs").read_text(encoding="utf8")
    assert "private readonly AStar3D _escortGraph91 = new();" in s
    assert "_graph.AddPoint(_nextSample, ground);" in s
    assert "_escortGraph91.AddPoint(_nextSample, ground);" in s
    assert "_graph.ConnectPoints(_nextLink, id);" in s
    assert "_escortGraph91.ConnectPoints(_nextLink,id);" in s
    assert "EscortSafeEdgeCount91++" in s
    assert "Pass89EscortSurfaceSafety.CanTravel(" in s
    a=s.index("public Vector3[] GetVerifiedEscortRoute89(")
    b=s.index("private static bool EscortAnchorMatches89(",a)
    method=s[a:b]
    assert "_escortGraph91.GetClosestPoint(" in method
    assert "_escortGraph91.GetPointPath(" in method
    assert "EscortSafeEdgeCount91<10" in method
    assert "_graph.GetPointPath(" not in method
    assert "Pass89EscortSurfaceSafety.CanTravel(" in method
    assert "GetRoute(Vector3 infectedCentre" in s

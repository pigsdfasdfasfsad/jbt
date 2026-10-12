"""Pass81: bounded collision-audited fallback Manor upper-wing detours."""
from conftest import ROOT

def code(path):
    return (ROOT/path).read_text(encoding="utf8")

def test_upper_navigation_same_wing_detours_are_bounded_and_safe():
    nav=code("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    assert "TryGetSupportedUpperDetour81" in nav
    assert "VerifiedUpperDetours81" in nav
    assert "RejectedUpperDetours81" in nav
    assert "var fromSide=Math.Sign(from.X-mid.X)" in nav
    assert "fromSide != targetSide" in nav
    assert "mid.X + fromSide*3.2f" in nav
    for leg in ("from,a","a,b","b,playerFeet"):
        assert f"UpperCorridorSupported79({leg})" in nav
    assert "distance >= bestLength" in nav
    assert "GlobalPosition =" not in nav

def test_crossing_takes_priority_over_within_deck_detour():
    nav=code("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    method=nav[nav.index("public Vector3[] GetRoute("):nav.index("private bool TryGetVerifiedManorAscent77(")]
    assert method.index("TryGetVerifiedUpperCrossing80") < method.index("TryGetSupportedUpperDetour81")
    assert method.index("IsManorSameUpperFloor79") < method.index("TryGetVerifiedManorAscent77")

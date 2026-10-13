"""Pass101: preserve original collision physics while avoiding stair-toe detours."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_only_verified_fallback_manor_downhill_steps_bypass_generic_steering():
    navigation = src("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    infected = src("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "public bool SafeManorDescentStep101(" in navigation
    assert 'if(MapName!="Manor" || StairLinkCount77!=3 ||' in navigation
    assert "Layout.ApproximateStairs77" in navigation
    assert "if(nextFraction>fraction+.012f)continue;" in navigation
    assert "Math.Abs(feet.X-expected.X)>2.18f" in navigation
    assert "Pass89EscortSurfaceSafety.CanTravel(" in navigation
    assert "if(infectedRid.IsValid)exclude.Add(infectedRid);" in navigation
    assert "manorDownhill78 &&" in infected
    assert "GroundNavigator73!.SafeManorDescentStep101(" in infected
    assert "direction=desired;" in infected
    assert "var direction = SteerAroundObstacles(desired);" in infected
    assert "MoveAndSlide();" in infected


def test_exported_windows_gate_enforces_downhill_all_three_and_retains_privacy():
    ci = src(".github/workflows/windows-build.yml")
    acceptance = src("src/Twr.Godot/Scripts/Pass78ManorBidirectionalStairAcceptance.cs")
    assert "TWR_PASS78_INFECTED_DOWN_OK" in acceptance
    assert "FrontHallUpper" in acceptance
    assert "infected.IsOnFloor()" in acceptance
    assert "enemyHigh-enemyLow < 2.25f" in acceptance
    assert "Smoke Pass 78 native three Manor stair flights" in ci
    assert "github.ref_name == 'twr-pass101-spawn-objective-walk'" in ci
    assert "TWR_PASS78_INFECTED_DOWN_OK" in ci
    assert "Verify no private map scenes entered the CI export" in ci

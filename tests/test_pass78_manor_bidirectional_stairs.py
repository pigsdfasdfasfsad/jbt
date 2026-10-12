"""Pass78 reconstructed Manor physical upstairs/downstairs verification contract."""
from conftest import ROOT

def src(path):
    return (ROOT/path).read_text(encoding="utf-8")

def test_fallback_descent_only_if_physically_visible_and_supported():
    nav=src("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    assert 'MapName == "Manor" && StairLinkCount77 == 3' in nav
    assert "VerifiedDescendingRoutes78" in nav
    assert "RejectedDescendingRoutes78" in nav
    assert "TryGetVerifiedManorDescent78(" in nav
    assert "VerifiedPhysicalStair77(stair)" in nav
    assert "!CorridorClear(stair.LowerFeet, playerFeet)" in nav
    assert "Math.Abs(playerFeet.Y - stair.LowerFeet.Y) > .9f" in nav
    assert "!CorridorClear(infectedFeet, stair.UpperFeet)" in nav
    assert "var onRamp = progress > .02f" in nav
    assert "Math.Ceiling(progress*12f)-1" in nav
    assert "for (var i = startSegment; i >= 0; i--)" in nav
    assert "points.Add(playerFeet);" in nav
    assert "MaximumGridCells = 2_800" in nav
    assert 'return CorridorClear(stair.LowerFeet, stair.UpperFeet);' in nav
    assert "GlobalPosition =" not in nav

def test_infected_does_not_fake_melee_across_story_or_skip_downhill_waypoints():
    infected=src("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "aboveLowerTarget78" in infected
    assert "belowUpperTarget77 || aboveLowerTarget78" in infected
    assert "manorDownhill78" in infected
    assert "GlobalPosition.Y - (waypoint.Y + .8f) > .18f" in infected
    assert 'GroundNavigator73!.MapName == "Manor"' in infected
    assert "Math.Abs(waypoint.Y + .8f - GlobalPosition.Y) > 1.15f" in infected
    assert "MoveAndSlide();" in infected
    assert "SourceJumpAttempts" in infected

def test_native_three_flight_player_and_real_infected_up_down():
    acceptance=src("src/Twr.Godot/Scripts/Pass78ManorBidirectionalStairAcceptance.cs")
    assert 'Runtime.StartMap("Manor")' in acceptance
    assert "new GameplayRoot" in acceptance
    assert "foreach (var stair in game.Pass77ApproximateStairs)" in acceptance
    for name in ("FrontHallUpper","WestWingUpper","EastWingUpper"):
        assert f'"{name}"' in acceptance
    assert "SetKey(Key.W, true)" in acceptance
    assert "SetKey(Key.S, true)" in acceptance
    assert "reachedTop" in acceptance and "reachedBottom" in acceptance
    assert "new InfectedAgent" in acceptance
    assert "GroundNavigator73 = navigation" in acceptance
    assert "enemyReachedTop" in acceptance and "enemyReachedBottom" in acceptance
    assert "navigation.GetRoute(" in acceptance
    assert "navigation.VerifiedDescendingRoutes78" in acceptance
    assert "infected.SourceJumpAttempts != 0" in acceptance
    assert "infected.GlobalPosition =" not in acceptance
    assert "Runtime.Player.Health =" not in acceptance
    assert "Runtime.Player.Credits =" not in acceptance
    assert "Runtime.Match.Wave =" not in acceptance
    assert "Runtime.ReturnToLobby()" in acceptance
    for marker in ("TWR_PASS78_PLAYER_UP_OK","TWR_PASS78_PLAYER_DOWN_OK",
                   "TWR_PASS78_INFECTED_UP_OK","TWR_PASS78_INFECTED_DOWN_OK",
                   "TWR_SMOKE_PASS78_BIDIRECTIONAL_STAIRS_OK"):
        assert marker in acceptance

def test_windows_export_isolated_and_asset_exclusion_gated():
    ci=src(".github/workflows/windows-build.yml")
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass78"' in boot
    assert "new Pass78ManorBidirectionalStairAcceptance" in boot
    assert '"--smoke-pass78"' in session
    assert '"user://pass78-manor-bidirectional-smoke-profile.json"' in session
    assert "else if(smoke78)" in session
    assert "Smoke Pass 78 native three Manor stair flights" in ci
    assert "TWR_SMOKE_PASS78_BIDIRECTIONAL_STAIRS_OK" in ci
    assert "twr-pass78-manor-bidirectional-stairs" in ci
    assert ci.index("Smoke Pass 77") < ci.index("Smoke Pass 78")
    assert ci.index("Smoke Pass 78") < ci.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass78-manor-bidirectional-stairs')" in ci

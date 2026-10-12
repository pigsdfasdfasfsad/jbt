"""Pass77: reconstructed Manor stair/landing geometry and native uphill physics."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_three_manor_stair_geometry_is_physical_and_still_approximate():
    blockout=read("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    assert blockout.count("= AddUpperFloor(root,") == 3
    for name in ("WestWingUpper", "EastWingUpper", "FrontHallUpper"):
        assert f'AddUpperFloor(root,"{name}"' in blockout
    assert "ApproximateStairs77 = [westStair77, eastStair77, frontStair77]" in blockout
    assert 'Name = name+"_WalkableRamp77"' in blockout
    assert 'name+"_Landing77"' in blockout
    assert 'Name=name+"_Landing77"' in blockout
    assert "bridgeSize=new Vector3(5.6f,bridgeThickness,1.40f)" in blockout
    assert "Rotation=new Vector3(angle,0,0)" in blockout
    assert "bridge.AddChild(new CollisionShape3D" in blockout
    assert 'DecorativeBox(root,name+"_Stair"+i' in blockout
    assert "const int steps=8" in blockout
    assert "ramp.AddChild(new CollisionShape3D" in blockout
    assert "new Vector3(5.60f,rampThickness,slopeLength+.20f)" in blockout
    assert "bridgeThickness*.5f*Mathf.Cos(angle)+.16f" in blockout
    assert "Rotation = new Vector3(angle,0,0)" in blockout
    assert "Math.Abs(angle) >= Mathf.DegToRad(35f)" in blockout
    assert "reconstructed stair slope unsafe" in blockout
    assert "APPROXIMATED Manor geometry, not source-recovered retail meshes" in blockout
    assert "public sealed record Pass77StairLink(" in blockout


def test_elevated_ai_path_requires_real_ramp_collider_and_ground_graph():
    graph=read("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'MapName == "Manor" && StairLinkCount77 == 3' in graph
    assert "infectedCentre.Y - .8f < playerCentre.Y - .01f" in graph
    assert "var onRamp = progress > .04f" in graph
    assert "if (!alreadyOnRamp) points.Add(selected.LowerFeet);" in graph
    assert "startSegment = begin;" in graph
    assert "belowUpperTarget77" in read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "playerCentre.Y >= 2.2f" in graph
    # Continue following the actual stair while the infected's FEET remain
    # below the target's upper-floor feet, not only on the first replan.
    assert "TryGetVerifiedManorAscent77(" in graph
    assert "VerifiedPhysicalStair77(stair)" in graph
    assert "var playerFeet = playerCentre;" in graph
    assert "var infectedFeet = infectedCentre - Vector3.Up * .8f;" in graph
    assert "((Vector3)hit[\"normal\"]).Y < .74f" in graph
    assert "Math.Abs(((Vector3)hit[\"position\"]).Y - expected.Y) > .24f" in graph
    assert "CorridorClear(stair.LowerFeet, stair.UpperFeet)" in graph
    assert "points.Add(selected.LowerFeet.Lerp(selected.UpperFeet, i/12f))" in graph
    assert "FallbackPathSearches" in graph
    assert "MaximumGridCells = 2_800" in graph
    assert "MaximumRouteCache = 192" in graph
    assert "!originalWorldAvailable && _sourceNavigator is null" in game
    assert 'MapName != "Expressway"' in game
    assert "Pass77ApproximateStairs" in game
    assert "Runtime.Player.Health =" not in graph
    assert "GlobalPosition =" not in graph


def test_native_real_player_keyboard_and_infected_physics_ascent():
    acceptance=read("src/Twr.Godot/Scripts/Pass77ManorStairAcceptance.cs")
    assert 'Runtime.StartMap("Manor")' in acceptance
    assert "new GameplayRoot" in acceptance
    assert "game.Pass77ApproximateStairs.Count != 3" in acceptance
    assert "nav.StairLinkCount77 != 3" in acceptance
    assert "SetKey(Key.W,true)" in acceptance
    assert "SetKey(Key.W,false)" in acceptance
    assert "highest-start.Y < 2.55f" in acceptance
    assert "upper_landing_reached=true" in acceptance
    assert "!reachedLanding" in acceptance
    assert "!infectedReachedLanding" in acceptance
    assert "nav.GetRoute(below, player.GlobalPosition)" in acceptance
    assert "new InfectedAgent" in acceptance
    assert "GroundNavigator73 = nav" in acceptance
    assert "infectedMaxY - infectedStartY < 2.35f" in acceptance
    assert "infected.SourceJumpAttempts != 0" in acceptance
    assert "Runtime.Player.Health =" not in acceptance
    assert "Runtime.Player.Credits =" not in acceptance
    assert "Runtime.ReturnToLobby()" in acceptance


def test_window_export_isolated_profile_owner_exclusion():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert 'new Pass77ManorStairAcceptance' in boot
    assert '"--smoke-pass77"' in boot and '"--smoke-pass77"' in session
    assert '"user://pass77-manor-stair-smoke-profile.json"' in session
    assert "else if(smoke77)" in session
    assert "TWR_PASS77_REAL_PLAYER_STAIRS_OK" in ci
    assert "TWR_PASS77_REAL_INFECTED_STAIRS_OK" in ci
    assert "TWR_SMOKE_PASS77_MANOR_STAIRS_OK" in ci
    assert "twr-pass77-manor-stair-traversal" in ci
    assert ci.index("Smoke Pass 76") < ci.index("Smoke Pass 77")
    assert ci.index("Smoke Pass 77") < ci.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass77-manor-stair-traversal')" in ci

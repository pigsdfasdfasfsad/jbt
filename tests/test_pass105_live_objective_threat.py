"""Pass105: production-spawned infected can attack during genuine material carrying."""
from conftest import ROOT


def src(path):
    return (ROOT/path).read_text(encoding="utf-8")


def test_true_wave_spawning_physics_damage_and_normal_pickup_deposit():
    harness=src("src/Twr.Godot/Scripts/Pass105LiveThreatObjectiveAcceptance.cs")
    for part in (
        'Runtime.StartMap("Ranch")',
        "var game=new GameplayRoot",
        "RegularWaveRules.CountdownSeconds+.25",
        'game.WaveStage!="WAVE"',
        "game.SetProcess(true)",
        "game.GetChildren().OfType<InfectedAgent>()",
        "spawned.Target!=player",
        "Pass89EscortSurfaceSafety.CanTravel(",
        "GetVerifiedEscortRoute89(",
        "excluded.Add(mobile.GetRid());",
        "away=player.GlobalPosition-depot;",
        "spawned.GlobalPosition=lane.Value+Vector3.Up*.47f",
        "spawned.IsOnFloor()",
        "minGap>startGap-.65f || damageTaken<.5f",
        "Runtime.Player.Health<initialHealth-.1f",
        "objective.Pass98CarryingMaterial",
        "objective.Pass98DepositedMaterials!=1",
        "objective.Pass98RequiredMaterials-1",
        "Runtime.Match?.CompletedObjectives.Contains(",
        "Runtime.Player.Credits!=creditsBefore",
        "Runtime.Player.Xp!=xpBefore",
        "Input.ParseInputEvent(new InputEventKey",
        "SetKey(Key.W,true)",
        "SetKey(Key.E,true)",
        "game.ActiveInfectedCount<1",
        "Runtime.FailMatch(",
        "Runtime.Match.SaveCommitted",
        "TWR_PASS105_RANCH_UNDER_FIRE_DEPOSIT_OK",
        "TWR_SMOKE_PASS105_LIVE_OBJECTIVE_OK",
    ):
        assert part in harness, part
    # The test is authorized to position its test-only infected lane, but
    # not teleport the actual player or inject health, money or objectives.
    for forbidden in (
        "player.GlobalPosition=", "player.Position=",
        "player.Velocity=", "Runtime.DamagePlayer(",
        "Runtime.HealPlayer(", "Runtime.CompleteObjective(",
        "Runtime.Player.Health=", "Runtime.Player.Credits=",
        "Runtime.Player.Xp=", "objective.GlobalPosition=",
        "spawned.SetPhysicsProcess(false)", "spawned.Damage=0",
    ):
        assert forbidden not in harness, forbidden
    assert "Runtime.DamagePlayer(Damage, InfectedType)" in src(
        "src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "MoveAndSlide();" in src(
        "src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "MoveAndSlide();" in src(
        "src/Twr.Godot/Scripts/FirstPersonPlayer.cs")


def test_pass105_native_windows_acceptance_retains_two_wave_tests_and_privacy():
    bootstrap=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=src(".github/workflows/windows-build.yml")
    assert 'args.Contains("--smoke-pass105"' in bootstrap
    assert "new Pass105LiveThreatObjectiveAcceptance" in bootstrap
    assert 'var smoke105=OS.GetCmdlineUserArgs().Contains("--smoke-pass105"' in session
    assert "pass105-live-threat-material-smoke-profile.json" in session
    assert "else if(smoke105)" in session
    assert "twr-pass105-live-objective-threat" in ci
    assert "Preflight Pass 105 native objective pickup and deposit during live infected attack" in ci
    assert "TWR_PASS105_RANCH_UNDER_FIRE_DEPOSIT_OK" in ci
    assert "TWR_SMOKE_PASS105_LIVE_OBJECTIVE_OK" in ci
    assert "Preflight Pass 104 full production Wave2 Repair by physical walking" in ci
    assert "Preflight Pass 103 real full production Load physical walking" in ci
    assert "TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK" in ci
    assert "TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK" in ci
    assert "TWR_PASS100_MAP_WALK_OK" in ci
    assert "Smoke Pass 78 native three Manor stair flights" in ci
    assert "Verify no private map scenes entered the CI export" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

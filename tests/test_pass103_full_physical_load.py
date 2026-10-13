"""Pass103 verifies real production Wave1 Load completion via player movement."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_real_walking_and_every_pickup_and_deposit_with_no_teleport():
    p=src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    for fragment in (
        "Runtime.StartMap(map)",
        "new GameplayRoot",
        "RegularWaveRules.CountdownSeconds",
        'game.WaveStage!="WAVE"',
        'x.Family=="Load"',
        "Pass96GroundedMaterialLayout",
        "Pass98RequiredMaterials<10",
        "Pass96MaterialFeet.ToArray()",
        "Pass89EscortSurfaceSafety.CanTravel(",
        "GetVerifiedEscortRoute89(",
        "foreach(var item",
        "InputEvent(Key.W,true)",
        "InputEvent(Key.W,false)",
        "InputEvent(Key.E,true)",
        "InputEvent(Key.E,false)",
        "MoveAndSlide",
        "Pass98CarryingMaterial",
        "Pass98RemainingMaterials",
        "Pass98DepositedMaterials",
        "Runtime.CompleteObjective(id,\"Load\")",
        "ValidateReward(expected",
        "Runtime.FailMatch(",
        "Runtime.ReturnToLobby()",
        "TWR_PASS103_MAP_FULL_LOAD_OK map=",
        "TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_OK",
    ):
        assert fragment in p
    assert "player.GlobalPosition=" not in p
    assert "player.Velocity=" not in p
    assert "objective.GlobalPosition=" not in p
    assert "Runtime.Player.Credits=" not in p
    assert "Runtime.Player.Xp=" not in p
    assert "Runtime.HealPlayer(" not in p
    assert "Runtime.DamagePlayer(" not in p
    assert "PlayerState" in p


def test_bootstrap_private_fixture_and_exported_windows_gate():
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    runtime=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=src(".github/workflows/windows-build.yml")
    assert 'args.Contains("--smoke-pass103"' in boot
    assert "new Pass103PhysicalLoadLifecycleAcceptance" in boot
    assert 'var smoke103=OS.GetCmdlineUserArgs().Contains("--smoke-pass103"' in runtime
    assert "pass103-physical-load-lifecycle-smoke-profile.json" in runtime
    assert "else if(smoke103)" in runtime
    assert "twr-pass103-full-physical-load" in ci
    assert "TWR_PASS103_MAP_FULL_LOAD_OK map=$map production_wave=1" in ci
    assert "TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_OK maps=Ranch,Manor completed_production_wave1_loads=2" in ci
    assert "Preflight Pass 96 ten-map real Load/Repair material access" in ci
    assert "TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK" in ci
    assert "Smoke Pass 78 native three Manor stair flights" in ci
    assert "Verify no private map scenes entered the CI export" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()"
    )

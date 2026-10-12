"""Pass94 acceptance requires actual production Wave5 Manor stair traversal."""
from conftest import ROOT


def src(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_native_manor_completion_is_real_npc_motion_and_one_reward():
    game=src("src/Twr.Godot/Scripts/Pass94ManorEscortAcceptance.cs")
    for token in (
        'Runtime.StartMap("Manor")',
        "new GameplayRoot",
        "Pass77ApproximateStairs",
        "Pass82UpperObjectiveMarkers",
        'getNodeOrNull',
        "RegularWaveRules.WaveDurationSeconds",
        'x.Family=="Escort"',
        "objective.UseManorElevatedEscort94",
        "reachedMidRamp",
        "minimumFeetY>1.1f",
        "maxTravel<15f",
        "EscortGroundedCompletion90",
        "CompletedObjectives.Contains(objectiveId)",
        'Runtime.CompleteObjective(objectiveId,"Escort")',
        "TWR_PASS94_MANOR_STAIR_ESCORT_REWARD_OK",
        "TWR_SMOKE_PASS94_MANOR_STAIR_ESCORT_OK",
    ):
        if token=="getNodeOrNull":
            assert "GetNodeOrNull<CharacterBody3D>" in game
        else: assert token in game
    assert "actor.GlobalPosition=" not in game
    assert "Runtime.Match.Wave=" not in game
    assert "Runtime.Player.Credits=" not in game
    assert "Runtime.Player.Xp=" not in game


def test_windows_export_replays_prior_escort_and_privacy_guards():
    ci=src(".github/workflows/windows-build.yml")
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    node=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass94"' in boot and '"--smoke-pass94"' in node
    assert "new Pass94ManorEscortAcceptance" in boot
    assert "pass94-manor-stair-escort-smoke-profile.json" in node
    assert "TWR_SMOKE_PASS94_MANOR_STAIR_ESCORT_OK" in ci
    assert ci.index("Smoke Pass 91")<ci.index("Smoke Pass 92")<ci.index("Smoke Pass 94")
    assert ci.index("Smoke Pass 94")<ci.index("Verify no private map scenes")
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

"""Pass95 real Windows Manor wave10/wave15 + cumulative native privacy."""
from conftest import ROOT


def src(p):
    return (ROOT/p).read_text(encoding="utf8")


def test_real_manor_west_east_escort_and_authoritative_reward_once():
    s=src("src/Twr.Godot/Scripts/Pass95ManorEscortAcceptance.cs")
    for token in (
        'Runtime.StartMap("Manor")',
        "new GameplayRoot",
        "Pass77ApproximateStairs",
        "Pass82UpperObjectiveMarkers",
        "Pass95ManorEscortRotation.UpperWingForWave(10)!=0",
        "Pass95ManorEscortRotation.UpperWingForWave(15)!=1",
        "RegularWaveRules.CountdownSeconds",
        "RegularWaveRules.WaveDurationSeconds",
        "objective.UseManorElevatedEscort94",
        "EscortGroundedCompletion90",
        "GetNodeOrNull<CharacterBody3D>",
        "CompletedObjectives.Contains(id)",
        'Runtime.CompleteObjective(id,"Escort")',
        "TWR_PASS95_MANOR_WING_ESCORT_OK",
        "TWR_SMOKE_PASS95_MANOR_WINGS_OK",
        "Runtime.FailMatch(",
        "Runtime.ReturnToLobby()",
    ):
        assert token in s
    assert "actor.GlobalPosition=" not in s
    assert "Runtime.Match.Wave=" not in s
    assert "Runtime.Player.Credits=" not in s
    assert "Runtime.Player.Xp=" not in s
    assert 'var finalGoalY=GodotObject.IsInstanceValid(target)' in s
    assert 'finalGoalY<2.2f && (!reachedRamp || minY>1.1f)' in s
    assert 'goalY="+finalGoalY.ToString("F2")' in s


def test_native_windows_ci_uses_isolated_profile_before_regressions():
    boot=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=src(".github/workflows/windows-build.yml")
    assert '"--smoke-pass95"' in boot and '"--smoke-pass95"' in session
    assert "new Pass95ManorEscortAcceptance" in boot
    assert "pass95-manor-rotation-smoke-profile.json" in session
    assert "else if(smoke95)" in session
    assert "TWR_SMOKE_PASS95_MANOR_WINGS_OK" in ci
    assert ci.index("Preflight Pass 95") < ci.index("Smoke Pass 78")
    assert ci.index("Smoke Pass 94") < ci.index("Verify no private map scenes")
    assert "'twr-pass95-manor-escort-rotation'" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

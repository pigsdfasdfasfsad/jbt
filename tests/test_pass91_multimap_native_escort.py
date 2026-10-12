"""Pass91 requires eight actual Escort objectives in exported Windows."""
from conftest import ROOT
def source(p):return (ROOT/p).read_text(encoding="utf8")

def test_native_contracts_real_8_map_goal_and_idempotent_rewards():
    s=source("src/Twr.Godot/Scripts/Pass91MultimapEscortAcceptance.cs")
    for map_name in ("Ranch","Mill","Bypass","Cabin",
                     "Cargo","District","Prison","Laboratory"):
        assert f'"{map_name}"' in s
    for marker in ("TWR_PASS91_MAP_ESCORT_OK",
        "TWR_SMOKE_PASS91_MULTIMAP_ESCORT_OK",
        "new GameplayRoot","new Vector3(2f,1.12f,7f)",
        'Runtime.StartMap(map)',"CompletedObjectives.Contains(id)",
        'Runtime.CompleteObjective(id,"Escort")',
        "RegularWaveRules.WaveDurationSeconds",
        "EscortGroundedCompletion90",
        "VerifiedFallbackEscortStarts91",
        "ProgressionRules.RequiredForNextLevel("):
        assert marker in s
    assert "survivor.GlobalPosition=" not in s
    assert "Runtime.Player.Credits=" not in s

def test_pass91_windows_ci_replays_previous_native_and_privacy_tests():
    ci=source(".github/workflows/windows-build.yml")
    boot=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=source("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass91"' in boot and '"--smoke-pass91"' in session
    assert "new Pass91MultimapEscortAcceptance" in boot
    assert "pass91-multimap-escort-smoke-profile.json" in session
    assert "TWR_SMOKE_PASS91_MULTIMAP_ESCORT_OK" in ci
    assert ci.index("Smoke Pass 90")<ci.index("Smoke Pass 91")
    assert ci.index("Smoke Pass 91")<ci.index("Verify no private map scenes")
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

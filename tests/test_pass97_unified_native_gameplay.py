"""Pass97: material-objective physics and full-match replay coexist in one executable."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_both_pass96_integrations_are_in_the_same_windows_entrypoint():
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    local = read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert 'args.Contains("--smoke-pass96",StringComparer.Ordinal)' in boot
    assert 'args.Contains("--smoke-pass96-replay",StringComparer.Ordinal)' in boot
    assert "new Pass96GroundedMaterialsAcceptance" in boot
    assert "new Pass96FullMatchReplayAcceptance" in boot
    assert "var smoke96Replay=" in local
    assert "pass96-material-objectives-smoke-profile.json" in local
    assert "pass96-full-match-replay-smoke-profile.json" in local
    assert "else if(smoke96)" in local
    assert "else if(smoke96Replay)" in local
    # Separate flags must not both match the same command line.
    assert '"--smoke-pass96-replay"' != '"--smoke-pass96"'


def test_windows_pipeline_verifies_both_real_features_before_regressions():
    ci = read(".github/workflows/windows-build.yml")
    assert "twr-pass97-unified-gameplay" in ci
    assert "Preflight Pass 96 ten-map real Load/Repair material access" in ci
    assert "Preflight Pass 96 full native fifteen-wave match and same-map payout replay" in ci
    assert "TWR_SMOKE_PASS96_GROUNDED_MATERIALS_OK" in ci
    assert "TWR_SMOKE_PASS96_FULL_MATCH_REPLAY_OK" in ci
    assert "TWR_PASS97_MATERIALS_IN_FULL_MATCH_OK" in ci
    assert "'--smoke-pass96'" in ci
    assert "'--smoke-pass96-replay'" in ci
    assert ci.index("Preflight Pass 96 ten-map") < ci.index("Preflight Pass 96 full native")
    assert ci.index("Preflight Pass 96 full native") < ci.index("Smoke test Pass 29")
    assert ci.index("Preflight Pass 95") < ci.index("Verify no private map scenes")
    assert ci.index("Smoke Pass 94") < ci.index("Verify no private map scenes")
    for marker in (
        "Smoke Pass 78 native", "Smoke Pass 83 native", "Smoke Pass 88 native",
        "Smoke Pass 89 native", "Smoke Pass 90", "Smoke Pass 91",
        "Smoke Pass 92", "Smoke Pass 94",
        "Verify no private map scenes entered the CI export"
    ):
        assert marker in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )


def test_offline_wave_receipts_and_grounded_materials_do_not_conflict():
    match = read("src/Twr.Domain/Runtime/LocalSession.cs")
    rewards = read("src/Twr.Domain/Services/WaveRewardService.cs")
    director = read("src/Twr.Domain/Services/MatchDirector.cs")
    objective = read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    gameplay = read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    materials = read("src/Twr.Godot/Scripts/Pass96GroundedMaterials.cs")
    assert "_waveRewards.ResetForNewMatch();" in match
    assert 'var receipt = $"WaveSurvival:{mapName}:{wave}";' in rewards
    assert 'if(m.Phase!=MatchPhase.Lobby)' in director
    assert 'UseFallbackMaterialGrounding96' in gameplay
    assert "Pass96GroundedMaterials.TryPlan(" in objective
    assert "Pass96GroundedMaterialLayout" in objective
    assert "Pass90EscortArrivalSafety.TryGroundDestination(" in materials
    assert "Pass89EscortSurfaceSafety.CanTravel(" in materials
    assert "Profile.Credits = 0" not in match
    assert "Profile.Xp = 0" not in match
    assert "CollisionMask=0" not in materials


def test_real_fifteen_wave_gameplay_also_grounds_live_material_missions():
    full = read("src/Twr.Godot/Scripts/Pass96FullMatchReplayAcceptance.cs")
    for marker in (
        "foreach(var objective in game.GetChildren().OfType<ObjectiveRuntime>())",
        "objective.UseFallbackMaterialGrounding96",
        "objective.Pass96GroundedMaterialLayout",
        "objective.Pass96MaterialFeet.Count",
        "groundedMaterialObjectives97<6",
        "groundedMaterialPieces97<60",
        "TWR_PASS97_MATERIALS_IN_FULL_MATCH_OK",
    ):
        assert marker in full
    # Do not fake the material planner, rewards or infected spawning.
    assert "objective.Pass96GroundedMaterialLayout=true" not in full
    assert "objective.Position=" not in full
    assert "Runtime.Player.Credits=" not in full
    assert "Runtime.Player.Xp=" not in full

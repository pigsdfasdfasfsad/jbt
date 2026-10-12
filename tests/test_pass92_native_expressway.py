"""Pass92 native ninth-map Escort scope and reward protections."""
from conftest import ROOT
def source(p):return (ROOT/p).read_text(encoding="utf-8")

def test_native_expressway_escort_keeps_infected_nav_and_credits_domain():
    code=source("src/Twr.Godot/Scripts/Pass92ExpresswayEscortAcceptance.cs")
    for token in (
        'Runtime.StartMap("Expressway")',
        'new GameplayRoot',
        '"ExpresswayReconstruction/HighwayNavigation"',
        '"Pass92ExpresswayEscortGroundNavigation"',
        "Pass92ExpresswayEscortNavReady",
        "game.Pass73FallbackNavReady",
        "EscortGroundedCompletion90",
        "CompletedObjectives.Contains(id)",
        "RegularWaveRules.WaveDurationSeconds",
        'Runtime.CompleteObjective(id,"Escort")',
        "TWR_PASS92_EXPRESSWAY_ESCORT_REWARD_OK",
        "TWR_SMOKE_PASS92_EXPRESSWAY_ESCORT_OK",
        "Runtime.FailMatch(",
        "Runtime.ReturnToLobby()",
    ): assert token in code
    assert "survivor.GlobalPosition=" not in code
    assert "Runtime.Player.Credits=" not in code

def test_pass92_ci_runs_prior_pass91_before_native_expressway():
    ci=source(".github/workflows/windows-build.yml")
    boot=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=source("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass92"' in boot and '"--smoke-pass92"' in session
    assert "new Pass92ExpresswayEscortAcceptance" in boot
    assert "pass92-expressway-escort-smoke-profile.json" in session
    assert "TWR_SMOKE_PASS92_EXPRESSWAY_ESCORT_OK" in ci
    assert ci.index("Smoke Pass 91")<ci.index("Smoke Pass 92")
    assert ci.index("Smoke Pass 92")<ci.index("Verify no private map scenes")
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

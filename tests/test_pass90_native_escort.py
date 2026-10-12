"""Pass90 real Wave5 Escort completion and reward verification."""
from conftest import ROOT
def s(p): return (ROOT/p).read_text(encoding="utf8")
def test_native_pass90_requires_grounded_completion_and_domain_reward():
    c=s("src/Twr.Godot/Scripts/Pass90EscortCompletionAcceptance.cs")
    for token in ("TWR_PASS90_GROUNDED_GOAL_OK",
        "TWR_PASS90_ESCORT_FINISH_OK","TWR_PASS90_REWARD_ONCE_OK",
        "TWR_SMOKE_PASS90_ESCORT_COMPLETION_OK",
        'Runtime.StartMap("Ranch")',"new GameplayRoot",
        "RegularWaveRules.WaveDurationSeconds",
        'x.Family=="Escort"',"EscortGroundedCompletion90",
        "Runtime.Match.CompletedObjectives.Contains(id)",
        'Runtime.CompleteObjective(id,"Escort")',
        "expectedCredits=initialCredits+4000",
        "expectedXp=initialXp+3500"):
        assert token in c
    assert "survivor.GlobalPosition=" not in c
    assert "Runtime.Player.Credits=" not in c

def test_pass90_still_runs_old_native_smokes_and_refuses_public_uploads():
    c=s(".github/workflows/windows-build.yml")
    b=s("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=s("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass90"' in b and '"--smoke-pass90"' in session
    assert "new Pass90EscortCompletionAcceptance" in b
    assert "pass90-escort-completion-smoke-profile.json" in session
    assert c.index("Smoke Pass 89")<c.index("Smoke Pass 90")<c.index("Verify no private map scenes")
    assert c.count("uses: actions/upload-artifact@v4")==c.count("if: false && success()")

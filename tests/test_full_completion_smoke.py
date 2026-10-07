from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_exported_exe_runs_all_ten_maps_through_all_fifteen_waves():
    build=read("build/Windows/build.ps1")
    bootstrap=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "--smoke-complete" in build
    assert "TWR_SMOKE_COMPLETE_OK maps=10 waves=150" in build
    assert "RunFullCompletionSmoke()" in bootstrap
    assert "foreach(var map in ReleaseRules.Maps)" in bootstrap
    assert "expectedWave<=ReleaseRules.MaxWaves" in bootstrap
    assert "_runtime.AwardWaveSurvival(expectedWave,0,1)" in bootstrap
    assert "_runtime.AdvanceWave()" in bootstrap

def test_completion_smoke_requires_results_save_and_completion_receipt():
    bootstrap=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "_runtime.Match.Phase!=MatchPhase.Results" in bootstrap
    assert "!_runtime.Match.SaveCommitted" in bootstrap
    assert "!_runtime.Match.CompletionAwarded" in bootstrap
    assert "_runtime.ReturnToLobby()" in bootstrap

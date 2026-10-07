from conftest import ROOT

RULES = ROOT / "src/Twr.Domain/Model/RegularWaveRules.cs"
DIRECTOR = ROOT / "src/Twr.Domain/Services/MatchDirector.cs"
PHASE = ROOT / "src/Twr.Domain/Model/MatchPhase.cs"

def test_wave_fifteen_requires_explicit_completion_boundary():
    s = DIRECTOR.read_text()
    assert "match.Phase = MatchPhase.CompletionPending" in s
    assert "match.Phase == MatchPhase.CompletionPending" in s
    assert "match.Wave == ReleaseRules.MaxWaves" in s
    assert "CompletionPending" in PHASE.read_text()

def test_wave_sixteen_is_not_created():
    s = DIRECTOR.read_text()
    terminal = s.index("if (match.Wave >= ReleaseRules.MaxWaves)")
    increment = s.index("match.Wave++;")
    assert terminal < increment

def test_regular_wave_duration_and_final_scale_are_recovered_contracts():
    s = RULES.read_text()
    assert "WaveDurationSeconds = 300.0" in s
    assert "FinalWaveDifficultyScale = 1.75" in s
    assert "Math.Pow" in s

def test_reconstruction_spawner_curve_is_labeled_fitted():
    s = RULES.read_text()
    assert "FITTED reconstruction spawner curve" in s
    for token in [
        "SpawnBaseIntervalSeconds = 2.2",
        "SpawnPerWaveFalloff = 0.94",
        "SpawnMinimumIntervalSeconds = 0.25",
        "AliveBase = 12",
        "AlivePerWave = 4",
        "AliveCap = 45",
    ]:
        assert token in s

from conftest import ROOT

SERVICE=(ROOT/"src/Twr.Domain/Services/WaveRewardService.cs").read_text()
SESSION=(ROOT/"src/Twr.Domain/Runtime/LocalSession.cs").read_text()
GAME=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()

def test_regular_wave_reward_formula_matches_recovered_bonus_module():
    assert "Math.Pow(playerCount, 0.055) * 1500.0" in SERVICE
    assert "190.0 * Math.Pow(wave, 0.17)" in SERVICE
    assert "completedObjectives * 200" in SERVICE
    assert "completedObjectives * 400" in SERVICE

def test_wave_reward_is_idempotent_per_map_and_wave():
    assert 'WaveSurvival:{mapName}:{wave}' in SERVICE
    assert "receipts.TryIssue" in SERVICE

def test_playable_loop_awards_survival_before_advancing():
    assert "Runtime.AwardWaveSurvival(survivedWave, _completedObjectivesThisWave, 1)" in GAME
    assert GAME.index("AwardWaveSurvival") < GAME.index("Runtime?.AdvanceWave()")

def test_proposed_completion_xp_is_not_applied_by_runtime():
    assert "_completion.Award" not in SESSION
    assert "new MatchCompletedEvent(State.Match.MapName, State.Match.Wave, 0, now)" in SESSION

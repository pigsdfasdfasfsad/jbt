"""Pass96: per-match wave receipts, replay awards and real match lifecycle."""
from conftest import ROOT


def src(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_wave_reward_receipts_are_per_match_not_per_app_session():
    ledger=src("src/Twr.Domain/Services/ReceiptLedger.cs")
    reward=src("src/Twr.Domain/Services/WaveRewardService.cs")
    session=src("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "public void ClearForNewMatch()=>_ids.Clear();" in ledger
    assert "public void ResetForNewMatch()=>receipts.ClearForNewMatch();" in reward
    assert "case StartMatchCommand x:" in session
    assert "_match.Start(State.Match, x.MapName, now);\n                _waveRewards.ResetForNewMatch();" in session
    assert 'var receipt = $"WaveSurvival:{mapName}:{wave}";' in reward
    assert "if (!receipts.TryIssue(receipt)) return false;" in reward
    # The player profile is deliberately NOT reset when replaying the map.
    assert "Profile.Credits = 0" not in session
    assert "Profile.Xp = 0" not in session


def test_bad_wave_claims_and_unsafe_restart_are_denied():
    session=src("src/Twr.Domain/Runtime/LocalSession.cs")
    director=src("src/Twr.Domain/Services/MatchDirector.cs")
    assert "if(m.Phase!=MatchPhase.Lobby)" in director
    assert "throw new InvalidOperationException(" in director
    assert "case AwardWaveSurvivalCommand x:" in session
    assert "if(State.Match.Phase!=MatchPhase.Wave ||" in session
    assert "State.Match.Wave!=x.Wave ||" in session
    assert "!State.Player.IsAlive)" in session
    assert 'PersistProfile($"Wave{x.Wave}Survival", now)' in session


def test_exported_native_production_waves_then_second_same_map_replay():
    game=src("src/Twr.Godot/Scripts/Pass96FullMatchReplayAcceptance.cs")
    for required in (
        'Runtime.StartMap(Map)',
        "new GameplayRoot",
        'private const string Map = "Ranch";',
        "for(var wave=1;wave<=ReleaseRules.MaxWaves;wave++)",
        "RegularWaveRules.CountdownSeconds",
        "RegularWaveRules.WaveDurationSeconds",
        "RegularWaveRules.IntermissionSeconds",
        "game.ActiveInfectedCount<1",
        "Runtime.AwardWaveSurvival(15,0,1);",
        "Runtime.StartMap(\"Cabin\")",
        "Runtime.Match.CompletionAwarded",
        "Runtime.Match.SaveCommitted",
        "new JsonProfileStore(",
        "Runtime.ReturnToLobby()",
        "second run Wave1 rewarded again",
        "TWR_PASS96_FIFTEEN_WAVES_OK",
        "TWR_PASS96_REPLAY_WAVE1_OK",
        "TWR_SMOKE_PASS96_FULL_MATCH_REPLAY_OK",
    ):
        assert required in game
    assert "Runtime.Match.Wave=" not in game
    assert "Runtime.Player.Credits=" not in game
    assert "Runtime.Player.Xp=" not in game
    assert "Runtime.Player.Health=" not in game
    assert "enemy.GlobalPosition=" not in game


def test_pass96_export_retains_native_regressions_and_private_exclusion():
    ci=src(".github/workflows/windows-build.yml")
    bootstrap=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    local=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert '"--smoke-pass96"' in bootstrap and '"--smoke-pass96"' in local
    assert "new Pass96FullMatchReplayAcceptance" in bootstrap
    assert "pass96-full-match-replay-smoke-profile.json" in local
    assert "else if(smoke96)" in local
    assert "Preflight Pass 96 full native" in ci
    assert "'--smoke-pass96'" in ci
    assert "TWR_SMOKE_PASS96_FULL_MATCH_REPLAY_OK" in ci
    assert ci.index("Preflight Pass 96") < ci.index("Smoke test Pass 29")
    assert ci.index("Smoke Pass 95") < ci.index("Verify no private map scenes") if "Smoke Pass 95" in ci else ci.index("Preflight Pass 95") < ci.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass96-wave-replay-persistence'" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

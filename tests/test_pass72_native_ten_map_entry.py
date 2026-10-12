"""Pass72 exported Windows all-ten-map native character, pickups and Wave 2 test."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_all_ten_real_gameplay_scenes_not_domain_only():
    source=read("src/Twr.Godot/Scripts/Pass72NativeTenMapEntryAcceptance.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "foreach (var map in ReleaseRules.Maps)" in source
    assert "ReleaseRules.Maps.Length != 10" in source
    assert "MapCatalogRuntime.All().Count != 10" in source
    assert "new GameplayRoot" in source
    assert "game.GetNodeOrNull<FirstPersonPlayer>" in source
    assert "game.GetNodeOrNull<GameplayHud>" in source
    assert "game.ActivePickupCount < 1" in source
    assert "await PhysicsFrames(48)" in source
    assert "!player.IsOnFloor()" in source
    assert "game.GetChildren().OfType<InfectedAgent>()" in source
    assert "game.ActiveInfectedCount < 1" in source
    assert 'game.WaveStage != "INTERMISSION"' in source
    assert 'game.WaveStage != "COUNTDOWN"' in source
    assert "game._Process(RegularWaveRules.CountdownSeconds + .2)" in source
    assert "Runtime.Match.Wave != 2" in source
    assert "AddChild(_player)" in game
    assert "SpawnInfected(Runtime.Match.Wave)" in game


def test_real_scene_roundtrip_cannot_cheat_authority_or_owner_profile():
    source=read("src/Twr.Godot/Scripts/Pass72NativeTenMapEntryAcceptance.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    bootstrap=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "Runtime.StartMap(map)" in source
    assert "Runtime.FailMatch(" in source
    assert "Runtime.ReturnToLobby()" in source
    assert "game.QueueFree()" in source
    assert "Runtime.Match.SaveCommitted" in source
    assert "Runtime.Match.Phase != MatchPhase.Lobby" in source
    assert "Runtime.Match.Wave =" not in source
    assert "Runtime.Player.Health =" not in source
    assert "Runtime.AwardWaveSurvival(" not in source
    assert "Runtime.AdvanceWave(" not in source
    assert "new StaticBody3D" not in source  # no fabricated raised floor
    assert '"--smoke-pass72"' in session
    assert '"user://pass72-ten-map-entry-smoke-profile.json"' in session
    assert "else if(smoke72)" in session
    assert "new Pass72NativeTenMapEntryAcceptance" in bootstrap


def test_windows_export_runs_all_ten_and_retains_privacy_gate():
    ci=read(".github/workflows/windows-build.yml")
    assert "twr-pass72-native-ten-map-entry" in ci
    assert "Smoke Pass 72 native entry" in ci
    assert "TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK maps=10" in ci
    for name in ("Ranch","Mill","Bypass","Cabin","Cargo","District",
                 "Expressway","Prison","Laboratory","Manor"):
        assert "'"+name+"'" in ci
    assert "--smoke-pass72" in ci
    assert ci.index("Smoke Pass 71") < ci.index("Smoke Pass 72")
    assert ci.index("Smoke Pass 72") < ci.index("Verify no private map scenes")
    assert "if: success() && github.ref_name == 'twr-pass72-native-ten-map-entry'" in ci
    # The separate owner-private exclusion gate intentionally covers later
    # verified branches, so Pass72 need not remain its last disjunct.

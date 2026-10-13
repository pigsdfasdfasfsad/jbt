"""Pass101: real player-spawn ingress to five Load mission depots."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_spawn_input_uses_actual_player_and_five_live_objective_origins():
    test = src("src/Twr.Godot/Scripts/Pass96GroundedMaterialsAcceptance.cs")
    game = src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'private static readonly string[] SpawnIngressMaps101' in test
    for map_name in ("Ranch", "Mill", "Bypass", "District", "Prison"):
        assert f'"{map_name}"' in test
    assert "var initial=player.GlobalPosition;" in test
    assert 'WalkTo100(\n                        player,deposit,map+"/spawn-to-Load-depot")' in test
    assert "if(!player.IsOnFloor())" in test
    assert "TWR_PASS101_MAP_SPAWN_INGRESS_OK map=" in test
    assert "TWR_SMOKE_PASS101_SPAWN_OBJECTIVE_INGRESS_OK" in test
    assert "_spawnToDepotWalks101!=SpawnIngressMaps101.Length" in test
    # Pass102 removed even the controlled initial depot placement.
    assert "player.GlobalPosition=deposit+Vector3.Up*1.1f" not in test
    assert test.count("player.GlobalPosition=") == 0
    assert "Position = _mapLayout.PlayerSpawn" in game
    assert "player.Velocity=" not in test


def test_physical_controls_retained_and_native_windows_run_enforces_markers():
    test = src("src/Twr.Godot/Scripts/Pass96GroundedMaterialsAcceptance.cs")
    ci = src(".github/workflows/windows-build.yml")
    assert "Forward100(true)" in test
    assert "Forward100(false)" in test
    assert "Keycode=Key.W" in test
    assert "player.Rotation=new Vector3(" in test
    assert "await Frames(1)" in test
    assert "player.IsOnFloor()" in test
    assert "TWR_SMOKE_PASS100_MATERIAL_WALK_OK" in ci
    assert "TWR_PASS101_MAP_SPAWN_INGRESS_OK map=$map player_spawn=" in ci
    assert "TWR_SMOKE_PASS101_SPAWN_OBJECTIVE_INGRESS_OK maps=5" in ci
    assert "twr-pass101-spawn-objective-walk" in ci
    assert "Preflight Pass 95 real Manor West and East Escort rotation" in ci
    assert "Verify no private map scenes entered the CI export" in ci
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )

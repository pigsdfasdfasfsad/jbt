"""Pass102: every fallback map uses actual player spawn for Load ingress."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_all_ten_spawn_ingress_require_actual_collision_and_keyboard():
    harness=src("src/Twr.Godot/Scripts/Pass96GroundedMaterialsAcceptance.cs")
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'private static readonly string[] SpawnIngressMaps101' in harness
    assert 'private static readonly string[] SpawnIngressMaps102' in harness
    for name in ("Cabin","Cargo","Laboratory","Expressway","Manor"):
        assert f'"{name}"' in harness
    assert "_spawnToDepotWalks102!=SpawnIngressMaps102.Length" in harness
    assert "_verifiedManorStairClimbs102!=1" in harness
    assert "private async Task<float> WalkGroundIngress102(" in harness
    assert "private async Task<float> WalkManorUpperIngress102(" in harness
    assert "GetVerifiedEscortRoute89(" in harness
    assert "Pass89EscortSurfaceSafety.CanTravel(" in harness
    assert "foreach(var waypoint in route)" in harness
    assert "Pass92ExpresswayEscortGroundNavigation" in harness
    assert 'item=>item.Name=="WestWingUpper"' in harness
    assert "await WalkTo100(" in harness
    assert "Forward100(true)" in harness and "Forward100(false)" in harness
    assert "player.GlobalPosition=deposit" not in harness
    assert "player.GlobalPosition=" not in harness
    assert "player.Velocity=" not in harness
    assert "Position = _mapLayout.PlayerSpawn" in game
    assert "TWR_PASS102_MAP_SPAWN_INGRESS_OK map=" in harness
    assert "TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK" in harness


def test_windows_export_requires_five_new_routes_and_previous_gates():
    ci=src(".github/workflows/windows-build.yml")
    assert "twr-pass102-remaining-spawn-ingress" in ci
    assert "TWR_PASS102_MAP_SPAWN_INGRESS_OK map=$map player_spawn=" in ci
    assert "TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK newly_verified_maps=5 total_verified_maps=10" in ci
    assert "TWR_PASS101_MAP_SPAWN_INGRESS_OK" in ci
    assert "TWR_PASS100_MAP_WALK_OK" in ci
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in ci
    assert "TWR_PASS97_MATERIALS_IN_FULL_MATCH_OK" in ci
    assert "TWR_SMOKE_PASS78_BIDIRECTIONAL_STAIRS_OK" in ci
    assert "Verify no private map scenes entered the CI export" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")


def test_cabin_fallback_enters_on_ground_level_not_unwalkable_floor_lip():
    blockout=src("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    cabin=blockout.split("private static RuntimeMapLayout BuildCabin(Node3D root)",1)[1].split(
        "private static RuntimeMapLayout BuildCargo",1)[0]
    assert 'InteriorShell(root, "CabinHouse"' in cabin
    assert "frontDoorRelief91:true" in cabin
    assert "StaticBox(root,name+\"_Floor\"" in blockout
    assert "bottom-.10f" in blockout

"""Pass100: production material routes are two-way and the player actually walks."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_fallback_materials_require_physical_return_to_depot():
    placement = src("src/Twr.Godot/Scripts/Pass96GroundedMaterials.cs")
    assert "Pass89EscortSurfaceSafety.CanTravel(\n                       world,origin,grounded.Value,exclude)" in placement
    assert "Pass89EscortSurfaceSafety.CanTravel(\n                       world,grounded.Value,origin,exclude)" in placement
    assert "source-absent" in placement or "public fallback maps ONLY" in placement


def test_exported_player_moves_from_real_forward_input_without_route_teleports():
    walk = src("src/Twr.Godot/Scripts/Pass96GroundedMaterialsAcceptance.cs")
    player = src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "private async Task<float> WalkTo100(" in walk
    assert "Input.ParseInputEvent(new InputEventKey" in walk
    assert "Keycode=Key.W" in walk
    assert "Pressed=pressed" in walk
    assert "Forward100(true)" in walk
    assert "Forward100(false)" in walk
    assert "player.Rotation=new Vector3(" in walk
    assert "await Frames(1)" in walk
    assert "player.IsOnFloor()" in walk
    assert "Pass98CarryingMaterial" in walk
    assert "Pass98DepositedMaterials!=1" in walk
    assert "TWR_PASS100_MAP_WALK_OK map=" in walk
    assert "TWR_SMOKE_PASS100_MATERIAL_WALK_OK" in walk
    assert "material_roundtrips=" in walk
    # Pass102 removed even the controlled initial depot placement.
    assert "player.GlobalPosition=deposit+Vector3.Up*1.1f" not in walk
    # Pass102 now starts at each map's natural spawn, even for Manor.
    assert walk.count("player.GlobalPosition=") == 0
    assert "player.GlobalPosition=objective.Pass96MaterialFeet[0]" not in walk
    assert "var world = GlobalTransform.Basis * local" in player
    assert "MoveAndSlide();" in player


def test_ci_requires_all_twenty_exported_windows_roundtrip_markers():
    ci = src(".github/workflows/windows-build.yml")
    assert "twr-pass100-physical-material-walk" in ci
    assert "TWR_PASS100_MAP_WALK_OK map=$map family=$family" in ci
    assert "TWR_SMOKE_PASS100_MATERIAL_WALK_OK maps=10" in ci
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in ci
    assert "TWR_PASS97_MATERIALS_IN_FULL_MATCH_OK" in ci
    assert "TWR_SMOKE_PASS98_MATERIAL_LIFECYCLE_OK" in ci
    assert ci.index("Preflight Pass 96 ten-map") < ci.index("Smoke Pass 78")
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )

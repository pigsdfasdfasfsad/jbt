"""Pass99: a player cannot carry material from multiple objectives simultaneously."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_player_scoped_material_ownership_and_cleanup():
    safety = src("src/Twr.Godot/Scripts/Pass76InteractionSafety.cs")
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    player = src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "ActiveMaterialCarriers99" in safety
    assert "TryClaimMaterialCarry99(" in safety
    assert "ReleaseMaterialCarry99(" in safety
    assert "ReferenceEquals(current, objective)" in safety
    assert "current.IsQueuedForDeletion()" in safety
    assert "ActiveMaterialCarriers99.Remove(player.GetInstanceId())" in safety
    assert "Pass76InteractionSafety.TryClaimMaterialCarry99(Player, this)" in objective
    assert objective.count("Pass76InteractionSafety.ReleaseMaterialCarry99(Player, this)") == 2
    assert "public override void _ExitTree()" in objective
    assert "Pass76InteractionSafety.ForgetPlayer(this)" in player


def test_real_e_key_overlap_regression_is_in_native_acceptance():
    native = src("src/Twr.Godot/Scripts/Pass98MaterialLifecycleAcceptance.cs")
    ci = src(".github/workflows/windows-build.yml")
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in native
    assert "await PressE();" in native
    assert "idle.Pass98CarryingMaterial" in native
    assert "second_press_blocked=true" in native
    assert "twr-pass99-global-material-carry" in ci
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in ci
    assert "TWR_SMOKE_PASS98_MATERIAL_LIFECYCLE_OK" in ci
    assert "if: false && success()" in ci

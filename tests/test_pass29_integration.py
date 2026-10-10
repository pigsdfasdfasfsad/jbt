"""Pass29: source-level integration guards; Windows CI runs the C# native smoke."""
from conftest import ROOT

def text(path):
    return (ROOT / path).read_text(encoding="utf-8")

def test_authoritative_inventory_capacity():
    assert "Pass24LootCapacityPolicy.CanGrant" in text("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert "DefaultSlots = 16" in text("src/Twr.Domain/Services/Pass24LootCapacityPolicy.cs")

def test_inventory_panel_wired_to_gameplay():
    gameplay=text("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "new Pass29InventoryPanel" in gameplay
    panel=text("src/Twr.Godot/Scripts/Pass29InventoryPanel.cs")
    assert "new Label[16]" in panel
    assert "Key.I" in panel
    assert "Read-only" in panel

def test_spores_use_swept_collision_and_tick_clock():
    projectile=text("src/Twr.Godot/Scripts/SporeProjectileRuntime.cs")
    gas=text("src/Twr.Godot/Scripts/SporeCloudRuntime.cs")
    assert "DirectSpaceState.IntersectRay(ray)" in projectile
    assert "WorldOccludes(aim)" in projectile
    assert "Pass24SporeImpact.ClusterDamage" in projectile
    assert "_clock.Advance" in gas
    assert "GasMaskActive" in gas
    assert "BlockedByWorld" in gas

def test_exported_game_has_native_contract_test():
    bootstrap=text("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow=text(".github/workflows/windows-build.yml")
    assert "TWR_SMOKE_PASS29_RUNTIME_OK" in bootstrap
    assert "--smoke-pass29" in bootstrap
    assert "--smoke-pass29" in workflow
    assert "actions/upload-artifact@v4" in workflow

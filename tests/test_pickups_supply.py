from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_pickup_values_preserve_recovered_item_rules():
    s=read("src/Twr.Godot/Scripts/PickupActor.cs")
    assert 'Runtime.HealPlayer(Runtime.HasPerk("Medic") ? 26 : 20)' in s
    assert "Runtime.HealPlayer(0, true)" in s
    assert "Runtime.EquipBodyArmor()" in s
    assert "RuntimeWeaponCatalog.Get(weapon)" in s
    assert "Runtime.GrantAmmo(weapon, spec.AmmoPickup, spec.Reserve)" in s

def test_supply_drop_collection_waits_for_ground():
    s=read("src/Twr.Godot/Scripts/SupplyDropRuntime.cs")
    assert "position.Y <= GroundY" in s
    assert "Landed?.Invoke(position)" in s
    assert "APPROXIMATED" in s

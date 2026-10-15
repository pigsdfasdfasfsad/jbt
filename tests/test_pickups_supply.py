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
    # Configured height is only a failure bound, never fabricated floor.
    assert "position.Y <= GroundY" not in s
    assert 'var hit=GetWorld3D().DirectSpaceState.IntersectRay(ray)' in s
    assert 'Landed?.Invoke(_landing)' in s
    assert 'Callable.From(NotifyOnIdle).CallDeferred()' in s
    assert 'QueueFailure("physical_landing_floor_missing")' in s
    assert "APPROXIMATED" in s

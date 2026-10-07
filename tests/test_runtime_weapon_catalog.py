import json
from conftest import ROOT

def read(path):
    return (ROOT/path).read_text()

def test_runtime_catalog_covers_all_active_weapons():
    catalog=json.loads((ROOT/"content/weapons/catalog.json").read_text())
    assert catalog["active_weapon_count"] == 91
    s=read("src/Twr.Godot/Scripts/RuntimeWeaponCatalog.cs")
    assert 'Content/weapons/catalog.json' in s
    assert 'GetProperty("weapons")' in s
    assert '"Damage"' in s and '"RPM"' in s and '"Mag"' in s and '"Pool"' in s
    assert '"MaxPen"' in s and '"Spread"' in s and '"ProjectileType"' in s

def test_catalog_preserves_verification_status_for_fallback_fields():
    s=read("src/Twr.Godot/Scripts/RuntimeWeaponCatalog.cs")
    assert "AmmoPickupVerified" in s
    assert "RangeVerified" in s
    assert 'weaponType == "Melee" ? 4.5f : 1000f' in s

def test_player_supports_catalog_weapon_families():
    s=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    for token in ["spec.IsAutomatic","spec.IsShotgun","spec.IsMelee","spec.IsLauncher","spec.IsFlamethrower"]:
        assert token in s
    assert "FittedShotgunPellets = 8" in s
    assert "spec.MaxPen" in s
    assert "ExplosiveProjectileRuntime" in s

def test_selected_weapon_ammo_is_configured_through_domain_command():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    session=read("src/Twr.Domain/Runtime/LocalSession.cs")
    ammo=read("src/Twr.Domain/Services/AmmoService.cs")
    assert "ConfigureWeaponAmmo" in player
    assert "case ConfigureWeaponAmmoCommand x:" in session
    assert "_ammo.Configure" in session
    assert "public void Configure" in ammo

def test_special_kill_bonus_covers_recovered_kill_types():
    s=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in ['"Explosion" => "Explosion"','"Fire" => "Fire"','"Decapitation" => "Decapitation"','"BarbedWire" => "BarbedWire"']:
        assert token in s

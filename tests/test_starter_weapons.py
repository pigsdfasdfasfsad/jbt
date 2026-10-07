from conftest import ROOT

def read(path):
    return (ROOT/path).read_text()

def test_all_three_starter_weapons_are_runtime_wired():
    s=read("src/Twr.Godot/Scripts/StarterWeaponCatalog.cs")
    assert "15f" in s and "18," in s and "400," in s and "1.85" in s
    assert "14.5f" in s and "300," in s and "3.7025" in s and "5f" in s
    assert "15.5f" in s and "0.70125" in s
    assert "APPROXIMATED pellet count" in s
    assert "APPROXIMATED melee reach" in s

def test_weapon_switching_is_keyboard_wired():
    s=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "Key.Key1" in s and "SawnOff" in s
    assert "Key.Key2" in s and "Glock17" in s
    assert "Key.Key3" in s and "TwoByFour" in s
    assert "StarterWeaponKind.Shotgun" in s
    assert "StarterWeaponKind.Melee" in s

def test_starter_loadout_preserves_source_ammo_pools():
    s=read("src/Twr.Domain/Services/StarterLoadoutService.cs")
    assert "Ammo[Glock17] = 18" in s and "ReserveAmmo[Glock17] = 136" in s
    assert "Ammo[SawnOff] = 2" in s and "ReserveAmmo[SawnOff] = 32" in s

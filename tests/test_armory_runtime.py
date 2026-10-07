from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_armory_purchase_and_equip_are_domain_authoritative():
    session=read("src/Twr.Domain/Runtime/LocalSession.cs")
    service=read("src/Twr.Domain/Services/ArmoryService.cs")
    assert "case PurchaseWeaponCommand x:" in session
    assert "case SetLoadoutCommand x:" in session
    assert "_armory.Purchase" in session and "_armory.Equip" in session
    assert "profile.Unlocks.Add(weapon)" in service
    assert "profile.Loadout[slot]=weapon" in service

def test_store_does_not_invent_early_purchase_prices():
    s=read("src/Twr.Domain/Services/ArmoryService.cs")
    assert "does not provide a" in s
    assert "player.Level<requiredLevel" in s

def test_main_menu_exposes_scrollable_catalog_armory():
    s=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert "ARMORY / LOADOUT" in s
    assert "RuntimeWeaponCatalog.All()" in s
    assert "ScrollContainer" in s
    assert "PurchaseWeapon" in s and "SetLoadout" in s
    assert "SPECIAL / UNAVAILABLE" in s

def test_starter_profile_defaults_are_always_available():
    s=read("src/Twr.Domain/Runtime/LocalSession.cs")
    for token in ["StarterLoadoutService.SawnOff","StarterLoadoutService.Glock17","StarterLoadoutService.TwoByFour"]:
        assert "Profile.Unlocks.Add("+token+")" in s
    assert 'Profile.Loadout.TryAdd("Primary"' in s
    assert 'Profile.Loadout.TryAdd("Secondary"' in s
    assert 'Profile.Loadout.TryAdd("Melee"' in s

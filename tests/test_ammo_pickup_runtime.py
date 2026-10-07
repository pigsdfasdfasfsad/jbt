from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_wiki_ammo_pickup_table_fills_missing_module_values():
    s=read("src/Twr.Godot/Scripts/RuntimeWeaponCatalog.cs")
    for token in ['["Glock 17"]=17','["Sawn Off Shotgun"]=8','["AK-47"]=30','["RPG-7"]=2','["M320"]=3']:
        assert token in s
    assert "VerifiedAmmoPickups" in s
    assert "wikiHasAmmoPickup" in s

def test_one_ammo_pickup_supplies_both_firearm_slots():
    pickup=read("src/Twr.Godot/Scripts/PickupActor.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "PrimaryWeaponName" in player and "SecondaryWeaponName" in player
    assert "Player.PrimaryWeaponName, Player.SecondaryWeaponName" in pickup
    assert "Runtime.GrantAmmo(weapon, spec.AmmoPickup, spec.Reserve)" in pickup
    assert "one Ammo pickup supplies both primary" in pickup

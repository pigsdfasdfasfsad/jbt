from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_documented_healing_and_armor_values_are_authoritative():
    h=read("src/Twr.Domain/Services/HealingService.cs")
    assert "player.ArmorDurability=40" in h
    d=read("src/Twr.Domain/Services/DamageService.cs")
    assert "healthDamage=raw*0.5f" in d
    assert "APPROXIMATED" in d

def test_item_mutations_live_in_domain_not_presentation():
    s=read("src/Twr.Domain/Runtime/LocalSession.cs")
    for token in ["HealPlayerCommand","EquipBodyArmorCommand","GrantAmmoCommand","GrantItemCommand","ConsumeItemCommand"]:
        assert token in s
    p=read("src/Twr.Domain/Model/PlayerState.cs")
    assert "ArmorDurability" in p and "Inventory" in p

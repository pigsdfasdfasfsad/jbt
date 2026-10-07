from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_perk_menu_reads_all_source_perks_and_enforces_three_slots():
    catalog=read("src/Twr.Godot/Scripts/RuntimePerkCatalog.cs")
    menu=read("src/Twr.Godot/Scripts/PerkMenuRuntime.cs")
    assert 'Content/perks/perks.json' in catalog
    assert 'GetProperty("perks")' in catalog
    assert "EQUIPPED" in menu and "/3" in menu
    assert "Runtime.SetPerk" in menu

def test_core_solo_perk_effects_are_wired():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    for token in ["Speed Demon","Adrenaline Rush","Brisk","Trigger Finger","Heavy Hitter"]:
        assert token in player
    pickup=read("src/Twr.Godot/Scripts/PickupActor.cs")
    assert 'HasPerk("Medic") ? 26 : 20' in pickup
    assert 'HasPerk("Play Maker") ? 2 : 1' in pickup
    assert 'HasPerk("Fortifier") ? 4 : 2' in pickup

def test_fortification_perk_modifiers_match_documented_percentages():
    controller=read("src/Twr.Godot/Scripts/FortificationController.cs")
    assert 'HasPerk("Carpenter") ? SwingSeconds*0.6' in controller
    assert 'HasPerk("Efficiency") && definition.Name=="50 Cal"' in controller
    assert 'multiplier*=1.35f' in controller
    assert 'multiplier*=1.2f' in controller

def test_barbed_wire_uses_documented_15_damage():
    actor=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    assert 'ApplyDamage(15f, false, "BarbedWire")' in actor
    assert "APPROXIMATED" in actor

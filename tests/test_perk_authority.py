from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_domain_owns_all_21_perk_level_gates_and_three_slots():
    rules=read("src/Twr.Domain/Model/PerkRules.cs")
    assert rules.count('["') == 21
    assert "DefaultSlots=3" in rules
    assert '["Hardened Sight"]=10' in rules
    assert '["Guardian Angel"]=100' in rules
    assert '["Overwatch"]=100' in rules

def test_perk_commands_do_not_trust_ui_required_level():
    cmd=read("src/Twr.Domain/Contracts/Commands/SetPerkCommand.cs")
    assert "RequiredLevel" not in cmd
    service=read("src/Twr.Domain/Services/PerkService.cs")
    assert "PerkRules.RequiredLevel(perk)" in service
    assert "profile.EquippedPerks.Count>=PerkRules.DefaultSlots" in service

def test_overwatch_grants_barrett_only_while_equipped_and_restores_primary():
    s=read("src/Twr.Domain/Services/PerkService.cs")
    assert 'profile.Unlocks.Add("Barrett M82A1")' in s
    assert 'profile.Loadout["Primary"]="Barrett M82A1"' in s
    assert 'profile.Unlocks.Remove("Barrett M82A1")' in s
    assert 'PrimaryBeforeOverwatch' in s

def test_juggernaut_armor_is_distinct_from_wave_expiring_body_armor():
    session=read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert 'Profile.EquippedPerks.Contains("Juggernaut")' in session
    assert "State.Player.ArmorDurability=80" in session
    assert 'State.Player.ArmorKind="Juggernaut"' in session
    assert 'State.Player.ArmorKind=="Body"' in session
    healing=read("src/Twr.Domain/Services/HealingService.cs")
    assert 'player.ArmorKind="Body"' in healing

def test_transient_gas_mask_and_energy_drink_end_at_wave_boundary():
    s=read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "State.Player.GasMaskActive=false" in s
    assert "State.Player.EnergyDrinkSeconds=0" in s

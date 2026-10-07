from conftest import ROOT

def read(path):
    return (ROOT / path).read_text()

def test_starter_loadout_is_source_backed():
    s = read("src/Twr.Domain/Services/StarterLoadoutService.cs")
    assert 'Glock17 = "Glock 17"' in s
    assert "Ammo[Glock17] = 18" in s
    assert "ReserveAmmo[Glock17] = 136" in s
    assert 'SawnOff = "Sawn Off Shotgun"' in s
    assert "Ammo[SawnOff] = 2" in s
    assert "ReserveAmmo[SawnOff] = 32" in s

def test_reload_moves_reserve_into_magazine_without_creating_ammo():
    s = read("src/Twr.Domain/Services/AmmoService.cs")
    assert "var moved = Math.Min(needed, reserve)" in s
    assert "player.ReserveAmmo[weapon] = reserve - moved" in s
    assert "player.Ammo[weapon] = loaded + moved" in s

def test_kill_rewards_flow_through_domain_command():
    session = read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "case AwardKillCommand x:" in session
    assert "_killRewards.Award" in session

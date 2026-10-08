from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_headshot_multiplier_is_exact_and_hitbox_boundary_is_labeled():
    infected=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "if (headshot) applied *= 2.5f;" in infected
    assert "InfectedCatalog.DamageMultiplier(InfectedType, damageKind)" in infected
    assert "VERIFIED head multiplier" in infected
    assert "APPROXIMATED geometric boundary" in player

def test_printed_special_kill_rewards_are_preserved():
    s=read("src/Twr.Godot/Scripts/InfectedCatalog.cs")
    for token in ['"Civilian" => (5, 3)', '"Bolter" => (8, 4)', '"Military" => (13, 7)', '"Hazmat" => (38, 15)', '"Bloater" => (50, 25)']:
        assert token in s
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "BonusReward" in game and "context.Headshot" in game

from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_burster_uses_source_damage_radius_and_armor_bypass_gas():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    cloud=read("src/Twr.Godot/Scripts/SporeCloudRuntime.cs")
    assert "27.5f" in game and "blastRadius = 20f" in game
    assert "TickDamage = 5f" in game
    assert "_tickTimer = 0.5" in cloud
    assert 'DamagePlayer(TickDamage, "Spore gas", true)' in cloud
    assert "!context.Headshot" in game

def test_bloater_cluster_uses_source_damage_radius_and_bypasses_armor():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    projectile=read("src/Twr.Godot/Scripts/SporeProjectileRuntime.cs")
    assert "Damage = 26.4f" in game and "Radius = 15f" in game
    assert 'DamagePlayer(Damage * factor, "Spore cluster", true)' in projectile
    assert "APPROXIMATED radial falloff" in projectile

def test_bolter_leap_keeps_direction_and_has_documented_recovery_behavior():
    s=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "_leapVelocity = direction * 28f" in s
    assert "_leapRecovery = 1.0" in s
    assert "fixed-direction leap then temporary immobilization" in s
    assert "APPROXIMATED launch speed" in s

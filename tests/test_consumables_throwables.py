from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_energy_drink_and_gas_mask_match_source_durations_and_wave_rules():
    service=read("src/Twr.Domain/Services/ConsumableService.cs")
    assert "caffeinated ? 40.0 : 30.0" in service
    session=read("src/Twr.Domain/Runtime/LocalSession.cs")
    assert "State.Player.GasMaskActive=false" in session
    assert "State.Player.EnergyDrinkSeconds=0" in session
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "APPROXIMATED magnitude" in player

def test_gas_mask_blocks_spore_gas_but_not_cluster_splash():
    cloud=read("src/Twr.Godot/Scripts/SporeCloudRuntime.cs")
    assert "!Runtime.Player.GasMaskActive" in cloud
    cluster=read("src/Twr.Godot/Scripts/SporeProjectileRuntime.cs")
    assert 'DamagePlayer(Damage * factor, "Spore cluster", true)' in cluster
    assert "GasMaskActive" not in cluster

def test_frag_uses_recovered_module_damage_radius_and_fuse():
    s=read("src/Twr.Godot/Scripts/ThrowableProjectileRuntime.cs")
    assert "FragDamage=300f" in s
    assert "FragRadius=30f" in s
    assert "FragFuse=3.0" in s
    assert "APPROXIMATED falloff equation" in s

def test_molotov_and_nerve_gas_lingering_rules_are_distinct():
    p=read("src/Twr.Godot/Scripts/ThrowableProjectileRuntime.cs")
    h=read("src/Twr.Godot/Scripts/ThrowableHazardRuntime.cs")
    assert 'ThrowableType=="Molotov" ? 32.0 : 34.0' in p
    assert 'if(infected.InfectedType=="Hazmat")continue;' in h
    assert 'infected.ApplyDamage(MolotovTickDamage,false,"Fire")' in h
    assert "infected.ApplySlow(NerveGasSlowFactor,0.7)" in h
    assert "FITTED reconstruction values" in h

def test_pc_grenade_controls_and_carry_caps_are_wired():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    for token in ["Key.Key5","Key.Key6","Key.Key7","Key.G","TryThrowThrowable"]:
        assert token in player
    pickup=read("src/Twr.Godot/Scripts/PickupActor.cs")
    assert '"Frag" or "Molotov" or "Nerve Gas" => 1' in pickup
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in ['"Energy Drink"','"Gas Mask"','"Frag"','"Molotov"','"Nerve Gas"']:
        assert token in game

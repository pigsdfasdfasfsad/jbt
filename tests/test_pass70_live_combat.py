"""Pass70: exported Windows *live* gameplay actors and combat acceptance.

Unlike the earlier 10 x 15 scripted domain progression loop, this
smoke runs a real GameplayRoot, first-person camera, wave-spawned
InfectedAgent collision and virtual mouse/R key inputs. It remains an
automated single-target synthetic stage, not a full survival match.
"""
from conftest import ROOT


def read(name):
    return (ROOT/name).read_text(encoding="utf8")


def test_live_combat_uses_same_gameplay_player_hit_and_reward_pipeline():
    harness=read("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    infected=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "new GameplayRoot" in harness
    assert 'MapName="Laboratory"' in harness
    assert "new StaticBody3D" in harness
    assert "new CollisionShape3D" in harness
    assert "GetNodeOrNull<FirstPersonPlayer>(\"Player\")" in harness
    assert "GetNodeOrNull<GameplayHud>(\"HUD\")" in harness
    assert "await PhysicsFrames(" in harness
    assert "game._Process(RegularWaveRules.CountdownSeconds+" in harness
    assert "game.GetChildren().OfType<InfectedAgent>()" in harness
    assert 'victim.InfectedType="Civilian"' in harness
    assert "PhysicsRayQueryParameters3D.Create" in harness
    assert "IntersectRay(query)" in harness
    assert "player._UnhandledInput(new InputEventMouseButton" in harness
    assert "player._UnhandledInput(new InputEventKey" in harness
    assert '"--smoke-pass70",StringComparer.Ordinal' in player
    assert "Input.MouseMode != Input.MouseModeEnum.Captured" in player
    assert "!OS.GetCmdlineUserArgs().Contains(" in player
    assert "Headless GitHub Windows runners cannot capture" in player
    assert "Input.MouseMode=Input.MouseModeEnum.Captured" in harness
    assert 'Keycode=Key.R' in harness
    assert "SpendAmmo" in player
    assert "FireHitscan(spec, -_camera.GlobalTransform.Basis.Z" in player
    assert "infected.ApplyDamage(appliedDamage, headshot, kind)" in player
    assert "Died?.Invoke(this, new InfectedDeathContext(" in infected
    assert "infected.Died = OnInfectedDied" in game
    assert "Runtime?.AwardKill(infected.InfectedType" in game


def test_authoritative_reload_and_second_wave_hooks_not_directly_mutated():
    harness=read("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "Runtime.Player.Ammo.GetValueOrDefault" in harness
    assert "Runtime.Player.ReserveAmmo.GetValueOrDefault" in harness
    assert "Runtime.Player.Credits<=initialCredits" in harness
    assert "Runtime.Player.Xp<=initialXp" in harness
    assert "afterReload+reserveAfterReload!=afterFire+initialReserve" in harness
    assert "game._Process(RegularWaveRules.WaveDurationSeconds+" in harness
    assert "game._Process(RegularWaveRules.WaveEndSeconds+" in harness
    assert "game._Process(RegularWaveRules.IntermissionSeconds+" in harness
    assert 'game.WaveStage!="WAVE"' in harness
    assert 'game.WaveStage!="WAVE END"' in harness
    assert 'game.WaveStage!="INTERMISSION"' in harness
    assert "Runtime.AwardWaveSurvival" in game
    assert "Runtime.EndWaveCleanup()" in game
    assert "Runtime?.AdvanceWave()" in game
    # Reload remains an authoritative domain call after the alive guard;
    # Pass71 intentionally removes the obsolete null-conditional form.
    assert "Runtime.ReloadWeapon(_reloadWeapon, reloadSpec.Magazine)" in player
    physics=player[player.index("public override void _PhysicsProcess"):player.index("public override void _UnhandledInput")]
    assert physics.index("!Runtime.Player.IsAlive") < physics.index("if (_reloadTimer > 0)")
    assert "Runtime.SpendAmmo(spec.Name, 1)" in player
    # The acceptance harness must not invent reload/ammo credits itself:
    assert "Runtime.AwardKill(" not in harness
    assert "Runtime.SpendAmmo(" not in harness
    assert "Runtime.ReloadWeapon(" not in harness
    assert "Runtime.AdvanceWave()" not in harness


def test_live_combat_test_never_touches_real_profile_or_archived_meshes():
    node=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    harness=read("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    reader=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    assert 'smoke70=OS.GetCmdlineUserArgs().Contains("--smoke-pass70"' in node
    assert '"user://pass70-live-combat-smoke-profile.json"' in node
    assert "if(smoke70 || smoke71)\n            store.Save(new Profile());" in node
    assert "user://pass71-multimap-damage-smoke-profile.json" in node
    assert '"TWR_SMOKE_PASS70_LIVE_COMBAT_OK"' in harness
    assert "real_laboratory_fallback_scene=true" in harness
    assert "no_manual_15_wave_completion_claim=true" in harness
    assert "no_original_private_assets_or_profile_exposed=true" in harness
    assert 'or "--smoke-pass70"' in reader
    assert 'or "--smoke-pass70"' in hierarchy
    assert "AddChild(new Pass70LiveCombatAcceptance" in boot
    assert "new LocalSessionNode" in boot
    assert "RunFullCompletionSmoke()" in boot
    assert "no original source" not in harness.lower() or "private" in harness.lower()


def test_windows_ci_runs_live_combat_after_previous_smokes_and_cleans_packs():
    workflow=read(".github/workflows/windows-build.yml")
    assert "twr-pass70-live-player-combat-loop" in workflow
    assert "Smoke Pass 70 native Laboratory first-person combat" in workflow
    assert "tools/maps/make_pass68_aimcf_fixture.py" in workflow
    assert "TWR_SMOKE_PASS70_LIVE_COMBAT_OK" in workflow
    assert "--quit-after','1200'" in workflow
    assert "Owner-private weapon motor hierarchy sidecar remained" in workflow
    assert "Verify no private map scenes entered the CI export" in workflow
    for n in range(59,71):
        assert "--smoke-pass"+str(n) in workflow
    assert workflow.index("Smoke Pass 69")<workflow.index("Smoke Pass 70")
    assert workflow.index("Smoke Pass 70")<workflow.index(
        "Verify no private map scenes entered the CI export")
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")

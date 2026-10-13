"""Pass111: bounded, source-compatible visual death silhouettes, not ragdoll physics.

The only native acceptance evidence comes from firing a genuine Glock 17
into a real wave infected in the exported Windows game. No private assets.
"""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_fallen_body_uses_actual_visible_meshes_without_extra_collision():
    corpse = read("src/Twr.Godot/Scripts/Pass111FallenInfected.cs")
    for anchor in (
        "class Pass111FallenInfected : Node3D",
        "MaxLiveBodies = 16",
        "MaxSnapshotMeshes = 64",
        "LifetimeSeconds = 5.5f",
        "GetNodeOrNull<InfectedVisualAssembler>(",
        '"InfectedVisual")',
        "GatherVisibleMeshes(visual, meshes);",
        "mesh.IsVisibleInTree()",
        "Mesh = mesh.Mesh",
        "parentInverse=limb.Pivot.GlobalTransform.AffineInverse();",
        "Transform = parentInverse * mesh.GlobalTransform",
        "mesh.GlobalTransform",
        "MaterialOverride = mesh.MaterialOverride",
        "MaterialOverlay = mesh.MaterialOverlay",
        "mesh.GetSurfaceOverrideMaterial(surface)",
        "staticMesh.SetSurfaceOverrideMaterial(surface, material)",
        "corpse.SourceMeshCount++",
        "new Basis(Vector3.Right, pitch)",
        "AgeSeconds >= LifetimeSeconds",
        "QueueFree();",
        "SnapshotWasTruncated = true",
    ):
        assert anchor in corpse, anchor
    for banned in (
        "new CollisionShape3D", "new StaticBody3D",
        "new CharacterBody3D", "new RigidBody3D",
        "new Area3D", "new NavigationAgent3D",
        "HttpClient", "ResourceLoader.Load", "GetTree().CurrentScene",
        "AwardKill(", "CompleteObjective(", "DamagePlayer(",
    ):
        assert banned not in corpse, banned


def test_real_infected_death_callback_snapshots_before_queue_free():
    agent = read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    root = read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert agent.index("Died?.Invoke(this, new InfectedDeathContext") < agent.index(
        "QueueFree();", agent.index("public void ApplyDamage")
    )
    callback = root.split("private void OnInfectedDied(")[1].split(
        "private void OnInfectedSpecialAttack(")[0]
    assert callback.index("SpawnFallenBody111(infected, context.Headshot)") < (
        callback.index('context.DamageKind == "ObjectiveExplosion"'))
    assert callback.index("SpawnFallenBody111(") < callback.index(
        "Runtime?.AwardKill(")
    assert 'if (infected.InfectedType == "Burster"' in callback
    assert "var reward = InfectedCatalog.Reward(infected.InfectedType)" in callback


def test_bounded_corpse_budget_and_wave_cleanup_do_not_change_live_infected():
    root = read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for anchor in (
        "Queue<Pass111FallenInfected>",
        "ActiveFallenBodies111",
        "PeakFallenBodies111",
        "TotalFallenSnapshots111",
        "_fallen111.Count>=Pass111FallenInfected.MaxLiveBodies",
        "var oldest=_fallen111.Dequeue();",
        "oldest.QueueFree();",
        "Pass111FallenInfected.Snapshot(this,infected,headshot)",
        "while (_fallen111.Count>0",
        "infected.GlobalPosition.DistanceTo(_player.GlobalPosition)>70f",
        "private void ClearFallenBodies111()",
        "ClearFallenBodies111();",
        "private void ClearInfected()",
    ):
        assert anchor in root, anchor
    assert 'AddToGroup("infected")' not in read(
        "src/Twr.Godot/Scripts/Pass111FallenInfected.cs")
    assert "_infected.Add(corpse)" not in root
    assert "CollisionLayer" not in read(
        "src/Twr.Godot/Scripts/Pass111FallenInfected.cs")


def test_native_real_weapon_kill_verifies_snapshot_and_wave_cleanup():
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    native = read("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    player = read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert '!OS.GetCmdlineUserArgs().Contains(\n                    "--smoke-pass111",StringComparer.Ordinal)' in player
    assert '!OS.GetCmdlineUserArgs().Contains(\n                    "--smoke-pass70",StringComparer.Ordinal)' in player
    assert 'if (Input.MouseMode != Input.MouseModeEnum.Captured &&' in player
    assert 'args.Contains("--smoke-pass111",StringComparer.Ordinal)' in boot
    assert "VerifyCorpse111=true" in boot
    for anchor in (
        "ButtonIndex=MouseButton.Left",
        "Pressed=true",
        "victim.ApplyDamage(victim.Health-1f,false",
        "var bodies=game.GetChildren().OfType<Pass111FallenInfected>()",
        "game.TotalFallenSnapshots111!=1",
        "game.PeakFallenBodies111!=1",
        "corpse111.SourceMeshCount<8",
        "corpse111.GetChildren().OfType<CollisionObject3D>().Any()",
        "corpse111.FallProgress < .96f",
        "game.ActiveFallenBodies111!=0",
        "corpse111.IsQueuedForDeletion()",
        "TWR_SMOKE_PASS111_INFECTED_CORPSE_OK",
        "authoritative_ammo_spent_once=true",
        "infected_death_awards_credits_and_xp=true",
        "reload_key_replenishes_authoritative_ammo=true",
        "real_wave_end_and_second_wave_transition=true",
    ):
        assert anchor in native, anchor
    assert "game._Process(RegularWaveRules.WaveDurationSeconds+.2)" in native


def test_windows_export_checks_real_combat_and_retains_every_prior_gate():
    ci = read(".github/workflows/windows-build.yml")
    assert "twr-pass111-bounded-infected-corpse-fall" in ci
    assert "Preflight Pass 111 native Glock kill and corpse lifecycle" in ci
    assert "'--smoke-pass111'" in ci
    assert "TWR_SMOKE_PASS111_INFECTED_CORPSE_OK" in ci
    for anchor in (
        "Preflight Pass 110 real two-map repair truck assembly",
        "Preflight Pass 109 reference generator and Repair badge",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 105 native objective pickup and deposit",
        "Preflight Pass 104 full production Wave2 Repair",
        "Preflight Pass 103 real full production Load",
        "Verify no private map scenes entered the CI export",
        "Smoke Pass 78 native three Manor stair flights",
    ):
        assert anchor in ci, anchor
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )


def test_synthetic_weapon_fixture_is_restricted_to_explicit_native_smoke():
    source = read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy = read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    for text in (source, hierarchy):
        assert 'or "--smoke-pass70" or "--smoke-pass111"' in text
    assert "if (synthetic && !testMode)" in source
    assert "if(synthetic && !testMode)" in hierarchy
    ci = read(".github/workflows/windows-build.yml")
    assert "make_pass68_aimcf_fixture.py --source $source --hierarchy $hierarchy --create" in ci
    assert "make_pass68_aimcf_fixture.py --source $source --hierarchy $hierarchy --clean" in ci
    assert "finally {" in ci

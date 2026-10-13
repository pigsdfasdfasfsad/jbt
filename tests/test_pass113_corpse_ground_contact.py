"""Pass113: bound corpse landing to real solid terrain after actual Glock kill.

No recovery of retail Roblox ragdoll ground-contact impulses is claimed.
The headless Windows game must report the measured mesh-to-floor gap after
physics-tick raycast while still finishing real ammunition/reward/reload.
"""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_ground_probe_excludes_live_actors_and_uses_real_static_collision():
    ground = src("src/Twr.Godot/Scripts/Pass113CorpseGroundContact.cs")
    for anchor in (
        "class Pass113CorpseGroundContact",
        "ProbeDepthMeters = 4.2f",
        "MinimumWalkableNormalY = .58f",
        "ClearanceMeters = .035f",
        "MaximumCorrectionMeters = 2.75f",
        "MaximumMeshRadiusMeters = 8f",
        "PhysicsRayQueryParameters3D.Create(from,to)",
        "request.CollisionMask=1",
        "request.CollideWithAreas=false",
        "DirectSpaceState.IntersectRay(request)",
        'hit["collider"].AsGodotObject() is not StaticBody3D',
        'hit["normal"].AsVector3()',
        'hit["position"].AsVector3()',
        "surfaceNormal.Y<MinimumWalkableNormalY",
        "contact.Y>deathOrigin.Y+.205f",
        "TryLowestVisiblePointY(",
        "part.Mesh.GetAabb()",
        "part.GlobalTransform",
        "for(var corner=0;corner<8;corner++)",
        "BoundedCorrection(",
        "-MaximumCorrectionMeters,MaximumCorrectionMeters",
    ):
        assert anchor in ground, anchor
    for forbidden in (
        "new StaticBody3D", "new CollisionShape3D",
        "new RigidBody3D", "new NavigationAgent3D",
        "QueueFree()", "AwardKill(", "CompleteObjective(",
        "GetTree().ChangeScene", "HttpClient",
    ):
        assert forbidden not in ground, forbidden


def test_actual_death_body_keeps_all_limb_and_material_snapshot_paths():
    corpse = src("src/Twr.Godot/Scripts/Pass111FallenInfected.cs")
    for anchor in (
        "MaxLiveBodies = 16",
        "MaxSnapshotMeshes = 64",
        "LifetimeSeconds = 5.5f",
        "GroundProbeAttempted113",
        "WalkableGroundFound113",
        "GroundSettled113",
        "GroundContactError113",
        "GroundCorrectionMeters113",
        "GroundSurfaceHeight113",
        "GroundNormal113",
        "public override void _PhysicsProcess(double delta)",
        "GroundProbeAttempted113=true",
        "Pass113CorpseGroundContact.TryProbe(",
        "out var floorY,out var floorNormal",
        "Pass113CorpseGroundContact.TryLowestVisiblePointY(",
        "Pass113CorpseGroundContact.BoundedCorrection(",
        "var contactProgress=Mathf.Clamp((ease-.42f)/.58f,0f,1f)",
        "GlobalPosition+=Vector3.Up*GroundCorrectionMeters113",
        "GroundSettled113=t>=.99f",
        "GroundContactError113<=.045f",
        "MaximumAppliedLimbDelta112",
        "snapshotParent.AddChild(staticMesh)",
        "MaterialOverride = mesh.MaterialOverride",
        "MaterialOverlay = mesh.MaterialOverlay",
        "corpse.SourceMeshCount++",
    ):
        assert anchor in corpse, anchor
    assert corpse.index("public override void _PhysicsProcess") < corpse.index(
        "public override void _Process")
    # Pass114 adds a terrain-plane branch while preserving the earlier
    # Pass113 flat-ground clearance correction below its else clause.
    assert "if (WalkableGroundFound113)" in corpse
    assert "else if (Pass113CorpseGroundContact.TryLowestVisiblePointY(" in corpse
    assert "AddToGroup" not in corpse
    assert "CollisionLayer" not in corpse
    assert "new CollisionShape3D" not in corpse


def test_native_glock_floor_contact_has_measured_error_and_exactly_once_rewards():
    native = src("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    boot = src("src/Twr.Godot/Scripts/Bootstrap.cs")
    player = src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    for term in (
        'args.Contains("--smoke-pass113",StringComparer.Ordinal)',
        "VerifyCorpse111=true",
        "VerifyArticulation112=true",
        "VerifyGroundContact113=true",
    ):
        assert term in boot
    assert '"--smoke-pass113",StringComparer.Ordinal' in player
    for term in (
        "VerifyGroundContact113",
        "corpse111.GroundProbeAttempted113",
        "corpse111.WalkableGroundFound113",
        "corpse111.GroundSettled113",
        "corpse111.GroundContactError113>.045f",
        "corpse111.GroundNormal113.Y",
        "MaximumCorrectionMeters",
        "TWR_SMOKE_PASS113_CORPSE_GROUND_OK",
        "one_static_floor_probe=true",
        "true_terrain_collision=true",
        "rewards_and_wave_preserved=true",
        "TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK",
        "TWR_SMOKE_PASS111_INFECTED_CORPSE_OK",
        "TWR_SMOKE_PASS70_LIVE_COMBAT_OK",
        "victim.ApplyDamage(victim.Health-1f,false",
        "ButtonIndex=MouseButton.Left",
        "reload_key_replenishes_authoritative_ammo=true",
        "authoritative_ammo_spent_once=true",
    ):
        assert term in native, term


def test_full_windows_regressions_and_private_fixture_safety_retained():
    ci = src(".github/workflows/windows-build.yml")
    assert "twr-pass113-ground-contact-corpse-settling" in ci
    assert "Preflight Pass 113 real corpse ground settling after Glock kill" in ci
    assert "'--smoke-pass113'" in ci
    assert "TWR_SMOKE_PASS113_CORPSE_GROUND_OK" in ci
    assert "contact_error_m=([0-9.]+)" in ci
    assert "$errorMeters -gt 0.045" in ci
    for gate in (
        "Preflight Pass 112 real Glock articulated corpse limb motion",
        "Preflight Pass 111 native Glock kill and corpse lifecycle",
        "Preflight Pass 110 real two-map repair truck assembly",
        "Preflight Pass 109 reference generator and Repair badge",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 104 full production Wave2 Repair",
        "Preflight Pass 103 real full production Load",
        "Verify no private map scenes entered the CI export",
    ):
        assert gate in ci, gate
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )
    for path in (
        "src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs",
        "src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs",
    ):
        source = src(path)
        assert ('or "--smoke-pass112" or "--smoke-pass113"') in source
        assert "synthetic && !testMode" in source
    assert "make_pass68_aimcf_fixture.py --source $source --hierarchy $hierarchy --clean" in ci

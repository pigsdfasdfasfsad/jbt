"""Pass114: make noncolliding fallen R6 visuals conform to static slopes.

The original Roblox Motor6D/ball-socket ragdoll and exact slopes have not
been reconstructed. A real native Glock kill on a tilted collision fixture
must prove the per-mesh support-plane contact and intact inherited gameplay.
"""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_static_slope_normal_produces_clamped_source_pose_orientation():
    support = read("src/Twr.Godot/Scripts/Pass114CorpseSlopeSupport.cs")
    for term in (
        "MaximumVisualTiltRadians = .349066f",
        "MinimumVisualTiltRadians = .05236f",
        "MinimumWalkableNormalY",
        "measuredNormal.LengthSquared()<.1f",
        "Vector3.Up.Cross(normal)",
        "Mathf.Acos(Mathf.Clamp(normal.Y,-1f,1f))",
        "Mathf.Min(angle,MaximumVisualTiltRadians)*smooth",
        "new Basis(axis/axisLength,appliedRadians)",
        "TryLowestSignedDistance(",
        "for(var vertex=0;vertex<8;vertex++)",
        "var transform=mesh.GlobalTransform",
        "(point-planeOrigin).Dot(normal)",
        "VerticalCorrection(",
        "minimumSignedDistance)/normal.Y",
        "-Pass113CorpseGroundContact.MaximumCorrectionMeters",
    ):
        assert term in support, term
    for banned in (
        "new CollisionShape3D", "new StaticBody3D",
        "new RigidBody3D", "new CharacterBody3D",
        "new Area3D", "AwardKill(", "CompleteObjective(", "QueueFree(",
        "HttpClient", "SourceWeaponModels.json.gz",
    ):
        assert banned not in support, banned


def test_slope_landing_preserves_full_mesh_R6_limb_support_and_flat_fallback():
    corpse = read("src/Twr.Godot/Scripts/Pass111FallenInfected.cs")
    for term in (
        "SlopeTiltRadians114",
        "SlopePlaneSupportMeasured114",
        "SlopePlaneGapMeters114",
        "SlopeSettled114",
        "Pass114CorpseSlopeSupport.TiltToNormal(",
        "GroundNormal113,ease,out var tilt114",
        "basis=surfaceTilt*basis",
        "Pass114CorpseSlopeSupport.TryLowestSignedDistance(",
        "GroundSurfaceHeight113,_originalPose.Origin.Z",
        "Pass114CorpseSlopeSupport.VerticalCorrection(",
        "GroundCorrectionMeters113=correction*settle",
        "GroundCorrectionMeters113*GroundNormal113.Y",
        "SlopeSettled114=t>=.99f",
        "SlopePlaneGapMeters114<=.045f",
        "else if (Pass113CorpseGroundContact.TryLowestVisiblePointY(",
        "Pass113CorpseGroundContact.BoundedCorrection(",
        "Pass112R6CorpseArticulation.Delta(",
        "MaxLiveBodies = 16",
        "MaxSnapshotMeshes = 64",
        "LifetimeSeconds = 5.5f",
        "MaterialOverride = mesh.MaterialOverride",
        "MaterialOverlay = mesh.MaterialOverlay",
    ):
        assert term in corpse, term
    assert corpse.count("Pass114CorpseSlopeSupport.TryLowestSignedDistance(")==1
    assert "new RigidBody3D" not in corpse
    assert "CollisionLayer" not in corpse


def test_real_glock_native_slope_acceptance_emits_measured_plane_contact():
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    native = read("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    player = read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    for term in (
        'args.Contains("--smoke-pass114",StringComparer.Ordinal)',
        "VerifySlope114=true",
        "VerifyGroundContact113=true",
        "VerifyCorpse111=true",
        "VerifyArticulation112=true",
    ):
        assert term in boot
    assert '"--smoke-pass114",StringComparer.Ordinal' in player
    for term in (
        "VerifySlope114",
        'Name="Pass114SyntheticSlopedTerrain"',
        "RotationDegrees=new Vector3(0f,0f,16f)",
        "CollisionLayer=1",
        "CollisionMask=0",
        "BoxShape3D",
        "_slopeFixtureBuilt114=true",
        "corpse111.SlopePlaneSupportMeasured114",
        "corpse111.SlopeSettled114",
        "corpse111.SlopeTiltRadians114<.14f",
        "corpse111.GroundNormal113.Y>.99f",
        "corpse111.SlopePlaneGapMeters114>.045f",
        "TWR_SMOKE_PASS114_SLOPE_CORPSE_OK",
        "plane_clearance_error_m=",
        "TWR_SMOKE_PASS113_CORPSE_GROUND_OK",
        "TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK",
        "TWR_SMOKE_PASS111_INFECTED_CORPSE_OK",
        "TWR_SMOKE_PASS70_LIVE_COMBAT_OK",
        "victim.ApplyDamage(victim.Health-1f,false",
        "ButtonIndex=MouseButton.Left",
        "authoritative_ammo_spent_once=true",
        "infected_death_awards_credits_and_xp=true",
        "reload_key_replenishes_authoritative_ammo=true",
    ):
        assert term in native, term


def test_ci_preserves_full_native_regressions_and_test_fixture_isolation():
    ci = read(".github/workflows/windows-build.yml")
    assert "twr-pass114-slope-aligned-corpse-settling" in ci
    assert "Preflight Pass 114 real Glock death on sloped terrain" in ci
    assert "'--smoke-pass114'" in ci
    assert "TWR_SMOKE_PASS114_SLOPE_CORPSE_OK" in ci
    assert "$gap -gt 0.045" in ci
    assert "$tilt -lt 0.14" in ci
    for stage in (
        "Preflight Pass 113 real corpse ground settling",
        "Preflight Pass 112 real Glock articulated corpse limb motion",
        "Preflight Pass 111 native Glock kill and corpse lifecycle",
        "Preflight Pass 110 real two-map repair truck assembly",
        "Preflight Pass 109 reference generator and Repair badge",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 105 native objective pickup and deposit",
        "Preflight Pass 104 full production Wave2 Repair",
        "Preflight Pass 103 real full production Load",
        "Verify no private map scenes entered the CI export",
    ):
        assert stage in ci, stage
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()"
    )
    for path in (
        "src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs",
        "src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs",
    ):
        c = read(path)
        assert 'or "--smoke-pass113" or "--smoke-pass114"' in c
        assert "synthetic && !testMode" in c
    assert "make_pass68_aimcf_fixture.py --source $source --hierarchy $hierarchy --clean" in ci

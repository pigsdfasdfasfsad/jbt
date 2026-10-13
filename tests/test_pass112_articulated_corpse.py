"""Pass112: source-pivot-aware, passive R6 limb motion on a real Glock kill.

Owner source metadata names R6 body regions but does not supply original
Motor6D/ball-socket constraints or motion curves. The rendered corpse remains
a short-lived noncolliding approximation with the Pass111 strict budget.
"""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_recover_both_known_source_and_fallback_r6_shoulder_hip_pivots():
    rig = read("src/Twr.Godot/Scripts/Pass112R6CorpseArticulation.cs")
    for exact in (
        '"LeftArmPivot" or "LeftShoulder"',
        '"RightArmPivot" or "RightShoulder"',
        '"LeftLegPivot" or "LeftHip"',
        '"RightLegPivot" or "RightHip"',
        "mesh.GetParent()",
        "parent != visual",
        "out string group, out Node3D? sourcePivot",
        "sourcePivot = node;",
        'return group switch',
        '"LeftArm"',
        '"RightArm"',
        '"LeftLeg"',
        '"RightLeg"',
        "headshot ? -1f : 1f",
        "Vector3.Zero",
    ):
        assert exact in rig, exact
    assert "GetWorld3D" not in rig
    assert "Random" not in rig
    assert "RigidBody3D" not in rig
    assert "Motor6D" not in rig.split("public static Vector3 Delta(")[-1]


def test_death_frame_meshes_reparent_relative_to_real_r6_limb_pivots():
    corpse = read("src/Twr.Godot/Scripts/Pass111FallenInfected.cs")
    for expected in (
        "Pass112R6CorpseArticulation.TryFindAnchor(",
        "mesh,visual,out var region,out var sourcePivot",
        "corpse._limbs112.TryGetValue(region,out var limb)",
        'Name = "Pass112" + region + "Pivot"',
        "sourcePivot.GlobalTransform",
        "OriginalRotation",
        "parentInverse=limb.Pivot.GlobalTransform.AffineInverse();",
        "Transform = parentInverse * mesh.GlobalTransform",
        "snapshotParent.AddChild(staticMesh);",
        "MeshesOnArticulatedLimbs112++",
        "ArticulatedLimbCount112 => _limbs112.Count",
        "CountSnapshotMeshes(this)",
        "Pass112R6CorpseArticulation.Delta(",
        "pair.Value.Pivot.Rotation=",
        "MaximumAppliedLimbDelta112=Math.Max(",
        "mesh.GetSurfaceOverrideMaterial(surface)",
        "staticMesh.SetSurfaceOverrideMaterial(surface, material)",
        "MaxSnapshotMeshes = 64",
        "LifetimeSeconds = 5.5f",
    ):
        assert expected in corpse, expected
    for forbidden in (
        "AddToGroup(\"infected\")",
        "new CollisionShape3D", "new StaticBody3D",
        "new RigidBody3D", "new CharacterBody3D",
        "AwardKill(", "CompleteObjective(", "HttpClient",
    ):
        assert forbidden not in corpse, forbidden


def test_real_glock_kill_checks_pose_movement_and_all_previous_combat_rules():
    boot = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    native = read("src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs")
    player = read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert 'args.Contains("--smoke-pass112",StringComparer.Ordinal)' in boot
    assert "VerifyCorpse111=true" in boot
    assert "VerifyArticulation112=true" in boot
    assert '"--smoke-pass112",StringComparer.Ordinal)' in player
    assert '"--smoke-pass70",StringComparer.Ordinal)' in player
    assert '"--smoke-pass111",StringComparer.Ordinal)' in player
    for anchor in (
        "victim.ApplyDamage(victim.Health-1f,false",
        "ButtonIndex=MouseButton.Left",
        "player._UnhandledInput(new InputEventMouseButton",
        "SourceMeshCount<8",
        "SnapshotMeshNodeCount!=",
        "ArticulatedLimbCount112!=4",
        "MeshesOnArticulatedLimbs112<6",
        "MaximumAppliedLimbDelta112<.15f",
        "VerifyArticulation112",
        "_articulatedLimbGroups112!=4",
        "_articulatedMeshParts112<6",
        "TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK",
        "TWR_SMOKE_PASS111_INFECTED_CORPSE_OK",
        "authoritative_ammo_spent_once=true",
        "infected_death_awards_credits_and_xp=true",
        "reload_key_replenishes_authoritative_ammo=true",
        "real_wave_end_and_second_wave_transition=true",
    ):
        assert anchor in native, anchor
    assert "corpses.Count" not in native
    # Pass70 deliberately positions its isolated temporary combat platform;
    # this is not teleporting a live player's real match to award a kill.
    assert 'Name="Pass70SyntheticCombatFloor"' in native


def test_pass112_ci_is_full_windows_gated_and_privacy_preserving():
    ci = read(".github/workflows/windows-build.yml")
    assert "twr-pass112-articulated-infected-death-pose" in ci
    assert "Preflight Pass 112 real Glock articulated corpse limb motion" in ci
    assert "'--smoke-pass112'" in ci
    assert "TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK" in ci
    assert "TWR_SMOKE_PASS111_INFECTED_CORPSE_OK" in ci
    assert "TWR_SMOKE_PASS70_LIVE_COMBAT_OK" in ci
    for gate in (
        "Preflight Pass 111 native Glock kill and corpse lifecycle",
        "Preflight Pass 110 real two-map repair truck assembly",
        "Preflight Pass 109 reference generator and Repair badge",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 105 native objective pickup and deposit",
        "Preflight Pass 104 full production Wave2 Repair",
        "Preflight Pass 103 real full production Load",
        "Verify no private map scenes entered the CI export",
    ):
        assert gate in ci, gate
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )


def test_owners_synthetic_weapon_packs_are_only_allowed_in_smoke_modes():
    for name in (
        "src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs",
        "src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs",
    ):
        code = read(name)
        assert 'or "--smoke-pass70" or "--smoke-pass111" or "--smoke-pass112"' in code
        assert "synthetic && !testMode" in code
    assert "GetNodeOrNull<WeaponViewModelRuntime>" in read(
        "src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs"
    )

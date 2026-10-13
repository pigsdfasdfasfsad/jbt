"""Pass109 reference-guided world/carry generator and live Repair HUD.

The owner screenshots are NOT included in public Git. Native verification
walks real Load/Repair objectives, including the original Pass108 carry tests.
"""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_generator_is_a_noncolliding_multicomponent_industrial_proxy():
    visual = src("src/Twr.Godot/Scripts/ObjectiveMaterialVisual106.cs")
    for part in (
        "Housing", "SkidBase", "TopAccessLid", "ServiceDoor",
        "DoorSeam", "FrontControlConsole", "InstrumentDisplay",
        "StatusRed", "StatusGreen", "IntakeVent",
        "LiftingRail", "LeftRailStand", "RightRailStand",
        "LeftExhaustPipe", "RightExhaustPipe",
        "LeftExhaustCap", "RightExhaustCap",
        "LeftSkidFoot", "RightSkidFoot"
    ):
        assert f'"{part}"' in visual or f'"{part}"+vent' in visual, part
    assert "vent<5" in visual
    assert 'materialName.StartsWith("Generator", StringComparison.Ordinal)' in visual
    assert "Pass106RecoveredItemName" in visual
    assert "new CollisionShape3D" not in visual
    assert "StaticBody3D" not in visual
    assert "Mesh=new BoxMesh" in visual
    assert "Mesh=new CylinderMesh" in visual


def test_repair_reference_badge_draws_locally_without_private_image():
    badge = src("src/Twr.Godot/Scripts/Pass109RepairObjectiveBadge.cs")
    hud = src("src/Twr.Godot/Scripts/GameplayHud.cs")
    for term in (
        "DrawCircle(center, 20f, Paper)",
        "DrawArc(center, 18.45f, 0, Mathf.Tau",
        "DrawLine(new Vector2(15.0f, 15.9f)",
        "DrawCircle(new Vector2(30f, 31f)",
        "var jaw = new Vector2(13.6f, 13.2f)",
        "DrawArc(jaw, 6.0f",
        "MouseFilter = MouseFilterEnum.Ignore",
    ):
        assert term in badge, term
    assert "Texture2D" not in badge
    assert "HttpClient" not in badge
    assert "ResourceLoader" not in badge
    assert 'Name = "Pass109RepairObjectiveBadge"' in hud
    assert "SetRepairObjectiveActive109(bool active)" in hud
    assert "_repairBadge109.Visible = active;" in hud


def test_actual_objective_lifecycle_controls_hud_regardless_of_status_callback_order():
    root = src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    hud = src("src/Twr.Godot/Scripts/GameplayHud.cs")
    assert root.count("_hud.SetRepairObjectiveActive109(") >= 3
    assert '_objectives.Any(x => x.Family == "Repair" &&' in root
    assert "GodotObject.IsInstanceValid(x) && !x.IsQueuedForDeletion()" in root
    assert "_hud.SetRepairObjectiveActive109(false);" in root
    assert "objective.StatusChanged = status => _hud.SetObjective(status);" in root
    # Arbitrary status text must never enable the Repair badge when none
    # of the real objectives are Repair.
    assert 'text.StartsWith("REPAIR' not in hud
    assert 'string.IsNullOrWhiteSpace(text)' not in hud


def test_native_real_input_coverage_of_world_and_carried_generator_and_repair_badge():
    bootstrap = src("src/Twr.Godot/Scripts/Bootstrap.cs")
    native = src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    assert 'args.Contains("--smoke-pass109",StringComparer.Ordinal)' in bootstrap
    assert "VerifyReferenceVisuals109=true" in bootstrap
    assert "ExtendToRepair104=true" in bootstrap
    assert "VerifyCarryAim108=true" in bootstrap
    assert 'objective.GetNodeOrNull<Node3D>("Generator")' in native
    assert '"Pass106CarriedObjectiveMaterial"' in native
    assert "CheckGenerator109(" in native
    assert "Pass109RepairBadgeVisible" in native
    assert "Pass109RepairObjectiveBadge" in native
    assert '"StatusGreen"' in native
    assert '"IntakeVent4"' in native
    assert "TWR_SMOKE_PASS109_REFERENCE_VISUALS_OK" in native
    for expected in (
        "_generatorWorldChecks109!=2", "_generatorHeldChecks109!=2",
        "_repairBadgeShown109!=2", "_repairBadgeCleared109!=2",
        "_visibleCarryPickups106<40"
    ):
        assert expected in native
    assert "player.GlobalPosition=" not in native
    assert "player.Velocity=" not in native


def test_pass109_branch_ci_preserves_windows_native_and_privacy_gates():
    workflow = src(".github/workflows/windows-build.yml")
    assert "twr-pass109-reference-generator-repair-icon" in workflow
    assert "Preflight Pass 109 reference generator and Repair badge" in workflow
    assert "'--smoke-pass109'" in workflow
    assert "TWR_SMOKE_PASS109_REFERENCE_VISUALS_OK" in workflow
    for required in (
        "Preflight Pass 103 real full production Load physical walking",
        "Preflight Pass 104 full production Wave2 Repair by physical walking",
        "Preflight Pass 105 native objective pickup and deposit during live infected attack",
        "Preflight Pass 95 real Manor West and East Escort rotation",
        "Verify no private map scenes entered the CI export",
    ):
        assert required in workflow
    assert workflow.count("uses: actions/upload-artifact@v4") == workflow.count(
        "if: false && success()"
    )

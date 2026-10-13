"""Pass106: recognized objective prop silhouettes and camera carry lifecycle."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_reconstructed_equipment_types_have_different_visual_components():
    visuals = src("src/Twr.Godot/Scripts/ObjectiveMaterialVisual106.cs")
    for name in (
        "Generator", "Tool Box", "Propane Tank", "Water Jug",
        "Can Package", "Spark Plug", "Wheel", "Jerry Can"
    ):
        assert f'StartsWith("{name}",StringComparison.Ordinal)' in visuals or (
            f'StartsWith("{name}", StringComparison.Ordinal)' in visuals
        )
    assert "new BoxMesh" in visuals
    assert "new CylinderMesh" in visuals
    assert 'model.SetMeta("Pass106VisualKind",kind);' in visuals
    assert 'model.SetMeta("Pass106RecoveredItemName",materialName);' in visuals
    assert "carried ? \"Pass106CarriedObjectiveMaterial\" : materialName" in visuals
    assert "new CollisionShape3D" not in visuals
    assert "StaticBody3D" not in visuals
    assert "TryGroundDestination" not in visuals
    assert "MapName" not in visuals


def test_world_materials_and_camera_prop_follow_real_interaction_authority():
    objective=src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "ObjectiveMaterialVisual106.Create(\n                names[i],local,false)" in objective
    assert "ShowHeldMaterial106(carriedName);" in objective
    assert "Player.CaptureCamera.AddChild(model);" in objective
    assert "ClearHeldMaterial106();" in objective
    assert objective.count("ClearHeldMaterial106();") >= 3
    assert "Pass106HeldMaterialVisible" in objective
    assert "Pass106HeldMaterialMeshParts" in objective
    assert "Pass106HeldMaterialName" in objective
    assert "Pass76InteractionSafety.TryClaimMaterialCarry99(Player, this)" in objective
    assert "Pass76InteractionSafety.ReleaseMaterialCarry99(Player, this)" in objective
    assert "Pass76InteractionSafety.CanReach(Player, nearest.GlobalPosition, 2.5f)" in objective
    assert "nearest.QueueFree();" in objective
    assert "Runtime?.CompleteObjective(ObjectiveId, Family);" in objective


def test_native_objective_loops_cover_each_production_material_pickup_and_deposit():
    lifecycle=src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    for marker in (
        "CheckCarriedVisual106(objective,player,map+\"/Load/\"+item);",
        "CheckClearedVisual106(objective,player,map+\"/Load/\"+item);",
        "CheckCarriedVisual106(objective,player,map+\"/Repair/\"+item);",
        "CheckClearedVisual106(objective,player,map+\"/Repair/\"+item);",
        "TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK",
        "_visibleCarryPickups106!=expectedVisuals",
        "_clearedCarryDeposits106!=expectedVisuals",
    ):
        assert marker in lifecycle, marker
    assert "player.GlobalPosition=" not in lifecycle
    assert "player.Velocity=" not in lifecycle


def test_native_live_infected_hit_keeps_visual_and_real_deposit_clears_it():
    live=src("src/Twr.Godot/Scripts/Pass105LiveThreatObjectiveAcceptance.cs")
    assert "presentedName106=objective.Pass106HeldMaterialName" in live
    assert "presentedParts106=objective.Pass106HeldMaterialMeshParts" in live
    assert "objective.Pass106HeldMaterialVisible" in live
    assert "disappeared_after_deposit=true" in live
    assert "survived_real_melee_damage=true" in live
    assert "TWR_PASS106_UNDER_FIRE_CARRY_VISUAL_OK" in live
    assert "player.GlobalPosition=" not in live
    assert "spawned.GlobalPosition=lane.Value+Vector3.Up*.47f" in live


def test_windows_build_checks_visuals_reuses_all_prior_native_and_privacy_gates():
    ci=src(".github/workflows/windows-build.yml")
    assert "twr-pass106-material-models-and-carry-view" in ci
    assert "Preflight Pass 103 real full production Load physical walking" in ci
    assert "Preflight Pass 104 full production Wave2 Repair by physical walking" in ci
    assert "Preflight Pass 105 native objective pickup and deposit during live infected attack" in ci
    assert "TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK families=Load maps=Ranch,Manor visual_pickups=20" in ci
    assert "TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK families=Load,Repair maps=Ranch,Manor visual_pickups=" in ci
    assert "TWR_PASS106_UNDER_FIRE_CARRY_VISUAL_OK" in ci
    assert "TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK" in ci
    assert "Smoke Pass 78 native three Manor stair flights" in ci
    assert "Verify no private map scenes entered the CI export" in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()"
    )

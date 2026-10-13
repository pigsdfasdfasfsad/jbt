"""Pass110: a visible pickup-truck repair bay assembled by real deposits.

Godot headless native checks validate model state after every actual W/E
objective interaction. Source-recovered repair QUOTAS do not establish
original Roblox pickup dimensions, colors or mesh fidelity.
"""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_repair_vehicle_has_detailed_approximate_3d_truck_geometry():
    code = src("src/Twr.Godot/Scripts/Pass110RepairVehicleVisual.cs")
    for part in (
        "Chassis", "CabinShell", "CabinRoof", "FrontWindshield",
        "RearWindow", "FrontHood", "FrontGrille", "GrilleSeparator",
        "PickupBed", "BedLining", "BedRailLeft", "BedRailRight",
        "Tailgate", "FrontBumper", "RearBumper",
        "LeftDoor", "RightDoor", "LeftDoorHandle", "RightDoorHandle",
        "LeftMirror", "RightMirror",
        "HeadlightLeft", "HeadlightRight", "BrakeLightLeft", "BrakeLightRight",
        "FrontWheelLeft", "FrontWheelRight", "RearWheelLeft",
        "RearWheelRight", "ServiceTray", "EnginePlug",
        "FuelGaugeTrack", "FuelGaugeFill", "RepairCompletedLamp",
    ):
        # Headlights use conditional string selection; check each branch.
        assert f'"{part}"' in code or f'"{part}"+' in code, part
    assert "new BoxMesh" in code and "new CylinderMesh" in code
    assert "HashSet<string>" in code
    assert "Scale = Vector3.One * (LargeVariant ? 1.15f : 1f);" in code
    assert "new Vector3(2.9f, 0, 1.25f)" in code
    assert 'SetMeta("Pass110RecoveredGeometry",false)' in code
    assert "new CollisionShape3D" not in code
    assert "StaticBody3D" not in code
    assert "CharacterBody3D" not in code
    assert "RigidBody3D" not in code


def test_both_source_named_repair_quotas_animate_only_after_valid_deposit():
    code = src("src/Twr.Godot/Scripts/Pass110RepairVehicleVisual.cs")
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    for marker in (
        'LargeVariant ? "Large Tundra" : "Small Silverado"',
        "LargeVariant ? 6 : 4",
        "4 + 2 + ExpectedFuelCans",
        'StartsWith("Spark Plug ",StringComparison.Ordinal)',
        'StartsWith("Wheel ",StringComparison.Ordinal)',
        'StartsWith("Jerry Can ",StringComparison.Ordinal)',
        "_installedNames.Contains(materialName)",
        "!IsExpectedName(materialName)",
        'name is "Spark Plug A" or "Spark Plug B"',
        '"Wheel A" or "Wheel B"',
        '"Jerry Can A" or "Jerry Can B"',
        '(LargeVariant && (name is "Jerry Can E" or "Jerry Can F"))',
        "_installedNames.Add(materialName)",
        "InstalledWheels >= 2",
        "InstalledSparkPlugs >= 4",
        "AddedFuelCans >= ExpectedFuelCans",
        "_rearLeft.Visible = true",
        "_rearRight.Visible = true",
        "_fuelFill.Scale = new Vector3(",
        "_completeLamp.Visible = true",
    ):
        assert marker in code, marker
    assert "if(UseFallbackMaterialGrounding96)" in objective
    assert 'Name="Pass110RepairVehicle"' in objective
    assert "Pass110RepairVehicleVisual" in objective
    assert "Family==\"Repair\"" in objective
    assert "_repairVehicle110?.PresentSuccessfulDeposit(" in objective
    assert "_repairVehicle110?.PresentSuccessfulDeposit(\n                    _heldMaterialName106);" in objective
    # Make sure the actual interaction conditions were retained.
    assert "Pass76InteractionSafety.CanReach(Player, GlobalPosition, 3f)" in objective
    assert "Pass76InteractionSafety.TryClaimMaterialCarry99(Player, this)" in objective
    assert "Pass76InteractionSafety.ReleaseMaterialCarry99(Player, this)" in objective
    assert "_deposited++;" in objective
    assert "if (_deposited >= _required)" in objective
    assert 'SetMeta("RepairVariant",large ? "Large" : "Small")' in objective


def test_repair_visual_receives_no_physics_or_reward_authority():
    visual = src("src/Twr.Godot/Scripts/Pass110RepairVehicleVisual.cs")
    banned = (
        "CompleteObjective(", "AwardWaveSurvival(", "CompleteMatch(",
        "CanReach(", "PhysicsRayQueryParameters3D", "QueueFree(",
        "AddAmmo(", "Health", "Credits", "Reward", "StatusChanged",
        "ChangeSceneTo", "GetWorld3D", "MoveAndSlide",
    )
    for token in banned:
        assert token not in visual, token


def test_native_physical_mission_checks_visual_install_after_every_actual_e():
    native=src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    bootstrap=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    player=src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert 'args.Contains("--smoke-pass110",StringComparer.Ordinal)' in bootstrap
    assert "VerifyRepairVehicle110=true" in bootstrap
    assert "ExtendToRepair104=true" in bootstrap
    assert "VerifyCarryAim108=true" in bootstrap
    assert "VerifyReferenceVisuals109=true" in bootstrap
    assert 'Contains("--smoke-pass110",' in player
    assert "objective.Pass110RepairVehicle" in native
    assert 'GetMeta("RepairVariant").AsString()' in native
    assert "vehicle110.ExpectedPartCount != objective.Pass98RequiredMaterials" in native
    assert "vehicle110.GetChildren().OfType<CollisionObject3D>().Any()" in native
    assert "CheckRepairVehicle110(" in native
    assert "var carriedName110=objective.Pass106HeldMaterialName;" in native
    assert "expectedSpark110++" in native
    assert "expectedWheels110++" in native
    assert "expectedFuel110++" in native
    assert "vehicle.InstalledWheels != expectedWheels" in native
    assert "vehicle.InstalledSparkPlugs != expectedPlugs" in native
    assert "vehicle.AddedFuelCans != expectedFuel" in native
    assert "var plugs=Math.Min(4,deposits);" not in native
    assert "vehicle110.AssemblyComplete" in native
    assert "vehicle110.AcceptedDeposits==count" in native
    assert "_repairVisualInstallChecks110!=_repairDeposits104" in native
    assert "TWR_SMOKE_PASS110_REPAIR_VEHICLE_OK" in native
    assert "player.GlobalPosition=" not in native
    assert "player.Velocity=" not in native


def test_windows_ci_requires_native_repair_assembly_and_prior_regressions():
    ci=src(".github/workflows/windows-build.yml")
    assert "twr-pass110-repair-vehicle-assembly" in ci
    assert "Preflight Pass 110 real two-map repair truck assembly" in ci
    assert "'--smoke-pass110'" in ci
    assert "TWR_SMOKE_PASS110_REPAIR_VEHICLE_OK" in ci
    for gate in (
        "Preflight Pass 103 real full production Load physical walking",
        "Preflight Pass 104 full production Wave2 Repair by physical walking",
        "Preflight Pass 105 native objective pickup and deposit during live infected attack",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 109 reference generator and Repair badge",
        "Verify no private map scenes entered the CI export",
    ):
        assert gate in ci
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()"
    )

"""Pass108: the *whole* real carried prop fits the camera frustum during
physical E-key objectives, and native RMB ADS never changes item authority."""
from conftest import ROOT


def src(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_frustum_projects_all_mesh_corners_instead_of_just_anchor():
    frustum = src("src/Twr.Godot/Scripts/Pass108CarryFrustum.cs")
    for token in (
        "held.Transform * part.Transform",
        "part.GetAabb()",
        "geometry.GetEndpoint(vertex)",
        "var x = .5f + point.X / (2f * halfW);",
        "var y = .5f - point.Y / (2f * halfH);",
        "point.Z >= -.08f",
        "Right <= .49f",
        "Bottom <= .985f",
        "Left >= .015f",
        "Top >= .10f",
        "public bool EntirePropSafe",
        "public static Bounds Fit(",
        "held.Scale = original * (1f - .05f * (n + 1));",
    ):
        assert token in frustum, token
    assert "StaticBody3D" not in frustum
    assert "CollisionShape3D" not in frustum
    assert "TryClaimMaterialCarry99" not in frustum
    assert "CompleteObjective" not in frustum


def test_real_authoritative_world_items_and_ads_safe_camera_prop():
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    player = src("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert objective.count("FitHeldFrustum108();") == 2
    assert "Pass108CarryFrustum.Fit(_heldMaterial106," in objective
    assert "public bool Pass108HeldSilhouetteSafe" in objective
    assert "Pass106HeldMaterialVisible && Player is not null" in objective
    assert "ObjectiveMaterialVisual106.Create(\n                names[i],local,false)" in objective
    assert "ShowHeldMaterial106(carriedName);" in objective
    assert "ClearHeldMaterial106();" in objective
    assert "Pass76InteractionSafety.TryClaimMaterialCarry99(Player, this)" in objective
    assert "Pass76InteractionSafety.ReleaseMaterialCarry99(Player, this)" in objective
    assert 'OS.GetCmdlineUserArgs().Contains("--smoke-pass108",' in player
    assert "Input.IsMouseButtonPressed(MouseButton.Right)" in player
    assert "(Input.MouseMode==Input.MouseModeEnum.Captured ||" in player
    assert "public bool Pass107Aiming => _aiming;" in player


def test_native_production_load_repair_objective_and_rmb_stow_release():
    bootstrap = src("src/Twr.Godot/Scripts/Bootstrap.cs")
    lifecycle = src("src/Twr.Godot/Scripts/Pass103PhysicalLoadLifecycleAcceptance.cs")
    assert 'args.Contains("--smoke-pass108",StringComparer.Ordinal)' in bootstrap
    assert "ExtendToRepair104=true" in bootstrap
    assert "VerifyCarryAim108=true" in bootstrap
    assert 'Pass108CarryFrustum.Measure(' in lifecycle
    assert "objective.Pass108HeldSilhouetteSafe" in lifecycle
    assert "ButtonIndex=MouseButton.Right" in lifecycle
    assert "PressRight108(true);" in lifecycle
    assert "PressRight108(false);" in lifecycle
    assert "_verifiedAimStows108 != 4" in lifecycle
    assert 'TWR_SMOKE_PASS108_CARRY_ADS_FRUSTUM_OK' in lifecycle
    assert 'await CheckRealAimStow108(' in lifecycle
    assert lifecycle.count('await CheckRealAimStow108(') == 2
    assert "player.GlobalPosition=" not in lifecycle
    assert "player.Velocity=" not in lifecycle
    assert "objective.Pass98CarryingMaterial" in lifecycle
    assert "objective.Pass106HeldMaterialName != itemName" in lifecycle


def test_windows_ci_runs_real_two_wave_native_and_prior_gates():
    workflow = src(".github/workflows/windows-build.yml")
    assert "twr-pass108-native-carry-ads-frustum" in workflow
    assert "Preflight Pass 108 real RMB ADS carry preservation" in workflow
    assert "'--smoke-pass108'" in workflow
    assert "genuine_RMB_aim_stows=4" in workflow
    for gate in (
        "Preflight Pass 103 real full production Load physical walking",
        "Preflight Pass 104 full production Wave2 Repair by physical walking",
        "Preflight Pass 105 native objective pickup and deposit during live infected attack",
        "Preflight Pass 95 real Manor West and East Escort rotation",
        "Verify no private map scenes entered the CI export",
    ):
        assert gate in workflow
    assert workflow.count("uses: actions/upload-artifact@v4") == workflow.count(
        "if: false && success()"
    )

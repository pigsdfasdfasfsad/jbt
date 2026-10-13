"""Pass107: camera-bound objective props remain out of sights, without touching authority."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_actual_pickup_controls_carry_visual_in_runtime():
    source = read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "Pass107CarryPresentation.Apply(" in source
    assert source.count("Pass107CarryPresentation.Apply(") == 2
    assert "ShowHeldMaterial106(carriedName);" in source
    assert "ClearHeldMaterial106();" in source
    assert "Pass76InteractionSafety.TryClaimMaterialCarry99(Player, this)" in source
    assert "Pass76InteractionSafety.ReleaseMaterialCarry99(Player, this)" in source
    assert "Pass107CarryPresentation.Apply(" not in source.split(
        "private void ClearHeldMaterial106()")[1]


def test_viewport_aware_anchor_and_actual_ads_state():
    source = read("src/Twr.Godot/Scripts/Pass107CarryPresentation.cs")
    player = read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "public bool Pass107Aiming => _aiming;" in player
    assert "Mathf.Tan(Mathf.DegToRad(fov) * .5f)" in source
    assert "viewportSize.X / viewportSize.Y" in source
    assert "return new Vector3(" in source
    assert "LeftOfCentre = -.56f" in source
    assert "BelowCentre = -.43f" in source
    assert "aimBlend < .08f && cameraFovDegrees >= 54f" in source
    assert "held.Visible = false;" in source
    assert "held.Visible = true;" in source
    assert "held.Position = HipPosition(" in source
    assert "held.Scale = Vector3.One * HipScale;" in source


def test_carry_visual_does_not_interfere_with_combat_aim_or_placement():
    source = read("src/Twr.Godot/Scripts/Pass107CarryPresentation.cs")
    assert "CollisionShape3D" not in source
    assert "StaticBody3D" not in source
    assert "RayCast3D" not in source
    assert "SpendAmmo(" not in source
    assert "TryClaimMaterialCarry99(" not in source
    assert "CompleteObjective(" not in source
    assert "AimDirection" not in source
    assert "AimOrigin" not in source
    assert "HasSafeAnchor(" in source


def test_ci_branch_registered_without_executable_publication():
    workflow = read(".github/workflows/windows-build.yml")
    assert "twr-pass107-carry-aim-safe-presentation" in workflow
    assert workflow.count("uses: actions/upload-artifact@v4") == workflow.count(
        "if: false && success()"
    )

"""Pass69 source-scaled, reconstructed scope and reflex sight overlays.

Original numeric source ScopeDiameter / ScopeRadius / FOV values are
public normalized weapon module evidence. The scope art is original,
approximate geometry; eight screenshots are named only as private
comparative references and not embedded into distributed artifacts.
"""
from __future__ import annotations
import json
from conftest import ROOT


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def weapon(slug: str):
    return json.loads(read("content/weapons/entries/"+slug+".json"))


def test_original_eight_scope_apertures_and_source_fov_values():
    sizes = {
        "aug": ("AUG", "ScopeDiameter", .106, 40),
        "barrett-m82a1": ("Barrett M82A1", "ScopeDiameter", .205, 10),
        "cbj-ms": ("CBJ-MS", "ScopeRadius", .0555, 60),
        "m110-sass": ("M110 SASS", "ScopeDiameter", .188, 10),
        "m14": ("M14", "ScopeDiameter", .179, 10),
        "svd": ("SVD", "ScopeDiameter", .21, 10),
        "vss": ("VSS", "ScopeDiameter", .21, 10),
        "winchester-model-70": (
            "Winchester Model 70", "ScopeDiameter", .184, 10),
    }
    for slug, (name, key, expected, fov) in sizes.items():
        original=weapon(slug)
        assert original["name"]==name
        assert abs(original["stats"][key]-expected)<1e-6
        assert original["stats"]["FOV"]==fov
        assert original["animations"]["Aim"]["Sequence"][0]["Time"]==.5
    assert "ScopeDiameter" not in weapon("glock-17")["stats"]


def test_optic_profiles_are_bounded_inert_source_data_not_screenshot_assets():
    optic=read("src/Twr.Godot/Scripts/Pass69SourceScopeOptics.cs")
    assert "Pass69SourceScopeOptics : Control" in optic
    assert 'stats.TryGetProperty("ScopeDiameter"' in optic
    assert 'stats.TryGetProperty("ScopeRadius"' in optic
    assert "diameter=half*2f" in optic
    assert 'stats.TryGetProperty("FOV"' in optic
    assert "found[name]=new Profile(name,diameter,fov," in optic
    assert "magnified=name is not (\"AUG\" or \"CBJ-MS\")" in optic
    assert "SourceReferenceStill" in optic
    for pattern in (
        '"AUG-ADS.png"',
        '"CBJMS-ADS.png"',
        '"M82A1-ADS.png"',
        '"M110-ADS.png"',
        '"M14-ADS.png"',
        '"SVD-ADS.png"',
        '"VSS-ADS.png"',
        '"M70-ADS.png"',
    ):
        assert pattern in optic
    assert "Texture2D.Load" not in optic
    assert "FileAccess.Open(" not in optic
    assert "HttpClient" not in optic
    assert "Process.Start" not in optic
    assert "Input.Is" not in optic
    assert "Shoot(" not in optic and "SpendAmmo" not in optic


def test_source_fov_and_eye_relief_projection_rejects_invalid_inputs():
    optic=read("src/Twr.Godot/Scripts/Pass69SourceScopeOptics.cs")
    assert "ProjectRadius(Profile? profile,Vector2 viewport," in optic
    assert "RobloxUnits.MetersPerStud*.5f" in optic
    assert "MathF.Tan(Mathf.DegToRad(cameraFov)*.5f)" in optic
    assert "px=focal*distance/eyeRelief" in optic
    assert "Math.Clamp(px,12f," in optic
    assert "viewport.X<64f || viewport.Y<64f" in optic
    assert "cameraFov is <5f or >100f" in optic
    assert "eyeRelief is <.10f or >2f" in optic
    assert "float.IsFinite(cameraFov)" in optic
    assert "float.IsFinite(eyeRelief)" in optic
    assert "Math.Clamp(blend,0f,1f)" in optic
    assert "_eyeRelief=sourceSightAvailable" in optic
    assert "_blend>.08f" in optic
    assert "Visible=OpticVisible;" in optic


def test_six_procedural_reticle_styles_and_screen_space_layers():
    optic=read("src/Twr.Godot/Scripts/Pass69SourceScopeOptics.cs")
    for name in ("CompactCross","ReflexDot","CircleCross",
                 "Duplex","MilDot","EasternPost"):
        assert name in optic
    for shape in ("DrawCircle(","DrawArc(","DrawLine(",
                  "DrawRect(","DrawColoredPolygon("):
        assert shape in optic
    assert "DrawScopeRim(" in optic
    assert "DrawDarkOutside(" in optic
    assert "if(style==Style.Duplex)" in optic
    assert "index<=4" in optic
    assert "DrawScopeRim(center,radius,rim,edge);" in optic
    assert "DrawReticle(" in optic
    assert "MouseFilter=MouseFilterEnum.Ignore" in optic
    assert "GetTree().Quit(" not in optic
    assert "Input.MouseMode" not in optic


def test_first_person_optics_respect_existing_combat_fov_and_weapon_switches():
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    hud=read("src/Twr.Godot/Scripts/GameplayHud.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'Name="Pass69OpticsCanvas",Layer=9' in player
    assert "AnchorRight=1f,AnchorBottom=1f" in player
    assert "new Pass69SourceScopeOptics" in player
    assert "SourceScopeVisible69" in player
    assert "ResetSourceScope69();" in player
    assert "var sightPoint=_viewModel.Transform*" in player
    assert "_scopeOverlay69?.SetAimState(spec.Name" in player
    assert "_aiming?1f:0f" in player
    assert "_scopeBlend69=Mathf.MoveToward(" in player
    assert "Pass67SourceAimAlignment.SourceSightCameraDepth" in read(
        "src/Twr.Godot/Scripts/Pass69SourceScopeOptics.cs")
    assert "var desiredFov=_aiming ?" in player
    assert "FireHitscan(spec, -_camera.GlobalTransform.Basis.Z" in player
    assert 'if(plus is null' not in player
    assert "SetScopeCrosshairSuppressed(bool suppressed)" in hud
    assert "_crosshair.Visible=!suppressed" in hud
    assert ' _crosshair.Text = "+";' in hud
    assert "_hud.SetScopeCrosshairSuppressed(" in game
    assert "_player.SourceScopeVisible69" in game


def test_native_pass69_exported_game_regression_and_private_asset_exclusion():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow=read(".github/workflows/windows-build.yml")
    source_reader=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "RunPass69Smoke()" in boot
    assert "TWR_SMOKE_PASS69_SOURCE_OPTICS_OK" in boot
    for marker in (
        "original_source_optic_profiles=8",
        "source_scope_diameter_and_radius=true",
        "original_fov_viewport_projection=true",
        "source_reflex_vs_magnified_modes=true",
        "reference_guided_procedural_reticles=true",
        "invalid_fov_depth_and_nan_inputs_rejected=true",
        "source_winchester_aimpart_eye_relief=true",
        "scope_disappears_without_aim_permission=true",
        "original_game_hud_plus_suppressed_and_restored=true",
        "source_pass68_aimcf_reticle_center_unchanged=true",
        "no_ads_screenshot_assets_embedded=true",
        "no_luau_and_no_weapon_damage_changes=true",
        "synthetic_owner_pack_cleanup_required=true",
    ):
        assert marker in boot
    assert 'or "--smoke-pass69"' in source_reader
    assert 'or "--smoke-pass69"' in hierarchy
    assert "twr-pass69-source-optical-reticles" in workflow
    assert "--smoke-pass69" in workflow
    assert "make_pass68_aimcf_fixture.py" in workflow
    assert "--create" in workflow and "--clean" in workflow
    assert "TWR_SMOKE_PASS69_SOURCE_OPTICS_OK" in workflow
    assert "Owner-private weapon motor hierarchy sidecar remained" in workflow
    for n in range(59,70):
        assert "--smoke-pass"+str(n) in workflow
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")

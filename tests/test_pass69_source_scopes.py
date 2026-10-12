"""Pass69 — original scoped optics and safe Godot-only visual masking.

Original ModuleScript Stats.ScopeDiameter/ReduceSensitivity are normalized
into checked-in public weapon records. All original source 3D lens CFrames
remain in the independent SHA-gated owner-only weapon pack. No original
reticle images or scope 2D pixel shader have been recovered.
"""
from __future__ import annotations
import gzip
import json
import subprocess
import sys
from conftest import ROOT

SCOPES={
    "aug":("AUG",.106,False),
    "barrett-m82a1":("Barrett M82A1",.205,True),
    "m110-sass":("M110 SASS",.188,True),
    "m14":("M14",.179,True),
    "svd":("SVD",.21,True),
    "vss":("VSS",.21,True),
    "winchester-model-70":("Winchester Model 70",.184,True),
}


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def weapon(slug):
    return json.loads(read(f"content/weapons/entries/{slug}.json"))


def test_original_seven_scope_diameters_and_six_sensitivity_flags():
    catalog=json.loads(read("content/weapons/catalog.json"))
    scoped=set()
    for entry in catalog["weapons"]:
        data=json.loads(read("content/weapons/"+entry["path"]))
        if "ScopeDiameter" in data.get("stats",{}):
            scoped.add(data["name"])
    assert scoped=={v[0] for v in SCOPES.values()}
    for slug,(name,diameter,reduce) in SCOPES.items():
        stats=weapon(slug)["stats"]
        assert weapon(slug)["name"]==name
        assert stats["ScopeDiameter"]==diameter
        assert stats.get("ReduceSensitivity",False)==reduce
    for slug in ("glock-17","mossberg-500","p90","m4a1"):
        assert "ScopeDiameter" not in weapon(slug)["stats"]


def test_bounded_perspective_aperture_original_metadata_and_reticle_placeholder():
    src=read("src/Twr.Godot/Scripts/Pass69SourceScope.cs")
    assert "public static float SensitivityScale(" in src
    assert "Math.Clamp(liveFov/hipFov,.08f,1f)" in src
    assert "if(profile is null || !profile.ReduceSensitivity)return 1f;" in src
    assert "ScopeDiameter" in src and "ReduceSensitivity" in src
    assert "diameterStuds*RobloxUnits.MetersPerStud" in src
    assert "MathF.Tan(Mathf.DegToRad(verticalFovDegrees)*.5f)" in src
    assert "TryProjectLens(Camera3D camera,Vector3 worldLens" in src
    assert "camera.IsPositionBehind(worldLens)" in src
    assert "camera.UnprojectPosition(worldLens-sideways)" in src
    assert "camera.UnprojectPosition(worldLens+sideways)" in src
    assert "Pass66ArmKinematics.Finite" in src
    assert "public partial class Pass69ScopeMask : Control" in src
    assert "const int bands=100;" in src
    assert "DrawColoredPolygon(" in src
    assert "DrawArc(center,radius" in src
    assert "DrawLine(center-up*reach" in src
    assert "MouseFilter" not in src or "MouseFilter" in read(
        "src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "unavailable original" in src.lower()
    assert "HttpClient" not in src and "Process.Start" not in src
    assert "Damage" not in src and "SpendAmmo" not in src


def test_owner_sha_gated_scope_lens_names_and_first_person_wiring():
    source_reader=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    hud=read("src/Twr.Godot/Scripts/GameplayHud.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "TryGetScopeLensLocal(string weaponName" in source_reader
    for marker in ("ReticleGlass","ScopeGlass",
                   "ScopeReticle","ReflectiveGlass"):
        assert marker in source_reader
    assert "SourceBarrelFacesBackwards(parts)" in source_reader
    assert "if(!Pass69SourceScope.TryGet(weaponName" in source_reader
    assert "if(!synthetic &&" in source_reader
    assert "OriginalPackSha" in source_reader
    assert "TryGetSourceScopeLensWorld69(out Vector3 worldLens)" in view
    assert "OwnerSourceScopeLensVerified69" in view
    assert "TryGetScopeLensLocal(" in view
    assert "_scopeLensLocal69=null" in view
    assert 'Name="ScopeLayer69",Layer=9' in player
    assert 'Name="SourceScopeMask69"' in player
    assert "UpdateSourceScope69(spec);" in player
    assert "Pass69SourceScope.SensitivityScale(" in player
    assert "MouseSensitivity*_scopeSensitivityScale69" in player
    assert "Pass69SourceScope.TryProjectLens(" in player
    assert "Pass69SourceScope.ProjectedDiameterPx(" in player
    assert "Pass67SourceAimAlignment.SourceSightCameraDepth" in player
    assert "SourceScopeMaskAlpha69" in player
    assert "_crosshair.Visible=" in hud
    assert "Player?.SourceScopeMaskAlpha69 is not >.15f" in hud
    assert "Player = _player" in game
    # Scope HUD/sensitivity is client-only. Authoritative firing,
    # weapon FOV rules, damage and reload clocks remain unchanged.
    assert "FireHitscan(spec, -_camera.GlobalTransform.Basis.Z" in player
    assert "var desiredFov=_aiming ?" in player
    assert "spec.AimSeconds" in player


def test_fake_source_lens_models_are_owner_safe_and_have_expected_glass(tmp_path):
    fixture=ROOT/"tools/maps/make_pass69_scope_fixture.py"
    original=tmp_path/"SourceWeaponModels.json.gz"
    hierarchy=tmp_path/"SourceWeaponMotorHierarchy62.json.gz"
    argv=[sys.executable,str(fixture),
          "--source",str(original),"--hierarchy",str(hierarchy)]
    subprocess.run(argv+["--create"],capture_output=True,check=True)
    pack=json.loads(gzip.decompress(original.read_bytes()))
    groups=json.loads(gzip.decompress(hierarchy.read_bytes()))
    assert pack["synthetic"] is True and groups["synthetic"] is True
    assert len(pack["models"])==12
    expected={
        "AUG":"ReticleGlass",
        "Barrett M82A1":"ScopeReticle",
        "M110 SASS":"ReticleGlass",
        "M14":"ScopeGlass",
        "SVD":"ReticleGlass",
        "VSS":"ReticleGlass",
        "Winchester Model 70":"ScopeGlass",
    }
    for name,glass in expected.items():
        parts=pack["models"][name]["parts"]
        assert len([p for p in parts if p["name"]==glass])==1
        assert len([p for p in parts if p["name"]=="AimPart"])==1
        assert "Handle" in [p["name"] for p in parts]
    assert subprocess.run(argv+["--create"],capture_output=True).returncode!=0
    subprocess.run(argv+["--clean"],capture_output=True,check=True)
    assert not original.exists() and not hierarchy.exists()


def test_new_native_windows_scope_smoke_replays_prior_gates_without_private_upload():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    ci=read(".github/workflows/windows-build.yml")
    source_reader=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "RunPass69Smoke()" in boot
    assert "TWR_SMOKE_PASS69_SOURCE_SCOPE_OK" in boot
    for marker in (
        "original_scoped_weapons=7",
        "original_reduce_sensitivity=6",
        "source_aug_no_sensitivity_reduction=true",
        "live_camera_fov_sensitivity=true",
        "perspective_scope_hole_scales_with_fov_and_depth=true",
        "seven_source_lens_name_families=true",
        "procedural_mask_and_reticle_bounded=true",
        "missing_source_glass_fallback=true",
        "no_original_optic_texture_claim=true",
    ):
        assert marker in boot
    assert 'or "--smoke-pass69"' in source_reader
    assert 'or "--smoke-pass69"' in hierarchy
    assert "twr-pass69-source-scope-optics" in ci
    assert "make_pass69_scope_fixture.py" in ci
    assert "TWR_SMOKE_PASS69_SOURCE_SCOPE_OK" in ci
    assert "Owner-private weapon motor hierarchy sidecar remained" in ci
    for n in range(59,70):
        assert "--smoke-pass"+str(n) in ci
    assert ci.count("if: false && success()")>=14
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

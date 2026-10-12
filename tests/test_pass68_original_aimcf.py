"""Pass68 — original source AimCF families with no Luau execution.

Source classifications were recovered from the user's original
ModuleScript weapon files; only family labels/finite special offsets
are checked in. Original scripts and SHA-gated owner weapon models
are deliberately absent from public CI.
"""
from __future__ import annotations
import gzip
import json
import subprocess
import sys
from pathlib import Path

from conftest import ROOT


def source(path):
    return (ROOT / path).read_text(encoding="utf8")


def manifest():
    return json.loads(source("content/weapons/aim_cf_families68.json"))


def test_original_73_aimcf_expression_families_and_special_offsets():
    data=manifest()
    assert data["format"]=="twr-inert-source-aim-formula-families-v1"
    assert data["weapon_count"]==73
    profiles=data["profiles"]
    assert len(profiles)==73
    assert len(set(profiles))==73
    kinds={}
    for name,spec in profiles.items():
        kinds[spec["family"]]=kinds.get(spec["family"],0)+1
        assert name and len(name)<128
        assert all(key in ("family","sight_pre_z_studs","post_z_studs",
                           "special_z_studs") for key in spec)
    assert kinds=={
        "sight_handle_with_base":38,
        "sight_handle_then_base":9,
        "inverse_sight":24,
        "spas_special":1,
        "literal":1,
    }
    assert profiles["Glock 17"]["family"]=="inverse_sight"
    assert profiles["M4A1"]["family"]=="sight_handle_with_base"
    assert profiles["Mossberg 500"]["family"]=="sight_handle_then_base"
    assert profiles["MP7"]=={
        "family":"inverse_sight","sight_pre_z_studs":.45}
    assert profiles["SPAS-12"]=={
        "family":"spas_special","special_z_studs":-.6}
    assert profiles["Winchester Model 70"]=={
        "family":"sight_handle_with_base","post_z_studs":-.2}
    assert profiles["Mosin Nagant"]=={
        "family":"sight_handle_with_base","post_z_studs":-.2}
    assert profiles["Obrez Mosin"]=={
        "family":"sight_handle_with_base","post_z_studs":-.1}
    assert profiles["Flamethrower"]=={"family":"literal"}
    catalog=json.loads(source("content/weapons/catalog.json"))
    names={item["name"] for item in catalog["weapons"]}
    assert set(profiles).issubset(names)


def test_all_source_formula_entries_have_inert_grip_and_aim_records():
    metadata=manifest()["profiles"]
    catalog=json.loads(source("content/weapons/catalog.json"))
    lookups={x["name"]:x["path"] for x in catalog["weapons"]}
    for name,record in metadata.items():
        weapon=json.loads(source(
            "content/weapons/"+lookups[name]))
        assert weapon["name"]==name
        assert "Aim" in weapon["animations"]
        assert weapon["animations"]["Aim"]["Sequence"][0][
            "AV"]["Aim"]["To"]["expression"]=="module.Stats.AimCF"
        if record["family"]!="literal":
            assert "CFrame.new(" in weapon["stats"]["GripCF"]["expression"]
        assert "AimCF" not in weapon["stats"]


def test_source_aimcf_matrix_implementation_is_bounded_and_ordered():
    lib=source("src/Twr.Godot/Scripts/Pass68OriginalAimFormulas.cs")
    assert "Family.InverseSight" in lib
    assert "Family.SightHandleBaseInside" in lib
    assert "Family.SightHandleThenBase" in lib
    assert "Family.SpasSpecial" in lib
    assert "Family.NumericLiteral" in lib
    assert "invGrip*" in lib
    assert "handle.AffineInverse()*sight" in lib
    assert "invSight*handle*invGrip*AimBase" in lib
    assert "(invSight*handle*invGrip)*AimBase" in lib
    assert "source.SpecialZStuds" in lib
    assert "source.PostZStuds" in lib
    assert "source.SightPreZStuds" in lib
    assert "originalAimCf=result;" in lib
    assert "translation" in lib.lower()
    assert "float.IsFinite" in lib
    assert "source.Grip.AffineInverse()" in lib
    assert "sourceAimPart.AffineInverse()" in lib
    assert "reflection*source.Basis*reflection" in lib
    assert "canonical.AffineInverse()" in lib
    assert "ReticleError(candidate," in lib
    assert "Math.Clamp(settled.Z,-1.2f,-.12f)" in lib
    assert "source AimCF" in lib or "AimCF assignment" in lib
    assert "Process.Start" not in lib and "HttpClient" not in lib
    assert "SpendAmmo" not in lib and "AwardKill" not in lib


def test_primary_tool_handle_and_aimpart_source_frames_are_owner_gated():
    reader=source("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    view=source("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    player=source("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "TryGetSourceFormulaFrames(string weaponName" in reader
    assert "TryGetSourceSightLocal(weaponName" in reader
    assert 'p.GetProperty("name").GetString()=="Handle"' in reader
    assert "SourcePosition(p).LengthSquared()<.00001f" in reader
    assert "originalAim=new Transform3D" in reader
    assert "originalHandle=new Transform3D" in reader
    assert "OriginalPackSha" in reader
    assert "OwnerSourcePackVerified = !synthetic" in reader
    assert "SourceExactAimCFAvailable68" in view
    assert "TryGetOriginalAimCF68(out Transform3D originalAimCf)" in view
    assert "Pass68OriginalAimFormulas.TryGet(spec.Name" in view
    assert "Pass68OriginalAimFormulas.TryVisualRoot(" in view
    assert "Pass67SourceAimAlignment.TryAimRoot(sight,out target)" in view
    assert "Pass67SourceAimAlignment.TryGet(spec.Name" in player
    assert "SourceAimReticleError67" in player
    assert "AimDirection => -_camera.GlobalTransform.Basis.Z" in player


def test_synthetic_aimcf_fixture_covers_five_styles_without_private_files(tmp_path):
    fixture=ROOT/"tools/maps/make_pass68_aimcf_fixture.py"
    original=tmp_path/"SourceWeaponModels.json.gz"
    hierarchy=tmp_path/"SourceWeaponMotorHierarchy62.json.gz"
    argv=[sys.executable,str(fixture),
          "--source",str(original),"--hierarchy",str(hierarchy)]
    subprocess.run(argv+["--create"],check=True,capture_output=True)
    source_pack=json.loads(gzip.decompress(original.read_bytes()))
    sidecar=json.loads(gzip.decompress(hierarchy.read_bytes()))
    assert source_pack["synthetic"] is True
    assert sidecar["synthetic"] is True
    assert len(source_pack["models"])==6
    for name in ("Glock 17","Mossberg 500","SPAS-12",
                 "MP7","Winchester Model 70"):
        pieces=source_pack["models"][name]["parts"]
        assert len([p for p in pieces if p["name"]=="AimPart"])==1
        assert "Handle" in [p["name"] for p in pieces]
    assert not any(p["name"]=="AimPart" for p in
                   source_pack["models"]["Mosin Nagant"]["parts"])
    assert subprocess.run(argv+["--create"],
                          capture_output=True).returncode!=0
    subprocess.run(argv+["--clean"],check=True,capture_output=True)
    assert not original.exists() and not hierarchy.exists()


def test_exported_windows_smoke_preserves_all_native_previous_passes():
    boot=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    work=source(".github/workflows/windows-build.yml")
    reader=source("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=source("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "RunPass68Smoke()" in boot
    assert "TWR_SMOKE_PASS68_ORIGINAL_AIMCF_OK" in boot
    assert "original_source_formula_count=73" in boot
    assert "noncommuting_matrix_order_verified=true" in boot
    assert "source_spas_minus_06_literal_semantics=true" in boot
    assert "source_mp7_plus_045_sight_offset=true" in boot
    assert "source_five_weapon_sights_centered=true" in boot
    assert "source_spas_eye_relief_bounded=true" in boot
    assert "source_mp7_special_visual_offset=true" in boot
    assert "original_owner_pack_never_published=true" in boot
    assert 'or "--smoke-pass68"' in reader
    assert 'or "--smoke-pass68"' in hierarchy
    assert "twr-pass68-original-aimcf" in work
    assert "--smoke-pass68" in work
    assert "make_pass68_aimcf_fixture.py" in work
    assert "--create" in work and "--clean" in work
    assert "TWR_SMOKE_PASS68_ORIGINAL_AIMCF_OK" in work
    assert "Owner-private weapon motor hierarchy sidecar remained" in work
    for n in range(59,69):
        assert "--smoke-pass"+str(n) in work
    assert work.count("if: false && success()")>=14
    assert work.count("uses: actions/upload-artifact@v4")==work.count(
        "if: false && success()")

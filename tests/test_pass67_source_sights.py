"""Pass67: original numeric Aim/UnAim cues and SHA-gated AimPart geometry.

This deliberately does NOT claim the dynamic original module.Stats.AimCF
expression has been executed or that the owner's private source pack
can be included in public GitHub Windows artifacts.
"""
from __future__ import annotations
import gzip
import json
import subprocess
import sys
from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf-8")


def gun(slug):
    return json.loads(read("content/weapons/entries/"+slug+".json"))


def test_original_aim_clip_easing_and_durations_are_from_weapon_sources():
    for slug in ("glock-17","mossberg-500","m4a1","p90","ak-47",
                 "mosin-nagant","winchester-model-70"):
        weapon=gun(slug)
        clips=weapon["animations"]
        aim=clips["Aim"]["Sequence"]
        un=clips["UnAim"]["Sequence"]
        assert len(aim)==1 and len(un)==1
        assert abs(aim[0]["Time"]-.5)<.0001
        assert abs(un[0]["Time"]-.25)<.0001
        assert aim[0]["AV"]["Aim"]["To"]["expression"]==(
            "module.Stats.AimCF")
        assert aim[0]["AV"]["Aim"]["Ease"]=="SlowDown2"
        assert un[0]["AV"]["Aim"]["To"]["expression"]=="CFrame.new()"
        assert un[0]["AV"]["Aim"]["Ease"]=="SlowDown"
        assert aim[0]["AV"]["CamAim"]["To"]["expression"]==(
            "CFrame.new(0.25000000000000006, 0.4330127018922193, 0)")
        assert "AimCF" not in weapon["stats"]


def test_source_aim_frame_comes_only_from_owner_verified_invisible_tool_marker():
    reader=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    aim=read("src/Twr.Godot/Scripts/Pass67SourceAimAlignment.cs")
    assert "TryGetSourceSightLocal(string weaponName" in reader
    assert 'p.GetProperty("name").GetString()=="AimPart"' in reader
    assert "if(indices.Length!=1" in reader
    assert "SourceBarrelFacesBackwards(parts)" in reader
    assert "alignment*SourcePosition(part)" in reader
    assert "alignment*SourceRotation(part)" in reader
    assert "if(!LoadDocument()" in reader
    assert "OriginalPackSha" in reader
    assert "OriginalSourceSha" in reader
    assert "Synthetic weapon pack used outside smoke" in reader
    assert "SourceSightCameraDepth=.43f" in aim
    assert "targetPoint-originalSight.Origin" in aim
    assert "new Transform3D(Basis.Identity,offset)" in aim
    assert "offset.LengthSquared()>4f" in aim
    assert "TryAimRoot(Transform3D originalSight" in aim
    assert "ReticleError(" in aim
    assert "Math.Abs(determinant-1f)" in aim
    assert "No Roblox network access" in read(
        "src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")


def test_visual_aim_transition_and_camera_shot_authority_are_not_mixed():
    aim=read("src/Twr.Godot/Scripts/Pass67SourceAimAlignment.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    assert "public enum EaseKind { Linear, SlowDown, SlowDown2 }" in aim
    assert "1f-(1f-t)*(1f-t)*(1f-t)" in aim
    assert "sourceAimVisualBlend67" in player
    assert "TryGetSourceAimRoot(out var sightTarget)" in player
    assert "Pass67SourceAimAlignment.TryGet(spec.Name" in player
    assert "Pass67SourceAimAlignment.Eased(" in player
    assert "Pass60WeaponPoseMath.Interpolate(" in player
    assert "_viewModel.Transform=new Transform3D(" in player
    assert "ResetSourceAimVisual67();" in player
    assert "desiredFov=_aiming ?" in player
    assert "spec.AimSeconds" in player
    assert "AimDirection => -_camera.GlobalTransform.Basis.Z" in player
    assert "FireHitscan(spec, -_camera.GlobalTransform.Basis.Z" in player
    assert "TryGetSourceSightLocal(" in view
    assert "SourceAimGeometryOwnerVerified" in view
    assert "TryGetSourceAimRoot(out Transform3D target)" in view
    assert "_sourceSight67=null" in view
    assert "TryGetCurrentGroupPose" in read(
        "src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    # ADS moves the WeaponViewModel root, so camera-anchored shoulders
    # now counter-transform its animated root before solving endpoints.
    assert "_armRig66.Transform=Transform.AffineInverse();" in view
    assert "var leftWrist=Transform*" in view
    assert "var rightWrist=Transform*" in view
    assert "SpendAmmo" not in aim and "AwardKill" not in aim
    assert "Process.Start" not in aim and "HttpClient" not in aim


def test_synthetic_glock_and_mossberg_have_aimpart_but_bolt_fixture_does_not(tmp_path):
    fixture=ROOT/"tools/maps/make_pass64_bolt_shell_fixture.py"
    original=tmp_path/"SourceWeaponModels.json.gz"
    hierarchy=tmp_path/"SourceWeaponMotorHierarchy62.json.gz"
    cmd=[sys.executable,str(fixture),
        "--source",str(original),"--hierarchy",str(hierarchy)]
    subprocess.run(cmd+["--create"],check=True,capture_output=True)
    data=json.loads(gzip.decompress(original.read_bytes()))
    assert data["synthetic"] is True
    for gun_name in ("Glock 17","Mossberg 500"):
        parts=data["models"][gun_name]["parts"]
        markers=[p for p in parts if p["name"]=="AimPart"]
        assert len(markers)==1
        assert markers[0]["opacity"]==0
        assert "Handle" in [p["name"] for p in parts]
    assert "AimPart" not in [p["name"] for p in data[
        "models"]["Mosin Nagant"]["parts"]]
    subprocess.run(cmd+["--clean"],check=True,capture_output=True)
    assert not original.exists() and not hierarchy.exists()


def test_pass67_native_sight_smoke_reuses_all_prior_gates_and_no_upload():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow=read(".github/workflows/windows-build.yml")
    original=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "--smoke-pass67" in boot and "--smoke-pass67" in workflow
    assert 'or "--smoke-pass67"' in original
    assert 'or "--smoke-pass67"' in hierarchy
    assert "TWR_SMOKE_PASS67_SOURCE_SIGHT_OK" in boot
    assert "source_pistol_and_shotgun_sight_centered=true" in boot
    assert "camera_fixed_shoulders_during_ads=true" in boot
    assert "unsupported_source_sight_uses_fallback=true" in boot
    assert "no_dynamic_luau_aimcf_evaluation=true" in boot
    assert "shooting_recoil_and_fov_authority_unchanged=true" in boot
    assert "twr-pass67-source-sight-alignment" in workflow
    assert "make_pass64_bolt_shell_fixture.py" in workflow
    assert "--create" in workflow and "--clean" in workflow
    assert "Owner-private weapon motor hierarchy sidecar remained" in workflow
    for n in range(59,68):
        assert "--smoke-pass"+str(n) in workflow
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")

"""Pass64 original inert bolt, nested handle and shell ejection records."""
from __future__ import annotations
import gzip
import json
import subprocess
import sys
from conftest import ROOT

FIXTURE=ROOT/"tools/maps/make_pass64_bolt_shell_fixture.py"


def source(path):
    return (ROOT/path).read_text(encoding="utf8")


def gun(slug):
    return json.loads(source("content/weapons/entries/"+slug+".json"))


def test_source_bolt_handle_pump_and_shell_ejection_are_actual_source_records():
    mosin=gun("mosin-nagant")
    win=gun("winchester-model-70")
    moss=gun("mossberg-500")
    spas=gun("spas-12")
    assert mosin["stats"]["ReloadType"]=="Whole"
    assert moss["stats"]["ReloadType"]=="Increment"
    for rifle in (mosin,win):
        events=[v["ModelMotor"]["SlideSliding"]
                for v in rifle["animations"]["Sliding"]["Sequence"]
                if "SlideSliding" in v.get("ModelMotor",{})]
        bolt=[v for v in events if v["MotorHierarchy"]==[
            "Bolt","Handle","BoltMotor"]]
        handle=[v for v in events if v["MotorHierarchy"]==[
            "Bolt","Bolt Handle","Handle","Bolt HandleMotor"]]
        assert len(bolt)==2
        assert len(handle)==2
        assert bolt[0]["To"]["expression"]==(
            "module.Stats.Rels.BoltRel * CFrame.new(0, 0, 0.45)")
        assert bolt[1]["To"]["expression"]=="module.Stats.Rels.BoltRel"
        assert handle[-1]["To"]["expression"]=="CFrame.new()"
    assert mosin["animations"]["Sliding"]["Sequence"][2][
        "ModelMotor"]["SlideSliding"]["To"]["expression"]==(
            "CFrame.Angles(0, 0, (math.pi/2))")
    for shotgun in (moss,spas):
        sequence=shotgun["animations"]["Sliding"]["Sequence"]
        shells=[x for x in sequence if "Shell" in x]
        assert len(shells)==1
        spec=shells[0]["Shell"]["Shells"][0]
        assert spec["Name"]=="12 Gauge Shell"
        assert spec["Vel"]==15
        assert spec["Dir"]["expression"]=="Vector3.new(0.86602, 0.5, 0)"


def test_bounded_source_action_parser_rejects_executable_luau_and_preserves_authority():
    action=source("src/Twr.Godot/Scripts/Pass64SourceAction.cs")
    assert "TryBoltHandleAngle" in action
    assert 'term=="(math.pi/2)"' in action
    assert "TryShellDirection" in action
    assert "NumberStyles.Float" in action
    assert "float.IsFinite" in action
    assert "MotorPath(motor,false)" in action
    assert "MotorPath(motor,true)" in action
    assert 'TryRelativeMotorOffset(' in action
    assert '"BoltRel"' in action
    assert "sourceShell" in action
    assert "source.Name" not in action
    assert "speed*RobloxUnits.MetersPerStud" in action
    assert "Pass60WeaponPoseMath.TryParse(str.GetString()" in action
    assert "found[weapon]=data" in action
    assert "HttpClient" not in action
    assert "Process.Start" not in action
    assert "SpendAmmo" not in action
    assert "Credits" not in action


def test_native_nested_bolt_composition_and_casing_reclaim_are_render_only():
    binding=source("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    view=source("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    bootstrap=source("src/Twr.Godot/Scripts/Bootstrap.cs")
    ci=source(".github/workflows/windows-build.yml")
    assert "ApplyBoltAction(Vector3 delta,float handleRadians)" in binding
    assert '"Bolt/Bolt Handle"' in binding
    assert "part.Transform=carrierDelta*" in binding
    assert "nestedPart ? twist : Transform3D.Identity" in binding
    assert "Pass64SourceAction.TryGet(spec.Name,out var action)" in view
    assert "_sourceHierarchy.ApplyBoltAction(" in view
    assert "SourceNestedBoltHandleParts" in view
    assert "SourceBoltCarrierCueCount" in view
    assert "Pass64SourceAction.Sample(boltClip.Bolt,boltTime)" in view
    assert "SpawnSourceShell(" in view
    assert "SpentShellProxy64" in view
    assert "shell.Part.QueueFree();" in view
    assert "SourceIncrementalShellInsertPoseActive" in view
    assert "TWR_SMOKE_PASS64_SOURCE_BOLT_SHELL_OK" in bootstrap
    assert "source_incremental_shell_feed_pose=true" in bootstrap
    assert "source_shell_proxy_lifetime_bounded=true" in bootstrap
    assert "--smoke-pass64" in ci
    assert "make_pass64_bolt_shell_fixture.py" in ci
    assert "TWR_SMOKE_PASS64_SOURCE_BOLT_SHELL_OK" in ci
    assert "twr-pass64-source-shell-and-bolt" in ci
    assert "Owner-private weapon motor hierarchy sidecar remained" in ci
    assert ci.count("if: false && success()")>=14
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")


def test_fake_nested_bolt_and_shotgun_pack_is_deterministic_owner_safe(tmp_path):
    original=tmp_path/"Content"/"Weapons"/"SourceWeaponModels.json.gz"
    hierarchy=tmp_path/"Content"/"Weapons"/"SourceWeaponMotorHierarchy62.json.gz"
    base=[sys.executable,str(FIXTURE),"--source",str(original),
          "--hierarchy",str(hierarchy)]
    subprocess.run(base+["--create"],check=True,capture_output=True)
    a=json.loads(gzip.decompress(original.read_bytes()))
    b=json.loads(gzip.decompress(hierarchy.read_bytes()))
    assert a["synthetic"] is True and b["synthetic"] is True
    assert b["source_model_count"]==3
    for gun_name in ("Glock 17","Mossberg 500","Mosin Nagant"):
        assert gun_name in a["models"] and gun_name in b["models"]
    groups=b["models"]["Mosin Nagant"]["groups"]
    assert [g["path"] for g in groups]==[
        ["Bolt"],["Bolt","Bolt Handle"]]
    assert groups[0]["part_indices"]==[1,2,3,4,5]
    assert groups[1]["part_indices"]==[3,4,5]
    mosin_parts=a["models"]["Mosin Nagant"]["parts"]
    assert [p["name"] for p in mosin_parts]==[
        "Handle","Handle","BoltBody","Handle",
        "BoltKnob","BoltLever","Barrel","Stock"]
    assert subprocess.run(base+["--create"],capture_output=True).returncode!=0
    subprocess.run(base+["--clean"],check=True,capture_output=True)
    assert not original.exists() and not hierarchy.exists()

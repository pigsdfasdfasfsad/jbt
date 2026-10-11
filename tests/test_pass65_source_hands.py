"""Pass65: source-authored left and right hand grips/welds, not avatar meshes."""
import json
from conftest import ROOT


def read(name):
    return (ROOT/name).read_text(encoding="utf8")


def entry(slug):
    return json.loads(read("content/weapons/entries/"+slug+".json"))


def track(gun,clip,side):
    return [(step["Time"],step["AV"][side])
            for step in gun["animations"][clip]["Sequence"]
            if side in step.get("AV",{})]


def test_source_hand_grips_and_weld_tracks_are_original_immutable_records():
    glock=entry("glock-17")
    moss=entry("mossberg-500")
    mosin=entry("mosin-nagant")
    ak=entry("ak-47")
    assert glock["stats"]["LeftCF"]["expression"].startswith(
        "CFrame.new(-0.5, -0.35, 0.475)")
    assert glock["stats"]["RightCF"]["expression"].startswith(
        "CFrame.new(0.165, -0.1, 0.625)")
    assert "CFrame.Angles((math.pi/2)" in glock["stats"]["LeftCF"]["expression"]
    assert len(track(glock,"Reload","LeftWeld"))==6
    assert len(track(glock,"Reload","RightWeld"))==2
    assert len(track(glock,"Equip","LeftWeld"))==3
    assert any(step["TargetChar"] is True
        for _,step in track(glock,"Reload","LeftWeld") if "TargetChar" in step)
    assert len(track(moss,"Sliding","LeftWeld"))==2
    assert track(moss,"Sliding","LeftWeld")[0][1][
        "OffsetEnd"]["expression"]==(
        "module.Stats.Rels.PumpRel:inverse() * module.Stats.LeftCF")
    assert len(track(mosin,"Sliding","RightWeld"))==4
    assert len(track(mosin,"Reload","RightWeld"))==7
    assert len(track(ak,"Reload","LeftWeld"))==6
    assert all("LeftCF" in data["stats"] and "RightCF" in data["stats"]
               for data in (glock,moss,mosin,ak))


def test_hand_literal_parser_is_bounded_and_rejects_luau_execution():
    s=read("src/Twr.Godot/Scripts/Pass65SourceHands.cs")
    assert "CFrame.new(" in s and "CFrame.Angles(" in s
    assert 'token=="(math.pi/2)"' in s
    assert 'token=="(-math.pi/2)"' in s
    assert "NumberStyles.Float" in s
    assert "Math.Abs(result)<=Mathf.Pi+.001f" in s
    assert "Pass60WeaponPoseMath.TryParse(term,null,out factor)" in s
    assert "var reflect=new Basis(Vector3.Right,Vector3.Up,Vector3.Forward)" in s
    assert "float.IsFinite" in s
    assert "TargetChar" in s
    assert "TargetHierarchy" in s
    assert "module.Stats.LeftCF" in s
    assert "module.Stats.RightCF" in s
    assert '"module.Stats.Rels."+name+"Rel:inverse() * "+handSymbol' in s
    assert "binding.TryGetCurrentGroupPose(target.Group" in s
    assert "binding.TryGetGroupRestPose(target.Group" in s
    assert "group*rest.AffineInverse()*target.Offset" in s
    assert "Pass60WeaponPoseMath.Interpolate(from,to," in s
    assert "HttpClient" not in s and "Process.Start" not in s
    assert "SpendAmmo" not in s and "Credits" not in s


def test_native_source_hands_are_procedural_and_do_not_mutate_fire_authority():
    v=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "Pass65SourceHands.TryGet(spec.Name,out var hands)" in v
    assert "LeftHandProxy65" in v and "RightHandProxy65" in v
    assert "PalmProxy65" in v and "Finger{finger}Proxy65" in v
    assert "SourceHandProxyMeshCount+=7" in v
    assert "BuildSourceHandProxies();" in v
    assert "UpdateSourceHands(reloadActive,reloadFraction);" in v
    assert "Pass65SourceHands.Sample(_handSource," in v
    assert "SourceHandAnimationActive" in v
    assert "SourceRightCyclingWeldCues" in v
    assert "SourceLeftReloadWeldCues" in v
    assert "TryGetCurrentGroupPose" in hierarchy
    assert "part.Transform*home.AffineInverse()*group.Pivot" in hierarchy
    assert "OriginalWeaponSourceRuntime.TryBuild(_rig!, spec.Name)" in v
    assert "Reload(double seconds)" in v and "Fire(bool aiming = false)" in v
    assert "no_luau_execution" not in v


def test_pass65_windows_native_smoke_keeps_owner_packs_separate():
    workflow=read(".github/workflows/windows-build.yml")
    bootstrap=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    original=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "twr-pass65-source-hand-grips" in workflow
    assert "--smoke-pass65" in workflow
    assert "make_pass64_bolt_shell_fixture.py" in workflow
    assert "TWR_SMOKE_PASS65_SOURCE_HAND_GRIPS_OK" in workflow
    assert "TWR_SMOKE_PASS65_SOURCE_HAND_GRIPS_OK" in bootstrap
    assert "source_mossberg_left_pump_grip_follows=true" in bootstrap
    assert "source_mosin_right_bolt_handle_grip_follows=true" in bootstrap
    assert "source_reload_magazine_hand_follows=true" in bootstrap
    assert "no_original_character_mesh_claim=true" in bootstrap
    assert 'or "--smoke-pass65"' in original
    assert 'or "--smoke-pass65"' in hierarchy
    assert "--create" in workflow and "--clean" in workflow
    assert "Owner-private weapon motor hierarchy sidecar remained" in workflow
    for pass_num in range(59,66):
        assert "--smoke-pass"+str(pass_num) in workflow
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")

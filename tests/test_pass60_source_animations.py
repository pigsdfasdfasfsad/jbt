"""Pass60: bounded original source weapon animation keyframes in native FPS."""
import json
from conftest import ROOT


def read(name):
    return (ROOT / name).read_text(encoding="utf8")


def test_source_reload_and_recoil_records_exist_in_original_normalized_weapons():
    glock=json.loads(read("content/weapons/entries/glock-17.json"))
    ak=json.loads(read("content/weapons/entries/ak-47.json"))
    for gun in (glock,ak):
        assert len(gun["animations"]["Reload"]["Sequence"])>=10
        assert "GripCF" in gun["stats"] and "SprintCF" in gun["stats"]
        assert "CFrame.Angles(" in gun["stats"]["SprintCF"]["expression"]
        frames=gun["animations"]["Reload"]["Sequence"]
        assert any("Main" in frame.get("AV", {}) for frame in frames)
        assert any("CFrame.new(" in frame["AV"]["Main"]["To"]["expression"]
                   for frame in frames
                   if "Main" in frame.get("AV", {}) and
                      "To" in frame["AV"]["Main"])
    assert glock["animations"]["Fire"]["Sequence"][0]["Time"]==0.075
    assert glock["animations"]["Fire"]["Sequence"][0]["AV"]["Fire"]["To"][
        "expression"].startswith("CFrame.new(0, 0, 0.1)")
    assert glock["animations"]["AimFire"]["Sequence"][0]["AV"]["Fire"]["To"][
        "expression"].startswith("CFrame.new(0, 0, 0.05)")


def test_source_pose_parser_is_strict_and_interpolates_quaternions():
    src=read("src/Twr.Godot/Scripts/Pass60WeaponPoseMath.cs")
    assert "CFrame.new(" in src and "CFrame.Angles(" in src
    assert 'term=="module.Stats.GripCF" && sourceGrip.HasValue' in src
    assert "var terms=expression.Split(" in src
    assert "NumberStyles.Float" in src and "float.IsFinite" in src
    assert "basis=basis.Orthonormalized();" in src
    assert "value*=next;" in src
    assert "Slerp(b,t)" in src and "Origin.Lerp" in src
    assert "HttpClient" not in src and "Process.Start" not in src


def test_original_motion_library_samples_reload_without_gameplay_authority():
    src=read("src/Twr.Godot/Scripts/Pass60WeaponAnimationLibrary.cs")
    assert "res://Content/weapons/catalog.json" in src
    assert 'Sequence(animations,"Reload",out var steps)' in src
    assert 'FrameTarget(main,out var literal)' in src
    assert "grip.Inverse()*absolute" in src
    assert "new ReloadFrame(" in src
    assert "progress*clip.Duration" in src
    assert "if(targets<2" in src
    assert "new Shot(pose,timeOut,timeBack)" in src
    assert "SourceMotionCount" in src
    assert "SpendAmmo" not in src and "Player.Credits" not in src
    assert "HttpClient" not in src and "Process.Start" not in src


def test_pass59_legacy_contract_repaired_and_pass60_runtime_bound():
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow=read(".github/workflows/windows-build.yml")
    assert "Pass60WeaponAnimationLibrary.TryGet(spec.Name,out var motion)" in view
    assert "Pass60WeaponAnimationLibrary.SampleReload(" in view
    assert "reloadPose.Basis*sourceFirePose.Basis*sway" in view
    assert "SourceReloadFrames" in view
    assert "SourceSprintRotationAvailable" in view
    assert "poseMotion.Sprint.Basis.GetRotationQuaternion()" in player
    assert "poseMotion.Sprint.Origin" in player
    assert "The dynamic original" in player
    assert player.count("_viewModel.Fire(_aiming);")==2
    assert "TWR_SMOKE_PASS59_SOURCE_MOTION_OK" in boot
    assert "TWR_SMOKE_PASS60_ANIMATION_OK" in boot
    assert "Math.Abs(glock.Fire.Value.Target.Origin.Z-.028f)" in boot
    assert "original_reload_keyframes=true" in boot
    assert "--smoke-pass59" in workflow and "--smoke-pass60" in workflow
    assert "TWR_SMOKE_PASS60_ANIMATION_OK" in workflow
    assert "twr-pass60-source-weapon-animations" in workflow
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")

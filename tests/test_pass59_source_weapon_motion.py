"""Pass59 source-normalized recoil pose regression tests."""
import json
from conftest import ROOT

def read(path):
    return (ROOT/path).read_text(encoding="utf8")

def test_per_weapon_original_fire_pulls_are_distinct_and_supported():
    glock=json.loads(read("content/weapons/entries/glock-17.json"))
    shots=glock["animations"]
    for name in ("Fire","AimFire"):
        seq=shots[name]["Sequence"]
        assert len(seq)>=2 and 0<seq[0]["Time"]<2
        assert 0<seq[1]["Time"]<2
        assert seq[0]["AV"]["Fire"]["To"]["expression"].startswith("CFrame.new(")
    assert shots["Fire"]["Sequence"][0]["AV"]["Fire"]["To"]["expression"].startswith(
        "CFrame.new(0, 0, 0.1)")
    assert shots["AimFire"]["Sequence"][0]["AV"]["Fire"]["To"]["expression"].startswith(
        "CFrame.new(0, 0, 0.05)")

def test_native_viewmodel_uses_source_timing_without_combat_authority_mutation():
    motion=read("src/Twr.Godot/Scripts/Pass59WeaponMotion.cs")
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    action=read(".github/workflows/windows-build.yml")
    assert "res://Content/weapons/catalog.json" in motion
    assert "NumberStyles.Float" in motion and "float.IsFinite" in motion
    assert "Pass59WeaponMotion.TryGet(spec.Name,out var profile)" in view
    assert "public void Fire(bool aiming = false)" in view
    assert "motion.HasAim ? motion.Aim : motion.Hip" in view
    assert "sourcePullback" in view
    assert player.count("_viewModel.Fire(_aiming);")==2
    assert "TWR_SMOKE_PASS59_SOURCE_MOTION_OK" in boot
    assert "--smoke-pass59" in action
    assert "TWR_SMOKE_PASS59_SOURCE_MOTION_OK" in action
    assert "twr-pass59-source-weapon-motion" in action
    assert "SpendAmmo" not in motion
    assert "HttpClient" not in motion and "Process.Start" not in motion

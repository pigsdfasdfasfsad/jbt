"""Pass61: source-authoritative equip and motor displacement visual animation."""
import json
from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_original_weapon_script_motor_offsets_are_preserved_in_catalog():
    glock=json.loads(read("content/weapons/entries/glock-17.json"))
    moss=json.loads(read("content/weapons/entries/mossberg-500.json"))
    glock_slide=glock["animations"]["Sliding"]["Sequence"]
    assert len(glock_slide)==2
    assert glock_slide[0]["ModelMotor"]["SlideSliding"]["To"][
        "expression"]=="module.Stats.Rels.SlideRel * CFrame.new(0, 0, 0.225)"
    assert glock_slide[1]["ModelMotor"]["SlideSliding"]["To"][
        "expression"]=="module.Stats.Rels.SlideRel"
    assert len(glock["animations"]["Equip"]["Sequence"])==4
    assert glock["animations"]["Equip"]["Sequence"][-1]["AV"]["Main"]["To"][
        "expression"]=="module.Stats.GripCF"
    moss_slide=moss["animations"]["Sliding"]["Sequence"]
    assert len(moss_slide)==5
    assert moss_slide[2]["ModelMotor"]["Pump"]["To"][
        "expression"]=="module.Stats.Rels.PumpRel * CFrame.new(0, 0, 0.3)"
    assert moss_slide[3]["ModelMotor"]["Pump"]["To"][
        "expression"]=="module.Stats.Rels.PumpRel"
    assert moss_slide[3]["ModelMotor"]["SlideSliding"]["To"][
        "expression"]=="module.Stats.Rels.SlideRel"


def test_handling_library_keeps_literal_only_source_motor_security():
    lib=read("src/Twr.Godot/Scripts/Pass61SourceWeaponHandling.cs")
    assert "TryRelativeMotorOffset" in lib
    assert 'relation is not ("SlideRel" or "PumpRel" or "MagRel" or "BoltRel")' in lib
    assert "Pass60WeaponPoseMath.TryParse(literal,null,out var pose)" in lib
    assert 'hierarchy.GetArrayLength()==3' in lib
    assert 'hierarchy[1].GetString()=="Handle"' in lib
    assert "frames.Add(new EquipFrame(" in lib
    assert "frames.Add(new MotorFrame(" in lib
    assert "ReadMotor(animations,\"Sliding\",\"SlideSliding\"" in lib
    assert "ReadMotor(animations,\"Sliding\",\"Pump\"" in lib
    assert "ReadMotor(animations,\"Reload\",\"Pump\"" in lib
    assert 'res://Content/weapons/catalog.json' in lib
    assert "HttpClient" not in lib and "Process.Start" not in lib
    assert "SpendAmmo" not in lib and "Credits" not in lib
    assert "Rels.MagRel * CFrame.new" not in lib  # do not invent magazines


def test_native_weapon_viewmodel_cycles_real_mesh_nodes_and_keeps_authority():
    visual=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    player=read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    smoke=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert "Pass61SourceWeaponHandling.TryGet(spec.Name,out var handling)" in visual
    assert "public void BeginEquip(double seconds)" in visual
    assert "SampleEquip(sourceEquip," in visual
    assert "part.Basis.Orthonormalized()*LastSlideMotorOffset" in visual
    assert "part.Basis.Orthonormalized()*LastPumpMotorOffset" in visual
    assert "SourceCyclingPumpFrames" in visual
    assert "SourceReloadPumpFrames" in visual
    assert 'name.Equals("Pump",StringComparison.OrdinalIgnoreCase)' in visual
    assert "_viewModel.BeginEquip(_equipTimer);" in player
    assert player.count("_viewModel.Fire(_aiming);")==2
    assert "TWR_SMOKE_PASS61_CYCLING_OK" in smoke
    assert "--smoke-pass61" in ci and "TWR_SMOKE_PASS61_CYCLING_OK" in ci
    assert "twr-pass61-source-weapon-cycling" in ci
    assert ci.count("if: false && success()")>=14
    assert ci.count("uses: actions/upload-artifact@v4")==ci.count(
        "if: false && success()")

"""Pass63 — source magazine removal/insertion poses, no Luau execution."""
import gzip
import json
import subprocess
import sys
from conftest import ROOT

FIXTURE=ROOT/"tools/maps/make_pass63_magazine_fixture.py"


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_exact_original_magazine_keyframes_are_preserved():
    expected={"glock-17":4,"ak-47":5,"m4a1":4,
              "p90":8,"mossberg-500":3}
    for slug,count in expected.items():
        data=json.loads(read("content/weapons/entries/"+slug+".json"))
        magazine=[(k,step["ModelMotor"]["Mag"]) for k,step in enumerate(
            data["animations"]["Reload"]["Sequence"])
            if "Mag" in step.get("ModelMotor",{})]
        assert len(magazine)==count
        assert all(motor["MotorHierarchy"]==[
            "Mag","Handle","MagMotor"] for _,motor in magazine)
        assert magazine[-1][1]["To"]["expression"]==(
            "module.Stats.Rels.MagRel")
        assert any(item["To"]["expression"].startswith("CFrame.new(")
                   for _,item in magazine)
    glock=json.loads(read("content/weapons/entries/glock-17.json"))
    assert glock["animations"]["Reload"]["Sequence"][5][
        "ModelMotor"]["Mag"]["Time"]==.25
    assert glock["animations"]["Reload"]["Sequence"][8][
        "ModelMotor"]["Mag"]["Time"]==.15


def test_strict_magazine_pose_parser_and_source_clip_engine():
    src=read("src/Twr.Godot/Scripts/Pass63SourceMagazineReload.cs")
    binding=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    assert "module.Stats.Rels.MagRel" in src
    assert "source.StartsWith(symbol+" in src
    assert "Pass60WeaponPoseMath.TryParse(literal,null,out var raw)" in src
    assert "reflect*raw.Basis*reflect" in src
    assert "reflect*raw.Origin" in src
    assert "Math.Abs(converted.Basis.Determinant()-1f)" in src
    assert "if(!Time(step,out var dt)" in src
    assert "float.IsFinite(n)" in src
    assert "ExactMagMotor(motor)" in src
    assert "destination.RelativeToHandle" in src
    assert "Pass60WeaponPoseMath.Interpolate(from,to,t)" in src
    assert "TryGetGroupRestPose" in binding
    assert "ResolveSourcePose" in binding
    assert "ApplyPose(string name,Transform3D target)" in binding
    assert "part.Transform=motion*home;" in binding
    assert "HttpClient" not in src and "Process.Start" not in src
    assert "SpendAmmo" not in src and "Credits" not in src


def test_source_magazine_group_moves_as_subassembly_not_generic_drop():
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow=read(".github/workflows/windows-build.yml")
    assert "Pass63SourceMagazineReload.TryGet(spec.Name,out var magClip)" in view
    assert "SourceMagazineGroupParts" in view
    assert "_sourceHierarchy?.HasGroup(\"Mag\")==true" in view
    assert "Pass63SourceMagazineReload.Sample(" in view
    assert '_sourceHierarchy!.ApplyPose("Mag",pose);' in view
    assert '_sourceHierarchy?.ResetGroup("Mag");' in view
    assert "SourceMagazineHandleTravel" in view
    assert "TWR_SMOKE_PASS63_MAGAZINE_OK" in boot
    assert "source_two_piece_magazine_moves_and_rotates=true" in boot
    assert "source_group_spacing_preserved=true" in boot
    assert "--smoke-pass63" in workflow
    assert "make_pass63_magazine_fixture.py" in workflow
    assert "TWR_SMOKE_PASS63_MAGAZINE_OK" in workflow
    assert "twr-pass63-source-magazine-reload" in workflow
    assert "Owner-private weapon motor hierarchy sidecar remained" in workflow
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")


def test_synthetic_two_piece_magazine_pack_never_overwrites_owner_assets(tmp_path):
    source=tmp_path/"Content"/"Weapons"/"SourceWeaponModels.json.gz"
    hierarchy=tmp_path/"Content"/"Weapons"/"SourceWeaponMotorHierarchy62.json.gz"
    args=[sys.executable,str(FIXTURE),"--source",str(source),
          "--hierarchy",str(hierarchy)]
    subprocess.run(args+["--create"],capture_output=True,
                   text=True,check=True)
    raw=json.loads(gzip.decompress(source.read_bytes()))
    groups=json.loads(gzip.decompress(hierarchy.read_bytes()))
    assert raw["synthetic"] is True and groups["synthetic"] is True
    for gun in ("Glock 17","Mossberg 500"):
        parts=raw["models"][gun]["parts"]
        mag=next(g for g in groups["models"][gun]["groups"]
                 if g["path"]==["Mag"])
        assert "MagazineSleeve" in [part["name"] for part in parts]
        assert len(mag["part_indices"])==3
        assert sum(parts[i]["opacity"]>.001
                   for i in mag["part_indices"])==2
    assert subprocess.run(args+["--create"],capture_output=True).returncode!=0
    subprocess.run(args+["--clean"],capture_output=True,
                   text=True,check=True)
    assert not source.exists() and not hierarchy.exists()

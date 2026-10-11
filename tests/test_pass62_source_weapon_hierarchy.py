"""Pass62 — original tool hierarchy recovery with no public owner source assets."""
import gzip
import json
import subprocess
import sys
from pathlib import Path
from zipfile import ZipFile
from lxml import etree
from conftest import ROOT

EXTRACT=ROOT/"tools/maps/extract_source_weapon_hierarchy62.py"
FIXTURE=ROOT/"tools/maps/make_pass62_motor_hierarchy_fixture.py"

def read(path):
    return (ROOT/path).read_text(encoding="utf-8")

def source_xml(tmp_path:Path):
    root=etree.Element("roblox")
    def node(parent,kind,name):
        item=etree.SubElement(parent,"Item",{"class":kind})
        props=etree.SubElement(item,"Properties")
        etree.SubElement(props,"string",name="Name").text=name
        return item
    def part(parent,name,x,y,z):
        item=node(parent,"MeshPart",name)
        cf=etree.SubElement(item.find("Properties"),
            "CoordinateFrame",name="CFrame")
        data={"X":str(x),"Y":str(y),"Z":str(z)}
        data.update({f"R{i}{j}":str(int(i==j))
                     for i in range(3) for j in range(3)})
        for key,value in data.items():
            etree.SubElement(cf,key).text=value
    storage=node(root,"ReplicatedStorage","ReplicatedStorage")
    models=node(storage,"Folder","Models")
    tools=node(models,"Folder","Tools")
    gun=node(tools,"Model","Glock 17")
    part(gun,"Handle",0,0,0)
    slide=node(gun,"Model","Slide")
    part(slide,"Handle",.4,0,-.2)
    part(slide,"Slide",.4,.1,-.4)
    part(slide,"HousingDetail",.4,.2,-.4)
    mag=node(gun,"Model","Mag")
    part(mag,"Handle",-.1,-.2,0)
    part(mag,"Magazine",-.1,-.3,0)
    part(gun,"Body",0,0,0)
    archive=tmp_path/"fixture.zip"
    with ZipFile(archive,"w") as z:
        z.writestr("TestPlace/TestPlace.rbxlx",etree.tostring(root))
    return archive

def test_owner_hierarchy_extractor_preserves_indices_group_pivots_and_determinism(tmp_path):
    archive=source_xml(tmp_path)
    first=tmp_path/"first"/"SourceWeaponMotorHierarchy62.json.gz"
    second=tmp_path/"second"/"SourceWeaponMotorHierarchy62.json.gz"
    for output in (first,second):
        subprocess.run([sys.executable,str(EXTRACT),
            "--archive",str(archive),"--out",str(output),"--synthetic"],
            check=True,capture_output=True,text=True)
    assert first.read_bytes()==second.read_bytes()
    data=json.loads(gzip.decompress(first.read_bytes()))
    assert data["synthetic"] is True
    assert data["format"]=="twr-source-weapon-hierarchy62-v1"
    assert data["source_model_count"]==1
    assert data["original_part_count"]==7
    group=data["models"]["Glock 17"]
    assert group["part_count"]==7
    assert len(group["groups"])==2
    slide=next(g for g in group["groups"] if g["path"]==["Slide"])
    assert slide["part_indices"]==[1,2,3]
    assert slide["pivot_t"]==[.4,0.,-.2]
    assert slide["pivot_r"]==[1.,0.,0.,0.,1.,0.,0.,0.,1.]
    manifest=json.loads((first.parent/"SOURCE_WEAPON_HIERARCHY_MANIFEST62.json").read_text())
    assert manifest["source_mesh_triangle_bytes_recovered"] is False
    assert manifest["motor6d_joint_c0c1_data_recovered"] is False
    bad=subprocess.run([sys.executable,str(EXTRACT),"--archive",
        str(archive),"--out",str(tmp_path/"notreal.gz")],
        capture_output=True,text=True)
    assert bad.returncode!=0

def test_windows_fixture_does_not_overwrite_or_leave_private_packs(tmp_path):
    original=tmp_path/"SourceWeaponModels.json.gz"
    hierarchy=tmp_path/"SourceWeaponMotorHierarchy62.json.gz"
    base=[sys.executable,str(FIXTURE),"--source",str(original),
          "--hierarchy",str(hierarchy)]
    subprocess.run(base+["--create"],check=True)
    a=json.loads(gzip.decompress(original.read_bytes()))
    b=json.loads(gzip.decompress(hierarchy.read_bytes()))
    assert a["synthetic"] is True and b["synthetic"] is True
    assert b["models"]["Glock 17"]["part_count"]==9
    assert b["models"]["Mossberg 500"]["part_count"]==10
    assert b["models"]["Glock 17"]["groups"][0]["part_indices"]==[1,2,3]
    assert b["models"]["Mossberg 500"]["groups"][0]["part_indices"]==[1,2,3]
    overwriting=subprocess.run(base+["--create"],capture_output=True)
    assert overwriting.returncode!=0
    subprocess.run(base+["--clean"],check=True)
    assert not original.exists() and not hierarchy.exists()

def test_native_motor_group_binding_uses_source_handle_pivots_not_part_names():
    original=read("src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs")
    hierarchy=read("src/Twr.Godot/Scripts/Pass62SourceWeaponHierarchy.cs")
    view=read("src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs")
    assert "LastSubassemblyBinding=Pass62SourceWeaponHierarchy.Bind(" in original
    assert "visibleBySourceIndex[partIndex]=originalPart" in original
    assert "sourceParts.Length,visibleBySourceIndex" in original
    assert 'or "--smoke-pass62"' in original
    assert "OwnerPackSha" in hierarchy and "MaxExpandedBytes" in hierarchy
    assert "OriginalWeaponSourceRuntime.OriginalPackSha" in hierarchy
    assert "OriginalWeaponSourceRuntime.OriginalSourceSha" in hierarchy
    assert "if(synthetic && !testMode)" in hierarchy
    assert "group.Pivot.AffineInverse()" in hierarchy
    assert "part.Transform=move*home;" in hierarchy
    assert "new Transform3D(alignment*basis.Orthonormalized()" in hierarchy
    assert "LastSubassemblyBinding" in view
    assert "_sourceHierarchy!.ApplyTranslation(\"Slide\"" in view
    assert "_sourceHierarchy!.ApplyTranslation(\"Pump\"" in view
    assert "SourceSlideGroupParts" in view and "SourcePumpGroupParts" in view
    assert "SpendAmmo" not in hierarchy and "Credits" not in hierarchy
    assert "HttpClient" not in hierarchy and "Process.Start" not in hierarchy

def test_windows_pass62_acceptance_and_private_cleanup_remain_enabled():
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    workflow=read(".github/workflows/windows-build.yml")
    assert "--smoke-pass62" in boot
    assert "TWR_SMOKE_PASS62_SOURCE_HIERARCHY_OK" in boot
    assert "all_glock_slide_children_animated=2" in boot
    assert "all_mossberg_pump_children_animated=2" in boot
    assert "rotated_local_motor_axes=true" in boot
    assert "twr-pass62-source-subassembly-hierarchy" in workflow
    assert "make_pass62_motor_hierarchy_fixture.py" in workflow
    assert "--create" in workflow and "--clean" in workflow
    assert "TWR_SMOKE_PASS62_SOURCE_HIERARCHY_OK" in workflow
    assert "Owner-private weapon motor hierarchy sidecar remained" in workflow
    assert "--smoke-pass59" in workflow and "--smoke-pass60" in workflow
    assert "--smoke-pass61" in workflow and "--smoke-pass62" in workflow
    assert workflow.count("if: false && success()")>=14
    assert workflow.count("uses: actions/upload-artifact@v4")==workflow.count(
        "if: false && success()")

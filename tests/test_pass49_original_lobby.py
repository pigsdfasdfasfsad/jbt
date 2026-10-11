"""Pass49: owner-bounded original Workspace/Lobby scene, cameras and UI isolation."""
from __future__ import annotations
import gzip
import hashlib
import json
import subprocess
import sys
from pathlib import Path
from zipfile import ZipFile
from lxml import etree
from conftest import ROOT

EXTRACT=ROOT/"tools/maps/extract_original_lobby49.py"
FIXTURE=ROOT/"tools/maps/make_pass49_lobby_smoke_fixture.py"

def add(parent, cls, name, position=(0,0,0),alpha=0):
    item=etree.SubElement(parent,"Item",attrib={"class":cls})
    props=etree.SubElement(item,"Properties")
    etree.SubElement(props,"string",name="Name").text=name
    if cls in ("Part","MeshPart","WedgePart","UnionOperation"):
        cf=etree.SubElement(props,"CoordinateFrame",name="CFrame")
        for key,v in {
            "X":str(position[0]),"Y":str(position[1]),"Z":str(position[2]),
            **{"R"+str(i)+str(j):str(int(i==j))
               for i in range(3) for j in range(3)}
        }.items():
            etree.SubElement(cf,key).text=v
        size=etree.SubElement(props,"Vector3",name="size")
        for axis,val in zip("XYZ",(2,1,3)):
            etree.SubElement(size,axis).text=str(val)
        etree.SubElement(props,"Color3uint8",name="Color3uint8").text="4283782485"
        etree.SubElement(props,"float",name="Transparency").text=str(alpha)
        etree.SubElement(props,"token",name="Material").text="272"
    if cls=="PointLight":
        etree.SubElement(props,"float",name="Brightness").text="1"
        etree.SubElement(props,"float",name="Range").text="12"
        etree.SubElement(props,"bool",name="Enabled").text="true"
    return item

def archive_fixture(tmp_path):
    roblox=etree.Element("roblox")
    workspace=add(roblox,"Workspace","Workspace")
    lobby=add(workspace,"Folder","Lobby")
    objects=add(lobby,"Folder","Objects")
    add(objects,"Part","OriginalFloor",(102,-202,20))
    add(objects,"WedgePart","OriginalWedge",(104,-202,21))
    add(objects,"MeshPart","UnresolvedChair",(105,-201,24))
    add(objects,"UnionOperation","UnresolvedCSG",(98,-201,18))
    add(objects,"Part","InvisibleOriginal",(99,-201,18),alpha=1)
    cams=add(lobby,"Folder","CamPoints")
    add(cams,"Part","Start",(100,-200,20),alpha=1)
    add(cams,"Part","Loadout",(101,-200,18),alpha=1)
    anchors=add(lobby,"Folder","LoadoutPoints")
    add(anchors,"Part","Primary",(104,-203,22),alpha=1)
    light_holder=add(lobby,"Folder","Lights")
    lamp=add(light_holder,"Part","SourceBulb",(103,-199,20))
    add(lamp,"PointLight","PointLight")
    file=tmp_path/"fixture.zip"
    with ZipFile(file,"w") as archive:
        archive.writestr("TestPlace/TestPlace.rbxlx",etree.tostring(roblox))
    return file

def test_source_xml_extraction_preserves_real_relative_transforms(tmp_path):
    original=archive_fixture(tmp_path)
    first=tmp_path/"a"/"SourceLobby49.json.gz"
    second=tmp_path/"b"/"SourceLobby49.json.gz"
    for output in (first,second):
        run=subprocess.run([sys.executable,str(EXTRACT),"--archive",str(original),
            "--out",str(output),"--synthetic"],capture_output=True,text=True)
        assert run.returncode==0,run.stderr
    assert first.read_bytes()==second.read_bytes()
    data=json.loads(gzip.decompress(first.read_bytes()))
    assert data["synthetic"] is True
    assert data["format"]=="twr-pass49-original-lobby-v1"
    assert data["source_offsets_relative_to_original_start_camera"] is True
    assert len(data["geometry"])==6  # 5 originals + source light-emitter carrier
    assert len(data["lights"])==1
    assert set(data["cameras"])=={"Start","Loadout"}
    assert set(data["loadout_points"])=={"Primary"}
    assert data["cameras"]["Start"]["t"]==[0,0,0]
    parts={p["name"]:p for p in data["geometry"]}
    assert parts["OriginalFloor"]["t"]==[2,-2,0]
    assert parts["UnresolvedChair"]["t"]==[5,-1,4]
    assert parts["InvisibleOriginal"]["opacity"]==0
    assert parts["OriginalWedge"]["class"]=="WedgePart"
    assert data["lights"][0]["t"]==[3,1,0]
    evidence=json.loads((first.parent/"SOURCE_LOBBY_MANIFEST49.json").read_text())
    assert evidence["source_parts"]==6
    assert evidence["mesh_or_csg_proxy_parts_visible"]==2
    assert evidence["original_camera_points"]==2
    assert evidence["compressed_sha256"]==hashlib.sha256(first.read_bytes()).hexdigest()
    bad=subprocess.run([sys.executable,str(EXTRACT),"--archive",str(original),
            "--out",str(tmp_path/"forged.gz")],capture_output=True,text=True)
    assert bad.returncode!=0
    assert "SHA mismatch" in bad.stderr

def test_synthetic_native_fixture_is_safe_and_does_not_delete_owner_lobby(tmp_path):
    outfile=tmp_path/"Content/Lobby/SourceLobby49.json.gz"
    def invoke(flag):
        return subprocess.run([sys.executable,str(FIXTURE),
            "--output",str(outfile),flag],capture_output=True,text=True)
    assert invoke("--create").returncode==0
    data=json.loads(gzip.decompress(outfile.read_bytes()))
    assert data["synthetic"] is True
    assert len(data["geometry"])==8
    assert sum(p["opacity"]>.001 for p in data["geometry"])==7
    assert len(data["cameras"])==8
    assert len(data["loadout_points"])==5
    assert len(data["lights"])==2
    assert invoke("--create").returncode!=0
    assert invoke("--clean").returncode==0
    assert not outfile.exists()
    outfile.write_bytes(gzip.compress(json.dumps({"format":"twr-pass49-original-lobby-v1",
        "synthetic":False,"owner_place_sha256":"owner","geometry":[]}).encode()))
    assert invoke("--clean").returncode!=0
    assert outfile.exists()

def test_authored_lobby_streamer_uses_recovered_spatial_parts_and_mesh_proxies():
    source=(ROOT/"src/Twr.Godot/Scripts/Pass49OriginalLobbyRuntime.cs").read_text()
    menu=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    assert "98a8bf71c0561be814b2de01831b48e413e349ed4925fab3c0b8a403d7845b49" in source
    assert "Original lobby source SHA mismatch" in source
    assert "Synthetic lobby used outside smoke test" in source
    assert "Lobby decompressed size cap exceeded" in source
    assert "SourceCameraCount" in source
    assert "UnresolvedMeshProxyParts" in source
    assert "RobloxPrimitiveGeometry.WedgeMesh()" in source
    assert "new SphereMesh" in source and "new CylinderMesh" in source
    assert "SourceGeometryParts!=818" not in source
    assert "geometry.GetArrayLength()!=818" in source
    assert "mm.CustomAabb=new Aabb(min,max-min)" in source
    assert "Light3D emitter;" in source
    assert 'Pass49OriginalLobbyRuntime.TryBuild(this)' in menu
    assert 'EnsureOriginalLobby("Start")' in menu
    assert 'EnsureOriginalLobby("Loadout")' in menu
    assert 'EnsureOriginalLobby("Perks")' in menu
    assert "MouseFilter=Control.MouseFilterEnum.Ignore" in menu
    assert "_sourceLobby=null;" in menu
    assert "StartGame(string map)" in menu

def test_windows_integration_uses_actual_functional_armory_and_private_cleanup():
    boot=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    workflow=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert "--smoke-pass49" in boot and "--smoke-pass49" in workflow
    assert "TWR_SMOKE_PASS49_LOBBY_OK" in boot and "TWR_SMOKE_PASS49_LOBBY_OK" in workflow
    assert "armoryButtons!=91" in boot
    assert "SourceGeometryParts!=8" in boot
    assert "source_cameras=8 loadout_points=5" in boot
    assert "make_pass49_lobby_smoke_fixture.py" in workflow
    assert "SourceLobby49.json.gz" in workflow
    assert "SOURCE_LOBBY_MANIFEST49.json" in workflow
    assert "twr-pass49-original-lobby-3d" in workflow
    assert "TWR-Pass49-Windows-x64-NoPrivateAssets" in workflow

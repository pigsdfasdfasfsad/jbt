"""Pass47: exact source 3D gun assemblies with safe synthetic Windows verification."""
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

TOOL=ROOT/"tools"/"maps"/"extract_original_weapon_assemblies47.py"
FAKE=ROOT/"tools"/"maps"/"make_pass47_weapon_smoke_fixture.py"

def make_original_like_fixture(tmp_path):
    def props(name):
        p=etree.Element("Properties")
        etree.SubElement(p,"string",name="Name").text=name
        return p
    def node(cls,name,parent):
        it=etree.SubElement(parent,"Item",attrib={"class":cls})
        it.append(props(name))
        return it
    def source_part(parent,name,x):
        p=node("MeshPart",name,parent)
        q=p.find("Properties")
        cf=etree.SubElement(q,"CoordinateFrame",name="CFrame")
        for k,v in {"X":str(x),"Y":"0","Z":"-3",
                    "R00":"1","R01":"0","R02":"0",
                    "R10":"0","R11":"1","R12":"0",
                    "R20":"0","R21":"0","R22":"1"}.items():
            etree.SubElement(cf,k).text=v
        size=etree.SubElement(q,"Vector3",name="size")
        for k,v in {"X":"0.8","Y":"0.3","Z":"1.2"}.items():
            etree.SubElement(size,k).text=v
        etree.SubElement(q,"Color3uint8",name="Color3uint8").text="4280558630"
        etree.SubElement(q,"token",name="Material").text="272"
        etree.SubElement(q,"float",name="Transparency").text="0"
        c=etree.SubElement(q,"Content",name="MeshId")
        etree.SubElement(c,"url").text="rbxassetid://999999"
        return p
    root=etree.Element("roblox")
    storage=node("ReplicatedStorage","ReplicatedStorage",root)
    models=node("Folder","Models",storage)
    tools=node("Folder","Tools",models)
    for name in ("Glock 17","Molotov"):
        model=node("Model",name,tools)
        source_part(model,"Handle",10)
        source_part(model,"Barrel",12)
    archive=tmp_path/"synthetic-place.zip"
    with ZipFile(archive,"w") as z:z.writestr("TestPlace/TestPlace.rbxlx",etree.tostring(root))
    catalog=tmp_path/"synthetic-catalog.json"
    catalog.write_text(json.dumps({"weapons":[{"name":"Glock 17"},{"name":"Molotov"}]}))
    return archive,catalog

def test_streaming_exporter_parses_realistic_xml_but_does_not_execute_scripts(tmp_path):
    archive,catalog=make_original_like_fixture(tmp_path)
    out1=tmp_path/"pack1.gz"
    out2=tmp_path/"pack2.gz"
    def run(output):
        return subprocess.run([sys.executable,str(TOOL),
            "--archive",str(archive),"--catalog",str(catalog),
            "--out",str(output),"--synthetic"],
            text=True,capture_output=True,check=True)
    run(out1)
    run(out2)
    assert out1.read_bytes()==out2.read_bytes()
    pack=json.loads(gzip.decompress(out1.read_bytes()))
    assert pack["synthetic"] is True
    assert pack["format"]=="twr-original-weapon-assemblies-v1"
    assert len(pack["models"])==2
    assert [p["t"] for p in pack["models"]["Glock 17"]["parts"]]==[
        [0.,0.,0.],[2.,0.,0.]
    ]
    assert pack["models"]["Glock 17"]["parts"][1]["meshId"]=="rbxassetid://999999"
    manifest=json.loads((tmp_path/"SOURCE_WEAPON_MODELS_MANIFEST47.json").read_text())
    assert manifest["catalog_covered"]==2
    assert manifest["original_source_parts"]==4
    assert manifest["source_mesh_triangle_bytes_recovered"] is False
    # Fabricated byte-identical scene without --synthetic cannot be called
    # real original source (the 98-model count and owner SHA are hard limits).
    bad=subprocess.run([sys.executable,str(TOOL),
        "--archive",str(archive),"--catalog",str(catalog),
        "--out",str(tmp_path/"bad.gz")],capture_output=True,text=True)
    assert bad.returncode != 0

def test_source_missing_mesh_archive_does_not_replace_original_viewmodel_logic(tmp_path):
    output=tmp_path/"Content"/"Weapons"/"SourceWeaponModels.json.gz"
    subprocess.run([sys.executable,str(FAKE),"--output",str(output),
                    "--create"],check=True,capture_output=True)
    d=json.loads(gzip.decompress(output.read_bytes()))
    assert d["synthetic"] is True
    assert len(d["models"])==6
    for name,model in d["models"].items():
        assert len(model["parts"])==5
        assert sum(part["opacity"]>.001 for part in model["parts"])==4
        assert sum(bool(part.get("meshId")) for part in model["parts"])==1
    subprocess.run([sys.executable,str(FAKE),"--output",str(output),
                    "--clean"],check=True,capture_output=True)
    assert not output.exists()

def test_runtime_requires_exact_source_sha_and_preserves_original_invisible_markers():
    runtime=(ROOT/"src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs").read_text()
    visual=(ROOT/"src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs").read_text()
    player=(ROOT/"src/Twr.Godot/Scripts/FirstPersonPlayer.cs").read_text()
    hud=(ROOT/"src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs").read_text()
    assert "fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7" in runtime
    assert "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2" in runtime
    assert 'Synthetic weapon pack used outside smoke' in runtime
    assert 'Original source weapon SHA mismatch' in runtime
    assert 'if (!float.IsFinite(alpha) || alpha <= .001f) continue;' in runtime
    assert 'new SphereMesh { Radius=.5f, Height=1f }' in runtime
    assert 'mesh ??=' in runtime
    assert 'OwnerSourcePackVerified' in runtime
    assert 'OriginalWeaponSourceRuntime.TryBuild(_rig!, spec.Name)' in visual
    assert 'SourceMissingMeshProxies' in visual
    assert 'SourceWeaponModelActive' in player
    assert 'pass47_owner_weapon_pack_sha_verified' in hud
    assert 'pass47_weapon_missing_mesh_proxies' in hud

def test_native_windows_synthetic_3d_model_smoke_and_public_asset_exclusion():
    bootstrap=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    workflow=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert '--smoke-pass47' in bootstrap and '--smoke-pass47' in workflow
    assert 'TWR_SMOKE_PASS47_SOURCE_WEAPONS_OK' in bootstrap
    assert 'TWR_SMOKE_PASS47_SOURCE_WEAPONS_OK' in workflow
    assert 'original_mesh_proxy_per_model=1' not in bootstrap
    assert 'absent_mesh_proxy_per_model=1' in bootstrap
    assert 'make_pass47_weapon_smoke_fixture.py' in workflow
    assert 'TWR-Pass47-Windows-x64-NoPrivateAssets' in workflow
    assert 'twr-pass47-original-weapon-assemblies' in workflow
    assert 'SourceWeaponModels.json.gz' in workflow
    assert 'Content/Art' in workflow

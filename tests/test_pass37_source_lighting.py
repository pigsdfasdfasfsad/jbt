from pathlib import Path
import hashlib
import importlib.util
import json
import sys
from lxml import etree
from conftest import ROOT

def module():
    source=ROOT/"tools/validation/recover_source_lighting37.py"
    spec=importlib.util.spec_from_file_location("twr_pass37_lighting",source)
    obj=importlib.util.module_from_spec(spec)
    sys.modules[spec.name]=obj
    spec.loader.exec_module(obj)
    return obj

def synthetic_rbxlx(path):
    names=("Ranch","Mill","Bypass","Cabin","Cargo","District",
           "Expressway","Prison","Laboratory","Manor")
    root=etree.Element("roblox")
    def add(parent,typ,name):
        i=etree.SubElement(parent,"Item",attrib={"class":typ})
        p=etree.SubElement(i,"Properties")
        etree.SubElement(p,"string",name="Name").text=name
        return i,p
    rep,_=add(root,"ReplicatedStorage","ReplicatedStorage")
    maps,_=add(rep,"Folder","CMaps")
    for name in names:
        m,_=add(maps,"Folder",name)
        lights,_=add(m,"Folder","Lighting Effects")
        sky,p=add(lights,"Sky","Sky")
        etree.SubElement(p,"int",name="StarCount").text="3000"
        etree.SubElement(p,"float",name="SunAngularSize").text="21"
        correction,p=add(lights,"ColorCorrectionEffect","Color")
        for key,val in (("Brightness","0"),("Contrast","-0.1"),("Saturation","0")):
            etree.SubElement(p,"float",name=key).text=val
        color=etree.SubElement(p,"Color3",name="TintColor")
        for k in ("R","G","B"): etree.SubElement(color,k).text="1"
        etree.SubElement(p,"bool",name="Enabled").text="true"
    import zipfile
    with zipfile.ZipFile(path,"w") as archive:
        archive.writestr("TestPlace/TestPlace.rbxlx",etree.tostring(root))

def test_pass37_export_determinism_and_provenance(tmp_path):
    script=module()
    fixture=tmp_path/"TestPlace.zip"
    synthetic_rbxlx(fixture)
    a=script.recover(fixture,tmp_path/"a")
    b=script.recover(fixture,tmp_path/"b")
    assert a==b
    assert len(a["maps"])==10
    for name,record in a["maps"].items():
        raw=(tmp_path/"a"/record["file"]).read_bytes()
        assert raw==(tmp_path/"b"/record["file"]).read_bytes()
        assert hashlib.sha256(raw).hexdigest()==record["sha256"]
        doc=json.loads(raw)
        assert doc["source_path"]==f"ReplicatedStorage/CMaps/{name}/Lighting Effects"
        assert doc["skybox_images_present"] is False
        assert len(doc["effects"])==2

def test_pass37_runtime_defaults_off_and_no_forged_skybox():
    runtime=(ROOT/"src/Twr.Godot/Scripts/Pass37SourceLightingRuntime.cs").read_text()
    gameplay=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    workflow=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert "Original source lighting SHA-256 mismatch" in runtime
    assert "SkyboxImageAvailable => false" in runtime
    assert "public bool Enabled { get; private set; }" in runtime
    assert "_world.Environment=enabled ? _preview : _baseline" in runtime
    assert "Pass37SourceLightingRuntime.TryBuild(this, MapName)" in gameplay
    assert "key.Keycode == Key.F3" in gameplay
    assert "twr-pass37-source-lighting" in workflow

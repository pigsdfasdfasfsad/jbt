"""Pass36 lobby sources are private; CI uses generated fixtures only.

Native Windows --smoke-pass36 checks C# Godot source rendering and camera switching.
These Python tests exercise the extraction script, integrity and release gates.
"""
from __future__ import annotations
import gzip
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from zipfile import ZipFile

from lxml import etree
from conftest import ROOT

RECOVER=ROOT/'tools/validation/recover_source_lobby36.py'
FIXTURE=ROOT/'tools/validation/make_pass36_lobby_fixture.py'
CAMERAS=('Start','Shop','WeaponNode','Loadout','Perks','Options','Leaderboards','Gifts')


def source(path):return (ROOT/path).read_text(encoding='utf8')


def _prop(item,key,value):
    p=item.find('Properties')
    if p is None:p=etree.SubElement(item,'Properties')
    e=etree.SubElement(p,'string',name=key);e.text=str(value)


def _item(parent,cls,name):
    e=etree.SubElement(parent,'Item',{'class':cls})
    _prop(e,'Name',name)
    return e


def _part(parent,name,cls='Part'):
    i=_item(parent,cls,name)
    p=i.find('Properties')
    cf=etree.SubElement(p,'CoordinateFrame',name='CFrame')
    fields={'X':293,'Y':-2945,'Z':20,'R00':1,'R01':0,'R02':0,
            'R10':0,'R11':1,'R12':0,'R20':0,'R21':0,'R22':1}
    for k,v in fields.items():etree.SubElement(cf,k).text=str(v)
    v=etree.SubElement(p,'Vector3',name='size')
    for k in ('X','Y','Z'):etree.SubElement(v,k).text='2'
    _prop(i,'Color3uint8','4291333294')
    _prop(i,'Transparency','0')
    _prop(i,'Material','512')
    return i


def test_source_lobby_converter_from_fake_owner_scene_is_byte_deterministic(tmp_path):
    root=etree.Element('roblox')
    workspace=_item(root,'Workspace','Workspace')
    lobby=_item(workspace,'Model','Lobby')
    objects=_item(lobby,'Model','Objects')
    for i in range(817):_part(objects,'SyntheticSource'+str(i), 'MeshPart' if i%6==0 else 'Part')
    cams=_item(lobby,'Model','CamPoints')
    for name in CAMERAS:_part(cams,name)
    # Deliberately skip lights; the original loader accepts zero for fixtures.
    archive=tmp_path/'fake_roblox.zip'
    with ZipFile(archive,'w') as z:
        z.writestr('TestPlace/TestPlace.rbxlx',etree.tostring(root,encoding='utf8'))
    a=tmp_path/'a'/'OriginalLobby.lobby36.jsonl.gz'
    b=tmp_path/'b'/'OriginalLobby.lobby36.jsonl.gz'
    for out in (a,b):
        subprocess.run([sys.executable,str(RECOVER),'--archive',str(archive),'--output',str(out)],check=True,capture_output=True)
    assert a.read_bytes()==b.read_bytes()
    h,*rows=(json.loads(l) for l in gzip.open(a,'rt'))
    assert h['format']=='twr-pass36-source-lobby-v1'
    assert h['counts']=={'geometry':817,'cameras':8,'lights':0}
    assert len([x for x in rows if x['kind']=='geometry'])==817
    assert {x['name'] for x in rows if x['kind']=='camera'}==set(CAMERAS)
    assert h['missing_mesh_representations']==137
    assert json.loads(a.with_name('lobby36-manifest.json').read_text())['pack_sha256']==hashlib.sha256(a.read_bytes()).hexdigest()


def test_synthetic_executable_fixture_is_non_destructive(tmp_path):
    cmd=[sys.executable,str(FIXTURE),'--output',str(tmp_path)]
    subprocess.run(cmd+['--create'],check=True,capture_output=True)
    pack=tmp_path/'Content/Lobby/OriginalLobby.lobby36.jsonl.gz'
    rows=[json.loads(l) for l in gzip.open(pack,'rt')]
    assert rows[0]['synthetic'] is True
    assert rows[0]['counts']=={'geometry':16,'cameras':8,'lights':1}
    assert {x['name'] for x in rows if x.get('kind')=='camera'}==set(CAMERAS)
    assert subprocess.run(cmd+['--create'],capture_output=True).returncode!=0
    assert len(pack.read_bytes())>250
    pack.write_bytes(b'private owner binary cannot be removed')
    assert subprocess.run(cmd+['--clean'],capture_output=True).returncode!=0
    assert pack.exists()
    pack.unlink()
    subprocess.run(cmd+['--create'],check=True,capture_output=True)
    subprocess.run(cmd+['--clean'],check=True,capture_output=True)
    assert not pack.exists()


def test_lobby_3d_runtime_integrated_and_fail_closed():
    loader=source('src/Twr.Godot/Scripts/Pass36SourceLobbyRuntime.cs')
    bootstrap=source('src/Twr.Godot/Scripts/Bootstrap.cs')
    assert 'OriginalLobby.lobby36.jsonl.gz' in loader
    assert 'OriginalPackSha' in loader
    assert 'checksum != OriginalPackSha && !smokeOnly' in loader
    assert 'ReadLineBounded' in loader
    assert 'public bool SwitchView' in loader and 'public void SetActive' in loader
    assert 'Environment=enabled ? _sourceEnvironment : null' in loader
    assert '_sourceLobby = Pass36SourceLobbyRuntime.TryBuild(this);' in bootstrap
    assert '_sourceLobby?.SwitchView("Start");' in bootstrap
    assert '_sourceLobby?.SwitchView("Perks");' in bootstrap
    assert 'sourceLobby ? opacity : 1f' in bootstrap
    assert '_sourceLobby?.SetActive(false);' in bootstrap
    assert 'TWR_SMOKE_PASS36_LOBBY_OK' in bootstrap


def test_public_artifact_excludes_owner_lobby_pack():
    workflow=source('.github/workflows/windows-build.yml')
    assert "'twr-pass41-original-source-lobby'" in workflow
    assert 'TWR-Pass41-Windows-x64-NoPrivateAssets' in workflow
    assert 'make_pass36_lobby_fixture.py --output $outputDir --create' in workflow
    assert 'make_pass36_lobby_fixture.py --output $outputDir --clean' in workflow
    assert "Filter '*.lobby36.jsonl.gz'" in workflow
    assert '--smoke-pass36' in workflow
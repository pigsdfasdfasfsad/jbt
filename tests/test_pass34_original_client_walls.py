"""Pass34 original client-wall recovery and opt-in Godot native collision contracts.

Provenance tests use only a generated RBXLX fixture. Owner-held original
TestPlace is NOT uploaded to GitHub or used in public Windows CI.
"""
from __future__ import annotations
import gzip
import hashlib
import json
from pathlib import Path
import sys
from xml.etree import ElementTree as ET
import zipfile

from conftest import ROOT
from tools.validation.recover_source_client_walls34 import MAPS, MEMBER, recover


def source(path):
    return (ROOT/path).read_text(encoding='utf8')


def child(parent, cls, name):
    node=ET.SubElement(parent,'Item',{'class':cls})
    props=ET.SubElement(node,'Properties')
    ET.SubElement(props,'string',{'name':'Name'}).text=name
    return node


def original_part(parent,name="ClientWall",collide=True):
    p=child(parent,'Part',name)
    props=p.find('Properties')
    ET.SubElement(props,'bool',{'name':'CanCollide'}).text='true' if collide else 'false'
    cf=ET.SubElement(props,'CoordinateFrame',{'name':'CFrame'})
    numbers={'X':8.0,'Y':7.0,'Z':-20.0,
             'R00':1.,'R01':0.,'R02':0.,
             'R10':0.,'R11':1.,'R12':0.,
             'R20':0.,'R21':0.,'R22':1.}
    for k,v in numbers.items(): ET.SubElement(cf,k).text=str(v)
    size=ET.SubElement(props,'Vector3',{'name':'Size'})
    for k,v in zip('XYZ',(2.,6.,2.)):ET.SubElement(size,k).text=str(v)
    return p


def fixture_xml(maps=MAPS):
    root=ET.Element('roblox',{'version':'4'})
    service=child(root,'ReplicatedStorage','ReplicatedStorage')
    cmaps=child(service,'Folder','CMaps')
    for name in maps:
        selected=child(cmaps,'Folder',name)
        walls=child(selected,'Folder','Walls')
        client=child(walls,'Model','Client Walls')
        original_part(client)
        # This part is genuinely present but CanCollide=false: exclude it.
        original_part(client,'InvisibleDecor',False)
        infected=child(walls,'Model','Infected Walls')
        original_part(infected,'ZombieBarrier')
    return ET.tostring(root,encoding='utf8',xml_declaration=True)


def test_original_wall_extraction_is_deterministic_and_separated(tmp_path):
    source_zip=tmp_path/'source.zip'
    with zipfile.ZipFile(source_zip,'w') as z:z.writestr(MEMBER,fixture_xml())
    scene=tmp_path/'Laboratory.scene.jsonl.gz'
    scene.write_bytes(gzip.compress(b'{"format":"synthetic-scene"}\n',mtime=0))
    a=tmp_path/'a';b=tmp_path/'b'
    one=recover(source_zip,scene,a)
    two=recover(source_zip,scene,b)
    assert one==two
    assert {f.name for f in a.iterdir()}=={f.name for f in b.iterdir()}
    assert all(f.read_bytes()==(b/f.name).read_bytes() for f in a.iterdir())
    assert len(one['release_maps'])==10
    assert all(v['client_walls']==1 for v in one['release_maps'].values())
    assert all(v['infected_walls_source_count']==1 for v in one['release_maps'].values())
    header,part=[json.loads(x) for x in gzip.decompress(
        (a/'Laboratory.clientwalls34.jsonl.gz').read_bytes()).splitlines()]
    assert header['format']=='twr-client-walls34-v1'
    assert header['scene_sha256']==hashlib.sha256(scene.read_bytes()).hexdigest()
    assert header['collision_group_id']==8
    assert header['default_enabled'] is False
    assert part['s']==[2.,6.,2.] and part['class']=='Part'
    assert header['wall_count']==1
    # Other maps need their own validated complete scene before runtime use.
    other=json.loads(gzip.decompress((a/'Ranch.clientwalls34.jsonl.gz').read_bytes()).splitlines()[0])
    assert other['scene_sha256'] is None


def test_no_claim_of_original_group_matrix_or_world_ingress():
    loader=source('src/Twr.Godot/Scripts/Pass34ClientWallsRuntime.cs')
    python=source('tools/validation/recover_source_client_walls34.py')
    assert 'collision_group_matrix_known' in python
    assert 'if (mapName != "Laboratory") return null;' in loader
    assert 'h.GetProperty("scene_sha256").GetString()!=sceneSha' in loader
    assert 'h.GetProperty("collision_group_id").GetInt32()!=8' in loader
    assert 'walls.SetEnabled(false)' in loader
    assert 'CollisionLayer = enabled ? ClientWallCollisionLayer : 0;' in loader
    assert 'CollisionMask=0' in loader


def test_f8_client_only_original_collision_is_integrated_not_forced():
    game=source('src/Twr.Godot/Scripts/GameplayRoot.cs')
    player=source('src/Twr.Godot/Scripts/FirstPersonPlayer.cs')
    infected=source('src/Twr.Godot/Scripts/InfectedAgent.cs')
    diag=source('src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs')
    assert 'Pass34ClientWallsRuntime.TryBuild(this,MapName)' in game
    assert 'key.Keycode == Key.F8' in game
    assert '_originalClientWalls.SetEnabled(!_originalClientWalls.Enabled);' in game
    assert 'CollisionMask = 1 | 2 | Pass34ClientWallsRuntime.ClientWallCollisionLayer |' in player
    assert 'CollisionMask = 1 | Pass35InfectedWallsRuntime.InfectedWallCollisionLayer |' in infected
    assert 'Pass34ClientWallsRuntime.ClientWallCollisionLayer' not in infected
    assert 'OriginalClientWallCount' in diag
    assert 'original_source_client_walls_opt_in' in diag


def test_native_windows_executable_checks_wall_physics_and_fixtures():
    code=source('src/Twr.Godot/Scripts/Bootstrap.cs')
    workflow=source('.github/workflows/windows-build.yml')
    fixture=source('tools/validation/make_pass30_synthetic_sidecars.py')
    assert '--smoke-pass34' in code and '--smoke-pass34' in workflow
    assert 'TWR_SMOKE_PASS34_WALLS_OK' in code
    assert 'game.GetWorld3D().DirectSpaceState.IntersectRay(ray)' in code
    assert 'if (Hits(1))' in code
    assert 'TWR-Pass36-Windows-x64-NoPrivateAssets' in workflow
    assert "base/'Walls'/'Laboratory.clientwalls34.jsonl.gz'" in fixture
    assert "'wall_count': 1" in fixture
    assert "*.clientwalls34.jsonl.gz" in workflow
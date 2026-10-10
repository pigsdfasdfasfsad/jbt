"""Pass35 source Infected Walls: synthetic-only provenance + Windows integration contracts.

No owner-owned Roblox data is checked into GitHub or required for public CI.
"""
from __future__ import annotations
from pathlib import Path
from xml.etree import ElementTree as ET
from zipfile import ZipFile
import gzip
import json
import hashlib
from conftest import ROOT
from tools.validation.recover_original_infected_walls35 import recover, MAPS


def source(p): return (ROOT / p).read_text(encoding='utf8')


def child(parent,cls,name):
    e=ET.SubElement(parent,'Item',{'class':cls});p=ET.SubElement(e,'Properties')
    ET.SubElement(p,'string',{'name':'Name'}).text=name
    return e


def part(parent,cls,can_collide=True):
    e=child(parent,cls,cls);p=e.find('Properties')
    ET.SubElement(p,'bool',{'name':'CanCollide'}).text='true' if can_collide else 'false'
    cf=ET.SubElement(p,'CoordinateFrame',{'name':'CFrame'})
    for k,v in {'X':14.,'Y':7.,'Z':-23.,'R00':1.,'R01':0.,'R02':0.,
                'R10':0.,'R11':1.,'R12':0.,'R20':0.,'R21':0.,'R22':1.}.items():
        ET.SubElement(cf,k).text=str(v)
    size=ET.SubElement(p,'Vector3',{'name':'Size'})
    for k,v in {'X':2.,'Y':6.,'Z':3.}.items():ET.SubElement(size,k).text=str(v)
    return e


def fixture():
    root=ET.Element('roblox',{'version':'4'})
    cmaps=child(child(root,'Folder','ReplicatedStorage'),'Folder','CMaps')
    for name in ('Manor','District'):
        walls=child(child(cmaps,'Folder',name),'Folder','Walls')
        infected=child(walls,'Model','Infected Walls')
        for cls in ('Part','WedgePart','MeshPart'):part(infected,cls)
        client=child(walls,'Model','Client Walls');part(client,'Part')
    return ET.tostring(root,encoding='utf8',xml_declaration=True)


def test_converter_keeps_original_wall_types_and_is_deterministic(tmp_path):
    with ZipFile(tmp_path/'original.zip','w') as z:z.writestr('TestPlace/TestPlace.rbxlx',fixture())
    a,b=tmp_path/'first',tmp_path/'second'
    one=recover(tmp_path/'original.zip',a)
    two=recover(tmp_path/'original.zip',b)
    assert one==two
    assert set(x.name for x in a.iterdir())==set(x.name for x in b.iterdir())
    assert all(f.read_bytes()==(b/f.name).read_bytes() for f in a.iterdir())
    assert set(one['maps'])==set(MAPS)
    assert one['maps']['Manor']['wall_count']==3
    assert one['maps']['District']['native_primitives']==2
    assert one['maps']['District']['approximated_meshparts']==1
    assert one['maps']['Laboratory']['wall_count']==0
    assert not (a/'Laboratory.infectedwalls35.jsonl.gz').exists()
    header,p,w,m=[json.loads(x) for x in gzip.decompress(
        (a/'Manor.infectedwalls35.jsonl.gz').read_bytes()).splitlines()]
    assert header['format']=='twr-infected-walls35-v1'
    assert header['wall_count']==3 and header['collision_layer']==8
    assert header['default_enabled'] is False
    assert header['source_rbxlx_sha256']==hashlib.sha256(fixture()).hexdigest()
    assert p['class']=='Part' and w['class']=='WedgePart'
    assert m['class']=='MeshPart' and m['approximated_mesh'] is True
    assert p['t']==[14.,7.,-23.] and p['s']==[2.,6.,3.]
    assert not any(x['class']=='Client Walls' for x in (p,w,m))


def test_loader_provenance_and_fail_closed():
    runtime=source('src/Twr.Godot/Scripts/Pass35InfectedWallsRuntime.cs')
    assert 'OwnerSourceSha = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2"' in runtime
    assert 'sha!=expected.Sha' in runtime
    assert 'sourceSha != (synthetic ? new string' in runtime
    assert 'new BoxShape3D{Size=size}' in runtime
    assert 'RobloxPrimitiveGeometry.WedgeCollision(size)' in runtime
    assert 'shapeClass=="MeshPart"' in runtime
    assert 'CollisionLayer=0,CollisionMask=0' in runtime
    assert 'body.CollisionLayer = enabled ? InfectedWallCollisionLayer : 0;' in runtime
    assert 'if (!Expected.TryGetValue(mapName,out var exact)) return null;' in runtime
    assert 'MaximumWalls = 400' in runtime


def test_f7_collision_layer_only_for_infected():
    root=source('src/Twr.Godot/Scripts/GameplayRoot.cs')
    agent=source('src/Twr.Godot/Scripts/InfectedAgent.cs')
    player=source('src/Twr.Godot/Scripts/FirstPersonPlayer.cs')
    assert 'Pass35InfectedWallsRuntime.TryBuild(this, MapName)' in root
    assert 'key.Keycode == Key.F7' in root
    assert '_originalInfectedWalls.SetEnabled(!_originalInfectedWalls.Enabled)' in root
    assert 'CollisionMask = 1 | Pass35InfectedWallsRuntime.InfectedWallCollisionLayer |' in agent
    assert 'query.CollisionMask=1 | Pass35InfectedWallsRuntime.InfectedWallCollisionLayer |' in agent
    assert 'CollisionMask = 1 | 2 | Pass34ClientWallsRuntime.ClientWallCollisionLayer |' in player
    assert 'Pass35InfectedWallsRuntime.InfectedWallCollisionLayer' not in player
    assert 'CollisionMask = 1 | 2 | Pass34ClientWallsRuntime' in player
    assert 'OriginalInfectedMeshProxyCount' in root


def test_native_game_and_public_ci_exclude_private_owner_walls():
    bootstrap=source('src/Twr.Godot/Scripts/Bootstrap.cs')
    workflow=source('.github/workflows/windows-build.yml')
    fixture=source('tools/validation/make_pass30_synthetic_sidecars.py')
    assert '--smoke-pass35' in bootstrap
    assert 'TWR_SMOKE_PASS35_INFECTED_WALLS_OK' in bootstrap
    assert 'TWR_SMOKE_PASS35_INFECTED_WALLS_OK' in workflow
    assert 'Pass35InfectedWallsRuntime.InfectedWallCollisionLayer' in bootstrap
    assert "base/'Walls'/'Manor.infectedwalls35.jsonl.gz':infectedwalls35()" in fixture
    assert "'source_rbxlx_sha256':'0'*64" in fixture
    assert '--smoke-pass35' in workflow
    assert '*.infectedwalls35.jsonl.gz' in workflow
    assert 'TWR-Pass36-Windows-x64-NoPrivateAssets' in workflow
    assert 'make_pass30_synthetic_sidecars.py --output $outputDir --clean' in workflow
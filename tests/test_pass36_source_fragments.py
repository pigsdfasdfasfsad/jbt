"""Pass36: synthetic-only nine-map CMaps recovery and compiled-game source contracts."""
from __future__ import annotations
import gzip
import json
import zipfile
from pathlib import Path
import importlib.util
import sys
from lxml import etree
from conftest import ROOT


def source(path):
    return (ROOT / path).read_text(encoding='utf8')


def item(parent,cls,name):
    el=etree.SubElement(parent,'Item',attrib={'class':cls})
    props=etree.SubElement(el,'Properties')
    etree.SubElement(props,'string',name='Name').text=name
    return el,props


def original_part(parent,cls,name,at=(0,5,0)):
    el,props=item(parent,cls,name)
    frame=etree.SubElement(props,'CoordinateFrame',name='CFrame')
    for key,value in zip(('X','Y','Z','R00','R01','R02','R10','R11','R12','R20','R21','R22'),
                         (*at,1,0,0,0,1,0,0,0,1)):
        etree.SubElement(frame,key).text=str(value)
    size=etree.SubElement(props,'Vector3',name='Size')
    for key,value in zip(('X','Y','Z'),(2,3,4)):
        etree.SubElement(size,key).text=str(value)
    etree.SubElement(props,'bool',name='CanCollide').text='true'
    etree.SubElement(props,'token',name='Material').text='512'
    etree.SubElement(props,'float',name='Transparency').text='0'
    return el


def synthetic_rbxlx(destination):
    root=etree.Element('roblox')
    rep,_=item(root,'ReplicatedStorage','ReplicatedStorage')
    maps,_=item(rep,'Folder','CMaps')
    names=('Ranch','Mill','Bypass','Cabin','Cargo','District','Expressway','Prison','Manor')
    for n,map_name in enumerate(names):
        cur,_=item(maps,'Folder',map_name)
        walls,_=item(cur,'Folder','Walls')
        server,_=item(walls,'Model','Server Walls')
        original_part(server,'Part','ServerWall',(n*10,5,0))
        mapping,_=item(cur,'Folder','Map')
        objects,_=item(mapping,'Folder','Objects')
        original_part(objects,'Part','NativeProp',(n*10,5,1))
        original_part(objects,'MeshPart','MissingOriginalMesh',(n*10,5,2))
    with zipfile.ZipFile(destination,'w',compression=zipfile.ZIP_DEFLATED) as z:
        z.writestr('TestPlace/TestPlace.rbxlx',etree.tostring(root,encoding='utf8'))


def test_original_fragment_recovery_is_repeatable_with_explicit_unavailable_meshes(tmp_path):
    p=ROOT/'tools/validation/recover_cmaps_source_fragments36.py'
    spec=importlib.util.spec_from_file_location('pass36_recover',p)
    module=importlib.util.module_from_spec(spec)
    sys.modules[spec.name]=module
    spec.loader.exec_module(module)
    archive=tmp_path/'fixture.zip'
    synthetic_rbxlx(archive)
    out=tmp_path/'out1'; out2=tmp_path/'out2'
    first=module.recover(archive,out);second=module.recover(archive,out2)
    assert first==second
    assert first['total_wall_records']==9
    assert first['total_object_records']==18
    assert first['missing_custom_object_meshes']==9
    assert len(first['source_maps'])==9
    for map_name,info in first['source_maps'].items():
        assert info['server_walls']==1 and info['server_native']==1
        assert info['cmap_objects']==2 and info['object_native']==1
        assert info['object_missing_custom_geometry']==1
        wfile=out/(map_name+'.serverwalls36.jsonl.gz')
        ofile=out/(map_name+'.objects36.jsonl.gz')
        assert wfile.read_bytes()==(out2/wfile.name).read_bytes()
        assert ofile.read_bytes()==(out2/ofile.name).read_bytes()
        objects=[json.loads(line) for line in gzip.decompress(ofile.read_bytes()).splitlines()]
        assert [r['geometry_status'] for r in objects[1:]]==[
            'native_primitive','external_mesh_or_csg_unavailable']
    assert len(list(out.iterdir()))==19


def test_optional_source_collision_and_native_props_are_provenance_checked():
    code=source('src/Twr.Godot/Scripts/Pass36SourceFragmentsRuntime.cs')
    assert 'SourceServerCollisionLayer = 16' in code
    assert 'Original source server-wall header rejected' in code
    assert 'Source fragment pack SHA-256 mismatch' in code
    assert 'ApproximateServerMeshWallCount' in code
    assert 'if(!real)continue' in code  # custom mesh/CSG not forged
    assert 'ServerCollisionEnabled=enabled;' in code
    assert 'NativeObjectPreviewEnabled=enabled;' in code
    assert 'Visible=false' in code
    assert 'CollisionLayer=0,CollisionMask=0' in code
    game=source('src/Twr.Godot/Scripts/GameplayRoot.cs')
    assert 'Pass36SourceFragmentsRuntime.TryBuild(this, MapName)' in game
    assert 'key.Keycode == Key.F5' in game
    assert 'key.Keycode == Key.F6' in game
    assert 'Pass36MapPlanOverlay.TryBuild(this, MapName)' in game


def test_previous_collision_layers_and_source_map_plan_stay_separate():
    player=source('src/Twr.Godot/Scripts/FirstPersonPlayer.cs')
    infected=source('src/Twr.Godot/Scripts/InfectedAgent.cs')
    cloud=source('src/Twr.Godot/Scripts/SporeCloudRuntime.cs')
    projectile=source('src/Twr.Godot/Scripts/SporeProjectileRuntime.cs')
    assert 'Pass36SourceFragmentsRuntime.SourceServerCollisionLayer' in player
    assert 'Pass34ClientWallsRuntime.ClientWallCollisionLayer' in player
    assert 'Pass35InfectedWallsRuntime.InfectedWallCollisionLayer' in infected
    assert 'Pass36SourceFragmentsRuntime.SourceServerCollisionLayer' in infected
    assert 'Pass34ClientWallsRuntime.ClientWallCollisionLayer' not in infected
    assert 'Pass36SourceFragmentsRuntime.SourceServerCollisionLayer' in cloud
    assert 'Pass36SourceFragmentsRuntime.SourceServerCollisionLayer' in projectile
    overlay=source('src/Twr.Godot/Scripts/Pass36MapPlanOverlay.cs')
    assert 'key.Keycode!=Key.F4' in overlay
    assert 'map plan outline SHA mismatch' in overlay or 'source map outline SHA mismatch' in overlay
    hud=source('src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs')
    assert 'Pass36SourceServerWallCount' in hud
    assert 'Pass36MissingOriginalMeshCount' in hud
    assert 'source_server_walls_enabled' in hud


def test_windows_native_smoke_and_public_artifact_privacy():
    app=source('src/Twr.Godot/Scripts/Bootstrap.cs')
    workflow=source('.github/workflows/windows-build.yml')
    assert '--smoke-pass36' in app and '--smoke-pass36' in workflow
    assert 'TWR_SMOKE_PASS36_SOURCE_MAP_OK' in app and 'TWR_SMOKE_PASS36_SOURCE_MAP_OK' in workflow
    assert 'serverwalls36.jsonl.gz' in workflow
    assert 'objects36.jsonl.gz' in workflow
    assert 'sourcefragment36.png' in workflow
    assert 'TWR-Pass36-Windows-x64-NoPrivateAssets' in workflow
    assert 'make_pass30_synthetic_sidecars.py --output $outputDir --clean' in workflow
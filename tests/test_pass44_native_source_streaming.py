"""Pass44 originally-positioned native Laboratory batching, strict fallback and CI smoke."""
import gzip
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from conftest import ROOT

TOOLS=ROOT/'tools'/'validation'

def fixture(root,action):
    command=[sys.executable,str(TOOLS/'make_pass44_synthetic.py'),
             '--output',str(root),action]
    subprocess.run(command,check=True,capture_output=True,text=True)

def test_source_bound_native_cache_is_deterministic_and_complete(tmp_path):
    sys.path.insert(0,str(TOOLS))
    import build_source_primitives44 as exporter
    fixture(tmp_path,'--create')
    scene=tmp_path/'Content'/'Maps'/'Laboratory.scene.jsonl.gz'
    content=tmp_path/'Content'/'Geometry'
    packed=content/'Laboratory.native28.gz'
    first=packed.read_bytes()
    manifest=json.loads((content/'LABORATORY_NATIVE_RENDER_MANIFEST44.json').read_text())
    assert manifest['synthetic_fixture'] is True
    assert manifest['original_geometry_count']==20
    assert manifest['native_instances']==18
    assert manifest['tiles']==3
    assert manifest['batches']>=3
    assert manifest['native_parts']==18
    assert manifest['native_wedges']==0
    assert manifest['excluded_original_geometry']=={
        'invisible':1,
        'special-mesh-preserved-in-fallback':1
    }
    assert manifest['original_custom_mesh_triangles_recovered'] is False
    assert manifest['original_special_meshes_preserved_in_legacy_proxy'] is True
    assert manifest['source_collision_cache_unchanged'] is True
    decoded=exporter.read_pack(packed)
    assert decoded['instances']==18
    assert decoded['tiles']==3
    assert decoded['source_sha256']==hashlib.sha256(scene.read_bytes()).hexdigest()
    exporter.extract(scene,packed,synthetic=True)
    assert packed.read_bytes()==first
    assert manifest['native28_sha256']==hashlib.sha256(first).hexdigest()
    fixture(tmp_path,'--clean')
    assert not packed.exists() and not scene.exists()

def test_native_exporter_excludes_missing_custom_meshes_and_special_shapes():
    sys.path.insert(0,str(TOOLS))
    import build_source_primitives44 as exporter
    base={'class':'Part','shape':'1','mat':'512','opacity':1,
          'rgb':[100,110,120],'shadow':True,
          'r':[1,0,0,0,1,0,0,0,1],
          't':[1,2,3],'s':[4,5,6]}
    matrix=exporter.to_godot_transform(base)
    assert len(matrix)==12
    assert abs(matrix[9]-.28)<1e-7
    assert abs(matrix[10]-.56)<1e-7
    assert abs(matrix[11]+.84)<1e-7
    key,why=exporter.classify(base)
    assert key is not None and why is None
    for cls in ('MeshPart','UnionOperation'):
        assert exporter.classify({**base,'class':cls})[0] is None
    assert exporter.classify({**base,'specialMeshType':'6'})[0] is None
    assert exporter.classify({**base,'specialMeshType':'5','specialMeshOffset':[2,0,0]})[0] is None
    assert exporter.classify({**base,'opacity':0})[0] is None
    assert exporter.classify({**base,'class':'WedgePart'})[0][0]==1

def test_live_godot_loader_keeps_special_mesh_visual_fallback():
    source=(ROOT/'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    streamer=(ROOT/'src/Twr.Godot/Scripts/Pass28PrimitiveStreamer.cs').read_text()
    game=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    hud=(ROOT/'src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs').read_text()
    assert 'Pass28PrimitiveStreamer.TryBuild(stage, mapName, filePath)' in source
    assert 'string.IsNullOrEmpty(Str(r, "specialMeshType"))' in source
    assert '!r.TryGetProperty("specialMeshOffset", out _)' in source
    assert 'if (Flag(r, "collidable"))' in source
    assert source.index('if (Flag(r, "collidable"))')<source.index('if (packedNativeParts')
    assert 'CanSeeBounds(Vector2 viewer, Vector2 minimum' in streamer
    assert 'SourceInstanceCount' in streamer and 'VisibleBatchCount' in streamer
    assert 'Pass44NativePrimitiveCount' in game
    assert 'pass44_source_native_stream_loaded' in hud
    assert 'pass44_source_visible_batches' in hud

def test_ci_win_native_visibility_special_fallback_and_private_cleanup():
    boot=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    workflow=(ROOT/'.github/workflows/windows-build.yml').read_text()
    assert '--smoke-pass44' in boot and '--smoke-pass44' in workflow
    assert 'TWR_SMOKE_PASS44_NATIVE_OK' in boot
    assert 'TWR_SMOKE_PASS44_NATIVE_OK' in workflow
    assert 'renderer.VisibleBatchCount' in boot
    assert 'legacySpecial.Length!=1' in boot
    assert 'make_pass44_synthetic.py --output $outputDir --create' in workflow
    assert 'make_pass44_synthetic.py --output $outputDir --clean' in workflow
    assert '*.native28.gz' in workflow
    assert 'TWR-Pass44-Windows-x64-NoPrivateAssets' in workflow
    assert 'twr-pass44-native-lab-streaming' in workflow

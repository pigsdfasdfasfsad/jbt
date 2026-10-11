"""Pass45: tiled offline MeshPart/CSG fallback rendering and F1 visual-only A/B."""
from __future__ import annotations
import gzip
import hashlib
import json
import subprocess
import sys
from pathlib import Path
from conftest import ROOT

VALIDATION = ROOT / "tools" / "validation"

def synthetic(root,mode):
    subprocess.run([
        sys.executable,str(VALIDATION/"make_pass45_synthetic.py"),
        "--output",str(root),mode
    ],check=True,capture_output=True,text=True)

def test_synthetic_source_native_cache_and_near_far_proxy_pack(tmp_path):
    sys.path.insert(0,str(VALIDATION))
    from build_source_primitives44 import read_pack
    synthetic(tmp_path,"--create")
    source=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    raw=source.read_bytes()
    records=[json.loads(v) for v in gzip.decompress(raw).splitlines()]
    scene=records[0]
    assert scene["format"]=="twr-source-map-v2"
    assert scene["map"]=="Laboratory"
    assert scene["synthetic"] is True
    assert scene["counts"]["geometry"]==22
    assert len([r for r in records[1:] if r.get("kind")=="geometry"])==22
    proxies=[r for r in records if r.get("class")=="MeshPart"]
    assert len(proxies)==2
    assert sorted(r["t"][0] for r in proxies)==[4.,1800.]
    special=[r for r in records if r.get("specialMeshType")=="6"]
    assert len(special)==1
    pack=tmp_path/"Content"/"Geometry"/"Laboratory.native28.gz"
    original=pack.read_bytes()
    assert read_pack(pack)["instances"]==18
    assert read_pack(pack)["source_sha256"]==hashlib.sha256(raw).hexdigest()
    synthetic(tmp_path,"--create")
    assert original==pack.read_bytes()
    synthetic(tmp_path,"--clean")
    assert not source.exists() and not pack.exists()

def test_original_collision_and_nonlaboratory_fallback_are_unchanged():
    source=(ROOT/"src/Twr.Godot/Scripts/LaboratorySourceLoader.cs").read_text()
    geometry=(ROOT/"src/Twr.Godot/Scripts/Pass45FallbackProxyStreamer.cs").read_text()
    assert 'useProxyTiles = mapName == "Laboratory" && packedNativeParts' in source
    assert 'if (packedNativeParts && (cls == "Part" || cls == "WedgePart")' in source
    assert 'if (Flag(r, "collidable"))' in source
    assert source.index('if (Flag(r, "collidable"))')<source.index("if (packedNativeParts")
    assert 'Pass26CollisionRuntime.TryBuild' in source
    assert 'appearanceCache.Add(appearanceKey,shared)' in source
    assert 'Pass45FallbackProxyStreamer.TileSizeStuds' in source
    assert 'sourceCentre[0]' in source and 'sourceCentre[2]' in source
    assert 'Pass45FallbackProxyStreamer.WorldBounds' in source
    assert 'mm.CustomAabb = new Aabb(low,high-low)' in source
    assert 'fallbackStreamer.Register(renderNode,mm.CustomAabb' in source
    assert 'if (fallbackStreamer is null)' in source
    assert 'TileSizeStuds = 256' in geometry
    assert 'Pass28PrimitiveStreamer.CanSeeBounds' in geometry
    assert 'group.Node.Visible ? HideRadiusMetres : DrawRadiusMetres' in geometry
    assert 'CullEnabled { get; private set; } = true;' in geometry
    assert 'visible = !CullEnabled ||' in geometry
    assert 'local.Position + local.Size * .5f' in geometry
    assert 'transform.Basis.X' in geometry
    assert 'SourceProxyInstanceCount' in geometry
    assert 'VisibleInstanceCount' in geometry
    assert 'CollisionLayer' not in geometry and 'CollisionShape3D' not in geometry

def test_pass45_culling_is_toggleable_without_modifying_gameplay():
    game=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    hud=(ROOT/"src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs").read_text()
    assert 'Pass45FallbackProxyStream")?.Track(_player)' in game
    assert 'key.Keycode == Key.F1' in game
    assert 'proxy.ToggleCull()' in game
    assert 'F1 SOURCE PROXY CULL ON' in game and 'F1 SOURCE PROXY CULL OFF' in game
    assert 'Pass45ProxyVisibleInstances' in game and 'Pass45ProxySourceInstances' in game
    assert 'pass45_proxy_cull_enabled' in hud
    assert 'pass45_total_fallback_instances' in hud
    assert 'pass45_visible_fallback_instances' in hud
    assert 'pass43_frame_p95_ms' in hud
    assert 'pass43_frame_p99_ms' in hud
    assert 'Key.F11' in hud

def test_windows_smoke_compilation_privacy_and_instancing_guard():
    boot=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    action=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert '--smoke-pass45' in boot and '--smoke-pass45' in action
    assert 'TWR_SMOKE_PASS45_PROXY_OK' in boot and 'TWR_SMOKE_PASS45_PROXY_OK' in action
    assert 'rotated_mesh_bounds=PASS' in boot
    assert 'visible_at_spawn=2 visible_far=1' in boot
    assert 'original_collision_retained=true' in boot
    assert 'make_pass45_synthetic.py --output $outputDir --create' in action
    assert 'make_pass45_synthetic.py --output $outputDir --clean' in action
    assert 'twr-pass45-lab-proxy-visibility' in action
    assert 'TWR-Pass45-Windows-x64-NoPrivateAssets' in action
    assert '*.native28.gz' in action
    assert "*.scene.jsonl.gz" in action

"""Pass 41 conservative source-native Laboratory bridge regression contracts."""
from __future__ import annotations
import gzip
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import subprocess
import sys
import numpy as np
from shapely.geometry import box
from shapely.strtree import STRtree
from conftest import ROOT

TOOLS=ROOT/"tools"/"validation"

def module(name):
    path=TOOLS/(name+".py")
    spec=importlib.util.spec_from_file_location(name,path)
    result=importlib.util.module_from_spec(spec)
    sys.modules[spec.name]=result
    if str(TOOLS) not in sys.path:sys.path.insert(0,str(TOOLS))
    spec.loader.exec_module(result)
    return result

def test_pass41_synthetic_v3_exported_game_fixture_has_bridged_corridor(tmp_path):
    fixture=TOOLS/"make_pass41_synthetic.py"
    subprocess.run([sys.executable,str(fixture),"--output",str(tmp_path),"--create"],check=True)
    scene=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    native=tmp_path/"Content"/"Navigation"/"Laboratory.nav41.gz"
    assert scene.exists() and native.exists()
    payload=gzip.decompress(native.read_bytes())
    assert payload[:8]==b"TWRNAV41"
    assert struct.unpack_from("<Iii",payload,8)==(3,19,18)
    assert payload[20:52]==hashlib.sha256(scene.read_bytes()).digest()
    pairs={struct.unpack_from("<ii",payload,52+12*19+8*i) for i in range(18)}
    assert (8,9) not in pairs and (8,10) in pairs
    for i in range(3):
        png=tmp_path/"Content"/"MapPlans"/f"Laboratory.navgraph41-{i}.png"
        assert png.read_bytes().startswith(b"\x89PNG\r\n\x1a\n")
    subprocess.run([sys.executable,str(fixture),"--output",str(tmp_path),"--clean"],check=True)
    assert not native.exists()

def test_pass41_exporter_is_deterministic_and_preserves_base_nav(tmp_path):
    source_fixture=TOOLS/"make_pass40_synthetic.py"
    exporter=TOOLS/"recover_source_bridges41.py"
    subprocess.run([sys.executable,str(source_fixture),"--output",
                    str(tmp_path),"--create"],check=True)
    scene=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    base=tmp_path/"Content"/"Navigation"/"Laboratory.nav31.gz"
    original=base.read_bytes()
    for out in ("repair1.nav41.gz","repair2.nav41.gz"):
        subprocess.run([sys.executable,str(exporter),"--scene",str(scene),
                        "--base",str(base),"--out",str(tmp_path/out),
                        "--synthetic"],check=True,capture_output=True,text=True)
    assert (tmp_path/"repair1.nav41.gz").read_bytes()==(tmp_path/"repair2.nav41.gz").read_bytes()
    assert base.read_bytes()==original
    payload=gzip.decompress((tmp_path/"repair1.nav41.gz").read_bytes())
    prior=gzip.decompress(original)
    assert payload[:8]==b"TWRNAV41"
    assert struct.unpack_from("<Iii",payload,8)[:2]==(3,19)
    assert payload[20:52]==hashlib.sha256(scene.read_bytes()).digest()
    assert payload[52:52+12*19]==prior[52:52+12*19]  # All old nodes unaltered.
    report=json.loads((tmp_path/"LABORATORY_NATIVE_BRIDGES_MANIFEST41.json").read_text())
    assert not report["authoritative_roblox_navmesh"]
    assert report["original_waypoints_unchanged"] and report["all_original_edges_preserved"]
    assert report["maximum_bridge_studs"]==8.0
    assert report["bridge_interior_physics_samples"]==9
    # SHA pinning is mandatory even in fixture mode: mutating the scene
    # makes the embedded source digest invalid.
    scene.write_bytes(scene.read_bytes()+b"modified")
    failed=subprocess.run([sys.executable,str(exporter),"--scene",str(scene),
                           "--base",str(base),"--out",str(tmp_path/"bad.gz"),
                           "--synthetic"],capture_output=True,text=True)
    assert failed.returncode!=0
    assert "not bound to owner scene bytes" in (failed.stderr+failed.stdout)

def test_pass41_no_shortcuts_across_missing_floor_proxy_or_wall():
    source40=module("recover_source_navigation40")
    bridge=module("recover_source_bridges41")
    native=source40.Surface(box(-1,-3,9,3),1.0,1,False)
    proxy=source40.Surface(box(-1,-3,9,3),1.0,1,True)
    clear=[source40.Collider(box(-1,-3,9,3),-.1,0.0,1)]
    wall=source40.Collider(box(3,-3,4,3),0.0,8.0,2)
    a=np.array([0.,1.,0.]);b=np.array([7.,1.,0.])
    def check(surfaces,colliders):
        return bridge.bridge_clear(
            a,b,STRtree([x.polygon for x in surfaces]),surfaces,
            STRtree([x.polygon for x in colliders]),colliders)
    assert check([native],clear)
    assert not check([proxy],clear)
    assert not check([native],clear+[wall])
    assert not check([source40.Surface(box(-1,-3,2,3),1,3,False),
                      source40.Surface(box(5,-3,9,3),1,4,False)],clear)
    assert not bridge.bridge_clear(a,np.array([12.,1.,0.]),
                    STRtree([native.polygon]),[native],
                    STRtree([clear[0].polygon]),clear)
    result,stats=bridge.bridge_candidates(
        np.array([a,b]),[0,1],STRtree([native.polygon]),[native],
        STRtree([clear[0].polygon]),clear)
    assert result==[(0,1)] and stats["distinct_component_links"]==1

def test_pass41_runtime_loader_prefers_verified_bridge_then_pass40_fallback():
    nav=(ROOT/"src/Twr.Godot/Scripts/Pass25SourceNavigationRuntime.cs").read_text()
    overlay=(ROOT/"src/Twr.Godot/Scripts/Pass39LaboratoryFloorplan.cs").read_text()
    gameplay=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    diagnostic=(ROOT/"src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs").read_text()
    assert 'CandidatePaths("Navigation", "Laboratory.nav41.gz")' in nav
    assert 'Concat(CandidatePaths("Navigation", "Laboratory.nav31.gz"))' in nav
    assert '12ac9bae602cbdea3ae6a6789289d1068f1c8efd5d603de34d6186698c375dd0' in nav
    assert 'Pass41 native source bridge SHA mismatch' in nav
    assert 'TWRNAV41' in nav and '3u' in nav
    assert 'ConnectedComponentCount' in nav
    assert 'Pass41NativeBridgeCount' in gameplay
    assert 'pass41_remaining_navigation_components' in diagnostic
    assert 'Laboratory.navgraph41-' in overlay
    assert 'HasPass41RepairDiagnostic' in overlay

def test_pass41_real_export_smoke_and_private_content_excluded():
    game=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    actions=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert '--smoke-pass41' in game and '--smoke-pass41' in actions
    assert 'TWR_SMOKE_PASS41_NAV_OK' in game
    assert 'TWR_SMOKE_PASS41_NAV_OK' in actions
    assert 'make_pass41_synthetic.py --output $outputDir --create' in actions
    assert 'make_pass41_synthetic.py --output $outputDir --clean' in actions
    assert '*.nav41.gz' in actions and '*.navgraph41-*.png' in actions
    assert 'TWR-Pass41-Windows-x64-NoPrivateAssets' in actions

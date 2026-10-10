"""Pass40: source-bound grounded Laboratory graph and opt-in F9 entry policy."""
from __future__ import annotations
import gzip
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import subprocess
import sys
from conftest import ROOT

MAPS=ROOT/"tools"/"validation"

def test_synthetic_graph_is_deterministic_and_sha_bound(tmp_path):
    fixture=MAPS/"make_pass40_synthetic.py"
    generator=MAPS/"recover_source_navigation40.py"
    subprocess.run([sys.executable,str(fixture),"--output",str(tmp_path),"--create"],check=True)
    scene=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    nav=tmp_path/"Content"/"Navigation"/"Laboratory.nav31.gz"
    assert scene.is_file() and nav.is_file()
    original=nav.read_bytes()
    assert gzip.decompress(original).startswith(b"TWRNAV31")
    version,nodes,edges=struct.unpack_from("<Iii",gzip.decompress(original),8)
    assert version==2 and nodes==19 and edges==18
    assert gzip.decompress(original)[20:52]==hashlib.sha256(scene.read_bytes()).digest()
    first=tmp_path/"grid1.nav31.gz"
    second=tmp_path/"grid2.nav31.gz"
    subprocess.run([sys.executable,str(generator),"--scene",str(scene),
                    "--out",str(first),"--synthetic"],check=True)
    subprocess.run([sys.executable,str(generator),"--scene",str(scene),
                    "--out",str(second),"--synthetic"],check=True)
    assert first.read_bytes()==second.read_bytes()
    data=gzip.decompress(first.read_bytes())
    assert data[:8]==b"TWRNAV31"
    version,node_count,edge_count=struct.unpack_from("<Iii",data,8)
    assert version==2 and 1000<=node_count<=125000 and 1000<=edge_count<=350000
    assert data[20:52]==hashlib.sha256(scene.read_bytes()).digest()
    assert len(data)==52+12*node_count+8*edge_count
    metadata=json.loads((tmp_path/"LABORATORY_NAVIGATION_MANIFEST40.json").read_text())
    assert metadata["original_roblox_navmesh_recovered"] is False
    assert metadata["meshpart_csg_geometry_missing"] is True
    assert metadata["walkable_surfaces_approximate"] is True
    subprocess.run([sys.executable,str(fixture),"--output",str(tmp_path),"--clean"],check=True)
    assert not scene.exists() and not nav.exists()


def test_runtime_grounded_graph_f9_optin_and_no_unchecked_near_teleports():
    nav=(ROOT/"src/Twr.Godot/Scripts/Pass25SourceNavigationRuntime.cs").read_text()
    safety=(ROOT/"src/Twr.Godot/Scripts/Pass32SpawnSafety.cs").read_text()
    policy=(ROOT/"src/Twr.Godot/Scripts/Pass40AdaptiveEntry.cs").read_text()
    gameplay=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    diagnostics=(ROOT/"src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs").read_text()
    assert "b021b905a0a87619997a0e2da08a4c7b2db18ecd2a5250f93dfd84d08cb26bdc" in nav
    assert "IsPass40GroundedGraph" in nav
    assert "minimumPlayerDistance * minimumPlayerDistance" in nav
    assert "minimumPlayerDistance * minimumPlayerDistance" in safety
    assert "Pass32SpawnSafety.FindSupportedPlacement" in policy
    assert "MinimumFallbackDistance = 10f" in policy
    assert "[24f,14f,10f]" in policy
    assert "AssistedInfectedSpawnsEnabled && _sourceNavigator?.IsBridgePackActive == true" in gameplay
    assert "Pass40AdaptiveEntry.TryFind" in gameplay
    assert "Pass40AdaptiveNearSpawns" in gameplay
    assert "pass40_grounded_navigation_verified" in diagnostics
    assert "pass40_assisted_close_spawns" in diagnostics


def test_windows_pass40_native_smoke_and_privacy():
    smoke=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    workflow=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert "--smoke-pass40" in smoke and "--smoke-pass40" in workflow
    assert "TWR_SMOKE_PASS40_NAV_OK" in smoke and "TWR_SMOKE_PASS40_NAV_OK" in workflow
    assert "make_pass40_synthetic.py --output $outputDir --create" in workflow
    assert "make_pass40_synthetic.py --output $outputDir --clean" in workflow
    assert "*.nav31.gz" in workflow
    assert "TWR-Pass40-Windows-x64-NoPrivateAssets" in workflow
    assert "twr-pass40-grounded-lab-navigation" in workflow

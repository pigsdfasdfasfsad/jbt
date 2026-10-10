"""Pass 42: source-bound typed jump links and real exported Windows smoke."""
import gzip
import hashlib
import json
import struct
import subprocess
import sys
from pathlib import Path
import numpy as np
from shapely.geometry import box
from shapely.strtree import STRtree
from conftest import ROOT

TOOLS=ROOT/"tools"/"validation"

def run_fixture(tmp_path,create):
    return subprocess.run(
        [sys.executable,str(TOOLS/"make_pass42_synthetic.py"),
         "--output",str(tmp_path),"--create" if create else "--clean"],
        capture_output=True,text=True,check=True)

def test_fixture_is_fabricated_source_bound_v4_and_cleans(tmp_path):
    run_fixture(tmp_path,True)
    scene=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    nav=tmp_path/"Content"/"Navigation"/"Laboratory.nav42.gz"
    raw=gzip.decompress(nav.read_bytes())
    assert raw[:8]==b"TWRNAV42"
    assert struct.unpack_from("<Iii",raw,8)==(4,19,18)
    assert raw[20:52]==hashlib.sha256(scene.read_bytes()).digest()
    assert len(raw)==52+19*12+18*9
    edges=[struct.unpack_from("<iiB",raw,52+19*12+9*i) for i in range(18)]
    assert sum(action for _,_,action in edges)==1
    assert (8,10,1) in edges
    assert all(action in (0,1) for _,_,action in edges)
    for level in range(3):
        assert (tmp_path/"Content"/"MapPlans"/
                f"Laboratory.navgraph42-{level}.png").read_bytes().startswith(b"\x89PNG")
    run_fixture(tmp_path,False)
    assert not nav.exists()
    assert not scene.exists()
    assert not (tmp_path/"Content"/"MapPlans"/"Laboratory.navgraph42-0.png").exists()

def test_jump_recovery_provenance_and_rejects_modified_scene(tmp_path):
    # Run the public reproducible exporter against fabricated geometry. Real
    # source pack is SHA-bound and must never enter public GitHub Actions.
    script=TOOLS/"recover_source_jumps42.py"
    subprocess.run(
        [sys.executable,str(TOOLS/"make_pass41_synthetic.py"),
         "--output",str(tmp_path),"--create"],check=True)
    scene=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    nav=tmp_path/"Content"/"Navigation"/"Laboratory.nav41.gz"
    outputs=[tmp_path/"out1.nav42.gz",tmp_path/"out2.nav42.gz"]
    for out in outputs:
        subprocess.run([sys.executable,str(script),"--scene",str(scene),
                        "--base",str(nav),"--out",str(out),"--synthetic"],
                       capture_output=True,text=True,check=True)
    assert outputs[0].read_bytes()==outputs[1].read_bytes()
    raw=gzip.decompress(outputs[0].read_bytes())
    assert raw[:8]==b"TWRNAV42"
    version,nodes,edges=struct.unpack_from("<Iii",raw,8)
    assert version==4 and nodes==19
    assert len(raw)==52+nodes*12+edges*9
    assert raw[20:52]==hashlib.sha256(scene.read_bytes()).digest()
    report=json.loads((tmp_path/"LABORATORY_JUMP_LINKS_MANIFEST42.json").read_text())
    assert report["original_jump_capability_source_verified"] is True
    assert report["original_climb_capability"] is False
    assert report["recovered_original_roblox_navmesh"] is False
    assert report["jump_links_approximate"] is True
    assert report["missing_meshes_or_terrain_restored"] is False
    scene.write_bytes(scene.read_bytes()+b"corrupted")
    invalid=subprocess.run([sys.executable,str(script),"--scene",str(scene),
                            "--base",str(nav),"--out",str(tmp_path/"bad.nav42.gz"),
                            "--synthetic"],capture_output=True,text=True)
    assert invalid.returncode!=0
    assert "not bound to original source scene" in invalid.stderr

def test_jump_arc_rejects_solid_wall_and_long_missing_floor_span():
    # Synthetic tests for the original-point, custom-mesh-free collision
    # sampling. Wall/collider rejection is physical, not source guessing.
    sys.path.insert(0,str(TOOLS))
    import recover_source_jumps42 as jumps
    import recover_source_navigation40 as source
    floor=source.Surface(box(-1,-3,10,3),0.5,1,False)
    wall=source.Collider(box(3,-3,4,3),0,14,2)
    ground=source.Collider(box(-1,-3,10,3),-.5,.4,1)
    a=np.array([0.,.5,0.]); b=np.array([7.,.5,0.])
    assert jumps.native_support(*a,STRtree([floor.polygon]),[floor])
    assert jumps.arc_clear(a,b,STRtree([ground.polygon]),[ground])
    assert not jumps.arc_clear(a,b,STRtree([ground.polygon,wall.polygon]),[ground,wall])
    assert not jumps.arc_clear(a,np.array([12.,.5,0.]),
        STRtree([ground.polygon]),[ground])
    # A missing custom MeshPart surface may NOT stand in for native support.
    proxy=source.Surface(floor.polygon,.5,1,True)
    assert not jumps.native_support(*a,STRtree([proxy.polygon]),[proxy])

def test_native_loader_infected_jump_and_15_wave_stage_progression_wired():
    nav=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Pass25SourceNavigationRuntime.cs").read_text()
    agent=(ROOT/"src"/"Twr.Godot"/"Scripts"/"InfectedAgent.cs").read_text()
    bootstrap=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Bootstrap.cs").read_text()
    overlay=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Pass39LaboratoryFloorplan.cs").read_text()
    game=(ROOT/"src"/"Twr.Godot"/"Scripts"/"GameplayRoot.cs").read_text()
    assert 'CandidatePaths("Navigation", "Laboratory.nav42.gz")' in nav
    assert 'Pass42 source jump navigation SHA mismatch' in nav
    assert 'c9f03bfe0c5da13972d5c149f88c4084da61b5804501419aebb6cddac51be4fd' in nav
    assert 'IsJumpLinkBetween' in nav and 'reader.ReadByte()' in nav
    assert 'TryStartSourceJump' in agent and 'TickSourceJump' in agent
    assert 'MoveAndSlide()' in agent and 'SourceJumpLandings' in agent
    assert 'Pass42JumpLinkCount' in game
    assert 'HasPass42JumpDiagnostic' in overlay
    assert '0fb83b5e262ca846f31862c7cc9b5097d3ae3e9833ede87e0c25f15bcbbb3169' in overlay
    assert '--smoke-pass42' in bootstrap and 'RunPass42Smoke' in bootstrap
    assert 'TWR_SMOKE_PASS42_JUMP_OK' in bootstrap
    assert 'for(var expectedWave=1;expectedWave<=ReleaseRules.MaxWaves;expectedWave++)' in bootstrap

def test_ci_cleans_source_jump_fixture_from_windows_public_output():
    workflow=(ROOT/".github/workflows/windows-build.yml").read_text()
    assert 'twr-pass42-jump-navigation' in workflow
    assert 'make_pass42_synthetic.py --output $outputDir --create' in workflow
    assert 'make_pass42_synthetic.py --output $outputDir --clean' in workflow
    assert '--smoke-pass42' in workflow
    assert 'TWR_SMOKE_PASS42_JUMP_OK' in workflow
    assert '*.nav42.gz' in workflow
    assert '*.navgraph42-*.png' in workflow
    assert 'TWR-Pass42-Windows-x64-NoPrivateAssets' in workflow

"""Pass 43: actual physics-gap traversal acceptance, failure accounting and frame telemetry."""
from __future__ import annotations
import gzip
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys
from conftest import ROOT

TOOLS=ROOT/"tools"/"validation"

def fixture(folder,flag):
    subprocess.run([sys.executable,str(TOOLS/"make_pass43_gap_synthetic.py"),
                    "--output",str(folder),flag],capture_output=True,text=True,check=True)

def test_two_disjoint_floor_platforms_and_sha_bound_typed_jump(tmp_path):
    fixture(tmp_path,"--create")
    scene=tmp_path/"Content"/"Maps"/"Laboratory.scene.jsonl.gz"
    rows=[json.loads(x) for x in gzip.decompress(scene.read_bytes()).splitlines()]
    header=rows[0]
    supports=[r for r in rows[1:] if r.get("kind")=="geometry" and r.get("collidable")]
    assert header["synthetic"] is True
    assert header["counts"]["geometry"]==21
    assert len(supports)==2
    spans=sorted((round(p["t"][0]-p["s"][0]/2,5),
                  round(p["t"][0]+p["s"][0]/2,5)) for p in supports)
    assert abs(spans[0][1]-29.4)<1e-4
    assert abs(spans[1][0]-33.6)<1e-4
    assert abs(spans[1][0]-spans[0][1]-4.2)<1e-4
    assert spans[0][0]<28<spans[0][1]
    assert spans[1][0]<35<spans[1][1]
    assert all(not (start <= 31.5 <= end) for start,end in spans)
    assert len([r for r in rows[1:] if r.get("kind")=="spawn" and
                r.get("side")=="infected"])==1
    digest=hashlib.sha256(scene.read_bytes()).digest()
    for version,name in ((2,"Laboratory.nav31.gz"),
                         (3,"Laboratory.nav41.gz"),
                         (4,"Laboratory.nav42.gz")):
        nav=tmp_path/"Content"/"Navigation"/name
        raw=gzip.decompress(nav.read_bytes())
        assert raw[:8] == {2:b"TWRNAV31",3:b"TWRNAV41",4:b"TWRNAV42"}[version]
        assert struct.unpack_from("<I",raw,8)[0]==version
        assert raw[20:52]==digest
    fixture(tmp_path,"--clean")
    assert not scene.exists()
    assert not (tmp_path/"Content"/"Navigation"/"Laboratory.nav42.gz").exists()

def test_jump_landing_policy_refuses_takeoff_side_and_airborne_contacts():
    policy=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Pass43JumpLandingPolicy.cs").read_text()
    agent=(ROOT/"src"/"Twr.Godot"/"Scripts"/"InfectedAgent.cs").read_text()
    assert 'IsSuccessful(bool isOnFloor, double elapsedSeconds,' in policy
    assert 'HorizontalToleranceMetres = .70f' in policy
    assert 'MinimumFlightSeconds = .15' in policy
    assert 'if (!isOnFloor' in policy
    assert 'SourceJumpFailures' in agent
    assert 'Pass43JumpLandingPolicy.IsSuccessful' in agent
    assert 'SourceJumpExpectedLanding' not in agent  # Actual private target is not shared.
    assert '_sourceJumpExpectedLanding = landingWaypoint + Vector3.Up * .8f;' in agent
    assert 'SourceJumpLastLandingErrorMetres' in agent
    assert 'SourceJumpLandings++;' in agent and 'SourceJumpFailures++;' in agent
    assert 'MoveAndSlide();' in agent

def test_match_retirement_and_performance_window_are_bounded_and_reported():
    game=(ROOT/"src"/"Twr.Godot"/"Scripts"/"GameplayRoot.cs").read_text()
    summary=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Pass43FrameWindow.cs").read_text()
    hud=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Pass30DiagnosticsHud.cs").read_text()
    assert 'Capacity = 2048' in summary
    assert 'deltaSeconds > .5' in summary
    assert 'Percentile(ordered,.95)' in summary
    assert 'PeakLivingInfected' in summary
    assert 'Pass43FrameTimes.Record(delta, ActiveInfectedCount)' in game
    assert 'RetirePass43Traversal(infected);' in game
    assert 'Pass43VerifiedJumpLandings' in game
    assert 'Pass43FailedJumpAttempts' in game
    assert 'pass43_frame_p95_ms' in hud
    assert 'pass43_jump_confirmed_landings_match' in hud
    assert 'pass43_jump_failed_attempts_match' in hud
    assert 'TWR_Pass43_Diagnostics-' in hud
    assert 'TWR_Pass33_Diagnostics.json' in hud  # older automation compatibility

def test_native_windows_gap_blocked_wall_and_fifteen_waves_are_real_checks():
    bootstrap=(ROOT/"src"/"Twr.Godot"/"Scripts"/"Bootstrap.cs").read_text()
    workflow=(ROOT/".github"/"workflows"/"windows-build.yml").read_text()
    assert '--smoke-pass43' in bootstrap and '--smoke-pass43' in workflow
    assert 'HasFloorAt(31.5f)' in bootstrap
    assert 'SourceJumpLandings!=1' in bootstrap
    assert 'blocked.SourceJumpFailures<1' in bootstrap
    assert 'Pass43JumpLandingPolicy.IsSuccessful' in bootstrap
    assert 'Pass43TotalJumpAttempts<2' in bootstrap
    assert 'expected<=ReleaseRules.MaxWaves' in bootstrap
    assert 'TWR_SMOKE_PASS43_TRAVERSAL_OK' in bootstrap
    assert 'TWR_SMOKE_PASS43_TRAVERSAL_OK' in workflow
    assert 'make_pass43_gap_synthetic.py --output $outputDir --create' in workflow
    assert 'make_pass43_gap_synthetic.py --output $outputDir --clean' in workflow
    assert 'TWR-Pass43-Windows-x64-NoPrivateAssets' in workflow
    assert 'twr-pass43-physical-jump-verification' in workflow

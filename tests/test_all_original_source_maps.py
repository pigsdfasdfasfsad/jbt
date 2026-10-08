"""Ten-source-map private loader contract with original geometry never in public Git."""
import gzip
import json
import subprocess
import sys
from conftest import ROOT

MAPS=("Ranch","Mill","Bypass","Cabin","Cargo","District",
      "Expressway","Prison","Laboratory","Manor")

def test_source_map_fixture_generates_ten_safe_v2_packs(tmp_path):
    script=ROOT/"tools/maps/make_source_map_smoke_fixtures.py"
    r=subprocess.run([sys.executable,str(script),"--output-dir",str(tmp_path)],
                     capture_output=True,text=True,check=True)
    assert "TWR_SYNTHETIC_SOURCE_MAPS_OK maps=10" in r.stdout
    for name in MAPS:
        with gzip.open(tmp_path/(name+".scene.jsonl.gz"),"rt") as stream:
            head=json.loads(next(stream))
            assert head["synthetic"] is True
            assert head["format"] == "twr-source-map-v2"
            assert head["map"] == name
            assert head["counts"]["geometry"] == 60
            assert head["counts"]["item_markers"] == 5
            assert head["counts"]["fortification_markers"] == 3
            rows=[json.loads(line) for line in stream]
            assert len(rows)==60+3+2+4+8

def test_gameplay_prefers_user_private_full_source_maps():
    game=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    loader=(ROOT/"src/Twr.Godot/Scripts/LaboratorySourceLoader.cs").read_text()
    assert "LaboratorySourceLoader.TryBuild(this, MapName, out var recovered)" in game
    assert '"twr-source-map-v2"' in loader
    assert 'CandidatePaths(mapName)' in loader
    assert '"Recovered" + mapName' in loader
    assert 'expectedCounts["geometry"]' in loader
    assert 'expectedCounts["fortification_markers"]' in loader
    assert 'UseExactInfectedSpawns = true' in loader

def test_exported_windows_runner_tests_ten_packs_and_removes_test_content():
    script=(ROOT/"build/Windows/build.ps1").read_text()
    boot=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    assert "--smoke-all-source-maps" in script
    assert "--smoke-all-source-maps" in boot
    assert "TWR_SMOKE_ALL_SOURCE_MAPS_OK maps=10" in script
    assert "TWR_SMOKE_ALL_SOURCE_MAPS_OK maps=10" in boot
    assert "make_source_map_smoke_fixtures.py" in script
    assert "Remove-Item $testMapFiles -Force" in script

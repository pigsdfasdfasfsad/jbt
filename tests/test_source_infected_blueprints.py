"""Regression tests for optional original R6 infected part assemblies."""
import gzip
import json
import subprocess
import sys
from conftest import ROOT


def test_infected_blueprint_fixture_contains_source_shaped_r6_parts(tmp_path):
    file=tmp_path/"InfectedSourceVariants.json.gz"
    proc=subprocess.run(
        [sys.executable,str(ROOT/"tools/maps/make_source_infected_smoke_fixture.py"),
         "--output",str(file)],
        capture_output=True,text=True,check=True)
    assert "TWR_SYNTHETIC_INFECTED_PACK_OK types=6" in proc.stdout
    with gzip.open(file,"rt") as stream: data=json.load(stream)
    assert data["synthetic"] is True
    assert len(data["types"])==6
    assert all(len(v[0]["parts"])==6 for v in data["types"].values())

def test_real_source_blueprint_takes_priority_over_procedural_proxy():
    source=(ROOT/"src/Twr.Godot/Scripts/InfectedVisualAssembler.cs").read_text()
    loader=(ROOT/"src/Twr.Godot/Scripts/InfectedSourceModelRuntime.cs").read_text()
    assert "InfectedSourceModelRuntime.TryBuild(this, safeType" in source
    assert "UsingSourceBlueprint = true;" in source
    assert "BuildTemporaryHumanoid(safeType)" in source
    assert 'Content","Enemies",PackName' in loader
    assert '"twr-source-infected-variants-v1"' in loader
    assert 'SourceR6Body' in loader
    assert 'Region' not in loader or 'region' in loader
    assert "HttpClient" not in loader

def test_windows_exe_loads_blueprint_and_removes_fixture():
    runner=(ROOT/"build/Windows/build.ps1").read_text()
    boot=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    assert "--smoke-source-infected" in runner
    assert "--smoke-source-infected" in boot
    assert "TWR_SMOKE_SOURCE_INFECTED_OK types=6" in boot
    assert "TWR_SMOKE_SOURCE_INFECTED_OK types=6" in runner
    assert "Remove-Item $privateZombieFixture" in runner

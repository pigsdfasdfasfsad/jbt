"""The original 100-weapon hierarchy must override procedural weapon fallbacks."""
import gzip,json,subprocess,sys
from conftest import ROOT

def test_original_weapon_fixture_has_six_source_tool_models(tmp_path):
    out=tmp_path/"SourceWeaponModels.json.gz"
    proc=subprocess.run(
        [sys.executable,str(ROOT/"tools/maps/make_source_weapon_smoke_fixture.py"),
         "--output",str(out)],capture_output=True,text=True,check=True)
    assert "TWR_SYNTHETIC_ORIGINAL_WEAPONS_OK models=6" in proc.stdout
    with gzip.open(out,"rt") as f:data=json.load(f)
    assert data["format"]=="twr-original-weapon-assemblies-v1"
    assert data["synthetic"] is True
    assert len(data["models"])==6

def test_source_tool_parts_take_priority_over_procedural_models():
    player=(ROOT/"src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs").read_text()
    source=(ROOT/"src/Twr.Godot/Scripts/OriginalWeaponSourceRuntime.cs").read_text()
    assert "OriginalWeaponSourceRuntime.TryBuild(_rig!, spec.Name)" in player
    assert "OriginalWeaponSourceRuntime.TryBuild(_rig!, type)" in player
    assert "UsingOriginalToolAssembly = true" in player
    assert '"twr-original-weapon-assemblies-v1"' in source
    assert 'Content","Weapons",PackName' in source
    assert "TWR_ORIGINAL_TOOL_ASSEMBLED" in source
    assert "HttpClient" not in source

def test_exported_exe_verifies_original_tool_runtime():
    source=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    runner=(ROOT/"build/Windows/build.ps1").read_text()
    assert "--smoke-source-weapons" in source
    assert "--smoke-source-weapons" in runner
    assert "TWR_SMOKE_SOURCE_WEAPONS_OK models=6" in source
    assert "TWR_SMOKE_SOURCE_WEAPONS_OK models=6" in runner
    assert "Remove-Item $toolFixture" in runner

"""Offline infected model assembly contract and exported executable smoke."""
from conftest import ROOT

TYPES = ["Civilian", "Sprinter", "Military", "Hazmat",
         "Riot", "Burster", "Bloater", "Bolter"]

def test_all_infected_visuals_have_offline_model_fallbacks():
    source = (ROOT / 'src/Twr.Godot/Scripts/InfectedVisualAssembler.cs').read_text()
    for cls in TYPES:
        assert f'"{cls}"' in source
    assert 'PreparedOriginalInfected' in source
    assert 'ResourceLoader.Exists(scenePath)' in source
    assert 'BuildTemporaryHumanoid(safeType)' in source
    assert 'rbxassetid://' not in source
    assert 'HttpClient' not in source

def test_active_infected_use_humanoids_not_visible_capsules():
    source = (ROOT / 'src/Twr.Godot/Scripts/InfectedAgent.cs').read_text()
    assert 'new InfectedVisualAssembler' in source
    assert 'AddChild(_visual)' in source
    assert 'new CapsuleMesh' not in source
    assert 'new CapsuleShape3D' in source
    assert '_visual?.Attack();' in source

def test_windows_exe_smoke_instantiates_all_eight_infected_types():
    build = (ROOT / 'build/Windows/build.ps1').read_text()
    boot = (ROOT / 'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    assert '--smoke-infected-models' in build
    assert 'TWR_SMOKE_INFECTED_VISUALS_OK types=8' in build
    assert 'TWR_SMOKE_INFECTED_VISUALS_OK types=' in boot
    for cls in TYPES:
        assert f'"{cls}"' in boot

"""Owner-local screenshots plus reproducible map camera metadata."""
from conftest import ROOT

def test_f12_screenshot_is_explicit_and_private():
    game=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    capture=(ROOT/'src/Twr.Godot/Scripts/FidelityCaptureRuntime.cs').read_text()
    player=(ROOT/'src/Twr.Godot/Scripts/FirstPersonPlayer.cs').read_text()
    assert 'key.Keycode == Key.F12' in game
    assert 'FidelityCaptureRuntime.TryCapture' in game
    assert 'CaptureCamera => _camera' in player
    assert 'user://fidelity-captures' in capture
    assert 'viewport.GetTexture().GetImage()' in capture
    assert 'image.SavePng(imagePath)' in capture
    assert 'camera_fov_degrees = fov' in capture
    assert 'camera_position_metres' in capture
    assert 'camera_basis_columns' in capture
    assert 'HttpClient' not in capture

def test_exported_runtime_checks_capture_metadata_schema():
    boot=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    build=(ROOT/'build/Windows/build.ps1').read_text()
    assert 'FidelityCaptureRuntime.MetadataJson(' in boot
    assert 'TWR_SMOKE_FIDELITY_METADATA_OK' in boot
    assert 'TWR_SMOKE_FIDELITY_METADATA_OK' in build

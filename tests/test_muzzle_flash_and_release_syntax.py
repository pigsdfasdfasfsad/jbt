"""Firing must show a brief emitted muzzle flash without changing weapon stat rules."""
from conftest import ROOT

def test_firearm_effect_and_reloading_remain_connected():
    visual=(ROOT/"src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs").read_text()
    player=(ROOT/"src/Twr.Godot/Scripts/FirstPersonPlayer.cs").read_text()
    assert 'Name = "MuzzleFlash"' in visual
    assert '_flashSeconds = .055f' in visual
    assert '_flash.Visible = _flashSeconds > 0;' in visual
    assert 'if (!_melee && _flash is not null)' in visual
    assert 'EmissionEnergyMultiplier = 2.0f' in visual
    assert '_viewModel.Fire()' in player
    assert '_viewModel.Reload(_reloadTimer)' in player

def test_private_power_shell_package_has_ast_gate_in_ci():
    build=(ROOT/"build/Windows/build.ps1").read_text()
    assert 'Parser]::ParseFile(' in build
    assert 'Private package PowerShell syntax invalid' in build

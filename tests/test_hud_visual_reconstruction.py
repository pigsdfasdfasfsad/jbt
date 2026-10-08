"""Visible UI regression coverage against user's original weapon HUD reference."""
from conftest import ROOT

def test_hud_uses_original_inspired_circular_weapon_dial():
    dial=(ROOT/'src/Twr.Godot/Scripts/WeaponDialHud.cs').read_text()
    hud=(ROOT/'src/Twr.Godot/Scripts/GameplayHud.cs').read_text()
    assert 'DrawArc(center' in dial
    assert 'for (var segment = 0; segment < 8; segment++)' in dial
    assert 'OriginalStyleAmmoDial' in dial
    assert 'WeaponDialHud _weaponDial' in hud
    assert '_weaponDial.Display(weaponName' in hud
    assert 'definition.IsShotgun ? "SHOTGUN"' in hud
    assert 'definition.IsLauncher ? "LAUNCHER"' in hud

def test_hud_health_and_armor_are_bottom_anchored():
    hud=(ROOT/'src/Twr.Godot/Scripts/GameplayHud.cs').read_text()
    assert 'WaveHeaderBackdrop' in hud
    assert 'SurvivalStatusBackdrop' in hud
    assert 'HealthTrack' in hud
    assert 'HealthFill' in hud
    assert '383f * hpFraction' in hud
    assert 'AnchorTop = 1' in hud
    assert 'AnchorBottom = 1' in hud
    assert 'new ColorRect[4]' in hud

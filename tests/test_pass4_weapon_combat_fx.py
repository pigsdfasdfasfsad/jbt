"""Actual bullet feedback and source weapon mechanical presentation."""
from conftest import ROOT
CS=ROOT/"src/Twr.Godot/Scripts"

def test_hitscan_draws_tracer_and_impact_when_it_hits():
    player=(CS/"FirstPersonPlayer.cs").read_text()
    fx=(CS/"BallisticImpactRuntime.cs").read_text()
    assert 'BallisticImpactRuntime.Spawn(environment' in player
    assert 'damageKind == "Bullet"' in player
    assert 'Name = "ShotTrace"' in fx
    assert 'Name = infectedTarget ? "InfectedImpact" : "WallImpact"' in fx
    assert 'MaximumActive = 80' in fx
    assert '_remaining <= 0' in fx

def test_original_source_weapons_have_barrel_alignment_and_animated_mechanics():
    tool=(CS/"OriginalWeaponSourceRuntime.cs").read_text()
    view=(CS/"WeaponViewModelRuntime.cs").read_text()
    assert 'SourceBarrelFacesBackwards' in tool
    assert 'EstimatedMuzzle(string weaponName)' in tool
    assert 'alignment * SourcePosition(part)' in tool
    assert 'OriginalWeaponSourceRuntime.EstimatedMuzzle(spec.Name)' in view
    assert 'TrackMechanicalParts()' in view
    assert '_mechanicalKick = .11f' in view
    assert 'Vector3.Down * magazineDrop' in view

def test_ballistic_effect_constructs_in_exported_windows():
    boot=(CS/"Bootstrap.cs").read_text()
    runner=(ROOT/"build/Windows/build.ps1").read_text()
    assert "TWR_SMOKE_BALLISTIC_FX_OK" in boot
    assert "TWR_SMOKE_BALLISTIC_FX_OK" in runner

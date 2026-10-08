"""Recovered radial distances must be converted into the same world units as maps."""
from conftest import ROOT

def test_original_grenade_radius_is_not_treated_as_metres():
    source=(ROOT/'src/Twr.Godot/Scripts/ThrowableProjectileRuntime.cs').read_text()
    assert 'FragRadius=30f' in source
    assert 'FragRadiusMeters => RobloxUnits.Distance(FragRadius)' in source
    assert 'if(distance>FragRadiusMeters)continue;' in source
    assert 'distance/FragRadiusMeters' in source

def test_original_spore_and_cloud_radii_match_world_scale():
    proj=(ROOT/'src/Twr.Godot/Scripts/SporeProjectileRuntime.cs').read_text()
    cloud=(ROOT/'src/Twr.Godot/Scripts/SporeCloudRuntime.cs').read_text()
    gameplay=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    assert 'effectiveRadius = RobloxUnits.Distance(Radius)' in proj
    assert 'EffectiveRadius => RobloxUnits.Distance(Radius)' in cloud
    assert 'TopRadius = EffectiveRadius' in cloud
    assert 'RobloxUnits.Distance(blastRadius)' in gameplay
    assert 'Radius = 15f' in gameplay
    assert 'const float blastRadius = 20f' in gameplay

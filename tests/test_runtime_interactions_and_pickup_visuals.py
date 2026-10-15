"""Presentation and interaction contracts backed by native production-scene tests."""
from conftest import ROOT


def source(name):
    return (ROOT/'src/Twr.Godot/Scripts'/name).read_text(encoding='utf8')


def test_fortification_world_transform_is_set_only_after_attachment():
    code = source('FortificationController.cs')
    body = code.split('var actor = new FortificationActor', 1)[1]
    assert 'GlobalPosition = groundedPosition' not in body.split('};', 1)[0]
    assert body.index('AddChild(actor);') < body.index('actor.GlobalPosition = groundedPosition;')
    assert 'if (Player?.GameplayControlsEnabled != true) return;' in code


def test_pickups_and_mounted_weapons_respect_gameplay_control_gate():
    pickup = source('PickupActor.cs')
    assert 'if (Player?.GameplayControlsEnabled != true) return false;' in pickup
    assert 'if (Player?.GameplayControlsEnabled != true) return;' in pickup
    actor = source('FortificationActor.cs')
    assert 'if (Player?.GameplayControlsEnabled != true)' in actor
    assert 'GodotObject.IsInstanceValid(Player)' in actor
    assert 'using var query=PhysicsRayQueryParameters3D.Create' in actor


def test_distinct_pickup_silhouettes_are_authored_and_bounded():
    visual = source('ReconstructedPickupVisuals.cs')
    names = ('Bandages','Medkit','Body Armor','Ammo','Energy Drink','Gas Mask',
             'Frag','Molotov','Nerve Gas','Barbed Wire','Clap Bomb','Jack','50 Cal')
    for name in names:
        assert f'case "{name}":' in visual
    assert 'MaximumMeshParts = 56' in visual
    assert 'new(.55f, .35f, .55f)' in visual
    assert 'Normalize(b.Root);' in visual
    assert 'SetMeta("OriginalGeometryRecovered", false)' in visual
    assert 'SetMeta("OriginalTexturePixels", false)' in visual
    assert 'mesh.Transform * corner' in visual
    for prohibited in ('HttpClient', 'ResourceLoader', 'FileAccess', 'new StaticBody3D', 'new Area3D'):
        assert prohibited not in visual
    assert 'ReconstructedPickupVisuals.Build(PickupType, PickupColor(PickupType))' in source('PickupActor.cs')


def test_native_checks_use_actual_actor_integration_and_old_footprint():
    native = source('RuntimePickupVisualAcceptance.cs')
    assert 'new PickupActor' in native
    assert 'ReconstructedPickupVisuals.Measure(model)' in native
    assert 'actors.Count != 13' in native
    assert 'OriginalGeometryRecovered' in native
    assert 'TWR_RUNTIME_PICKUP_VISUALS_OK' in native
    interaction = source('RuntimeInteractionAcceptance.cs')
    assert 'runtime.GrantItem("Barbed Wire", 1, 2)' in interaction
    assert 'forts._UnhandledInput(swing);' in interaction
    assert 'actor.GlobalPosition' not in interaction
    assert 'TWR_RUNTIME_INTERACTION_OK' in interaction
    readiness = source('RuntimeReadinessAcceptance.cs')
    assert 'await RuntimePickupVisualAcceptance.Verify(game);' in readiness
    assert 'await RuntimeInteractionAcceptance.Verify(game, player, Runtime);' in readiness

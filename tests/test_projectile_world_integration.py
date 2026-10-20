"""Contracts supplement actual --smoke-projectile-sweep and real renderer tests."""
from conftest import ROOT

def read(p):
    return (ROOT/p).read_text(encoding='utf8')

def test_physics_uses_sphere_extent_and_explicit_initial_overlap():
    s=read('src/Twr.Godot/Scripts/ProjectileSweep.cs')
    assert 'new SphereShape3D' in s
    assert s.index('space.IntersectShape(')<s.index('space.CastMotion(')
    assert 'space.GetRestInfo(_query)' in s
    assert '_query.Motion = Vector3.Zero;' in s
    assert 'safe - Skin / length' in s
    assert '_query.Dispose();' in s and '_shape.Dispose();' in s
    assert 'float.IsFinite' in s

def test_actual_player_starts_projectiles_at_eye_then_sweeps_launch_offset():
    s=read('src/Twr.Godot/Scripts/FirstPersonPlayer.cs')
    assert 'LaunchOffset=.55f' in s and 'LaunchOffset = .6f' in s
    assert 'Position=parent.ToLocal(AimOrigin)' in s
    assert 'Position = parent.ToLocal(_camera.GlobalPosition)' in s
    assert 'projectile.GlobalPosition=AimOrigin+AimDirection' not in s
    assert 'projectile.GlobalPosition = _camera.GlobalPosition + ' not in s

def test_frag_support_is_rechecked_and_resolution_is_idempotent():
    s=read('src/Twr.Godot/Scripts/ThrowableProjectileRuntime.cs')
    assert 'Vector3.Down * .06f' in s
    assert '_restingOnGround = false;' in s
    assert s.count('if (Resolved) return;')==2
    assert 'private const double FragFuse=3.0;' in s
    assert 'SourcePlayer' in s and 'Pass75BlastOcclusion.CanReachTarget(' in s

def test_startup_quit_is_deferred_and_deduplicates_pending_request():
    s=read('src/Twr.Godot/Scripts/RuntimeShutdownCoordinator.cs')
    assert 'root.SetMeta(CoordinatorName, coordinator)' in s
    assert 'root.CallDeferred(Node.MethodName.AddChild, coordinator)' in s
    assert 'root.HasMeta(CoordinatorName)' in s
    assert 'if (exitCode != 0) existing._exitCode = exitCode' in s
    assert 'root.RemoveMeta(CoordinatorName)' in s

def test_unstreamed_source_batches_are_bounded_without_changing_mesh_transforms():
    s=read('src/Twr.Godot/Scripts/LaboratorySourceLoader.cs')
    assert 'SourceRenderPartition.Cell(visualTransform.Origin)' in s
    assert 'SourceRenderPartition.MaximumInstances' in s
    assert 'batch.Instances.Add(visualTransform)' in s
    assert 'if (!useProxyTiles)' in s
    assert 'appearanceCache.Add(appearanceKey,shared)' in s
    assert 'mm.CustomAabb = new Aabb(low,high-low)' in s


def test_native_and_release_gates_require_the_new_behavior():
    native=read('src/Twr.Godot/Scripts/ProjectileCollisionAcceptance.cs')
    for token in ('player._UnhandledInput(select); player._UnhandledInput(fire);',
                  'TWR_PROJECTILE_COLLISION_OK', 'Runtime.Player.Inventory.GetValueOrDefault("Frag")==count-1',
                  'await RestingFloorRemoval();'):
        assert token in native
    graphics=read('src/Twr.Godot/Scripts/SourceMeshPresentationAcceptance.cs')
    assert 'await SourceRenderPartitionAcceptance.Verify(this);' in graphics
    assert 'TWR_SOURCE_RENDER_PARTITION_OK' in read('tools/validation/run_graphics_validation.py')
    assert '--smoke-projectile-sweep' in read('tools/build/package_source_safe.py')

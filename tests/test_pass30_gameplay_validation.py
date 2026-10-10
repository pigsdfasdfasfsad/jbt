"""Pass30 integration contracts; Godot executable smoke verifies C# execution."""
from conftest import ROOT


def source(path):
    return (ROOT / path).read_text(encoding='utf-8')


def test_geometry_culls_bounds_not_tile_center():
    code = source('src/Twr.Godot/Scripts/Pass28PrimitiveStreamer.cs')
    assert 'CanSeeBounds(Vector2 viewer, Vector2 minimum' in code
    assert 'new Vector2(boundsMin.X, boundsMin.Z)' in code
    assert 'new Vector2(boundsMax.X, boundsMax.Z)' in code
    assert 'CanSeeBounds(center, minimum, maximum, radius)' in code
    assert 'tileCenter.DistanceSquaredTo(center)' not in code


def test_spore_wall_ray_runs_in_physics_process():
    code = source('src/Twr.Godot/Scripts/SporeCloudRuntime.cs')
    assert 'public override void _PhysicsProcess(double delta)' in code
    assert 'BlockedByWorld(aim)' in code


def test_diagnostics_read_only_and_local():
    game = source('src/Twr.Godot/Scripts/GameplayRoot.cs')
    code = source('src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs')
    assert 'new Pass30DiagnosticsHud' in game
    assert 'Key.F10' in code and 'Key.F11' in code
    assert 'OS.GetUserDataDir()' in code
    assert any(name in code for name in ('TWR_Pass30_Diagnostics.json', 'TWR_Pass33_Diagnostics.json'))
    assert 'VisibleBatchCount' in code


def test_export_runs_pass30_contracts():
    bootstrap = source('src/Twr.Godot/Scripts/Bootstrap.cs')
    workflow = source('.github/workflows/windows-build.yml')
    assert '--smoke-pass30' in bootstrap
    assert 'TWR_SMOKE_PASS30_GAMEPLAY_OK' in bootstrap
    assert '--smoke-pass30' in workflow
    assert 'twr-pass30-gameplay-validation' in workflow

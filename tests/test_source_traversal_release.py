"""Lifecycle contracts plus executable-test wiring; actual movement is native."""
from pathlib import Path
from conftest import ROOT
from tools.validation.offline_audit import violations


def test_offline_audit_ignores_only_generated_outputs(tmp_path):
    root=tmp_path
    code=root/'src/Twr.Godot/Scripts/Safe.cs'
    code.parent.mkdir(parents=True)
    code.write_text('class Safe {}')
    generated=root/'src/Twr.Godot/.godot/mono/temp/obj/GlobalUsings.g.cs'
    generated.parent.mkdir(parents=True)
    generated.write_text('global using System.Net.Http;')
    assert violations(root)==[]
    code.write_text('class Bad { HttpClient client; }')
    assert any(token=='httpclient' for _,token in violations(root))


def test_offline_audit_still_checks_gd_and_project_files(tmp_path):
    p=tmp_path/'src/Twr.Godot/Scripts/network.gd'
    p.parent.mkdir(parents=True)
    p.write_text('var client = WebSocket.new()')
    assert violations(tmp_path)==[('src/Twr.Godot/Scripts/network.gd','websocket')]


def test_complete_graph_paths_and_real_collision_are_required():
    text=(ROOT/'src/Twr.Godot/Scripts/SourceTraversalRoutes.cs').read_text()
    assert 'proposed[^1].DistanceTo(end.Value) > MaximumAnchorGap' in text
    assert 'MaximumWaypoints = 160' in text
    assert 'MaximumLength = 75f' in text
    assert 'Pass89EscortSurfaceSafety.CanTravel(world, a, b, exclude)' in text
    assert 'Pass89EscortSurfaceSafety.CanTravel(world, b, a, exclude)' in text


def test_full_source_material_plan_preserves_repair_variants():
    game=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    objective=(ROOT/'src/Twr.Godot/Scripts/ObjectiveRuntime.cs').read_text()
    plan=(ROOT/'src/Twr.Godot/Scripts/SourceObjectiveLayout.cs').read_text()
    assert 'SourceObjectiveLayout.TryPlan(' in game
    assert '_sourceObjectiveLayouts[i] = fullLayout' in game
    assert 'SourceMaterialLayout.MaterialFeet.Length == 12' in objective
    assert 'SourceMaterialLayout.MaterialRoutes.Length != names.Length' in objective
    assert 'largeRepair ? 12 : 10' in plan
    assert 'material.Count == count' in plan


def test_native_acceptance_requires_walking_and_three_complete_material_loops():
    native=(ROOT/'src/Twr.Godot/Scripts/SourceTraversalAcceptance.cs').read_text()
    assert '_deposits!=32 || _receipts!=3 || _walked<70' in native
    assert 'TWR_SOURCE_DETOUR_WALK_OK' in native
    assert '(_,_)=>points[..2]' in native
    assert 'TemporaryRouteBlock' in native
    assert 'Runtime.CompleteObjective(id,config.family)' in native
    runner=(ROOT/'tools/validation/run_source_fidelity.py').read_text()
    assert '--smoke-source-traversal' in runner
    assert 'TWR_SMOKE_SOURCE_TRAVERSAL_OK' in runner
    assert '"--max-fps", "60"' in runner

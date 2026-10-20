"""Wiring contracts supplement the executable behavioral acceptance checks.

Movement, dynamic obstacles, ramps, guidance, real E deposits and reward
idempotency are exercised in Godot, not inferred from these source checks.
"""
from conftest import ROOT
from tools.validation import run_source_fidelity
from tools.build.package_source_safe import verify_receipt
import json
import pytest


def read(path):
    return (ROOT / path).read_text(encoding='utf8')


def test_no_legacy_floorplan_binding_replaced_and_guidance_read_only():
    guide=read('src/Twr.Godot/Scripts/ObjectiveRouteGuidance.cs')
    targets=read('src/Twr.Godot/Scripts/ObjectiveGuidanceTargets.cs')
    assert 'Key.H' in guide and 'Key.F4' not in guide
    assert 'public bool GuideEnabled { get; private set; }' in guide
    assert 'MaximumDots=96' in guide
    assert 'SourceTraversalRoutes.TryPlan(' in guide
    for mutation in ('ConsumeItem(', 'CompleteObjective(', 'SpendAmmo(', 'DamagePlayer('):
        assert mutation not in guide and mutation not in targets
    assert 'if(_carrying)' in targets and 'RETURN MATERIAL' in targets


def test_source_escort_uses_complete_routes_and_own_rid_exclusion():
    layout=read('src/Twr.Godot/Scripts/SourceEscortLayout.cs')
    follower=read('src/Twr.Godot/Scripts/SourceEscortFollower.cs')
    assert layout.count('SourceTraversalRoutes.TryPlan(')==2
    assert 'survivor.MoveAndSlide();' in follower
    assert 'player.GetRid(),survivor.GetRid()' in follower
    assert 'out var renewed,survivor.GetRid()' in follower
    assert 'survivor.IsOnFloor()' in follower
    assert 'Pass90EscortArrivalSafety.CanComplete(' in follower
    assert not any('GlobalPosition =' in line or 'GlobalPosition=' in line for line in follower.splitlines())
    game=read('src/Twr.Godot/Scripts/GameplayRoot.cs')
    assert 'Array.Clear(_sourceEscortLayouts' in game
    assert 'reserved.Add(_sourceEscortLayouts[i]!.Destination)' in game


def test_bolter_recovery_keeps_gravity_and_grounded_launch():
    agent=read('src/Twr.Godot/Scripts/InfectedAgent.cs')
    recovery=agent.split('if (_leapRecovery > 0)',1)[1].split('if (_specialCooldown <= 0',1)[0]
    assert '22f * (float)delta' in recovery
    assert 'MoveAndSlide();' in recovery
    assert 'Target is not null && IsOnFloor()' in agent
    native=read('src/Twr.Godot/Scripts/BolterGroundingAcceptance.cs')
    assert 'TWR_BOLTER_GROUNDING_OK' in native


def test_load_stage_has_no_gameplay_authority_or_collision():
    code=read('src/Twr.Godot/Scripts/LoadStagingVisual.cs')
    assert 'PresentationOnly' in code and 'OriginalGeometryRecovered' in code
    assert 'new StaticBody3D' not in code
    assert 'new CollisionShape3D' not in code
    assert 'ConsumeItem(' not in code and 'CompleteObjective(' not in code


def test_synthetic_native_fixtures_validate_critical_behavior():
    escort=read('src/Twr.Godot/Scripts/SourceEscortAcceptance.cs')
    guide=read('src/Twr.Godot/Scripts/ObjectiveGuidanceAcceptance.cs')
    assert 'VerifyRampTraversal()' in escort
    assert 'new[]{true,false}' in escort
    assert 'TWR_SOURCE_ESCORT_RAMP_OK' in escort
    assert 'blocker.QueueFree()' in escort and 'receipts!=1' in escort
    assert 'for(var i=0;i<10;i++)' in guide
    assert 'AcceptanceKeyboardDriver.PressE(this)' in guide
    assert 'Runtime.CompleteObjective(' in guide
    assert 'guide.VisibleSegments!=0' in guide


def test_release_runner_invokes_all_new_tests_and_keeps_old_tests():
    source=read('tools/validation/run_source_fidelity.py')
    for flag in ('--smoke-runtime-readiness','--smoke-pass76','--smoke-source-traversal',
                 '--smoke-bolter-grounding','--smoke-source-escort','--smoke-objective-guidance'):
        assert flag in source
    graphics=read('tools/validation/run_graphics_validation.py')
    assert 'source-mesh-real-renderer' in graphics
    assert 'actual-gameplay-capture' in graphics
    assert 'guided-load-real-renderer' in graphics
    assert 'guided-load-actual-engine.png' in graphics


def test_package_requires_new_tests_without_weakening_inherited_gates():
    code=read('tools/build/package_source_safe.py')
    assert 'full.json", commit, 34)' in code
    assert 'focus.json", commit, 9)' in code
    # Tanker/audio completion adds two required cases. The release now
    # validates the named receipts and their markers, not just a count.
    assert 'verify_targeted_receipt(' in code
    assert '"--smoke-source-tanker": "TWR_SOURCE_DAMAGE_OK"' in code
    assert '"--smoke-audio-lifecycle": "TWR_AUDIO_LIFECYCLE_OK"' in code
    assert 'graphics.json", commit, 4)' in code


def test_incomplete_receipt_cannot_authorize_new_release(tmp_path):
    receipt=tmp_path/'targeted.json'
    receipt.write_text(json.dumps({'tested_commit':'a'*40,'passed':3,'failed':0}))
    with pytest.raises(ValueError):verify_receipt(receipt,'a'*40,6)

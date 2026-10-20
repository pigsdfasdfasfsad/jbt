"""Contracts supplement native tanker and audio lifecycle execution.
These string checks do not claim scene physics, rendering or playthrough proof.
"""
from pathlib import Path
import importlib.util
import json
import pytest
from conftest import ROOT


def source(name):
    return (ROOT / 'src/Twr.Godot/Scripts' / name).read_text(encoding='utf8')


def package_module():
    spec=importlib.util.spec_from_file_location('tanker_package', ROOT/'tools/build/package_source_safe.py')
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_tanker_plan_preserves_authority_and_validates_whole_vehicle():
    code=source('SourceDamageLayout.cs')
    for token in ('new(4.8f, 2.2f, 1.9f)', 'TryGroundFootprint', 'SourceTraversalRoutes.TryPlan',
                  'RouteCrossesTarget(route,target.Value)', 'IntersectShape(query,1)',
                  'MaximumCandidates = 48', 'highest-lowest>MaximumFloorDifference'):
        assert token in code
    assert 'SourceDamageLayout.TryPlan(' in source('GameplayRoot.cs')
    assert 'SourceDamagePlan = _mapLayout.ProvisionalSourceObjectives122' in source('GameplayRoot.cs')
    assert 'Array.Clear(_sourceDamageLayouts' in source('GameplayRoot.cs')
    assert 'SourceDamagePlan.FiringPosition' in source('ObjectiveGuidanceTargets.cs')
    assert 'player.GlobalPosition =' not in code
    assert 'CompleteObjective(' not in code


def test_tanker_visual_does_not_add_gameplay_collision():
    code=source('ReconstructedTankerVisual.cs')
    assert 'CylindricalTank' in code and 'HazardPanel' in code and 'Wheel' in code
    assert 'CollisionShape3D' not in code and 'StaticBody3D' not in code
    target=source('DamageObjectiveTarget.cs')
    assert 'new Vector3(4.8f, 2.2f, 1.9f)' in target
    assert '!float.IsFinite(amount)' in target
    assert 'damageKind is "Melee" or "Fire"' in target


def test_lifetime_retirement_covers_finished_stop_and_detach():
    code=source('OfflineAudioRuntime.cs')
    for token in ('public override void _ExitTree()', 'voice.Finished -= handler',
                  'voice.Stop();', 'voice.Stream = null;', '_streams.Clear();', '!voice.Playing'):
        assert token in code
    assert 'cue.Positioned.Stop(); cue.Positioned.Stream=null;' in source('Pass38SourceSoundscapeRuntime.cs')
    native=source('AudioLifecycleAcceptance.cs')
    assert 'RemoveChild(audio);' in native and 'stopped.Stop();' in native
    assert 'TWR_AUDIO_LIFECYCLE_OK' in native


def test_native_tanker_exercises_shooting_reload_pickup_and_save():
    code=source('SourceDamageAcceptance.cs')
    for token in ('AcceptanceKeyboardDriver.Walk', 'AcceptanceKeyboardDriver.PressE',
                  'new InputEventMouseButton', 'new InputEventKey {Keycode=Key.R',
                  'ObjectiveRewardPolicy().Reward("Damage")', 'ProgressionService',
                  'SaveCommitted', 'TWR_SOURCE_DAMAGE_OK'):
        assert token in code
    assert 'target.Health =' not in code
    assert 'SetValue(' not in code
    assert 'GetImage()' in code


def make_receipt(package):
    tests=[{'flag':flag, 'passed':True, 'exit_code':0, 'marker_seen':True,
            'required_marker':marker} for flag,marker in package.REQUIRED_TARGETED.items()]
    return {'tested_commit':'checked', 'passed':len(tests), 'failed':0, 'tests':tests}


def test_targeted_release_receipt_requires_named_successful_cases(tmp_path):
    package=package_module(); path=tmp_path/'targeted.json'; data=make_receipt(package)
    path.write_text(json.dumps(data))
    assert package.verify_targeted_receipt(path,'checked')['failed']==0
    # The prior eight cases remain mandatory; new projectile and startup
    # close regressions add requirements rather than replace old coverage.
    assert len(package.REQUIRED_TARGETED)==11
    assert package.REQUIRED_TARGETED["--smoke-held-clearance"]=="TWR_HELD_MODEL_CLEARANCE_OK"
    assert '--smoke-projectile-sweep' in package.REQUIRED_TARGETED
    assert '--smoke-startup-close' in package.REQUIRED_TARGETED
    assert '--smoke-source-tanker' in package.REQUIRED_TARGETED
    assert '--smoke-audio-lifecycle' in package.REQUIRED_TARGETED


@pytest.mark.parametrize('alter', ['missing','duplicate','wrong-marker','failed','exit','unseen','wrong-source'])
def test_targeted_receipt_fails_closed(tmp_path,alter):
    package=package_module(); path=tmp_path/'targeted.json'; data=make_receipt(package)
    if alter=='missing': data['tests'].pop()
    if alter=='duplicate': data['tests'][-1]=data['tests'][0]
    if alter=='wrong-marker': data['tests'][-1]['required_marker']='UNRELATED_OK'
    if alter=='failed': data['tests'][-1]['passed']=False
    if alter=='exit': data['tests'][-1]['exit_code']=139
    if alter=='unseen': data['tests'][-1]['marker_seen']=False
    if alter=='wrong-source': data['tested_commit']='older'
    path.write_text(json.dumps(data))
    with pytest.raises(ValueError): package.verify_targeted_receipt(path,'checked')

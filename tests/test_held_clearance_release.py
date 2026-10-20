"""Recovered viewmodel fitting must ship with real-renderer and native evidence."""
import copy
import json
import pytest
from conftest import ROOT
from tools.build.package_source_safe import (
    REQUIRED_GRAPHICS, REQUIRED_TARGETED,
    verify_graphics_receipt, verify_targeted_receipt,
)


def test_integration_keeps_latest_projectile_api_and_arm_animation_pose():
    player=(ROOT/'src/Twr.Godot/Scripts/FirstPersonPlayer.cs').read_text()
    clearance=(ROOT/'src/Twr.Godot/Scripts/HeldModelClearance.cs').read_text()
    model=(ROOT/'src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs').read_text()
    assert '_heldClearance?.Restore(_viewModel);' in player
    assert '_heldClearance?.Apply(_viewModel,_camera,GetWorld3D(),this,delta);' in player
    assert '_heldClearance?.Dispose();' in player
    assert '_probe.Move(world,from,to-from,WorldMask,player)' in clearance
    assert 'var animationPose=ClearanceBasePose ?? Transform;' in model
    assert '_armRig66.Transform=animationPose.AffineInverse();' in model
    assert 'LaunchOffset' in player and 'LaunchAdvance' not in player
    assert 'SpendAmmo' not in clearance and 'DamagePlayer' not in clearance
    assert 'camera.Fov=' not in clearance


def graphics_report():
    return dict(tested_commit='abc', failed=0, passed=len(REQUIRED_GRAPHICS),
        cases=[dict(name=n, exit_code=0, required_marker=m, marker_seen=True,
                    passed=True) for n,m in REQUIRED_GRAPHICS.items()])


def test_all_named_graphics_cases_required(tmp_path):
    receipt=tmp_path/'graphics.json'
    data=graphics_report()
    receipt.write_text(json.dumps(data))
    assert verify_graphics_receipt(receipt,'abc')==data
    for name in REQUIRED_GRAPHICS:
        broken=copy.deepcopy(data)
        broken['cases']=[c for c in broken['cases'] if c['name']!=name]
        broken['passed']=999 # Aggregate counts never excuse absent execution.
        receipt.write_text(json.dumps(broken))
        with pytest.raises(ValueError,match='Missing or unsuccessful'):
            verify_graphics_receipt(receipt,'abc')


@pytest.mark.parametrize('field,value',[
    ('exit_code',-1073740940),('marker_seen',False),('passed',False),
    ('required_marker','OTHER_PASS'),
])
def test_held_graphics_must_exit_cleanly_and_emit_correct_marker(tmp_path,field,value):
    data=graphics_report()
    held=next(c for c in data['cases'] if c['name']=='held-clearance-real-renderer')
    held[field]=value
    receipt=tmp_path/'graphics.json';receipt.write_text(json.dumps(data))
    with pytest.raises(ValueError): verify_graphics_receipt(receipt,'abc')


def test_duplicate_or_other_revision_cannot_authorize_graphics(tmp_path):
    data=graphics_report();data['cases'].append(data['cases'][-1].copy())
    receipt=tmp_path/'graphics.json';receipt.write_text(json.dumps(data))
    with pytest.raises(ValueError,match='Duplicate'):verify_graphics_receipt(receipt,'abc')
    receipt.write_text(json.dumps(graphics_report()))
    with pytest.raises(ValueError,match='current source'):verify_graphics_receipt(receipt,'other')


def test_native_held_case_is_required_not_just_a_graphics_test(tmp_path):
    assert REQUIRED_TARGETED['--smoke-held-clearance']=='TWR_HELD_MODEL_CLEARANCE_OK'
    data=dict(tested_commit='abc',failed=0,passed=999,tests=[
        dict(flag=f,exit_code=0,required_marker=m,marker_seen=True,passed=True)
        for f,m in REQUIRED_TARGETED.items() if f!='--smoke-held-clearance'])
    receipt=tmp_path/'targeted.json';receipt.write_text(json.dumps(data))
    with pytest.raises(ValueError,match='held-clearance'):verify_targeted_receipt(receipt,'abc')

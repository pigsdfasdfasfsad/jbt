"""Behavioral packaging guards: private files and incomplete runtimes fail closed."""
import importlib.util
import json
from pathlib import Path
import pytest
from conftest import ROOT
spec=importlib.util.spec_from_file_location('source_safe_package',ROOT/'tools/build/package_source_safe.py')
package=importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)

def runtime(tmp_path):
    output=tmp_path/'output';output.mkdir()
    (output/'ThoseWhoRemainOffline.exe').write_bytes(b'fake executable for selection test only')
    data=output/'data_game_windows_x86_64';data.mkdir()
    (data/'coreclr.dll').write_bytes(b'fixture')
    (data/'Twr.Godot.dll').write_bytes(b'fixture')
    (data/'game.runtimeconfig.json').write_text('{}')
    return output,data

def test_runtime_selection_preserves_required_companions(tmp_path):
    output,data=runtime(tmp_path)
    (output/'smoke.stdout.txt').write_text('do not package test logs')
    selected=package.choose_files(output)
    assert {p.name for p in selected}=={'ThoseWhoRemainOffline.exe','coreclr.dll','Twr.Godot.dll','game.runtimeconfig.json'}

def test_missing_bundled_runtime_is_a_release_failure(tmp_path):
    output,data=runtime(tmp_path)
    (data/'coreclr.dll').unlink()
    with pytest.raises(ValueError,match='Self-contained'):package.choose_files(output)

def test_owner_scene_or_leftover_fixture_blocks_entire_package(tmp_path):
    output,_=runtime(tmp_path)
    source=output/'Content/Maps';source.mkdir(parents=True)
    (source/'Laboratory.scene.jsonl.gz').write_bytes(b'private fixture')
    with pytest.raises(ValueError,match='External Content'):package.choose_files(output)

def test_empty_post_test_directories_are_not_bundled(tmp_path):
    output,_=runtime(tmp_path)
    (output/'Content/Assets/Meshes').mkdir(parents=True)
    assert all('Content' not in p.parts for p in package.choose_files(output))

def test_profiles_never_enter_dependency_directory(tmp_path):
    output,data=runtime(tmp_path)
    (data/'profile.json').write_text('{}')
    with pytest.raises(ValueError,match='Unexpected runtime'):package.choose_files(output)

def test_export_input_requires_metadata_only(tmp_path):
    project=tmp_path/'project';(project/'Content').mkdir(parents=True)
    (project/'Content/weapons.json').write_text('{}')
    package.validate_embedded_inputs(project)
    (project/'Content/owner.png').write_bytes(b'private pixels')
    with pytest.raises(ValueError,match='non-metadata'):package.validate_embedded_inputs(project)

def test_receipt_matches_exact_current_commit_and_all_checks(tmp_path):
    receipt=tmp_path/'full.json'
    receipt.write_text(json.dumps({'tested_commit':'exact','passed':34,'failed':0}))
    assert package.verify_receipt(receipt,'exact',34)['passed']==34
    with pytest.raises(ValueError,match='current source'):package.verify_receipt(receipt,'newer',34)
    with pytest.raises(ValueError,match='Incomplete'):package.verify_receipt(receipt,'exact',35)
    receipt.write_text(json.dumps({'tested_commit':'exact','passed':34,'failed':1}))
    with pytest.raises(ValueError,match='current source'):package.verify_receipt(receipt,'exact',34)

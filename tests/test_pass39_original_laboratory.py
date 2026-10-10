"""Pass39: owner Laboratory source, private SHA and synthetic Windows scene contracts."""
from __future__ import annotations
import gzip
import json
import hashlib
from pathlib import Path
import subprocess
import sys
from conftest import ROOT


def test_synthetic_laboratory_floorplan_pack_for_native_windows(tmp_path):
    helper=ROOT/'tools/validation/make_pass39_synthetic.py'
    subprocess.run([sys.executable,str(helper),'--output',str(tmp_path),'--create'],check=True)
    scene=tmp_path/'Content'/'Maps'/'Laboratory.scene.jsonl.gz'
    plans=tmp_path/'Content'/'MapPlans'
    with gzip.open(scene,'rt') as fd:
        doc=json.loads(next(fd))
        records=[json.loads(line) for line in fd]
    assert doc['map']=='Laboratory'
    assert doc['format']=='twr-source-map-v2'
    assert doc['synthetic'] is True
    assert doc['owner_rbxlx_sha256']=='0'*64
    assert doc['counts']=={
        'geometry':20,'collision':1,'lights':1,
        'player_spawns':1,'infected_spawns':1,
        'item_markers':1,'fortification_markers':1}
    assert len(records)==26
    for i in range(3):
        raw=(plans/f'Laboratory.sourceplan39-{i}.png').read_bytes()
        assert raw[:8]==b'\x89PNG\r\n\x1a\n'
        assert len(raw)>128
    subprocess.run([sys.executable,str(helper),'--output',str(tmp_path),'--clean'],check=True)
    assert not scene.exists()
    assert not (plans/'Laboratory.sourceplan39-0.png').exists()


def test_runtime_plan_only_accepts_sha_bound_real_source_and_has_three_floors():
    source=(ROOT/'src/Twr.Godot/Scripts/Pass39LaboratoryFloorplan.cs').read_text()
    game=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    diagnostics=(ROOT/'src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs').read_text()
    assert '35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74' in source
    for sha in ('ee07c7c550e605902fdc448e44d516867a67a7e3f86a3f16008a15cf5dc08ffb',
                '68c7ede384828f0e3e5c56db54ca34edccb030de22c25de79e70393306c089f4',
                '176aa6bc6956751e8914e2c659f90e4c9b35bee1dbfe0ed55503c8f1dd73c60d'):
        assert sha in source
    assert 'Original Laboratory source scene SHA mismatch' in source
    assert 'Original Laboratory floor plan SHA mismatch' in source
    assert 'GetNodeOrNull<Node3D>("RecoveredLaboratory")' in source
    assert 'key.Keycode==Key.F4' in source
    assert 'Key.Right or Key.Left' in source
    assert 'Pass39LaboratoryFloorplan.TryBuild(this, MapName)' in game
    assert 'pass39_verified_original_laboratory' in diagnostics
    assert 'pass39_laboratory_floorplan_active_level' in diagnostics


def test_pass39_build_has_real_export_smoke_and_no_private_artifacts():
    boot=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    workflow=(ROOT/'.github/workflows/windows-build.yml').read_text()
    assert '--smoke-pass39' in boot and '--smoke-pass39' in workflow
    assert 'TWR_SMOKE_PASS39_LAB_OK' in boot
    assert 'TWR_SMOKE_PASS39_LAB_OK' in workflow
    assert 'make_pass39_synthetic.py --output $outputDir --create' in workflow
    assert 'make_pass39_synthetic.py --output $outputDir --clean' in workflow
    assert 'twr-pass39-original-laboratory' in workflow
    assert 'TWR-Pass39-Windows-x64-NoPrivateAssets' in workflow
    assert '*.scene.jsonl.gz' in workflow
    assert '*.sourceplan39-*.png' in workflow
    assert '*.col26.gz' in workflow

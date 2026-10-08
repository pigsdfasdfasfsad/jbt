"""Public CI tests for synthetic pack import; private game assets are excluded."""
import collections
import gzip
import hashlib
import json
import subprocess
import sys

from conftest import ROOT

def test_synthetic_laboratory_fixture_is_deterministic(tmp_path):
    generator = ROOT / 'tools/maps/make_lab_smoke_fixture.py'
    output = tmp_path / 'Laboratory.scene.jsonl.gz'
    command = [sys.executable, str(generator), '--output', str(output)]
    subprocess.run(command, check=True, capture_output=True)
    first = hashlib.sha256(output.read_bytes()).hexdigest()
    subprocess.run(command, check=True, capture_output=True)
    assert hashlib.sha256(output.read_bytes()).hexdigest() == first
    with gzip.open(output, 'rt', encoding='utf-8') as f:
        header = json.loads(next(f))
        counts = collections.Counter(json.loads(line)['kind'] for line in f)
    assert header['synthetic'] is True
    assert header['source_member'] == 'CI GENERATED (NO ROBLOX ASSETS)'
    assert counts == {'geometry': 30000, 'collision': 1000, 'light': 801, 'spawn': 23}

def test_exported_exe_has_laboratory_source_smoke_contract():
    source = (ROOT / 'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    loader = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    build = (ROOT / 'build/Windows/build.ps1').read_text()
    assert '--smoke-lab-source' in source
    assert 'TWR_SMOKE_LAB_SOURCE_OK' in source
    assert 'TWR_LAB_SOURCE_LOADED' in loader
    assert loader.index('root.AddChild(stage);') < loader.index('AddEmitter(stage, emitter);')
    assert 'make_lab_smoke_fixture.py' in build
    assert 'TWR_SMOKE_LAB_SOURCE_OK infected_spawns=15' in build
    assert 'Remove-Item $fixture -Force' in build
    assert 'upload-artifact' not in (ROOT / '.github/workflows/windows-build.yml').read_text()

def test_recovered_spawn_positions_are_not_randomly_offset():
    layout = (ROOT / 'src/Twr.Godot/Scripts/MapBlockoutBuilder.cs').read_text()
    importer = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    gameplay = (ROOT / 'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    assert 'public bool UseExactInfectedSpawns { get; init; }' in layout
    assert 'UseExactInfectedSpawns = true' in importer
    assert 'if (_mapLayout.UseExactInfectedSpawns) return basePoint;' in gameplay

def test_laboratory_light_budget_follows_player():
    loader = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    lights = (ROOT / 'src/Twr.Godot/Scripts/LaboratoryLightStreamer.cs').read_text()
    gameplay = (ROOT / 'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    assert 'lightStreamer.Configure(emitters, playerSpawn)' in loader
    assert 'stage.AddChild(lightStreamer)' in loader
    assert 'public const int ActiveLimit = 128' in lights
    assert 'if (next.DistanceSquaredTo(_referencePosition) < 16f) return;' in lights
    assert 'spot.LookAt(spot.GlobalPosition + direction, up);' in lights
    assert '?.Track(_player);' in gameplay

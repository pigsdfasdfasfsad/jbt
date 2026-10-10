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
    assert counts == {'geometry': 30000, 'collision': 1000, 'light': 801, 'spawn': 23,
                      'pickup_spawn': 174}

def test_exported_exe_has_laboratory_source_smoke_contract():
    source = (ROOT / 'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    loader = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    build = (ROOT / 'build/Windows/build.ps1').read_text()
    assert '--smoke-lab-source' in source
    assert 'TWR_SMOKE_LAB_SOURCE_OK' in source
    assert 'TWR_LAB_SOURCE_LOADED' in loader
    assert loader.index('root.AddChild(stage);') < loader.index('stage.AddChild(lightStreamer);')
    assert 'make_lab_smoke_fixture.py' in build
    assert 'TWR_SMOKE_LAB_SOURCE_OK infected_spawns=15' in build
    assert 'Remove-Item $fixture -Force' in build
    workflow = (ROOT / '.github/workflows/windows-build.yml').read_text()
    # Only the synthetic-asset Pass29 integration branch exports CI binaries.
    assert "github.ref_name == 'twr-pass29-integration'" in workflow
    assert "TWR-Pass36-Windows-x64-NoPrivateAssets" in workflow
    assert "PrivateSourceZip" not in workflow

def test_recovered_spawn_positions_are_not_randomly_offset():
    layout = (ROOT / 'src/Twr.Godot/Scripts/MapBlockoutBuilder.cs').read_text()
    importer = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    gameplay = (ROOT / 'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    assert 'public bool UseExactInfectedSpawns { get; init; }' in layout
    assert 'UseExactInfectedSpawns = true' in importer
    assert 'if (_mapLayout.UseExactInfectedSpawns) return basePoint + Vector3.Up * 0.8f;' in gameplay

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

def test_original_collidable_parts_and_invisible_walls_are_preserved():
    loader = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    fixture = (ROOT / 'tools/maps/make_lab_smoke_fixture.py').read_text()
    assert 'if (Flag(r, "collidable"))' in loader
    assert loader.index('if (Flag(r, "collidable"))') < loader.index('if (opacity < 0.001f) return;')
    assert 'shapeCache.TryGetValue((size, wedge), out var shape)' in loader
    assert 'batchMeshKey = prepared is null ? "" : id' in loader
    assert '(0.0 if i == 0 else 1.0)' in fixture
    assert "'collidable': (i < 16600)" in fixture

def test_laboratory_prepared_textures_require_local_asset_files():
    loader = (ROOT / 'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    assert 'res://Content/Assets/Textures/{id}.png' in loader
    assert 'ResourceLoader.Load<Texture2D>(path)' in loader
    assert 'AlbedoTexture = preparedTexture' in loader
    assert 'batchTextureKey = preparedTexture is null ? "" : textureId' in loader
    assert 'HttpClient' not in loader

def test_prepared_mesh_materials_apply_to_all_mesh_kinds_and_wedges():
    loader = (ROOT/'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    geometry = (ROOT/'src/Twr.Godot/Scripts/RobloxPrimitiveGeometry.cs').read_text()
    fixture = (ROOT/'tools/maps/make_lab_smoke_fixture.py').read_text()
    assert 'MaterialOverride = batch.Material' in loader
    assert 'Material = material,' in loader
    assert 'RobloxPrimitiveGeometry.WedgeMesh()' in loader
    assert 'RobloxPrimitiveGeometry.WedgeCollision(size)' in loader
    assert 'cls == "WedgePart"' in loader
    assert 'new ConvexPolygonShape3D { Points = vertices }' in geometry
    assert 'mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface)' in geometry
    assert "'class': 'WedgePart' if i == 1 else 'Part'" in fixture

def test_original_spawn_markers_are_preserved_as_feet_positions():
    gameplay=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    assert 'UseExactInfectedSpawns' in gameplay
    assert 'basePoint + Vector3.Up * 0.8f' in gameplay
    assert 'return basePoint + new Vector3(_rng.RandfRange' in gameplay

def test_laboratory_original_pickup_markers_are_loaded_separately():
    loader=(ROOT/'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    layout=(ROOT/'src/Twr.Godot/Scripts/MapBlockoutBuilder.cs').read_text()
    gameplay=(ROOT/'src/Twr.Godot/Scripts/GameplayRoot.cs').read_text()
    bootstrap=(ROOT/'src/Twr.Godot/Scripts/Bootstrap.cs').read_text()
    assert 'case "pickup_spawn":' in loader
    assert 'itemMarkers.Count != 127 || fortificationMarkers.Count != 47' in loader
    assert 'FortificationPoints = fortificationMarkers' in loader
    assert 'FortificationPoints { get; init; }' in layout
    assert 'ChooseUniqueMarkers(_mapLayout.FortificationPoints, 2)' in gameplay
    assert 'ChooseUniqueMarkers(_mapLayout.PickupPoints, 4)' in gameplay
    assert 'layout.PickupPoints.Count != 127' in bootstrap
    assert 'layout.FortificationPoints.Count != 47' in bootstrap

def test_specialmesh_visual_scale_does_not_inflate_collision_shapes():
    loader=(ROOT/'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    fixture=(ROOT/'tools/maps/make_lab_smoke_fixture.py').read_text()
    assert 'private static Vector3 VisualExtents(JsonElement r, Vector3 physicalPartSize)' in loader
    assert 'visualSize = VisualExtents(r, size)' in loader
    assert 'ToTransform(r, visualSize, true)' in loader
    assert 'colliders.Add((ToTransform(r, size, false), size,' in loader
    assert "'specialMeshScale': [480.0, 40.0, 480.0]" in fixture

def test_asset_resource_lookups_are_deduplicated():
    loader=(ROOT/'src/Twr.Godot/Scripts/LaboratorySourceLoader.cs').read_text()
    fixture=(ROOT/'tools/maps/make_lab_smoke_fixture.py').read_text()
    assert 'var meshCache = new Dictionary<string, Mesh?>' in loader
    assert 'var textureCache = new Dictionary<string, Texture2D?>' in loader
    assert 'meshCache.TryGetValue(id, out prepared)' in loader
    assert 'textureCache.TryGetValue(textureId, out preparedTexture)' in loader
    assert 'mesh_ids_missing=' in loader
    assert 'texture_ids_missing=' in loader
    assert "'meshId': str(3000 + i % 3)" in fixture
    assert "'textureId': str(2000 + i % 2)" in fixture

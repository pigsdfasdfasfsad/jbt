"""Actual private post-export art must load from disk without network or editor."""
from conftest import ROOT

SRC=ROOT/"src/Twr.Godot/Scripts"

def test_source_maps_support_external_offline_art_and_material_detail():
    resolver=(SRC/"OfflineAssetResolver.cs").read_text()
    map_loader=(SRC/"LaboratorySourceLoader.cs").read_text()
    palette=(SRC/"RobloxMaterialSurface.cs").read_text()
    assert 'Path.Combine(executable, "Content", "Assets", folder, name)' in resolver
    assert 'Image.LoadFromFile(path)' in resolver
    assert 'ImageTexture.CreateFromImage(image)' in resolver
    assert 'new SurfaceTool()' in resolver
    assert 'case "f" when fields.Length >= 4:' in resolver
    assert 'MaxTriangles = 300_000' in resolver
    assert 'if (!NumericId(id)) return null' in resolver
    assert 'OfflineAssetResolver.Mesh(id)' in map_loader
    assert 'OfflineAssetResolver.Texture(id)' in map_loader
    assert 'RobloxMaterialSurface.Apply(material, materialCode, preparedTexture)' in map_loader
    assert 'FallbackTexture(materialCode)' in palette
    assert 'HttpClient' not in resolver

def test_source_zombie_weapon_parts_reuse_offline_meshes_and_textures():
    infected=(SRC/"InfectedSourceModelRuntime.cs").read_text()
    weapons=(SRC/"OriginalWeaponSourceRuntime.cs").read_text()
    for code in (infected,weapons):
        assert 'OfflineAssetResolver.Mesh(' in code
        assert 'OfflineAssetResolver.Texture(textureId)' in code

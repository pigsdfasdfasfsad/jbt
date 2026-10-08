"""Private asset files must be real installed binaries, never just asset IDs."""
import importlib.util
import json
from conftest import ROOT

spec=importlib.util.spec_from_file_location(
    'private_asset_inventory', ROOT/'tools/assets/private_asset_inventory.py')
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

def test_asset_inventory_detects_missing_mesh_and_valid_png(tmp_path):
    manifest=tmp_path/'manifest.json'
    manifest.write_text(json.dumps({
        'format':'twr-laboratory-assets-v1',
        'source_sha256':'test-hash',
        'references':{'MeshId':{'123':2},'TextureID':{'456':3}}
    }))
    texture=tmp_path/'Textures'/'456.png'
    texture.parent.mkdir()
    texture.write_bytes(b'\x89PNG\r\n\x1a\n' + b'dummy')
    data=mod.inspect(manifest,tmp_path)
    assert data['resources']['meshes']['required'] == 1
    assert data['resources']['meshes']['missing'] == 1
    assert data['resources']['textures']['installed'] == 1
    assert not data['fully_resolved']
    assert not data['original_union_csg_render_binaries_available']
    assert data['resources']['meshes']['items'][0]['sha256'] is None

def test_asset_inventory_rejects_zero_byte_and_invalid_headers(tmp_path):
    manifest=tmp_path/'manifest.json'
    manifest.write_text(json.dumps({
        'format':'twr-laboratory-assets-v1',
        'references':{'MeshId':{'3':1},'TextureID':{'4':1}}
    }))
    mesh=tmp_path/'Meshes'/'3.res'
    mesh.parent.mkdir()
    mesh.write_bytes(b'not-a-real-godot-mesh')
    tex=tmp_path/'Textures'/'4.png'
    tex.parent.mkdir()
    tex.write_bytes(b'not-png')
    result=mod.inspect(manifest,tmp_path)
    assert result['resources']['meshes']['installed'] == 0
    assert result['resources']['textures']['installed'] == 0

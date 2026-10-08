"""Checks the source-pack validator with synthetic fixture data."""
import importlib.util
import subprocess
import sys
from conftest import ROOT

spec=importlib.util.spec_from_file_location(
    "map_validator",ROOT/"tools/maps/validate_private_original_scene_pack.py")
validation=importlib.util.module_from_spec(spec)
spec.loader.exec_module(validation)

def test_validator_checks_counts_spawns_colors_and_geometry(tmp_path):
    generator=ROOT/"tools/maps/make_source_map_smoke_fixtures.py"
    subprocess.run([sys.executable,str(generator),"--output-dir",str(tmp_path)],
                   check=True,capture_output=True)
    import pytest
    with pytest.raises(ValueError,match="synthetic fixture"):
        validation.verify_folder(tmp_path)

def test_validator_has_complete_original_map_contract():
    code=(ROOT/"tools/maps/validate_private_original_scene_pack.py").read_text()
    assert '"twr-source-map-v2"' in code
    assert 'collidable_render_parts' in code
    assert 'server_collision_shapes' in code
    assert 'has_source_lighting' in code
    assert 'TWR_SOURCE_MAPS_ALL_VALID maps=' in code
    assert len(validation.MAPS)==10

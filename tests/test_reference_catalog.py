"""Reference-image catalog is deterministic and never republishes source images."""
import importlib.util
import zipfile
from conftest import ROOT

spec = importlib.util.spec_from_file_location(
    "reference_catalog", ROOT/"tools/visual/index_reference_archive.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

def test_reference_image_indexing_and_exclusion(tmp_path):
    archive = tmp_path/"examples.zip"
    with zipfile.ZipFile(archive, "w") as z:
        z.writestr("images/Laboratory-Entrance.png", b"test-png-data")
        z.writestr("images/ManorLayout.jpg", b"test-jpg-data")
        z.writestr("images/Card-mill.png", b"test-card-data")
        z.writestr("images/RandomWeapon.png", b"not-map-data")
    indexed = module.index(archive)
    assert len(indexed["maps"]) == 10
    assert indexed["maps"]["Laboratory"]["count"] == 1
    assert indexed["maps"]["Manor"]["count"] == 1
    assert indexed["maps"]["Mill"]["count"] == 1
    assert sum(v["count"] for v in indexed["maps"].values()) == 3
    assert indexed["only_filenames_and_hashes"] is True
    assert indexed["not_gameplay_validation"] is True
    assert not any("test-png-data" in str(v) for v in indexed["maps"].values())
    assert module.index(archive) == indexed

def test_reference_image_map_prefix_rule():
    assert module.map_from_filename("images/Laboratory-ExaminationRoom.png") == "Laboratory"
    assert module.map_from_filename("images/Card-manor.png") == "Manor"
    assert module.map_from_filename("images/DistrictOverview.png") == "District"
    assert module.map_from_filename("images/Fortification-Deploy.png") is None

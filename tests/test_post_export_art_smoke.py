"""Exported binary must load user-installed original art from the disk beside it."""
import subprocess
import sys
from conftest import ROOT

def test_synthetic_external_obj_png_have_real_format_headers(tmp_path):
    result=subprocess.run(
        [sys.executable,str(ROOT/"tools/assets/make_private_art_smoke_fixture.py"),
         "--output-dir",str(tmp_path)],capture_output=True,text=True,check=True)
    assert "TWR_PRIVATE_ART_FIXTURE_OK id=99887766" in result.stdout
    assert (tmp_path/"Meshes"/"99887766.obj").read_text().count("v ")==4
    assert (tmp_path/"Textures"/"99887766.png").read_bytes().startswith(b"\x89PNG\r\n\x1a\n")

def test_windows_build_exercises_actual_external_resource_resolution():
    script=(ROOT/"build/Windows/build.ps1").read_text()
    bootstrap=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    assert "--smoke-private-art" in script
    assert "--smoke-private-art" in bootstrap
    assert "TWR_SMOKE_PRIVATE_ART_OK mesh=99887766 texture=99887766" in script
    assert "TWR_SMOKE_PRIVATE_ART_OK mesh=99887766 texture=99887766" in bootstrap
    assert "Remove-Item $artMeshFile,$artTextureFile -Force" in script

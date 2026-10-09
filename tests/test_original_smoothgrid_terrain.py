"""Original binary voxel decoding, conversion, and exported Godot integration."""
import gzip
import importlib.util
import struct
import subprocess
import sys
from conftest import ROOT

spec=importlib.util.spec_from_file_location(
    "twr_terrain_decoder", ROOT/"tools/terrain/convert_smoothgrid.py")
terrain=importlib.util.module_from_spec(spec)
spec.loader.exec_module(terrain)

def source_blob(cells):
    # Native packed voxel ordering: X fastest, Z second, Y third.
    raw=bytearray(b"\x01\x05"+bytes(12))
    current=0
    for position,material in sorted(cells):
        while current<position:
            length=min(256,position-current)
            raw += bytes([0x80,length-1])
            current+=length
        raw.append(material)
        current+=1
    while current<32768:
        length=min(256,32768-current)
        raw += bytes([0x80,length-1])
        current+=length
    return bytes(raw)

def test_real_v1_rle_source_conversion_to_offline_chunks():
    assert terrain.SIZE==32
    voxels=source_blob([
        ((6*32+7)*32+5,2),
        ((7*32+8)*32+15,1)
    ])
    chunks=terrain.decode_smoothgrid(voxels)
    assert len(chunks)==1
    m,o=chunks[(0,0,0)]
    assert (int(m[6,7,5]),int(o[6,7,5]))==(2,255)
    assert int(m[7,8,15])==1
    built,stats=terrain.convert_smoothgrid(voxels,bytes(range(69)))
    assert built[:8]==b"TWRTERR1"
    assert struct.unpack_from("<I",built,8)[0]==1
    assert stats["terrain_quads"]==12
    assert stats["solid_voxels"]==1
    assert stats["water_voxels"]==1
    assert len(built)==8+4+69+16+12*7

def test_terrain_conversion_rejects_corrupted_cell_runs():
    import pytest
    with pytest.raises(ValueError,match="Unsupported"):
        terrain.decode_smoothgrid(b"\x09\x05")
    with pytest.raises(ValueError,match="overflow"):
        terrain.decode_smoothgrid(
            b"\x01\x05"+bytes(12)+bytes([0x80,255])*129)

def test_windows_export_loads_original_terrain_collision_and_water(tmp_path):
    generated=tmp_path/"Cabin.terrainmesh.gz"
    subprocess.run([
        sys.executable,str(ROOT/"tools/terrain/make_terrain_smoke_fixture.py"),
        "--output",str(generated)],check=True,capture_output=True)
    with gzip.open(generated,"rb") as f:content=f.read()
    assert content.startswith(b"TWRTERR1")
    assert struct.unpack_from("<I",content,8)[0]==1
    boot=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    build=(ROOT/"build/Windows/build.ps1").read_text()
    game=(ROOT/"src/Twr.Godot/Scripts/GameplayRoot.cs").read_text()
    source=(ROOT/"src/Twr.Godot/Scripts/RecoveredTerrainRuntime.cs").read_text()
    for token in ("--smoke-original-terrain","TWR_SMOKE_ORIGINAL_TERRAIN_OK"):
        assert token in build
        assert token in boot
    assert "RecoveredTerrainRuntime.TryBuild(this, MapName)" in game
    assert "ConcavePolygonShape3D" in source
    assert "SourceWater" in source
    assert "TWR_TERRAIN_LOADED map=" in source
    assert "Remove-Item $terrainFixture" in build

"""Source-space collision and SpecialMesh transform regressions.

These are source-contract checks. Full rendering/physics validation is
performed by the separate exported-Windows executable smoke suite.
"""
from conftest import ROOT

SOURCE = (ROOT / "src/Twr.Godot/Scripts/LaboratorySourceLoader.cs").read_text(encoding="utf-8")


def test_special_mesh_offset_does_not_move_collision():
    assert 'r.TryGetProperty("specialMeshOffset", out var offset)' in SOURCE
    assert 'if (scaled && r.TryGetProperty("specialMeshOffset"' in SOURCE
    assert 'origin += rotation * new Vector3(values[0], values[1],' in SOURCE
    assert '-values[2]) * Stud;' in SOURCE
    assert 'colliders.Add((ToTransform(r, size, false), size,' in SOURCE


def test_source_collision_wedges_retain_wedge_shape():
    assert 'Str(record, "class") == "WedgePart"' in SOURCE
    assert 'cls == "WedgePart"' in SOURCE
    assert 'RobloxPrimitiveGeometry.WedgeCollision(size)' in SOURCE


def test_source_mesh_offset_is_rotated_before_part_visual_scale():
    start = SOURCE.index('private static Transform3D ToTransform')
    offset = SOURCE.index('origin += rotation * new Vector3', start)
    scale = SOURCE.index('rotation = new Basis(', start)
    assert start < offset < scale

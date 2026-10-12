"""Pass86: fallback-only natural loot physically above a clear floor."""
from conftest import ROOT

def src(p): return (ROOT / p).read_text(encoding="utf8")

def test_natural_loot_checks_world_support_volume():
    s=src("src/Twr.Godot/Scripts/Pass86NaturalLootPlacement.cs")
    assert "MaximumMarkerFloorGap = 1.20f" in s
    assert "MinimumUpNormal = .70f" in s
    assert "space.IntersectRay(ray)" in s
    assert "space.IntersectShape(clearance,1)" in s
    assert "new BoxShape3D" in s
    assert "floor+Vector3.Up*.62f" in s
    assert "CollisionMask=1u" in s
    assert "GlobalPosition =" not in s

def test_shuffled_retries_allow_four_item_and_two_fortification_markers():
    g=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "_isFallbackScene86 = !originalWorldAvailable" in g
    assert "private bool SpawnNaturalAt(Vector3 position,string group)" in g
    assert "Pass86NaturalLootPlacement.TryGround(" in g
    assert "RejectedFallbackNaturalPickups86++" in g
    assert "VerifiedFallbackNaturalPickups86++" in g
    assert "++placedItems86>=4" in g
    assert "++placedForts86>=2" in g
    assert "++fortCount>=2" in g
    assert "if (_isFallbackScene86)" in g
    assert "ScheduleNaturalRespawn(position,naturalGroup)" in g

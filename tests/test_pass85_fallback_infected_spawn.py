"""Pass85: physically supported infected spawns only on fallback maps."""
from conftest import ROOT

def read(p):return (ROOT/p).read_text(encoding="utf-8")

def test_fallback_infected_spawn_checks_capsule_floor_and_distance():
    safety=read("src/Twr.Godot/Scripts/Pass85FallbackInfectedSpawn.cs")
    assert "MinimumPlanarPlayerDistance = 11f" in safety
    assert "MaximumFloorAdjustment = .85f" in safety
    assert "MaximumCandidates = 24" in safety
    assert "new CapsuleShape3D" in safety
    assert "space.IntersectRay(ray)" in safety
    assert "space.IntersectShape(query,1)" in safety
    assert "CollisionMask=1u" in safety
    assert "Radius = .30f, Height = 1.60f" in safety
    assert "return actualCentre;" in safety
    assert "GlobalPosition =" not in safety

def test_authoritative_original_source_spawn_never_replaced():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "if (!_mapLayout.UseExactInfectedSpawns)" in game
    assert "Pass85FallbackInfectedSpawn.TryFind(" in game
    assert "RejectedFallbackSpawns85" in game
    assert "VerifiedFallbackSpawns85" in game
    assert "return; // no embedded, unsupported or near-player zombie" in game
    assert 'if (_mapLayout.UseExactInfectedSpawns) return basePoint + Vector3.Up * 0.8f;' in game
    assert "AssistedInfectedSpawnsEnabled && _sourceNavigator?.IsBridgePackActive == true" in game

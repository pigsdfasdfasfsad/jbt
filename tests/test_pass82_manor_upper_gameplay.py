"""Pass82: playable fallback Manor second-floor pickup/objective marker setup."""
from conftest import ROOT

def source(path):
    return (ROOT / path).read_text(encoding="utf-8")

def test_manor_only_upper_markers_come_from_physical_approaches():
    map_src=source("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    plan=source("src/Twr.Godot/Scripts/Pass82ManorUpperSpawnPlan.cs")
    assert "BuildPickupMarkers(" in map_src
    assert "BuildObjectiveMarkers(" in map_src
    assert "ApproximateUpperPickups82 = upperPickups82" in map_src
    assert "ApproximateUpperObjectives82 = upperObjectives82" in map_src
    assert "Vector3.Right*4.25f + Vector3.Up*.62f" in plan
    assert "Vector3.Left*4.25f + Vector3.Up*.35f" in plan
    assert "RequireThree(stairs)" in plan
    assert "Player.GlobalPosition" not in plan
    assert "owner-original" not in plan.lower()

def test_objective_wave_rotation_only_targets_approximate_manor():
    src=source("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "Pass82UpperPickupMarkers" in src
    assert "Pass82UpperObjectiveMarkers" in src
    assert 'MapName == "Manor"' in src
    assert "_mapLayout.ApproximateUpperObjectives82.Count == 3" in src
    assert "wave >= 3" in src
    assert "(wave-1+index) % _mapLayout.ObjectivePoints.Count" in src
    assert "return _mapLayout.ObjectivePoints[index % _mapLayout.ObjectivePoints.Count];" in src

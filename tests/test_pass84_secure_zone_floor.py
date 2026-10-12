"""Pass84: secure-zone progress cannot be earned across Manor decks."""
from conftest import ROOT

def src(p): return (ROOT / p).read_text(encoding="utf8")

def test_secure_does_not_progress_across_approximate_manor_floors():
    objective=src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    safety=src("src/Twr.Godot/Scripts/Pass84SecureZoneFloorSafety.cs")
    assert "RequireSameFloorSecure84" in objective
    assert "Pass84SecureZoneFloorSafety.CanAccumulate(" in objective
    assert "SameStoryBandMetres = 1.35f" in safety
    assert "Math.Abs(playerFeet.Y - target.Y)" in safety
    assert "float.IsFinite(v.X)" in safety
    assert 'MapName == "Manor"' in game
    assert "_mapLayout.ApproximateStairs77.Count == 3" in game
    assert "RequireSameFloorSecure84 = " in game
    assert "SecureRadius = 14.0f" in objective
    assert "SecureSeconds = 60.0" in objective

def test_map_source_pack_never_receives_fallback_floor_rule():
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "RequireSameFloorSecure84 = MapName" in game
    assert "MapBlockoutBuilder.Build(this, _mapDefinition)" in game
    assert "LaboratorySourceLoader.TryBuild(this, MapName" in game

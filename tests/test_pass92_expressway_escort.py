"""Pass92: Expressway Escort uses its own physics-derived graph."""
from conftest import ROOT

def src(path):
    return (ROOT/path).read_text(encoding="utf-8")

def test_expressway_escort_follows_collision_graph_without_replacing_zombie_routes():
    g=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    scene=src("src/Twr.Godot/Scripts/ExpresswaySceneBuilder.cs")
    ai=src("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "private Pass73GroundNavigationRuntime? _expresswayEscortNavigation92;" in g
    assert "Pass92ExpresswayEscortNavReady" in g
    assert "Pass92ExpresswayEscortGroundNavigation" in g
    assert "else if (!originalWorldAvailable && _sourceNavigator is null &&" in g
    assert 'MapName == "Expressway"' in g
    assert "var expresswayEscort92 = family == \"Escort\"" in g
    assert "if (groundEscort91 || expresswayEscort92 || manorUpperEscort94)" in g
    assert "Pass91EscortSpawnSafety.TryFind(" in g
    assert "(_groundNavigation73 is not null || expresswayEscort92)" in g
    assert "objective.EscortGroundNavigator89 =" in g
    assert 'new ExpresswayNavigationRuntime { Name = "HighwayNavigation" }' in scene
    assert "HighwayNavigator" in ai or "ExpresswayNavigator" in ai
    assert "SourceNavigation" in g

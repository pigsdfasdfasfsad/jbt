"""Expressway waypoint navigation through static roadway obstructions."""
from conftest import ROOT

SRC=ROOT/'src/Twr.Godot/Scripts'

def test_road_routes_use_astar_waypoints_with_source_obstacle_exclusions():
    code=(SRC/'ExpresswayNavigationRuntime.cs').read_text()
    assert 'private readonly AStar3D _graph' in code
    assert 'var z=-60f+row*Spacing' in code
    assert 'var x=-18f+column*6f' in code
    assert 'private static bool SegmentClear' in code
    assert 'GetPointPath(start,end)' in code
    for token in ['(-9f,-39f','(9f,-39f','(0f,-25f']:
        assert token in code

def test_expressway_enemies_use_route_then_nearfield_steering():
    scene=(SRC/'ExpresswaySceneBuilder.cs').read_text()
    infected=(SRC/'InfectedAgent.cs').read_text()
    game=(SRC/'GameplayRoot.cs').read_text()
    boot=(SRC/'Bootstrap.cs').read_text()
    assert 'Name = "HighwayNavigation"' in scene
    assert 'HighwayNavigator.GetRoute(' in infected
    assert 'SteerAroundObstacles(desired)' in infected
    assert 'ExpresswayReconstruction/HighwayNavigation' in game
    assert 'navigator.NavigablePoints < 140' in boot
    assert 'corridorRoute.Length < 12' in boot

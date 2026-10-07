from conftest import ROOT

def read(path): return (ROOT/path).read_text()

def test_infected_use_obstacle_steering_on_solid_blockout_geometry():
    s=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "SteerAroundObstacles" in s
    assert "ObstacleAhead" in s
    assert "PhysicsRayQueryParameters3D.Create" in s
    assert "query.CollisionMask=1" in s
    assert "infected are on layer 2" in s

def test_obstacle_steering_preserves_direct_chase_when_clear():
    s=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "if(!ObstacleAhead(desired,2.2f))return desired;" in s
    assert "_steerHold=0.9" in s
    assert "APPROXIMATED steering persistence" in s

def test_bolter_leap_remains_separate_from_ground_steering():
    s=read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert 'InfectedType == "Bolter" && TickBolterLeap' in s
    assert s.index("TickBolterLeap(delta") < s.index("SteerAroundObstacles(desired)")

"""Pass91: release native Godot physics objects deterministically."""
from conftest import ROOT

def source(path):return (ROOT/path).read_text(encoding="utf8")

def test_escort_floor_capsule_and_wall_probes_have_scoped_lifetimes():
    code=source("src/Twr.Godot/Scripts/Pass89EscortSurfaceSafety.cs")
    for literal in ("using var survivorCapsule=new CapsuleShape3D",
                    "using var capsuleSweep=new PhysicsShapeQueryParameters3D",
                    "using var wall=PhysicsRayQueryParameters3D.Create(",
                    "using var floorRay=PhysicsRayQueryParameters3D.Create("):
        assert literal in code

def test_escort_goal_floor_and_capsule_queries_disposed():
    code=source("src/Twr.Godot/Scripts/Pass90EscortArrivalSafety.cs")
    assert "using var ray=PhysicsRayQueryParameters3D.Create(" in code
    assert "using var survivorShape=new CapsuleShape3D" in code
    assert "using var query=new PhysicsShapeQueryParameters3D" in code

def test_nav_grid_releases_queries_and_long_lived_graphs_on_teardown():
    code=source("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    assert code.count("using var ray = PhysicsRayQueryParameters3D.Create(")==3
    assert "using var capsuleQuery = new PhysicsShapeQueryParameters3D" in code
    assert "public override void _ExitTree()" in code
    for literal in ("_standingCapsule.Dispose()","_escortGraph91.Dispose()",
                    "_graph.Dispose()","base._ExitTree()"):
        assert literal in code

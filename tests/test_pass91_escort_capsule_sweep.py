"""Pass91: Escort navigation casts the actual survivor collision capsule."""
from conftest import ROOT

def test_physical_capsule_clears_every_verified_escort_segment():
    s=(ROOT/"src/Twr.Godot/Scripts/Pass89EscortSurfaceSafety.cs").read_text(encoding="utf8")
    for token in (
        "using var survivorCapsule=new CapsuleShape3D",
        "Radius=.42f,Height=1.8f",
        "var capsuleSweep=new PhysicsShapeQueryParameters3D",
        "Motion=toFeet-fromFeet",
        "safeMotion=space.CastMotion(capsuleSweep)",
        "safeMotion[0]<.985f",
        "CollisionMask=1u",
        "space.IntersectRay(floorRay)",
        "space.IntersectRay(wall).Count>0",
    ):
        assert token in s
    assert "GlobalPosition =" not in s

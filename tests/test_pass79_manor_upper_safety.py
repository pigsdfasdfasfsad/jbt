"""Pass79: source-neutral elevated floor support and no-void pursuit."""
from conftest import ROOT

def text(path):
    return (ROOT / path).read_text(encoding="utf-8")

def test_upper_safety_samples_real_godot_collision_not_visual_bounds():
    script = text("src/Twr.Godot/Scripts/Pass79UpperFloorSafety.cs")
    assert "SampleSpacingMetres = .60f" in script
    assert "LateralOffsets = [0f, -.20f, .20f]" in script
    assert "space.IntersectRay(ray)" in script
    assert 'ray.CollisionMask=1u' in script
    assert "MinimumFloorNormal = .74f" in script
    assert "MaximumSegmentMetres = 45f" in script
    assert "GlobalPosition =" not in script
    assert "MoveAndSlide();" not in script

def test_upper_only_restricted_to_approximate_manor_and_safe_on_replanning():
    nav = text("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    ai = text("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert 'MapName == "Manor" && StairLinkCount77 == 3' in nav
    assert "IsManorSameUpperFloor79" in nav
    assert "Pass79UpperFloorSafety.IsSupported(" in nav
    assert "UpperCorridorSupported79" in nav
    assert "SafeUpperMove79" in nav
    assert "RejectedUpperDirectRoutes79" in nav
    assert "_navigationRoute.Length == 0" in ai
    assert "SafeUpperMove79(" in ai
    assert "direction = Vector3.Zero" in ai
    assert "MoveAndSlide();" in ai

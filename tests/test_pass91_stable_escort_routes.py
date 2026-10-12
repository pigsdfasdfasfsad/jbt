"""Pass91 navigation regression: never discard a supported route every 0.8s."""
from conftest import ROOT

def test_verified_escort_waypoints_survive_periodic_timer_ticks():
    code=(ROOT/"src/Twr.Godot/Scripts/ObjectiveRuntime.cs").read_text(encoding="utf8")
    assert "Pass91: Pass89's 0.8-second unconditional replan" in code
    assert "if (_escortRouteInvalidated91 ||" in code
    assert "_escortRouteIndex89 >= _escortRoute89.Length)" in code
    assert "EscortRouteInvalidations91++" in code
    assert "_escortRouteInvalidated91=true" in code
    assert "_escortRouteInvalidated91=false" in code
    assert "Pass89EscortSurfaceSafety.CanTravel(" in code
    assert "actor.MoveAndSlide()" in code
    assert "if (_escortRepathSeconds89 <= 0 ||" not in code
    assert "actor.GlobalPosition =" not in code

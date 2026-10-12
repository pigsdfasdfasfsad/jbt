"""Pass80 fallback Manor physical upper crossing and verified path checks."""
from conftest import ROOT

def source(path):
    return (ROOT / path).read_text(encoding="utf8")

def test_pass80_crossing_is_static_walking_collision_and_not_original():
    map_source=source("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    assert 'name+"_UpperCrossing80"' in map_source
    assert "new Vector3(9.0f,.16f,1.60f)" in map_source
    assert "MakeUpperCrossing80(westStair77)" in map_source
    assert "MakeUpperCrossing80(eastStair77)" in map_source
    assert "MakeUpperCrossing80(frontStair77)" in map_source
    assert "AddDividedManorWall80" in map_source
    assert "open from ground level" in map_source
    assert 'InteriorShell(root, "WestWing"' in map_source
    assert 'InteriorShell(root, "EastWing"' in map_source
    assert "ApproximateCrossings80" in map_source
    assert "source" not in map_source[map_source.index('name+"_UpperCrossing80"'):map_source.index('name+"_UpperCrossing80"')+90].lower()

def test_pass80_upper_crossing_route_checks_every_physical_leg():
    nav=source("src/Twr.Godot/Scripts/Pass73GroundNavigationRuntime.cs")
    enemy=source("src/Twr.Godot/Scripts/InfectedAgent.cs")
    assert "TryGetVerifiedUpperCrossing80" in nav
    for leg in ("from,incoming", "incoming,mid", "mid,outgoing", "outgoing,playerFeet"):
        assert f"UpperCorridorSupported79({leg})" in nav
    assert "VerifiedUpperCrossingRoutes80" in nav
    assert "RejectedUpperCrossingRoutes80" in nav
    assert "if (manorSameUpper79 &&" in enemy
    assert "_navigationRoute.Length > 0" in enemy
    assert "GlobalPosition =" not in nav

"""Pass91: fallback-only collision-validated ground Escort origins."""
from conftest import ROOT
def s(path):return (ROOT/path).read_text(encoding="utf8")

def test_nearby_candidates_have_floor_and_capsule_clearance():
    p=s("src/Twr.Godot/Scripts/Pass91EscortSpawnSafety.cs")
    assert "MaximumSpawnCandidates=17" in p
    assert "MaximumLateralRelocation=10f" in p
    assert "Pass90EscortArrivalSafety.TryGroundDestination(" in p
    assert "ground.Value.Y>2.2f" in p
    assert "planar.LengthSquared()<2f*2f" in p
    assert "Math.Min(Offsets.Length,MaximumSpawnCandidates)" in p
    assert "return ground.Value" in p
    assert "GlobalPosition =" not in p

def test_owner_exact_scenes_untouched_and_manor_has_a_separate_upper_policy():
    g=s("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert 'var groundEscort91 = family == "Escort"' in g
    assert "_isFallbackScene86 && _groundNavigation73 is not null" in g
    assert 'MapName != "Manor" || position.Y < 2.3f' in g
    assert "Pass91EscortSpawnSafety.TryFind(" in g
    assert "manorUpperEscort94" in g
    assert "Pass94ManorEscortRoute.TryGroundUpperSpawn(" in g
    assert "_isFallbackScene86 && MapName == \"Manor\"" in g
    assert "VerifiedFallbackEscortStarts91++" in g
    assert "AdjustedFallbackEscortStarts91++" in g
    assert "RejectedFallbackEscortStarts91++" in g
    assert "Position = position" in g
    assert "UseFallbackEscortNavigation89 =" in g

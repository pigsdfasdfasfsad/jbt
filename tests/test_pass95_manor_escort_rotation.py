"""Pass95 rotates only approximate upper Manor Escort markers."""
from conftest import ROOT


def src(p):
    return (ROOT/p).read_text(encoding="utf8")


def test_reconstructed_manor_escort_rotates_three_physical_upper_decks():
    rotation=src("src/Twr.Godot/Scripts/Pass95ManorEscortRotation.cs")
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "public const int UpperWingCount = 3;" in rotation
    assert "return (wave / 5 + 1) % UpperWingCount;" in rotation
    assert "if (wave < 3 || wave > 15)" in rotation
    assert "if(family == \"Escort\" && _isFallbackScene86 &&" in game
    assert 'MapName == "Manor" && position.Y >= 2.3f' in game
    assert 'Pass95ManorEscortRotation.UpperWingForWave(wave)' in game
    assert '_mapLayout.ApproximateUpperObjectives82[wing]' in game
    assert 'UseManorElevatedEscort94 = manorUpperEscort94' in game
    assert "Pass94ManorEscortRoute.TryGroundUpperSpawn(" in game
    assert "Pass91EscortSpawnSafety.TryFind(" in game


def test_catalogs_and_original_scenes_are_not_reauthored():
    rotation=src("src/Twr.Godot/Scripts/Pass95ManorEscortRotation.cs")
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "No owner-original objective marker" in rotation
    assert '_isFallbackScene86 &&' in game
    assert 'MapName == "Manor"' in game
    assert 'var groundEscort91 =' in game
    assert 'var expresswayEscort92 =' in game
    assert 'EscortGroundNavigator89 = _groundNavigation73' in game

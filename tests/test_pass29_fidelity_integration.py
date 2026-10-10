
"""Pass 29 optional Lab sidecars: all C# contracts must exist with fallbacks."""
from conftest import ROOT

def src(path):
    return (ROOT / path).read_text(encoding="utf-8")

def test_lab_navigation_wired_and_fail_closed():
    g=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    a=src("src/Twr.Godot/Scripts/InfectedAgent.cs")
    n=src("src/Twr.Godot/Scripts/Pass25SourceNavigationRuntime.cs")
    assert "Pass25SourceNavigationRuntime.TryBuild(this, MapName)" in g
    assert "SourceNavigator = _sourceNavigator" in g
    assert "if (highwayReady || sourceReady)" in a
    assert "fallback=local_steering" in n
    assert "SequenceEqual(sceneDigest)" in n

def test_lab_collision_cache_reuses_source_legacy_fallback():
    source=src("src/Twr.Godot/Scripts/LaboratorySourceLoader.cs")
    cache=src("src/Twr.Godot/Scripts/Pass26CollisionRuntime.cs")
    assert "if (!Pass26CollisionRuntime.TryBuild(stage, mapName, filePath))" in source
    assert "foreach (var (t, size, wedge) in collisions)" in source
    assert "SequenceEqual(sceneSha)" in cache

def test_source_primitives_and_reference_art_fallback():
    loader=src("src/Twr.Godot/Scripts/LaboratorySourceLoader.cs")
    stream=src("src/Twr.Godot/Scripts/Pass28PrimitiveStreamer.cs")
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    menu=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    hud=src("src/Twr.Godot/Scripts/WeaponDialHud.cs")
    assert "Pass28PrimitiveStreamer.TryBuild(stage, mapName, filePath)" in loader
    assert "Pass28PrimitiveStream" in game
    assert "CustomAabb" in stream
    assert "Pass27MapCardDecoration.Apply(button, map.Name)" in menu
    assert "Pass27SourceArtCatalog.WeaponIcon(name)" in hud
    assert "if (_pass27Icon is not null)" in hud

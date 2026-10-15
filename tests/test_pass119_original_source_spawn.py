"""Pass119 original-marker infected spawn safety without private source files."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_source_spawn_is_bounded_and_physics_derived_not_invented():
    s=read("src/Twr.Godot/Scripts/Pass119OriginalSourceSpawnSafety.cs")
    for required in (
        "MaximumSourceMarkers = 64",
        "MaximumSourceFootHeightError = .70f",
        "MinimumPlanarPlayerDistance = 2.5f",
        "originalFeet.Count > MaximumSourceMarkers",
        "selectedCentre - Vector3.Up * .8f",
        "if (first < 0) return null;",
        "(first + attempt) % originalFeet.Count",
        'hit["normal"]',
        'hit["position"]',
        "space.IntersectRay(ray)",
        "space.IntersectShape(shape, 1)",
        "marker.X, floor.Y + .90f, marker.Z",
        "alternateOriginalMarker = markerIndex != first",
    ):
        assert required in s
    assert "RandfRange" not in s and "Randomize" not in s


def test_production_spawns_use_original_only_physics_without_changing_fallback():
    code=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in (
        "VerifiedSourceSpawns119",
        "RejectedSourceSpawns119",
        "AlternateSourceMarkers119",
        "Pass119OriginalSourceSpawnSafety.TryFind(",
        "_mapLayout.UseExactInfectedSpawns",
        "spawn = supported.Value;",
        "if (alternateMarker) AlternateSourceMarkers119++;",
        "Pass85FallbackInfectedSpawn.TryFind(",
        "Pass40AdaptiveEntry.TryFind(",
        "sourceNeedsAssistedPlacement119 = true;",
        "if (sourceNeedsAssistedPlacement119)",
    ):
        assert token in code


def test_native_test_uses_synthetic_physics_not_user_source_geometry():
    s=read("src/Twr.Godot/Scripts/Pass119OriginalSourceSpawnAcceptance.cs")
    for token in (
        "new StaticBody3D",
        "new CollisionShape3D",
        "new CharacterBody3D",
        "Pass119OriginalSourceSpawnSafety.TryFind(",
        "TWR_PASS119_SOURCE_SPAWN_PHYSICS_OK",
        "TWR_SMOKE_PASS119_SOURCE_SPAWN_OK",
        "original_private_geometry_unverified=true",
    ):
        assert token in s
    assert "TestPlace.zip" not in s
    assert "Laboratory.scene.jsonl.gz" not in s


def test_windows_gate_and_isolated_test_profile():
    ci=read(".github/workflows/windows-build.yml")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    assert "twr-pass119-source-spawn-physics-safety" in ci
    assert "Native Pass 119 original-source infected spawn safety" in ci
    assert "TWR_PASS119_SOURCE_SPAWN_PHYSICS_OK" in ci
    assert "TWR_SMOKE_PASS119_SOURCE_SPAWN_OK" in ci
    assert "Preflight Pass 117 authenticated private Laboratory installer refusal" in ci
    assert "Smoke Pass 92 real Expressway Escort" in ci
    assert 'args.Contains("--smoke-pass119",StringComparer.Ordinal)' in boot
    assert "new Pass119OriginalSourceSpawnAcceptance" in boot
    assert 'smoke119 ? "user://pass119-source-spawn-smoke-profile.json"' in session
    assert "else if(smoke119)" in session
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count("if: false && success()")

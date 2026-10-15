"""Pass121 source-authored player spawn safety in the existing executable."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf8")


def test_source_marker_list_preserved_and_no_invented_coords():
    layout=read("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    loader=read("src/Twr.Godot/Scripts/LaboratorySourceLoader.cs")
    logic=read("src/Twr.Godot/Scripts/Pass121OriginalPlayerSpawnSafety.cs")
    assert "SourcePlayerFootMarkers121" in layout
    assert "SourcePlayerFootMarkers121 = players" in loader
    assert ".OrderBy(p => p.Name, StringComparer.Ordinal)" in loader
    assert "OriginalMarkerPlayerLift = .5f" in logic
    assert "selectedPlayerOrigin -" in logic
    assert "if (first < 0) return null;" in logic
    assert "(first + attempt) % originalFootMarkers.Count" in logic
    assert "MaximumOriginalPlayerMarkers = 64" in logic
    assert "RandfRange" not in logic


def test_real_floor_support_and_production_capsule_exclude_player_rid():
    logic=read("src/Twr.Godot/Scripts/Pass121OriginalPlayerSpawnSafety.cs")
    for required in (
        "MinimumWalkableNormalY = .74f",
        "MaximumSourceFloorOffset = .95f",
        "FootClearance = .08f",
        "new CapsuleShape3D",
        "Radius = .29f, Height = 1.80f",
        "SupportOffsets",
        "worldSpace.IntersectRay(centerRay)",
        "worldSpace.IntersectRay(ray)",
        "worldSpace.IntersectShape(standing, 1)",
        "Exclude = excluded",
        "CollideWithBodies = true",
        "originalFeet.X, floor.Y + FootClearance, originalFeet.Z",
    ):
        assert required in logic


def test_source_world_freezes_player_until_physics_clearance_passes():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for required in (
        "_sourcePlayerPlacementPending121",
        "Pass121OriginalPlayerSpawnSafety.TryFind(",
        "originalWorldAvailable",
        "_player.SetPhysicsProcess(false);",
        "_player.SetPhysicsProcess(true);",
        "VerifiedOriginalPlayerSpawn121",
        "RejectedOriginalPlayerSpawns121",
        "AlternativeOriginalPlayerSpawn121",
        "BlockedOriginalPlayerSpawn121",
        "Finish(false, \"ORIGINAL PLAYER SPAWN HAS NO RECOVERED FLOOR\")",
        "Runtime?.Match",
        "if (_isFallbackScene86) SpawnNaturalPickups();",
        "Pass120OriginalLootPlacement.TryGround(",
        "Pass119OriginalSourceSpawnSafety.TryFind(",
    ):
        assert required in game


def test_native_windows_physics_fixture_and_private_data_separation():
    native=read("src/Twr.Godot/Scripts/Pass121OriginalPlayerSpawnAcceptance.cs")
    assert "new StaticBody3D" in native
    assert "new CharacterBody3D" in native
    assert "new CollisionShape3D" in native
    assert "Pass121OriginalPlayerSpawnSafety.TryFind(" in native
    assert "TWR_PASS121_SOURCE_PLAYER_PHYSICS_OK" in native
    assert "TWR_SMOKE_PASS121_SOURCE_PLAYER_OK" in native
    assert "source_private_map_unverified=true" in native
    assert "TestPlace.zip" not in native
    assert "Laboratory.scene.jsonl.gz" not in native


def test_f10_f11_evidence_and_inherited_windows_native_gates():
    hud=read("src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs")
    bootstrap=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    for k in (
        "Game?.VerifiedOriginalPlayerSpawn121",
        "Game?.RejectedOriginalPlayerSpawns121",
        "Game?.AlternativeOriginalPlayerSpawn121",
        "Game?.BlockedOriginalPlayerSpawn121",
        "pass121_source_player_verified",
        "pass121_source_player_rejected_candidates",
        "pass121_source_player_alternate",
        "pass121_source_player_blocked",
    ):
        assert k in hud
    assert 'args.Contains("--smoke-pass121",StringComparer.Ordinal)' in bootstrap
    assert "new Pass121OriginalPlayerSpawnAcceptance" in bootstrap
    assert 'smoke121 ? "user://pass121-source-player-smoke-profile.json"' in session
    assert "Native Pass 119 original-source infected spawn safety" in ci
    assert "Native Pass 120 original item and fortification grounding" in ci
    assert "Native Pass 121 original player collision-safe spawn" in ci
    assert "TWR_PASS121_SOURCE_PLAYER_PHYSICS_OK" in ci
    assert "TWR_SMOKE_PASS121_SOURCE_PLAYER_OK" in ci
    assert "Preflight Pass 117 authenticated private Laboratory installer refusal" in ci
    assert "twr-pass121-original-player-spawn-clearance" in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count("if: false && success()")

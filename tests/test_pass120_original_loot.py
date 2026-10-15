"""Pass120 native and static regression for source-authored natural loot."""
from conftest import ROOT


def src(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_original_loot_uses_exact_xz_and_real_physics():
    policy=src("src/Twr.Godot/Scripts/Pass120OriginalLootPlacement.cs")
    for token in (
        "MaximumOriginalFloorGap = 1.35f",
        "MinimumWalkableNormalY = .70f",
        "PickupCentreAboveFloor = .62f",
        "originalMarker.X, floor.Y + PickupCentreAboveFloor",
        "originalMarker.Z",
        'hit["normal"]',
        'hit["position"]',
        "space.IntersectRay(floorRay)",
        "space.IntersectShape(freeSpace, 1)",
        "CollideWithBodies = true",
        "Size = new Vector3(.58f, .55f, .58f)",
    ):
        assert token in policy
    assert "RandfRange" not in policy
    assert "GlobalPosition =" not in policy


def test_game_source_loot_is_separated_from_fallback_and_original_marker_saved():
    game=src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in (
        "_pendingOriginalLoot120",
        "if (_isFallbackScene86) SpawnNaturalPickups();",
        "public override void _PhysicsProcess(double delta)",
        "Pass120OriginalLootPlacement.TryGround(",
        "VerifiedOriginalNaturalPickups120",
        "RejectedOriginalNaturalPickups120",
        "OriginalLootRespawns120",
        "authoredNaturalMarker",
        "naturalMarker ?? position",
        "Pass86NaturalLootPlacement.TryGround(",
        "_mapLayout.PickupPoints.Count",
        "_mapLayout.FortificationPoints.Count",
        "++placedOriginalItems >= 4",
        "++placedOriginalForts >= 2",
    ):
        assert token in game


def test_production_pickup_native_fixture_and_no_private_source_bytes():
    test=src("src/Twr.Godot/Scripts/Pass120OriginalLootAcceptance.cs")
    for token in (
        "new StaticBody3D",
        "new CollisionShape3D",
        "new PickupActor",
        "Pass120OriginalLootPlacement.TryGround(",
        "Pass120TemporaryLootBlock",
        "TWR_PASS120_ORIGINAL_LOOT_PHYSICS_OK",
        "TWR_SMOKE_PASS120_ORIGINAL_LOOT_OK",
        "private_original_map_unverified=true",
    ):
        assert token in test
    assert "TestPlace.zip" not in test
    assert "Laboratory.scene.jsonl.gz" not in test


def test_pass120_hud_and_offline_f11_snapshot_track_map_deficits():
    hud=src("src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs")
    for key in (
        "Game?.VerifiedSourceSpawns119",
        "Game?.RejectedSourceSpawns119",
        "Game?.AlternateSourceMarkers119",
        "Game?.VerifiedOriginalNaturalPickups120",
        "Game?.RejectedOriginalNaturalPickups120",
        "Game?.OriginalLootRespawns120",
        "pass119_supported_original_spawns",
        "pass119_rejected_original_spawns",
        "pass120_supported_original_pickups",
        "pass120_rejected_original_pickups",
        "pass120_original_loot_respawns",
    ):
        assert key in hud


def test_windows_build_runs_real_native_fixture_with_isolated_user_profile():
    ci=src(".github/workflows/windows-build.yml")
    bootstrap=src("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=src("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    for token in (
        "twr-pass120-original-loot-grounding",
        "Native Pass 119 original-source infected spawn safety",
        "Native Pass 120 original item and fortification grounding",
        "TWR_SMOKE_PASS120_ORIGINAL_LOOT_OK",
        "TWR_PASS120_ORIGINAL_LOOT_PHYSICS_OK",
        "Preflight Pass 117 authenticated private Laboratory installer refusal",
    ):
        assert token in ci
    assert 'args.Contains("--smoke-pass120",StringComparer.Ordinal)' in bootstrap
    assert "new Pass120OriginalLootAcceptance" in bootstrap
    assert 'smoke120 ? "user://pass120-original-loot-smoke-profile.json"' in session
    assert "else if(smoke120)" in session
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count("if: false && success()")

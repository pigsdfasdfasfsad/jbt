"""Pass123 source-neutral objective routes and physics-frame scheduling."""
from conftest import ROOT


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_source_objectives_are_planned_on_physics_then_instantiated_on_idle():
    code=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "private bool _sourceObjectivePlanPending123;" in code
    assert "private bool _sourceObjectivePlanReady123;" in code
    assert "_sourceObjectivePlanPending123 = true;" in code
    assert "if (_sourceObjectivePlanPending123)" in code
    assert "Pass123ProvisionalObjectiveRouting.TryFind(" in code
    assert "if (_sourceObjectivePlanReady123)" in code
    assert "SpawnWaveObjectives();" in code
    # Regression: the source-only physics probe must NOT be reached by a
    # direct StartWave -> SpawnWaveObjectives path on the render frame.
    start=code.split("private void StartWave()",1)[1].split(
        "private void EndWave()",1)[0]
    assert "if (_mapLayout.ProvisionalSourceObjectives122)" in start
    assert "else SpawnWaveObjectives();" in start
    assert "if (_sourceObjectivePlanPending123)" not in start
    assert "Game?.Runtime" not in start


def test_source_planning_does_not_claim_original_roblox_navmesh():
    routes=read("src/Twr.Godot/Scripts/Pass123ProvisionalObjectiveRouting.cs")
    assert "Pass122ProvisionalObjectiveSafety.TryFind(" in routes
    assert routes.count("Pass89EscortSurfaceSafety.CanTravel(")==2
    assert "MinimumObjectiveSeparation = 3.0f" in routes
    assert "MaximumProvisionalAnchors = 16" in routes
    assert "new[] { marker }" in routes
    assert "duplicateCandidates++" in routes
    assert "choseAlternate = index != preferredIndex" in routes
    assert "RandfRange" not in routes


def test_production_preserves_source_and_fallback_lifecycle():
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    for token in (
        "Pass119OriginalSourceSpawnSafety.TryFind(",
        "Pass120OriginalLootPlacement.TryGround(",
        "Pass121OriginalPlayerSpawnSafety.TryFind(",
        "if (_isFallbackScene86) SpawnNaturalPickups();",
        "Pass87FallbackObjectiveRotation.GetIndex(",
        "Pass123ProvisionalObjectiveRouting.TryFind(",
        "Pass123ReachableProvisionalObjectives",
        "Pass123BlockedProvisionalObjectives",
        "Pass123DuplicateProvisionalCandidates",
    ):
        assert token in game
    assert "originalWorldAvailable" in game


def test_native_fixture_rechecks_dynamic_corridor_after_wall_removal():
    code=read("src/Twr.Godot/Scripts/Pass123ProvisionalObjectiveAcceptance.cs")
    for token in (
        "new CharacterBody3D",
        "new CollisionShape3D",
        "Pass123ProvisionalObjectiveRouting.TryFind(",
        "new[]{anchors[2]}",
        "_temporaryWall.QueueFree();",
        "TWR_PASS123_PROVISIONAL_CORRIDOR_PHYSICS_OK",
        "TWR_SMOKE_PASS123_PROVISIONAL_OBJECTIVES_OK",
        "owner_source_scene_unverified=true",
    ):
        assert token in code
    assert "Laboratory.scene.jsonl.gz" not in code


def test_native_windows_gate_and_source_neutral_diagnostics():
    ci=read(".github/workflows/windows-build.yml")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    hud=read("src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs")
    assert "twr-pass123-physical-objective-routes" in ci
    assert "Native Pass 123 physical provisional objective routes" in ci
    assert "TWR_PASS123_PROVISIONAL_CORRIDOR_PHYSICS_OK" in ci
    assert "TWR_SMOKE_PASS123_PROVISIONAL_OBJECTIVES_OK" in ci
    assert "Native Pass 121 original player collision-safe spawn" in ci
    assert "Smoke Pass 92 real Expressway Escort" in ci
    assert 'args.Contains("--smoke-pass123",StringComparer.Ordinal)' in boot
    assert "new Pass123ProvisionalObjectiveAcceptance" in boot
    assert 'smoke123 ? "user://pass123-provisional-routes-smoke-profile.json"' in session
    for item in (
        "pass123_reachable_provisional_objectives",
        "pass123_blocked_provisional_objectives",
        "pass123_duplicate_provisional_candidates",
    ):
        assert item in hud
    assert ci.count("uses: actions/upload-artifact@v4") == \
        ci.count("if: false && success()")

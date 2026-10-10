"""Source contracts for the Pass32 optional collision-grounded accessibility layer.

These run with no owner-held Roblox map pack; actual runtime/physics and AI
movement are exercised separately by exported Windows --smoke-pass32.
"""
from conftest import ROOT

def read(path):
    return (ROOT / path).read_text(encoding="utf8")

def test_default_original_zombie_spawns_preserved():
    game = read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "public bool AssistedInfectedSpawnsEnabled { get; private set; }" in game
    assert 'key.Keycode == Key.F9' in game
    assert "if (AssistedInfectedSpawnsEnabled && _sourceNavigator?.IsBridgePackActive == true)" in game
    assert "if (_mapLayout.UseExactInfectedSpawns) return basePoint + Vector3.Up * 0.8f;" in game

def test_only_physically_supported_candidates_can_spawn():
    safety = read("src/Twr.Godot/Scripts/Pass32SpawnSafety.cs")
    game = read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    navigator = read("src/Twr.Godot/Scripts/Pass25SourceNavigationRuntime.cs")
    assert "GetAssistedInfectedSpawnCandidates" in navigator
    # Pass40 centralizes candidate validation in a shared policy, still used
    # by GameplayRoot for every opt-in entry and recovery.
    policy = read("src/Twr.Godot/Scripts/Pass40AdaptiveEntry.cs")
    assert "Pass32SpawnSafety.FindSupportedPlacement" in policy
    assert "Pass40AdaptiveEntry.TryFind" in game
    assert "space.IntersectRay(ray)" in safety
    assert "space.IntersectShape(shapeQuery, 1)" in safety
    assert "floorNormal.Y < MinWalkableNormalY" in safety
    assert "Math.Abs(floorPosition.Y - feet) > MaximumFloorOffset" in safety
    assert "MinimumPlayerDistance = 24f" in safety

def test_optional_stuck_recovery_bounded_and_telemetry():
    agent = read("src/Twr.Godot/Scripts/InfectedAgent.cs")
    safety = read("src/Twr.Godot/Scripts/Pass32SpawnSafety.cs")
    game = read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    diagnostic = read("src/Twr.Godot/Scripts/Pass30DiagnosticsHud.cs")
    assert "MaximumRescuesPerEnemy = 2" in safety
    assert "MinimumSecondsBetweenRescues = 18.0" in safety
    assert "NeedsAssistedRecovery" in agent
    assert "ApplyAssistedRecovery" in agent
    assert "if (!AssistedInfectedSpawnsEnabled" in game
    assert "AssistedInfectedRecoveryCount++" in game
    assert "assisted_physics_checked_recoveries" in diagnostic

def test_exported_windows_checks_ground_void_and_enemy_path():
    smoke = read("src/Twr.Godot/Scripts/Bootstrap.cs")
    ci = read(".github/workflows/windows-build.yml")
    fixture = read("tools/validation/make_pass30_synthetic_sidecars.py")
    assert "--smoke-pass32" in smoke and "--smoke-pass32" in ci
    assert "TWR_SMOKE_PASS32_GROUNDED_OK" in smoke
    assert "Pass32SpawnSafety.FindSupportedPlacement" in smoke
    assert "Pass32RescuePolicy.CanRescue" in smoke
    assert "moved < .20f" in smoke
    assert "(-4., 1.4, -4.)" in fixture
    assert "(36., 1.4, -4.)" in fixture
    assert "make_pass30_synthetic_sidecars.py --output $outputDir --clean" in ci

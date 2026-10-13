"""Pass115: screenshot-informed Laboratory interior props, with source boundaries.

The uploaded screenshots of ReceptionDesk/ServerRoom/ResearchRoom/
ExaminationRoom/ParkingGarage/Cafeteria motivated approximate geometry.
The private screenshots and original map mesh binaries are NOT in Git.
"""
from conftest import ROOT


def source(path):
    return (ROOT / path).read_text(encoding="utf-8")


def test_lab_fallback_has_distinct_reference_guided_render_landmarks():
    lab = source("src/Twr.Godot/Scripts/Pass115LaboratoryDressing.cs")
    for label in (
        "ReceptionDressing", "ServerDressing", "ResearchDressing",
        "ExaminationDressing", "GarageDressing", "WardDressing",
        "CafeteriaDressing",
    ):
        assert f'Landmark("{label}")' in lab
    for named_component in (
        "ReceptionCounterFace", "AlythReferenceSign",
        "BlueDoorScannerTower", "ServerOverheadRail",
        "Rack", "ResearchOverhead", "LabBench",
        "ExamGurney", "ExamMedicineCabinet",
        "ParkedCar", "ConcretePillarVisual",
        "EmergencyCot", "WardCurtain",
        "CafeteriaTable", "IndoorPlanter",
    ):
        assert named_component in lab, named_component
    assert 'Text="ALYTH"' in lab
    assert 'new OmniLight3D' in lab
    assert 'new CylinderMesh' in lab
    assert 'new BoxMesh' in lab
    assert "ExpectedLandmarks = 7" in lab
    assert "MaxVisualMeshes = 260" in lab
    assert "LocalLightCount++" in lab
    assert "VisualMeshCount++" in lab
    assert 'SetMeta("Pass115OwnerOriginalGeometry",false)' in lab
    assert 'SetMeta("Pass115SourceReferencesBundled",false)' in lab


def test_new_props_cannot_change_map_collision_navigation_or_player_spawns():
    lab = source("src/Twr.Godot/Scripts/Pass115LaboratoryDressing.cs")
    builder = source("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    gameplay = source("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "Pass115LaboratoryDressing.AddToFallback(root);" in builder
    before = builder.split("private static RuntimeMapLayout BuildLaboratory(")[1]
    lab_only = before.split("private static RuntimeMapLayout BuildManor(")[0]
    assert lab_only.count("Pass115LaboratoryDressing.AddToFallback(root)") == 1
    assert 'Layout(new Vector3(0, 1, 39)' in lab_only
    assert 'InteriorShell(root, "Research",' in lab_only
    assert 'InteriorShell(root, "ParkingGarage",' in lab_only
    assert "LaboratorySourceLoader.TryBuild(this, MapName, out var recovered)" in gameplay
    assert "MapBlockoutBuilder.Build(this, _mapDefinition)" in gameplay
    assert "originalWorldAvailable" in gameplay
    for forbidden in (
        "new StaticBody3D", "new CollisionShape3D", "new RigidBody3D",
        "new CharacterBody3D", "new NavigationAgent3D",
        "MoveAndSlide", "ChangeSceneTo", "AwardKill(", "CompleteObjective(",
        "HttpClient", "Texture2D.Load", "Image.LoadFromFile",
        'GetFileAsString("res://Content/',
    ):
        assert forbidden not in lab, forbidden
    assert "ContainsCollision(this)" in lab


def test_all_ten_real_game_maps_enforce_lab_only_dressing_and_safe_wave_cycle():
    boot = source("src/Twr.Godot/Scripts/Bootstrap.cs")
    smoke = source("src/Twr.Godot/Scripts/Pass72NativeTenMapEntryAcceptance.cs")
    assert 'args.Contains("--smoke-pass115",StringComparer.Ordinal)' in boot
    assert "VerifyLabDressing115=true" in boot
    assert "VerifyLabDressing115" in smoke
    assert 'if(map=="Laboratory")' in smoke
    assert 'game.GetNodeOrNull<Pass115LaboratoryDressing>(' in smoke
    assert "visual.LandmarkCount!=Pass115LaboratoryDressing.ExpectedLandmarks" in smoke
    assert "visual.HasAnyCollisionObjects" in smoke
    assert "visual.VisualMeshesByLandmark.TryGetValue(" in smoke
    assert "visual.LocalLightCount<8" in smoke
    assert "other_maps_undressed=9" in smoke
    assert "TWR_SMOKE_PASS115_LAB_DRESSING_OK" in smoke
    assert "TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK" in smoke
    assert "foreach (var map in ReleaseRules.Maps)" in smoke
    assert "player.IsOnFloor()" in smoke
    assert 'Runtime.FailMatch("Pass72 test-only map scene teardown")' in smoke
    assert "Runtime.ReturnToLobby();" in smoke


def test_windows_native_visual_gate_preserves_existing_private_pack_and_gameplay_checks():
    ci = source(".github/workflows/windows-build.yml")
    assert "twr-pass115-laboratory-reference-dressing" in ci
    assert "Preflight Pass 115 seven Laboratory visual landmark zones" in ci
    assert "'--smoke-pass115'" in ci
    assert "TWR_SMOKE_PASS115_LAB_DRESSING_OK" in ci
    assert "TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK" in ci
    assert "$counts.Groups[1].Value -lt 140" in ci
    assert "$counts.Groups[1].Value -gt 260" in ci
    for gate in (
        "Preflight Pass 114 real Glock death on sloped terrain",
        "Preflight Pass 113 real corpse ground settling",
        "Preflight Pass 112 real Glock articulated corpse limb motion",
        "Preflight Pass 111 native Glock kill and corpse lifecycle",
        "Preflight Pass 110 real two-map repair truck assembly",
        "Preflight Pass 109 reference generator and Repair badge",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 105 native objective pickup and deposit",
        "Preflight Pass 104 full production Wave2 Repair",
        "Preflight Pass 103 real full production Load",
        "Verify no private map scenes entered the CI export",
    ):
        assert gate in ci
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )

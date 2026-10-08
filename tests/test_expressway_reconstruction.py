"""Validate visible Expressway reconstruction in source and exported runtime."""
from conftest import ROOT

ROOT_CS = ROOT / "src/Twr.Godot/Scripts"

def test_expressway_uses_reconstructed_scene_not_grey_box_blockout():
    source = (ROOT_CS/"MapBlockoutBuilder.cs").read_text()
    start = source.index("private static RuntimeMapLayout BuildExpressway(")
    end = source.index("private static RuntimeMapLayout BuildPrison(",start)
    method = source[start:end]
    assert "return ExpresswaySceneBuilder.Build(root);" in method
    assert "BaseArena(root" not in method
    assert "DecorativeBox(root" not in method

def test_expressway_has_all_major_original_reference_landmarks():
    source = (ROOT_CS/"ExpresswaySceneBuilder.cs").read_text()
    for expected in [
        "ElevatedBridge","StructuralBridgeDeck","IndianapolisBackdrop",
        "AbandonedTraffic","StrandedSemiTruck","MedicalScreeningCheckpoint",
        "AbandonedMilitaryHumvee","WestApproachRamp","EastApproachRamp",
        "SandbagCollider","CheckpointFence","OrangeTrafficCone",
        "HighwayStreetlamps","MedicalTable","PalletWithSupplies",
        "AbandonedMedicalHelicopter","AlythAbandonedPatrolSUV",
        "StopOctagon","DashedLaneStripe","BridgeSupportPillar"
    ]:
        assert expected in source
    assert "original decorated" in source.lower()
    assert "StaticBody3D" in source
    assert "FogDensity" in source
    assert "ProceduralSkyMaterial" in source

def test_windows_export_instantiates_real_expressway_scene_layers():
    bootstrap=(ROOT_CS/"Bootstrap.cs").read_text()
    build=(ROOT/"build/Windows/build.ps1").read_text()
    assert "--smoke-expressway-scene" in bootstrap
    assert "--smoke-expressway-scene" in build
    assert "TWR_SMOKE_EXPRESSWAY_SCENE_OK" in bootstrap
    assert "TWR_SMOKE_EXPRESSWAY_SCENE_OK" in build
    assert "layout.InfectedSpawns.Count != 4" in bootstrap

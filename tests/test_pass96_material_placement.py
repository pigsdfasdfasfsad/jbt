"""Pass96 source-neutral Load/Repair layout cannot invent reachable positions."""
from conftest import ROOT


def src(name):
    return (ROOT / name).read_text(encoding="utf8")


def test_physics_grounded_materials_are_opt_in_on_approximate_scenes_only():
    game = src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "UseFallbackMaterialGrounding96 = _isFallbackScene86 &&" in game
    assert '(family == "Load" || family == "Repair")' in game
    assert "if(UseFallbackMaterialGrounding96 && Player is not null)" in objective
    assert "Pass96GroundedMaterials.TryPlan(" in objective
    assert "Pass96GroundedMaterialLayout=layout.IsComplete(names.Length)" in objective
    assert "GlobalPosition=layout.DepositFeet!.Value;" in objective
    assert "Pass96MaterialFeet => _materialFeet96" in objective
    assert "Pass96AuthoredMaterialAnchor=GlobalPosition;" in objective
    old_wave=src("src/Twr.Godot/Scripts/Pass88TenMapPhysicalWaveAcceptance.cs")
    assert "objective.Pass96AuthoredMaterialAnchor" in old_wave
    assert "Pass87FallbackObjectiveRotation.GetIndex(" in old_wave
    assert "expected.DistanceTo(scheduled)>.02f" in old_wave
    assert "objective.Pass96GroundedMaterialLayout" in old_wave
    assert "Pass76InteractionSafety.CanReach(Player, nearest.GlobalPosition, 2.5f)" in objective
    assert "Pass76InteractionSafety.CanReach(Player, GlobalPosition, 3f)" in objective


def test_every_supported_material_is_same_floor_swept_capsule_accessible():
    code = src("src/Twr.Godot/Scripts/Pass96GroundedMaterials.cs")
    for token in (
        "Pass90EscortArrivalSafety.TryGroundDestination(",
        "Pass89EscortSurfaceSafety.CanTravel(",
        "MaximumSameFloorDifference=.35f",
        "MinimumItemSeparation=0.95f",
        "OverlapFallbacks",
        "RejectedCandidates",
        "var fallback=points.Count>0",
        "source-owner-authored scene",
        "Rid playerRid,int itemCount",
    ):
        assert token in code
    assert "CollisionMask=0" not in code
    assert "GetVerifiedEscortRoute89" not in code
    assert "actor.GlobalPosition=" not in code


def test_owner_source_placements_remain_legacy_when_not_opted_in():
    objective = src("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "if(supportedFeet is not null)" in objective
    assert "else" in objective
    assert "var radius=6f+(i%3)*1.25f;" in objective
    assert "Pass96GroundedMaterialLayout" in objective

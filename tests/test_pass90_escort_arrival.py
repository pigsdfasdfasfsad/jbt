"""Pass90: fallback Escort destination feet and authoritative arrival."""
from conftest import ROOT

def source(path):
    return (ROOT / path).read_text(encoding="utf-8")

def test_escort_goal_matches_world_floor_and_capsule_clearance():
    helper=source("src/Twr.Godot/Scripts/Pass90EscortArrivalSafety.cs")
    for token in (
        "MaximumMarkerFloorGap=1.40f",
        "MinimumUpwardNormal=.74f",
        "MaximumArrivalHeightGap=.48f",
        "MaximumArrivalPlanarGap=1.50f",
        "FootprintSamples",
        "space.IntersectRay(ray)",
        "space.IntersectShape(query,1)",
        "new CapsuleShape3D",
        "Math.Abs(floor.Y-firstFloor.Value.Y)>.28f",
        "isOnFloor",
        "playerNearby",
        "Pass89EscortSurfaceSafety.CanTravel(",
    ):
        assert token in helper
    assert "GlobalPosition =" not in helper
    assert "MoveAndSlide(" not in helper

def test_only_existing_pass89_fallback_escort_uses_new_arrival_gate():
    obj=source("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    game=source("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "if (UseFallbackEscortNavigation89)" in obj
    assert "if (!actor.IsOnFloor())" in obj
    assert "Pass90EscortArrivalSafety.TryGroundDestination(" in obj
    assert "var destinationFeet=grounded.Value" in obj
    assert "_escortDestination.GlobalPosition=destinationFeet" in obj
    assert "EscortDestinationAdjusted89 = option!=target" in obj
    assert "Pass90EscortArrivalSafety.CanComplete(" in obj
    assert "EscortGroundedCompletion90=true" in obj
    assert "EscortGroundedDestinations90++" in obj
    assert "EscortRejectedDestinations90++" in obj
    assert "_groundNavigation73 is not null" in game
    assert 'ObjectivePosition(i).Y < 2.3f || manorUpperEscort94' in game
    assert "Runtime?.CompleteObjective(ObjectiveId, Family);" in obj

def test_reward_remains_idempotent_domain_authority():
    policy=source("src/Twr.Domain/Policies/ObjectiveRewardPolicy.cs")
    service=source("src/Twr.Domain/Services/ObjectiveService.cs")
    assert '"Load" or "Repair" or "Radio" or "Unpack" or "Escort" => (4000, 3500)' in policy
    assert "if (!match.CompletedObjectives.Add(id))" in service
    assert "player.Credits += credits" in service
    assert "player.Xp += xp" in service

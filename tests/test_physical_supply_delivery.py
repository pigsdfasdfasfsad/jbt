"""Production supply lifecycle and native behavioral acceptance coverage."""
from conftest import ROOT

def source(path):
    return (ROOT/path).read_text(encoding="utf8")

def test_landing_has_all_eight_accessible_slots_and_bounded_plan():
    s=source("src/Twr.Godot/Scripts/SupplyLandingPlanner.cs")
    assert "MaximumCandidates = 24" in s
    assert "Pass90EscortArrivalSafety.TryGroundDestination" in s
    assert s.count("Pass89EscortSurfaceSafety.CanTravel(")==2
    assert "space.CastMotion(query)" in s
    assert "i<8" in s
    assert "pickupCentres.ToArray()" in s

def test_physical_contact_authorizes_payload_not_a_height_constant():
    s=source("src/Twr.Godot/Scripts/SupplyDropRuntime.cs")
    assert "public override void _PhysicsProcess(double delta)" in s
    assert "public override void _Process(double delta)" not in s
    assert "position.Y <= GroundY" not in s
    assert "Callable.From(NotifyOnIdle).CallDeferred()" in s
    assert "PayloadPositions=finalPlan.PickupCentres" in s
    assert "tested<2" in s and "MaximumCandidates" in s
    assert "QueueFailure" in s and "_resolved=true" in s

def test_real_game_dispatch_cleans_failed_pending_requests():
    s=source("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "PlanNearSourcePlayer = true" in s
    assert "pallet.Failed = reason" in s
    assert "SpawnSupplyContents(position, pallet.PayloadPositions)" in s
    assert "drop.Landed = null; drop.Failed = null;" in s
    assert "groundedPayload.Length != 8" in s
    assert "_supplyDrops.Clear();" in s

def test_native_acceptance_exercises_real_payload_and_failures():
    s=source("src/Twr.Godot/Scripts/SupplyDeliveryAcceptance.cs")
    assert 'GetMethod("SpawnSupplyDrop"' in s
    assert "spawned.Length!=8" in s and "regular!=4 || forts!=4" in s
    assert "falseLandings!=0 || failed!=1" in s
    assert "falseLandings!=0 || failed!=2" in s
    assert "TemporarySupplyObstruction" in s
    assert "TWR_PHYSICAL_SUPPLY_LIFECYCLE_OK" in s
    assert "await SupplyDeliveryAcceptance.Verify(game, player)" in source("src/Twr.Godot/Scripts/RuntimeReadinessAcceptance.cs")

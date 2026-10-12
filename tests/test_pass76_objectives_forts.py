"""Pass76: physical interaction arbitration and objective/fortification native coverage."""
from conftest import ROOT


def read(path):
    return (ROOT/path).read_text(encoding="utf8")


def test_nearest_single_pickup_is_occluded_and_collects_once():
    actor=read("src/Twr.Godot/Scripts/PickupActor.cs")
    safe=read("src/Twr.Godot/Scripts/Pass76InteractionSafety.cs")
    assert 'AddToGroup("twr-pickup")' in actor
    assert "Pass76InteractionSafety.IsNearestPickup(this)" in actor
    assert "candidate.GetTree().GetNodesInGroup" in safe
    assert "other.GetInstanceId() < candidate.GetInstanceId()" in safe
    assert "candidateDistance" in safe
    assert "ClaimedPickupPresses.Contains(player.GetInstanceId())" in safe
    assert "ClaimPickupPress(Player)" in actor
    assert "ObservePickupKey(" in read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "ForgetPlayer(this)" in read("src/Twr.Godot/Scripts/FirstPersonPlayer.cs")
    assert "Pass36SourceFragmentsRuntime.SourceServerCollisionLayer" in safe
    assert "player.GetWorld3D().DirectSpaceState.IntersectRay(ray).Count == 0" in safe
    assert "Runtime?.Player?.IsAlive != true" in actor
    assert "Collected?.Invoke(this)" in actor


def test_objective_real_item_and_deposit_visibility_not_remote_grants():
    objective=read("src/Twr.Godot/Scripts/ObjectiveRuntime.cs")
    assert "Pass76InteractionSafety.CanReach(Player, GlobalPosition, 3f)" in objective
    assert "Pass76InteractionSafety.CanReach(Player, nearest.GlobalPosition, 2.5f)" in objective
    assert "_deposited++" in objective
    assert "nearest.QueueFree()" in objective
    assert "Runtime?.CompleteObjective(ObjectiveId, Family)" in objective
    assert "StatusChanged?.Invoke" in objective
    assert "Runtime?.Player?.IsAlive != true" in objective


def test_placement_keeps_item_until_real_floor_and_open_space():
    hammer=read("src/Twr.Godot/Scripts/FortificationController.cs")
    safety=read("src/Twr.Godot/Scripts/Pass76PlacementSafety.cs")
    assert "Pass76PlacementSafety.TryFindGround(Player," in hammer
    assert hammer.index("Pass76PlacementSafety.TryFindGround(Player,") < hammer.index(
        "Runtime.ConsumeItem(definition.Name)")
    assert "INVALID PLACEMENT" in hammer
    assert "if (Runtime?.Player?.IsAlive != true)" in hammer
    assert 'if (Runtime?.Player?.IsAlive != true) return;' in hammer
    assert "DeployedCount76" in hammer
    assert "PhysicsRayQueryParameters3D.Create" in safety
    assert "PhysicsShapeQueryParameters3D" in safety
    assert "world.IntersectShape(space, 1).Count != 0" in safety
    assert "((Vector3)hit[\"normal\"]).Y < .70f" in safety


def test_source_stud_bomb_radius_wall_occlusion_and_50cal_infected_layer():
    fort=read("src/Twr.Godot/Scripts/FortificationActor.cs")
    assert "RobloxUnits.Distance(Definition.Radius!.Value)" in fort
    assert fort.count("Pass75BlastOcclusion.CanReachTarget(") >= 4
    assert "query.CollisionMask =" in fort
    assert "1u | 2u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer" in fort
    assert "ShotsFired76++" in fort
    assert "if (_mounted) SetMounted(false);" in fort
    assert "Mounted76 => _mounted" in fort
    assert 'infected.ApplyDamage(15f, false, "BarbedWire")' in fort


def test_supply_drop_uses_collision_floor_once_not_global_y_only():
    pallet=read("src/Twr.Godot/Scripts/SupplyDropRuntime.cs")
    game=read("src/Twr.Godot/Scripts/GameplayRoot.cs")
    assert "var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray)" in pallet
    assert "((Vector3)hit[\"normal\"]).Y >= .70f" in pallet
    assert "SourcePlayer.GetRid()" in pallet
    assert "position.Y = ((Vector3)hit[\"position\"]).Y + .23f;" in pallet
    assert "if (position.Y <= GroundY)" in pallet
    assert "Landed?.Invoke(position)" in pallet
    assert "SourcePlayer = _player" in game
    assert "SpawnSupplyContents(position)" in game
    assert "SpawnUnpackContents(completionPosition)" in game


def test_real_windows_map_actor_harness_is_isolated_and_comprehensive():
    smoke=read("src/Twr.Godot/Scripts/Pass76ObjectiveFortificationAcceptance.cs")
    boot=read("src/Twr.Godot/Scripts/Bootstrap.cs")
    session=read("src/Twr.Godot/Scripts/LocalSessionNode.cs")
    ci=read(".github/workflows/windows-build.yml")
    assert 'await ValidateRanchInteractionAndClap()' in smoke
    assert 'await ValidateManorTurretAndSupply()' in smoke
    assert 'Begin("Ranch")' in smoke and 'Begin("Manor")' in smoke
    assert "new GameplayRoot" in smoke
    assert "Input.ParseInputEvent(new InputEventKey" in smoke
    assert "Input.ParseInputEvent(new InputEventMouseButton" in smoke
    assert "new ObjectiveRuntime" in smoke
    assert "new PickupActor" in smoke
    assert "new FortificationActor" in smoke
    assert "new DamageObjectiveTarget" in smoke
    assert "new SupplyDropRuntime" in smoke
    assert "Runtime.Player.Health =" not in smoke
    assert "Runtime.Player.Credits =" not in smoke
    assert "Runtime.Match.Wave =" not in smoke
    assert "Runtime.ReturnToLobby()" in smoke
    assert "new Pass76ObjectiveFortificationAcceptance" in boot
    assert '"--smoke-pass76"' in boot and '"--smoke-pass76"' in session
    assert '"user://pass76-objective-fort-smoke-profile.json"' in session
    assert "else if(smoke76)" in session
    assert "twr-pass76-interaction-objective-fortification" in ci
    assert "TWR_SMOKE_PASS76_OBJECTIVE_FORTIFICATION_OK" in ci
    assert ci.index("Smoke Pass 75") < ci.index("Smoke Pass 76")
    assert ci.index("Smoke Pass 76") < ci.index("Verify no private map scenes")
    assert "github.ref_name == 'twr-pass76-interaction-objective-fortification')" in ci

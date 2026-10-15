using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass120: test real Godot physics and pickup rendering in the exported
/// Windows build on SYNTHETIC fixtures only. Never commit owner's private
/// Laboratory map, mesh, screenshot, or proprietary source pack.
/// </summary>
public partial class Pass120OriginalLootAcceptance : Node3D
{
    public override async void _Ready()
    {
        try
        {
            await TestGroundedOriginalLoot();
            GD.Print("TWR_SMOKE_PASS120_ORIGINAL_LOOT_OK" +
                " source_xz_preserved=true floor_supported=true" +
                " embedded_rejected=true unsupported_rejected=true" +
                " displaced_floor_rejected=true dynamic_obstruction_rejected=true" +
                " genuine_pickup_actor=true private_original_map_unverified=true");
            GetTree().CallDeferred("quit", 0);
        }
        catch (Exception error)
        {
            GD.PushError("TWR_SMOKE_PASS120_ORIGINAL_LOOT_FAILED " +
                error.GetType().Name + " " + error.Message + "\n" +
                error.StackTrace);
            GetTree().CallDeferred("quit", 1);
        }
    }

    private StaticBody3D Box(string name, Vector3 position, Vector3 dimensions)
    {
        var body = new StaticBody3D
        {
            Name = name,
            Position = position,
            CollisionLayer = 1u,
            CollisionMask = 0u
        };
        AddChild(body);
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = dimensions }
        });
        return body;
    }

    private async System.Threading.Tasks.Task PhysicsFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async System.Threading.Tasks.Task TestGroundedOriginalLoot()
    {
        Box("Pass120SyntheticGround",
            new Vector3(0f, -.25f, 0f), new Vector3(8f, .5f, 8f));
        var wall = Box("Pass120BlockingWall",
            new Vector3(0f, .9f, 0f), new Vector3(1.4f, 1.8f, 1.4f));
        await PhysicsFrames(4);

        var unsupported = new Vector3(12, 0, 0);
        var embedded = Vector3.Zero;
        var supported = new Vector3(2.5f, 0f, 0f);
        var farAbove = new Vector3(2.5f, 3.0f, 0f);

        if (Pass120OriginalLootPlacement.TryGround(GetWorld3D(), unsupported).HasValue ||
            Pass120OriginalLootPlacement.TryGround(GetWorld3D(), embedded).HasValue ||
            Pass120OriginalLootPlacement.TryGround(GetWorld3D(), farAbove).HasValue)
            throw new InvalidOperationException(
                "Unsupported, embedded, or wrong-deck source pickup accepted");

        var centre = Pass120OriginalLootPlacement.TryGround(
            GetWorld3D(), supported);
        if (!centre.HasValue ||
            Math.Abs(centre.Value.X-supported.X) > .001f ||
            Math.Abs(centre.Value.Z-supported.Z) > .001f ||
            Math.Abs(centre.Value.Y-.62f) > .08f)
            throw new InvalidOperationException(
                "Supported original pickup moved or did not ground");

        // Dynamic world collision must block new pickups, not bury them.
        var dynamicBlock = Box("Pass120TemporaryLootBlock",
            new Vector3(2.5f, .9f, 0f), new Vector3(1.2f, 1.8f, 1.2f));
        await PhysicsFrames(3);
        if (Pass120OriginalLootPlacement.TryGround(
                GetWorld3D(), supported).HasValue)
            throw new InvalidOperationException(
                "Original pickup embedded despite dynamic world blocker");
        dynamicBlock.QueueFree();
        await PhysicsFrames(3);

        centre = Pass120OriginalLootPlacement.TryGround(
            GetWorld3D(), supported);
        if (!centre.HasValue)
            throw new InvalidOperationException(
                "Original pickup did not recover after obstruction removed");

        // Exercise the existing production PickupActor instead of a fake
        // test-only visual. No domain inventory grants or fake profile writes.
        var pickup = new PickupActor
        {
            Name = "Pass120ActualProductionPickup",
            PickupType = "Ammo",
            Position = centre.Value
        };
        AddChild(pickup);
        await PhysicsFrames(2);
        if (!pickup.IsInsideTree() ||
            pickup.GetChildCount() < 2 ||
            !pickup.IsInGroup("twr-pickup") ||
            pickup.GlobalPosition.DistanceTo(centre.Value) > .001f)
            throw new InvalidOperationException(
                "Genuine production pickup failed at source-derived centre");

        GD.Print("TWR_PASS120_ORIGINAL_LOOT_PHYSICS_OK" +
            " marker=" + supported + " pickup_centre=" + centre.Value +
            " no_owner_assets=true");
        pickup.QueueFree();
        wall.QueueFree();
        await PhysicsFrames(2);
    }
}

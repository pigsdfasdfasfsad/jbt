using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Exercises real pickup and fortification handlers against control gating.</summary>
public static class RuntimeInteractionAcceptance
{
    public static async Task Verify(GameplayRoot game, FirstPersonPlayer player,
        LocalSessionNode runtime)
    {
        var forts = game.GetNode<FortificationController>("Fortifications");
        using var hammer = new InputEventKey { Keycode = Key.F, Pressed = true };
        using var swing = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };
        var pickup = new PickupActor
        {
            PickupType = "Body Armor", Player = player, Runtime = runtime,
            Position = player.Position + Vector3.Right
        };
        game.AddChild(pickup);
        var collect = typeof(PickupActor).GetMethod("TryCollect",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Pickup handler missing");
        var armor = runtime.Player!.ArmorDurability;
        player.SetGameplayControlsEnabled(false);
        forts._UnhandledInput(hammer);
        if (forts.HammerMode76 || (bool)collect.Invoke(pickup, null)! ||
            runtime.Player.ArmorDurability != armor)
            throw new InvalidOperationException("Blocked controls still allowed pickup or hammer actions");

        player.SetGameplayControlsEnabled(true);
        if (!(bool)collect.Invoke(pickup, null)! || runtime.Player.ArmorDurability <= armor)
            throw new InvalidOperationException("Ready player could not collect actual armor pickup");
        pickup.QueueFree();
        if (!runtime.GrantItem("Barbed Wire", 1, 2))
            throw new InvalidOperationException("Could not establish fortification inventory fixture");
        forts._UnhandledInput(hammer);
        if (!forts.HammerMode76)
            throw new InvalidOperationException("Ready player's hammer toggle failed");
        for (var attempt = 0; attempt < 24 && forts.DeployedCount76 == 0; attempt++)
        {
            // Preserve the production per-swing cooldown and inventory path.
            // Only elapsed test time is advanced, as in existing native tests.
            forts._Process(.50);
            forts._UnhandledInput(swing);
            await game.ToSignal(game.GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
        var deployed = forts.GetChildren().OfType<FortificationActor>().SingleOrDefault();
        if (deployed is null || !deployed.IsInsideTree() ||
            deployed.GlobalPosition.DistanceTo(player.GlobalPosition) > 5f ||
            deployed.GlobalPosition.LengthSquared() < 1f)
            throw new InvalidOperationException("Fortification was not placed at its attached world-space location");
        if (deployed.DeployedVisual.WireSegments < 96)
            throw new InvalidOperationException("Actual hammer deployment lost its wire-coil model");
        forts._UnhandledInput(hammer);
        forts.ClearDeployed();
        player.SetGameplayControlsEnabled(false);
        GD.Print("TWR_RUNTIME_INTERACTION_OK" +
            " blocked_pickup=true blocked_hammer=true ready_armor_collected=true" +
            " actual_fortification_deployed=true world_position_after_attach=true");
    }
}

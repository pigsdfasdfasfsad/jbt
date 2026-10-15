using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Native production-scene regression for startup input and death while an
/// objective plan is pending. Only the scheduling flag is a test fixture;
/// damage, ammo, input handlers, player, HUD, results, and saving are real.
/// </summary>
public partial class RuntimeReadinessAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;

    public override async void _Ready()
    {
        try
        {
            await Verify();
            GD.Print("TWR_SMOKE_RUNTIME_READINESS_OK" +
                " real_gameplay_root=true gated_input=true reload_cancelled=true" +
                " death_during_pending_plan=true results_saved=true" +
                " original_private_map_unverified=true");
            RuntimeShutdownCoordinator.Request(GetTree(), 0);
        }
        catch (Exception error)
        {
            GD.PushError("TWR_SMOKE_RUNTIME_READINESS_FAILED " + error);
            RuntimeShutdownCoordinator.Request(GetTree(), 1);
        }
    }

    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async Task Verify()
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException("Requires fresh isolated profile");
        Runtime.StartMap("Ranch");
        var game = new GameplayRoot { MapName = "Ranch", Runtime = Runtime };
        AddChild(game);
        game.SetProcess(false);
        var player = game.GetNode<FirstPersonPlayer>("Player");
        await Frames(8);
        player.SetGameplayControlsEnabled(false);
        var position = player.GlobalPosition;
        var rotation = player.Rotation;
        var weapon = player.EquippedWeaponName;
        var loaded = Runtime.Player!.Ammo.GetValueOrDefault(weapon);
        using var equip = new InputEventKey { Keycode = Key.Key1, Pressed = true };
        using var reload = new InputEventKey { Keycode = Key.R, Pressed = true };
        using var jump = new InputEventKey { Keycode = Key.Space, Pressed = true };
        using var fire = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };
        using var look = new InputEventMouseMotion { Relative = new Vector2(200, 100) };
        player._UnhandledInput(equip);
        player._UnhandledInput(reload);
        player._UnhandledInput(jump);
        player._UnhandledInput(fire);
        player._UnhandledInput(look);
        player._PhysicsProcess(.25);
        await Frames(3);
        if (player.EquippedWeaponName != weapon ||
            player.ReloadRemainingSeconds70 != 0 ||
            Runtime.Player.Ammo.GetValueOrDefault(weapon) != loaded ||
            player.GlobalPosition.DistanceTo(position) > .001f ||
            player.Rotation.DistanceTo(rotation) > .001f)
            throw new InvalidOperationException("Suppressed startup accepted gameplay input");

        await RuntimePickupVisualAcceptance.Verify(game);
        await DeployedFortificationAcceptance.Verify(game, player, Runtime);
        await TimedJackAcceptance.Verify(game, player, Runtime);
        await RuntimeInteractionAcceptance.Verify(game, player, Runtime);
        FirstPersonModelLighting.Verify(player);
        await SupplyDeliveryAcceptance.Verify(game, player);
        player.SetGameplayControlsEnabled(true);
        player._UnhandledInput(equip);
        if (player.EquippedWeaponName != player.PrimaryWeaponName)
            throw new InvalidOperationException("Ready player did not resume normal equipment input");
        using var secondary = new InputEventKey { Keycode = Key.Key2, Pressed = true };
        player._UnhandledInput(secondary);
        if (!Runtime.SpendAmmo(player.EquippedWeaponName, 1))
            throw new InvalidOperationException("Could not establish reload fixture through ammo command");
        player._UnhandledInput(reload);
        if (player.ReloadRemainingSeconds70 <= 0)
            throw new InvalidOperationException("Ready player's real reload did not begin");
        player.SetGameplayControlsEnabled(false);
        if (player.ReloadRemainingSeconds70 != 0)
            throw new InvalidOperationException("Disabling control did not cancel pending reload");

        // Reproduce the scheduling race: render waits for a plan, but a
        // fatal hit makes physics refuse further planning. No health field
        // assignment or direct results manipulation is used.
        var pending = typeof(GameplayRoot).GetField(
            "_sourceObjectivePlanPending123", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Pending-plan field not found");
        pending.SetValue(game, true);
        Runtime.DamagePlayer(10000, "Readiness regression fatal hit", true);
        game._Process(.016);
        if (game.WaveStage != "RESULTS" ||
            Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true ||
            player.GameplayControlsEnabled || (bool)pending.GetValue(game)!)
            throw new InvalidOperationException("Fatal hit deadlocked behind pending source planning");
        var afterDeathWeapon = player.EquippedWeaponName;
        player._UnhandledInput(equip);
        if (player.EquippedWeaponName != afterDeathWeapon)
            throw new InvalidOperationException("Results screen still accepted equipment changes");
        if (game.GetChildren().OfType<ObjectiveRuntime>().Any())
            throw new InvalidOperationException("Late objective appeared after failed match");
    }
}

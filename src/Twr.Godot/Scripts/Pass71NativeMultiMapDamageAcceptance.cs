using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;
using Twr.Domain.Services;

namespace Twr.Godot;

/// <summary>
/// Pass71: exported Windows native pursuit, wall occlusion, melee damage,
/// death cleanup and reload interruption on two real gameplay scene types.
/// A raised, fabricated test lane isolates combat mechanics from incomplete
/// retail-map geometry. This is not original map navigation or a human run.
/// </summary>
public partial class Pass71NativeMultiMapDamageAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker = "TWR_SMOKE_PASS71_MULTIMAP_DAMAGE_OK";

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null)
                throw new InvalidOperationException(
                    "Pass71 test requires an isolated initialized LocalSession");
            foreach (var map in new[] { "Laboratory", "Manor" })
                await ValidateNativeDamageOnMap(map);
            GD.Print(SuccessMarker +
                " maps=Laboratory,Manor" +
                " real_gameplay_roots=2" +
                " wave_spawned_physics_infected=true" +
                " physical_pursuit=true" +
                " wall_blocks_melee=true" +
                " unblocked_melee_is_authoritative=true" +
                " player_death_and_results_cleanup=true" +
                " pending_reload_cancelled_on_death=true" +
                " test_floor_is_fabricated=true" +
                " retail_map_navigation_not_verified=true" +
                " owner_assets_and_saves_not_read=true");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS71_MULTIMAP_DAMAGE_FAILED " +
                ex.GetType().Name + " " + ex.Message + "\n" + ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task PhysicsFrames(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async Task ValidateNativeDamageOnMap(string map)
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass71 prior map did not return to the offline lobby");

        Runtime.StartMap(map);
        var game = new GameplayRoot
        {
            Name = "Pass71" + map + "GameplayRoot",
            Runtime = Runtime,
            MapName = map
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var hud = game.GetNodeOrNull<GameplayHud>("HUD");
        if (player is null || hud is null || player.CaptureCamera is null ||
            Runtime.Match?.MapName != map || Runtime.Match.Wave != 1 ||
            Runtime.Player is null || !Runtime.Player.IsAlive)
            throw new InvalidOperationException(
                "Pass71 scene/player/HUD/authoritative initial state: " + map);

        // A completely synthetic solid test floor high above each map.
        // No original geometry, spawn point, or navigation data is altered.
        const float floorY = 54f;
        var floor = new StaticBody3D
        {
            Name = "Pass71SyntheticPhysicsFloor",
            Position = new Vector3(0, floorY, 0),
            CollisionLayer = 1,
            CollisionMask = 0
        };
        floor.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(18f, .7f, 18f) }
        });
        game.AddChild(floor);
        player.GlobalPosition = new Vector3(0, floorY + .35f, 0);
        await PhysicsFrames(24);
        if (!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass71 real player physics body did not settle: " + map);

        // Start the genuine game countdown and native wave spawner.
        game._Process(RegularWaveRules.CountdownSeconds + .05);
        game._Process(.05);
        var infected = game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(GodotObject.IsInstanceValid);
        if (game.WaveStage != "WAVE" || infected is null ||
            infected.Target != player || game.ActiveInfectedCount < 1)
            throw new InvalidOperationException(
                "Pass71 actual Wave 1 infected did not spawn: " + map);

        // Test-only controlled enemy type and lane coordinates. In particular,
        // remove any source navigation reference from this one fabricated lane:
        // we are testing REAL CharacterBody3D pursuit, not claiming to test
        // unrecovered retail interiors or long-distance pathfinding.
        infected.InfectedType = "Civilian";
        infected.SourceNavigator = null;
        infected.HighwayNavigator = null;
        infected.MoveSpeed = 3.2f;
        infected.Damage = 8f;
        infected.GlobalPosition = new Vector3(0, floorY + .82f, -5f);
        await PhysicsFrames(2);
        var originalHealth = Runtime.Player.Health;
        var startDistance = PlanarDistance(infected, player);
        await PhysicsFrames(42);
        var pursuedDistance = PlanarDistance(infected, player);
        if (pursuedDistance >= startDistance - .65f ||
            !infected.IsOnFloor() || Runtime.Player.Health < originalHealth)
            throw new InvalidOperationException(
                "Pass71 infected failed actual floor-grounded pursuit: " +
                map + " start=" + startDistance + " end=" + pursuedDistance);

        // A real Godot StaticBody3D must prevent attacks through solid walls
        // even when the infected is well within melee range.
        var wall = new StaticBody3D
        {
            Name = "Pass71SyntheticLineOfSightWall",
            Position = new Vector3(0, floorY + 1.15f, -.66f),
            CollisionLayer = 1,
            CollisionMask = 0
        };
        wall.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(4f, 2.3f, .18f) }
        });
        game.AddChild(wall);
        infected.MoveSpeed = 0f;
        infected.GlobalPosition = new Vector3(0, floorY + .82f, -1.32f);
        await PhysicsFrames(80);
        if (Math.Abs(Runtime.Player.Health - originalHealth) > .001f)
            throw new InvalidOperationException(
                "Pass71 infected melee passed through an intact wall: " + map);

        // Create the ammo precondition through the same authoritative command
        // used by actual gunfire. Starting the reload uses a real R key event.
        // It must NOT finish after the infected kills this player.
        var weapon = RuntimeWeaponCatalog.Get("Glock 17");
        if (!Runtime.SpendAmmo(weapon.Name, 2))
            throw new InvalidOperationException(
                "Pass71 could not establish legitimate partial Glock magazine");
        var spentAmmo = Runtime.Player.Ammo.GetValueOrDefault(weapon.Name);
        var reserve = Runtime.Player.ReserveAmmo.GetValueOrDefault(weapon.Name);
        player._UnhandledInput(new InputEventKey
        {
            Keycode = Key.R, Pressed = true
        });
        if (player.ReloadRemainingSeconds70 <= 0)
            throw new InvalidOperationException(
                "Pass71 R key failed to begin a normal reload: " + map);

        // Removing the wall permits the ORIGINAL InfectedAgent attack path
        // to invoke Runtime.DamagePlayer, which must end the actual match.
        infected.Damage = 250f;
        wall.QueueFree();
        await PhysicsFrames(2);
        for (var frame = 0; frame < 65 && Runtime.Player.IsAlive; frame++)
            await PhysicsFrames(1);
        await PhysicsFrames(3);
        if (Runtime.Player.IsAlive ||
            game.WaveStage != "RESULTS" ||
            Runtime.Match.Phase != MatchPhase.Results ||
            !Runtime.Match.SaveCommitted ||
            game.ActiveInfectedCount != 0)
            throw new InvalidOperationException(
                "Pass71 unblocked infected attack did not kill player, save results, and clean up wave: " + map);

        // Fatal damage should cancel the pending reload, never grant ammo to
        // a corpse; subsequent R key presses must not restart an action.
        await PhysicsFrames(Math.Clamp(
            (int)Math.Ceiling(weapon.ReloadSeconds * 60.0) + 32, 50, 420));
        player._UnhandledInput(new InputEventKey
        {
            Keycode = Key.R, Pressed = true
        });
        await PhysicsFrames(3);
        if (player.ReloadRemainingSeconds70 > 0 ||
            Runtime.Player.Ammo.GetValueOrDefault(weapon.Name) != spentAmmo ||
            Runtime.Player.ReserveAmmo.GetValueOrDefault(weapon.Name) != reserve)
            throw new InvalidOperationException(
                "Pass71 a dead player finished/restarted reloading: " + map);

        GD.Print("TWR_PASS71_MAP_OK map=" + map +
            " pursued_metres=" + (startDistance - pursuedDistance).ToString("F2") +
            " melee_occluded=true death_saved=true interrupted_reload=true");

        game.QueueFree();
        await PhysicsFrames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass71 failed to restore isolated lobby after " + map);
    }

    private static float PlanarDistance(Node3D a, Node3D b)
    {
        var delta = a.GlobalPosition - b.GlobalPosition;
        return new Vector2(delta.X, delta.Z).Length();
    }
}

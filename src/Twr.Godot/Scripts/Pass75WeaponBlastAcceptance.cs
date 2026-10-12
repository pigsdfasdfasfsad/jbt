using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass75 exported Windows native weapon and splash regression on actual
/// Laboratory/Manor gameplay roots. Raised floors and wall are synthetic
/// PHYSICS TEST FIXTURES, not source-original map content. The wave-spawned
/// enemy, rockets, grenades, credits, damage objectives and hit physics use
/// production code. No owner profile or original asset pack is accessed.
/// </summary>
public partial class Pass75WeaponBlastAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker = "TWR_SMOKE_PASS75_WEAPON_BLAST_OK";
    private const float FloorY = 54f;
    private int _completedMaps;

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass75 expected independent offline test profile in lobby");

            await ValidateLauncher();
            await ValidateFrag();
            if (_completedMaps != 2)
                throw new InvalidOperationException(
                    "Pass75 incomplete real gameplay map tests");
            GD.Print(SuccessMarker +
                " maps=Laboratory,Manor" +
                " real_wave_enemy_and_hit_physics=true" +
                " launcher_direct_infected_collision=true" +
                " launcher_wall_occlusion=true" +
                " launcher_objective_damage_visible=true" +
                " frag_wall_occlusion=true" +
                " frag_visible_damage_and_rewards=true" +
                " objective_occlusion=true" +
                " original_weapon_stats_and_domain_authority_preserved=true" +
                " test_geometry_is_synthetic=true" +
                " original_map_visual_parity_unverified=true");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS75_WEAPON_BLAST_FAILED " +
                "maps=" + _completedMaps + " " + ex.GetType().Name +
                " " + ex.Message + "\n" + ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async Task<(GameplayRoot Game, FirstPersonPlayer Player,
        InfectedAgent Enemy)> Begin(string map)
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass75 prior map did not restore isolated lobby");
        Runtime.StartMap(map);
        var game = new GameplayRoot
        {
            Name = "Pass75" + map,
            MapName = map,
            Runtime = Runtime
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if (player is null || player.CaptureCamera is null ||
            Runtime.Player?.IsAlive != true ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null)
            throw new InvalidOperationException(
                "Pass75 native gameplay actor/HUD not present on " + map);

        var floor = new StaticBody3D
        {
            Name = "Pass75SyntheticFloor",
            Position = new Vector3(0f, FloorY, 0f),
            CollisionLayer = 1u,
            CollisionMask = 0u
        };
        floor.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(35f, .70f, 35f)
            }
        });
        game.AddChild(floor);
        player.GlobalPosition = new Vector3(0f, FloorY + .35f, 0f);
        await Frames(35);
        if (!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass75 normal player CharacterBody physics not grounded");

        game._Process(RegularWaveRules.CountdownSeconds + .05);
        game._Process(.05);
        if (game.WaveStage != "WAVE" || game.ActiveInfectedCount < 1)
            throw new InvalidOperationException(
                "Pass75 actual Wave 1 spawner not active on " + map);

        var enemy = game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(GodotObject.IsInstanceValid);
        if (enemy is null || enemy.Target != player)
            throw new InvalidOperationException(
                "Pass75 spawned enemy not target-bound on " + map);
        game.SetProcess(false); // still runs native physics and local session
        enemy.InfectedType = "Civilian"; // fixture avoids random special abilities
        enemy.MoveSpeed = 0;
        enemy.Damage = 0;
        enemy.GlobalPosition = new Vector3(0f, FloorY + .82f, -6f);
        await Frames(4);
        if (!enemy.IsOnFloor())
            throw new InvalidOperationException(
                "Pass75 native enemy did not settle onto synthetic physics lane");
        return (game, player, enemy);
    }

    private static StaticBody3D Wall(GameplayRoot game)
    {
        var wall = new StaticBody3D
        {
            Name = "Pass75SyntheticSolidOccluder",
            Position = new Vector3(0f, FloorY + 1.45f, -4f),
            CollisionLayer = 1u,
            CollisionMask = 0u
        };
        wall.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(12f, 2.8f, .55f)
            }
        });
        game.AddChild(wall);
        return wall;
    }

    private static ExplosiveProjectileRuntime Rocket(GameplayRoot game,
        FirstPersonPlayer player, float damage)
    {
        var rocket = new ExplosiveProjectileRuntime
        {
            Name = "Pass75RealLauncherRocket",
            SourcePlayer = player,
            WeaponName = "RPG-7",
            Damage = damage,
            Direction = Vector3.Forward,
            Position = new Vector3(0f, FloorY + 1.1f, -1f)
        };
        game.AddChild(rocket);
        return rocket;
    }

    private static ThrowableProjectileRuntime Frag(GameplayRoot game,
        FirstPersonPlayer player)
    {
        var grenade = new ThrowableProjectileRuntime
        {
            Name = "Pass75RealFuseFrag",
            ThrowableType = "Frag",
            SourcePlayer = player,
            // Stationary start removes random horizontal throw placement,
            // while preserving the real gravity, bounce and 3.0s fuse.
            Direction = Vector3.Zero,
            Position = new Vector3(0f, FloorY + 1.45f, -1.50f)
        };
        game.AddChild(grenade);
        return grenade;
    }

    private static DamageObjectiveTarget Tanker(GameplayRoot game,
        string name, Vector3 position)
    {
        var target = new DamageObjectiveTarget
        {
            Name = name,
            Position = position
        };
        game.AddChild(target);
        return target;
    }

    private async Task Finish(GameplayRoot game)
    {
        Runtime.FailMatch("Pass75 isolated weapon blast verification");
        if (Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass75 test-only results not saved through domain");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass75 could not restore isolated offline lobby");
        _completedMaps++;
    }

    private async Task ValidateLauncher()
    {
        var (game, player, enemy) = await Begin("Laboratory");
        var wall = Wall(game);
        // Existing tanker damage actor tests the separate objective target
        // branch of a single explosive ray/LOS implementation.
        var tanker = Tanker(game, "Pass75RocketTanker",
            new Vector3(3.2f, FloorY + .35f, -6f));
        await Frames(3);
        var beforeEnemy = enemy.Health;
        var beforeTanker = tanker.Health;
        var beforeCredits = Runtime.Player!.Credits;
        var launcher = RuntimeWeaponCatalog.Get("RPG-7");
        if (!launcher.IsLauncher || launcher.Damage <= 0)
            throw new InvalidOperationException(
                "Pass75 original RPG-7 catalog statistics missing");

        Rocket(game, player, launcher.Damage);
        await Frames(25);
        if (!GodotObject.IsInstanceValid(enemy) ||
            Math.Abs(enemy.Health - beforeEnemy) > .001f ||
            Math.Abs(tanker.Health - beforeTanker) > .001f ||
            Runtime.Player.Credits != beforeCredits)
            throw new InvalidOperationException(
                "Pass75 launcher blast damaged enemy/objective THROUGH solid wall");

        wall.QueueFree();
        await Frames(3);
        Rocket(game, player, launcher.Damage);
        await Frames(25);
        var hitEnemy = !GodotObject.IsInstanceValid(enemy) ||
            enemy.Health < beforeEnemy;
        if (!hitEnemy || tanker.Health >= beforeTanker)
            throw new InvalidOperationException(
                "Pass75 launcher physics did not hit infected layer 2 or tanker " +
                "enemyHit=" + hitEnemy +
                " tankerHealth=" + tanker.Health);
        GD.Print("TWR_PASS75_LAUNCHER_OK " +
            "blocked_enemy=true blocked_tanker=true " +
            "direct_infected_hit=true visible_objective_damage=true");
        await Finish(game);
    }

    private async Task ValidateFrag()
    {
        var (game, player, enemy) = await Begin("Manor");
        var wall = Wall(game);
        var tanker = Tanker(game, "Pass75FragTanker",
            new Vector3(3.2f, FloorY + .35f, -6f));
        var farTanker = Tanker(game, "Pass75OutOfRadiusTanker",
            new Vector3(14f, FloorY + .35f, -6f));
        await Frames(3);
        var enemyHealth = enemy.Health;
        var hp = tanker.Health;
        var remoteHp = farTanker.Health;
        var credits = Runtime.Player!.Credits;

        Frag(game, player);
        await Frames(205);
        if (!GodotObject.IsInstanceValid(enemy) ||
            Math.Abs(enemy.Health - enemyHealth) > .001f ||
            Math.Abs(tanker.Health - hp) > .001f ||
            Math.Abs(farTanker.Health - remoteHp) > .001f ||
            Runtime.Player.Credits != credits)
            throw new InvalidOperationException(
                "Pass75 Frag penetrated solid wall or damaged out-of-radius target");

        wall.QueueFree();
        await Frames(3);
        Frag(game, player);
        await Frames(205);
        var enemyHit = !GodotObject.IsInstanceValid(enemy) ||
            enemy.Health < enemyHealth;
        if (!enemyHit || tanker.Health >= hp ||
            farTanker.Health < remoteHp ||
            Runtime.Player.Credits <= credits)
            throw new InvalidOperationException(
                "Pass75 open Frag failed native hit/reward/target-range checks " +
                "infectedHit=" + enemyHit +
                " tankerHp=" + tanker.Health +
                " credits=" + Runtime.Player.Credits);
        GD.Print("TWR_PASS75_FRAG_OK " +
            "blocked_infected_and_tanker=true " +
            "visible_infected_damage_and_domain_kill_rewards=true " +
            "visible_objective_damage=true out_of_radius_preserved=true");
        await Finish(game);
    }
}

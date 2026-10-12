using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass74 exported-Windows special infected ability acceptance. The raised
/// lane and temporary occlusion walls are test fixtures, NOT original Roblox
/// map geometry. Enemy physics, death callbacks, projectile/cloud hit tests,
/// player health and progression are still production Godot/domain code.
/// </summary>
public partial class Pass74SpecialInfectedAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker = "TWR_SMOKE_PASS74_SPECIAL_INFECTED_OK";
    private const float ArenaHeight = 54f;
    private int _validatedMaps;

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null || Runtime.Match?.Phase != MatchPhase.Lobby ||
                InfectedCatalog.Active.Length != 8)
                throw new InvalidOperationException(
                    "Pass74 needs isolated session and all eight infected types");

            await ValidateRanchBolterAndResistance();
            await ValidateManorHazardsAndBloater();
            if (_validatedMaps != 2)
                throw new InvalidOperationException("Pass74 map lifecycle incomplete");

            GD.Print(SuccessMarker +
                " maps=Ranch,Manor" +
                " variants=8" +
                " bolter_supported_leap_and_contact=true" +
                " bolter_wall_block=true" +
                " unsupported_bolter_target_rejected=true" +
                " source_damage_and_smoke_resistances=true" +
                " burster_direct_blast_occluded=true" +
                " burster_direct_blast_unblocked=true" +
                " spore_gas_mask_immunity=true" +
                " bloater_volley_line_of_sight=true" +
                " spore_projectile_world_collision=true" +
                " player_health_domain_authoritative=true" +
                " owner_original_mesh_parity_unverified=true");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("TWR_SMOKE_PASS74_SPECIAL_INFECTED_FAILED " +
                "maps=" + _validatedMaps + " " + error.GetType().Name +
                " " + error.Message + "\n" + error.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task PhysicsFrames(int frames)
    {
        for (var index = 0; index < frames; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async Task<(GameplayRoot Game, FirstPersonPlayer Player)> BeginMap(
        string map)
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass74 prior scenario did not return to lobby");
        Runtime.StartMap(map);
        var game = new GameplayRoot
        {
            Name = "Pass74" + map + "Gameplay",
            MapName = map,
            Runtime = Runtime
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if (player is null || player.CaptureCamera is null ||
            Runtime.Player?.IsAlive != true ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null)
            throw new InvalidOperationException(
                "Pass74 actual gameplay scene/camera/HUD initialization failed: " + map);

        game.AddChild(new StaticBody3D
        {
            Name = "Pass74SyntheticArenaFloor",
            Position = new Vector3(0, ArenaHeight, 0),
            CollisionLayer = 1u,
            CollisionMask = 0u
        });
        var floor = game.GetNode<StaticBody3D>("Pass74SyntheticArenaFloor");
        floor.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(44f, .7f, 44f)
            }
        });

        // The artificial lane merely guarantees deterministic support for
        // physics tests; normal map spawning/visuals remain in the game.
        player.GlobalPosition = new Vector3(0, ArenaHeight + .35f, 0);
        await PhysicsFrames(40);
        if (!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass74 player failed real Godot ground contact on " + map);
        game._Process(RegularWaveRules.CountdownSeconds + .05);
        game._Process(.05);
        if (game.WaveStage != "WAVE" ||
            game.ActiveInfectedCount < 1 ||
            game.GetChildren().OfType<InfectedAgent>().Count() < 1)
            throw new InvalidOperationException(
                "Pass74 actual Wave 1 spawner did not construct an infected: " + map);
        game.SetProcess(false); // no unrelated hordes in deterministic test lanes
        return (game, player);
    }

    private static StaticBody3D OcclusionWall(GameplayRoot game,
        string name, float z)
    {
        var wall = new StaticBody3D
        {
            Name = name,
            Position = new Vector3(0, ArenaHeight + 1.65f, z),
            CollisionLayer = 1u,
            CollisionMask = 0u
        };
        wall.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(12f, 2.6f, .45f) }
        });
        game.AddChild(wall);
        return wall;
    }

    private static InfectedAgent FindLiveWaveInfected(GameplayRoot game)
    {
        return game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(a => GodotObject.IsInstanceValid(a) && a.Health > 0)
            ?? throw new InvalidOperationException(
                "Pass74 expected actual production Wave 1 infected actor");
    }

    private async Task SpawnNextWaveActor(GameplayRoot game)
    {
        game._Process(RegularWaveRules.SpawnIntervalSeconds(1) + .20);
        if (game.ActiveInfectedCount < 1)
            throw new InvalidOperationException(
                "Pass74 game wave spawner failed to create next actor");
        await PhysicsFrames(2);
    }

    private async Task EndMap(GameplayRoot game)
    {
        Runtime.FailMatch("Pass74 isolated native special attack test completed");
        if (Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass74 results not committed to isolated profile");
        game.QueueFree();
        await PhysicsFrames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass74 did not return to isolated lobby");
        _validatedMaps++;
    }

    private async Task ValidateRanchBolterAndResistance()
    {
        var (game, player) = await BeginMap("Ranch");
        var actor = FindLiveWaveInfected(game);
        actor.InfectedType = "Bolter"; // synthetic choice; production AI untouched
        actor.Health = 100f;
        actor.Damage = 10f;
        actor.MoveSpeed = 0f;
        actor.GlobalPosition = new Vector3(0, ArenaHeight + 1.15f, -8f);

        var wall = OcclusionWall(game, "Pass74BolterSolidWall", -4f);
        await PhysicsFrames(32);
        var healthBefore = Runtime.Player!.Health;
        if (actor.BolterLeapStarts74 != 0 ||
            Runtime.Player.Health < healthBefore)
            throw new InvalidOperationException(
                "Pass74 Bolter attacked through a solid world wall");

        if (!Pass74SpecialInfectedSafety.HasSupportedLanding(
                GetWorld3D(), player.GlobalPosition,
                actor.GetRid(), player.GetRid()) ||
            Pass74SpecialInfectedSafety.HasSupportedLanding(
                GetWorld3D(), player.GlobalPosition + Vector3.Up * 7f,
                actor.GetRid(), player.GetRid()))
            throw new InvalidOperationException(
                "Pass74 physical landing check accepted airborne destination");

        wall.QueueFree();
        await PhysicsFrames(3);
        for (var i = 0; i < 55 && actor.BolterLeapHits74 == 0; i++)
            await PhysicsFrames(1);

        if (actor.BolterLeapStarts74 != 1 ||
            actor.BolterLeapHits74 != 1 ||
            Runtime.Player.Health >= healthBefore)
            throw new InvalidOperationException(
                "Pass74 real Bolter physics failed supported leap/contact " +
                "launched=" + actor.BolterLeapStarts74 +
                " hits=" + actor.BolterLeapHits74 +
                " health=" + Runtime.Player.Health);
        var bolterDamage = healthBefore - Runtime.Player.Health;
        actor.QueueFree();
        await PhysicsFrames(3);

        // Original Infected Info.Source damage/smoke multipliers must be
        // honored by real native enemy actors, not only catalog lookup.
        foreach (var type in InfectedCatalog.Active.Select(x => x.Name))
        {
            var dummy = new InfectedAgent
            {
                Name = "Pass74" + type,
                InfectedType = type,
                Target = player,
                Runtime = Runtime,
                Health = 100,
                Damage = 0,
                MoveSpeed = 0,
                Position = new Vector3(14f, ArenaHeight + 1.15f, 14f)
            };
            game.AddChild(dummy);
            dummy.ApplySlow(.2f, 5.0);
            var expectedSlow = type switch
            {
                "Hazmat" or "Burster" => 1f,
                "Bolter" => .6f,
                _ => .2f
            };
            if (Math.Abs(dummy.EffectiveSlowFactor74 - expectedSlow) > .001f)
                throw new InvalidOperationException(
                    "Pass74 source smoke resistance incorrect on " + type);

            if (type == "Riot")
            {
                dummy.ApplyDamage(20f, false, "Melee");
                if (Math.Abs(dummy.Health - 90f) > .001f)
                    throw new InvalidOperationException(
                        "Pass74 Riot melee resistance did not apply");
            }
            if (type == "Bloater")
            {
                dummy.ApplyDamage(10f, false, "Fire");
                if (Math.Abs(dummy.Health - 70f) > .001f)
                    throw new InvalidOperationException(
                        "Pass74 Bloater fire vulnerability did not apply");
            }
            dummy.QueueFree();
        }
        await PhysicsFrames(3);
        GD.Print("TWR_PASS74_BOLTER_RESISTANCE_OK " +
            $"contact_damage={bolterDamage:F2} " +
            "solid_wall_block=true supported_landing=true variants=8");
        await EndMap(game);
    }

    private async Task ValidateManorHazardsAndBloater()
    {
        var (game, player) = await BeginMap("Manor");
        var wall = OcclusionWall(game, "Pass74BursterOcclusion", -1.5f);
        var burster = FindLiveWaveInfected(game);
        burster.InfectedType = "Burster";
        burster.MoveSpeed = 0;
        burster.Damage = 0;
        burster.Health = 100;
        burster.GlobalPosition = new Vector3(0, ArenaHeight + 1.15f, -3f);
        await PhysicsFrames(4);
        var blockedHp = Runtime.Player!.Health;
        burster.ApplyDamage(1000f, false, "Explosion");
        if (Runtime.Player.Health < blockedHp ||
            game.GetChildren().OfType<SporeCloudRuntime>().Count() < 1)
            throw new InvalidOperationException(
                "Pass74 Burster instant detonation passed through solid wall");

        await PhysicsFrames(40);
        if (Runtime.Player.Health < blockedHp)
            throw new InvalidOperationException(
                "Pass74 Burster gas damaged player through an intact wall");

        wall.QueueFree();
        await PhysicsFrames(3);
        if (!Runtime.ActivateGasMask())
            throw new InvalidOperationException(
                "Pass74 isolated gas-mask activation failed");
        await PhysicsFrames(40);
        if (Runtime.Player.Health < blockedHp)
            throw new InvalidOperationException(
                "Pass74 gas mask failed to prevent lingering Burster gas damage");

        // A fresh unoccluded Burster should still inflict its direct blast
        // even with a mask; only cloud gas ticks are blocked by that mask.
        await SpawnNextWaveActor(game);
        var exposed = FindLiveWaveInfected(game);
        exposed.InfectedType = "Burster";
        exposed.MoveSpeed = 0;
        exposed.Damage = 0;
        exposed.Health = 100;
        exposed.GlobalPosition = new Vector3(0, ArenaHeight + 1.15f, -2.5f);
        await PhysicsFrames(2);
        var clearHp = Runtime.Player.Health;
        exposed.ApplyDamage(1000f, false, "Explosion");
        var blastDamage = clearHp - Runtime.Player.Health;
        if (Math.Abs(blastDamage - 27.5f) > .1f)
            throw new InvalidOperationException(
                "Pass74 unoccluded Burster direct blast was unexpectedly prevented " +
                "damage=" + blastDamage);
        await PhysicsFrames(36);
        if (Runtime.Player.Health < clearHp - 27.6f)
            throw new InvalidOperationException(
                "Pass74 gas-mask allowed damage from fresh Burster cloud");

        // The genuine GameplayRoot wave-spawner callback must create a
        // spore projectile only when the Bloater can see the target.
        await SpawnNextWaveActor(game);
        var bloater = FindLiveWaveInfected(game);
        bloater.InfectedType = "Bloater";
        bloater.Health = 488f;
        bloater.MoveSpeed = 0f;
        bloater.Damage = 0f;
        bloater.GlobalPosition = new Vector3(0, ArenaHeight + 1.15f, -8f);
        wall = OcclusionWall(game, "Pass74BloaterProjectileWall", -4f);
        await PhysicsFrames(12);
        if (bloater.BloaterVolleyCount74 != 0)
            throw new InvalidOperationException(
                "Pass74 Bloater fired a projectile through an intact wall");

        // Independently fire a real swept-physics projectile into that wall
        // and ensure its spore burst cannot hit a protected target behind it.
        var blockedProjectile = new SporeProjectileRuntime
        {
            Name = "Pass74BlockedSpore",
            Target = player,
            Runtime = Runtime,
            Damage = 26.4f,
            Radius = 15f,
            Position = new Vector3(0, ArenaHeight + 1.15f, -8f)
        };
        game.AddChild(blockedProjectile);
        var projectileHp = Runtime.Player.Health;
        await PhysicsFrames(45);
        if (Runtime.Player.Health < projectileHp)
            throw new InvalidOperationException(
                "Pass74 spore projectile blast passed through physical wall");

        wall.QueueFree();
        await PhysicsFrames(3);
        for (var i = 0; i < 140 && Runtime.Player.Health >= projectileHp; i++)
            await PhysicsFrames(1);
        if (bloater.BloaterVolleyCount74 != 1 ||
            Runtime.Player.Health >= projectileHp)
            throw new InvalidOperationException(
                "Pass74 visible Bloater failed actual projectile damage " +
                "volleys=" + bloater.BloaterVolleyCount74 +
                " hp=" + Runtime.Player.Health);

        GD.Print("TWR_PASS74_HAZARDS_OK " +
            $"unoccluded_burster_damage={blastDamage:F2} " +
            "blocked_burster=true gas_mask=true " +
            "blocked_spore=true bloater_volley=true");
        await EndMap(game);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass73 native exported Windows acceptance on all ten LIVE GameplayRoot
/// scenes. The map-specific ground navigation is APPROXIMATE on fallback
/// maps; the single Ranch wall is a clearly synthetic obstacle fixture.
/// Actual keyboard input and real pickup/objective/enemy domain paths are
/// exercised without changing retail world coordinates or user profiles.
/// </summary>
public partial class Pass73FullMapGameplayAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker = "TWR_SMOKE_PASS73_FULLMAP_GAMEPLAY_OK";
    private int _mapsValidated;

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null || Runtime.Match?.Phase != MatchPhase.Lobby ||
                ReleaseRules.Maps.Length != 10)
                throw new InvalidOperationException(
                    "Pass73 native acceptance requires an isolated ten-map lobby");

            foreach (var map in ReleaseRules.Maps)
                await ValidateMap(map);

            if (_mapsValidated != 10)
                throw new InvalidOperationException(
                    "Pass73 did not validate all ten gameplay scenes");

            GD.Print(SuccessMarker + " maps=" + _mapsValidated +
                " fallback_nav_on_nine_maps=true" +
                " expressway_original_fallback_graph_preserved=true" +
                " player_walk_sprint_jump_input=true" +
                " wall_detour_uses_actual_infected_physics=true" +
                " pickups_and_objectives_use_domain_authority=true" +
                " postmortem_interactions_blocked=true" +
                " synthetic_ranch_wall_not_retail_geometry=true" +
                " owner_original_map_nav_parity_unverified=true" +
                " offline_profile_isolated=true");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS73_FULLMAP_GAMEPLAY_FAILED " +
                "maps=" + _mapsValidated + " " + ex.GetType().Name +
                " " + ex.Message + "\n" + ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task PhysicsFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void SetKey(Key key, bool pressed)
    {
        // Godot's public input parser, not direct position or velocity
        // assignment, drives the normal _UnhandledInput / IsKeyPressed loop.
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key,
            Pressed = pressed
        });
    }

    private async Task ValidateMap(string map)
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass73 previous scene did not return to lobby before " + map);

        Runtime.StartMap(map);
        var game = new GameplayRoot
        {
            Name = "Pass73" + map + "GameplayRoot",
            MapName = map,
            Runtime = Runtime
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if (player is null || player.CaptureCamera is null ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null ||
            Runtime.Player is null || !Runtime.Player.IsAlive ||
            Runtime.Match?.MapName != map ||
            game.ActivePickupCount < 1)
            throw new InvalidOperationException(
                "Pass73 real gameplay actor/HUD/pickup initialization missing: " + map);

        if (map == "Ranch")
        {
            // Created BEFORE the first physics frame / graph sampling, so
            // the graph must actually detour around a physical wall.
            var wall = new StaticBody3D
            {
                Name = "Pass73SyntheticRanchOcclusionWall",
                Position = new Vector3(0f, 1.4f, 3.5f),
                CollisionLayer = 1,
                CollisionMask = 0
            };
            wall.AddChild(new CollisionShape3D
            {
                Shape = new BoxShape3D
                {
                    Size = new Vector3(12f, 2.8f, .75f)
                }
            });
            game.AddChild(wall);
        }

        // Bounded graph construction runs in real physics frames, rather
        // than computing everything synchronously on map entry.
        await PhysicsFrames(70);
        if (!player.IsOnFloor() || !Runtime.Player.IsAlive)
            throw new InvalidOperationException(
                "Pass73 player lost ground support at actual map spawn: " + map);

        if (map == "Expressway")
        {
            var roadNav = game.GetNodeOrNull<ExpresswayNavigationRuntime>(
                "ExpresswayReconstruction/HighwayNavigation");
            if (roadNav is null || roadNav.NavigablePoints < 100 ||
                game.Pass73FallbackNavNodes != 0)
                throw new InvalidOperationException(
                    "Pass73 must preserve existing Expressway road navigation");
            GD.Print("TWR_PASS73_MAP_OK map=" + map +
                " navigation=road nodes=" + roadNav.NavigablePoints);
        }
        else
        {
            var nav = game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
                "Pass73GroundNavigation");
            if (nav is null || !nav.IsGraphReady ||
                nav.PointCount < 20 || nav.EdgeCount < 20 ||
                !game.Pass73FallbackNavReady)
                throw new InvalidOperationException(
                    "Pass73 collision-sampled fallback graph not ready on " + map +
                    " nodes=" + (nav?.PointCount ?? -1) +
                    " edges=" + (nav?.EdgeCount ?? -1));

            if (map == "Ranch")
                await ValidateRanchLiveTraversal(game, player, nav);
            GD.Print("TWR_PASS73_MAP_OK map=" + map +
                " navigation=collision-sampled nodes=" + nav.PointCount +
                " edges=" + nav.EdgeCount);
        }

        if (Runtime.Match?.Phase != MatchPhase.Results)
            Runtime.FailMatch("Pass73 test-only map scene teardown");
        if (Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass73 domain results save missing on " + map);
        game.QueueFree();
        await PhysicsFrames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass73 isolated session failed to return to lobby on " + map);
        _mapsValidated++;
    }

    private async Task ValidateRanchLiveTraversal(
        GameplayRoot game, FirstPersonPlayer player,
        Pass73GroundNavigationRuntime navigator)
    {
        // Real held W and Shift+S events must move the player at different
        // walk/sprint speeds without bypassing the CharacterBody collision.
        var beforeWalk = player.GlobalPosition;
        SetKey(Key.W, true);
        await PhysicsFrames(18);
        SetKey(Key.W, false);
        var walked = beforeWalk.DistanceTo(player.GlobalPosition);
        var beforeSprint = player.GlobalPosition;
        SetKey(Key.Shift, true);
        SetKey(Key.S, true);
        await PhysicsFrames(18);
        SetKey(Key.S, false);
        SetKey(Key.Shift, false);
        var sprinted = beforeSprint.DistanceTo(player.GlobalPosition);
        if (walked < .7f || sprinted < walked * 1.14f ||
            !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass73 native keyboard walking/sprinting did not move player: " +
                $"walk={walked:F2} sprint={sprinted:F2}");

        // Space must apply normal first-person jump physics and eventually
        // return to a solid floor. No position or velocity teleports.
        var startingY = player.GlobalPosition.Y;
        SetKey(Key.Space, true);
        await PhysicsFrames(5);
        SetKey(Key.Space, false);
        if (player.GlobalPosition.Y < startingY + .12f)
            throw new InvalidOperationException(
                "Pass73 real Space key did not initiate jump physics");
        await PhysicsFrames(80);
        if (!player.IsOnFloor() ||
            Math.Abs(player.GlobalPosition.Y - startingY) > .16f)
            throw new InvalidOperationException(
                "Pass73 player did not land on the authentic fallback floor");

        // Legitimate medkit and Radio interaction via the NORMAL pickup
        // and objective runtimes, never by assigning health/credits directly.
        Runtime!.DamagePlayer(25f, "Pass73 synthetic nonlethal health setup");
        var injured = Runtime.Player!.Health;
        var medkit = new PickupActor
        {
            Name = "Pass73PhysicalMedkit",
            PickupType = "Medkit",
            Player = player,
            Runtime = Runtime,
            Position = player.GlobalPosition + new Vector3(0, 0, -.8f)
        };
        game.AddChild(medkit);
        SetKey(Key.E, true);
        await PhysicsFrames(2);
        SetKey(Key.E, false);
        if (Runtime.Player.Health <= injured)
            throw new InvalidOperationException(
                "Pass73 actual E-key Medkit pickup did not heal player");

        var rewardCredits = Runtime.Player.Credits;
        const string objectiveId = "Ranch:Pass73:Radio";
        var radio = new ObjectiveRuntime
        {
            Name = "Pass73NativeRadio",
            ObjectiveId = objectiveId,
            Family = "Radio",
            Player = player,
            Runtime = Runtime,
            Position = player.GlobalPosition
        };
        game.AddChild(radio);
        // Accelerate only the 60-second secure timer. Completion still
        // calls the actual authoritative CompleteObjective command.
        radio._Process(60.1);
        if (!Runtime.Match!.CompletedObjectives.Contains(objectiveId) ||
            Runtime.Player.Credits <= rewardCredits)
            throw new InvalidOperationException(
                "Pass73 Radio secure objective did not award domain rewards");
        radio.QueueFree();

        // Direct sight at body height is physically blocked by the temporary
        // wall. Graph path and actual enemy body must navigate around it.
        var south = new Vector3(0, .82f, -2f);
        var north = player.GlobalPosition;
        var route = navigator.GetRoute(south, north);
        if (route.Length < 4 ||
            !route.Any(p => Math.Abs(p.X) >= 6f))
            throw new InvalidOperationException(
                "Pass73 fallback A* failed to detour around real synthetic wall " +
                "route_nodes=" + route.Length);

        game._Process(RegularWaveRules.CountdownSeconds + .05);
        game._Process(.05);
        var enemy = game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(GodotObject.IsInstanceValid);
        if (enemy is null || enemy.Target != player ||
            enemy.GroundNavigator73 != navigator)
            throw new InvalidOperationException(
                "Pass73 production wave spawn did not use fallback navigation");

        enemy.InfectedType = "Civilian";
        enemy.MoveSpeed = 4.0f;
        enemy.Damage = 0;
        enemy.GlobalPosition = south;
        game.SetProcess(false); // no additional spawns during deterministic chase
        await PhysicsFrames(4);
        var startingDistance = Planar(enemy.GlobalPosition, player.GlobalPosition);
        var furthestSide = 0f;
        for (var i = 0; i < 360; i++)
        {
            await PhysicsFrames(1);
            furthestSide = Math.Max(furthestSide, Math.Abs(enemy.GlobalPosition.X));
        }
        var endingDistance = Planar(enemy.GlobalPosition, player.GlobalPosition);
        if (furthestSide < 4.8f ||
            endingDistance >= startingDistance - 2f ||
            !enemy.IsOnFloor())
            throw new InvalidOperationException(
                "Pass73 actual infected physics body could not round wall " +
                $"side={furthestSide:F2} start={startingDistance:F2} " +
                $"end={endingDistance:F2} onfloor={enemy.IsOnFloor()}");

        // A fatal domain hit must end the scene. E-key interactions cannot
        // grant items or complete objectives after the player has died.
        Runtime.DamagePlayer(1000, "Pass73 controlled fatal damage");
        game._Process(.016);
        if (Runtime.Player.IsAlive || game.WaveStage != "RESULTS")
            throw new InvalidOperationException(
                "Pass73 fatal damage did not enter the Results stage");

        var fragBefore = Runtime.Player.Inventory.GetValueOrDefault("Frag");
        var deadPickup = new PickupActor
        {
            Name = "Pass73DeadPickupGuard",
            PickupType = "Frag",
            Player = player,
            Runtime = Runtime,
            Position = player.GlobalPosition
        };
        game.AddChild(deadPickup);
        SetKey(Key.E, true);
        deadPickup._Process(.016);
        SetKey(Key.E, false);
        var deadRadio = new ObjectiveRuntime
        {
            Name = "Pass73DeadObjectiveGuard",
            ObjectiveId = "Ranch:Pass73:DeadRadio",
            Family = "Radio",
            Runtime = Runtime,
            Player = player,
            Position = player.GlobalPosition
        };
        game.AddChild(deadRadio);
        deadRadio._Process(61.0);
        if (Runtime.Player.Inventory.GetValueOrDefault("Frag") != fragBefore ||
            Runtime.Match!.CompletedObjectives.Contains("Ranch:Pass73:DeadRadio"))
            throw new InvalidOperationException(
                "Pass73 dead player improperly collected items/objectives");

        GD.Print("TWR_PASS73_RANCH_INTERACTION_OK " +
            $"walk={walked:F2} sprint={sprinted:F2} " +
            $"detour_side={furthestSide:F2} " +
            $"pursuit_gain={startingDistance-endingDistance:F2} " +
            "medkit=true radio_rewards=true death_guards=true");
    }

    private static float Planar(Vector3 a, Vector3 b) =>
        new Vector2(a.X - b.X, a.Z - b.Z).Length();
}

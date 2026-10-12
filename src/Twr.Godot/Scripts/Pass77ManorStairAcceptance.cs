using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass77 native Windows acceptance for actual player stair ascent and
/// infected's physically audited upward path. The three stair flights are
/// reconstructed fallback Manor geometry, NOT owner-original Roblox parts.
/// No assertion here implies upper-level fidelity in the other nine maps.
/// </summary>
public partial class Pass77ManorStairAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker = "TWR_SMOKE_PASS77_MANOR_STAIRS_OK";

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass77 requires isolated offline test profile/lobby");
            await ValidateManor();
            GD.Print(SuccessMarker +
                " map=Manor flights=3" +
                " reconstructed_physics_ramps=true" +
                " upper_landings_connected=true" +
                " floor_support_physically_sampled=true" +
                " real_player_keyboard_w_ascent=true" +
                " collision_audited_ground_to_upper_route=true" +
                " infected_moves_with_real_character_physics=true" +
                " source_owner_map_not_imported=true" +
                " original_retail_geometry_parity_unverified=true" +
                " descending_stairs_unverified=true");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            Input.ParseInputEvent(new InputEventKey
            {
                Keycode = Key.W, Pressed = false
            });
            GD.PushError("TWR_SMOKE_PASS77_MANOR_STAIRS_FAILED " +
                ex.GetType().Name + " " + ex.Message +
                "\n" + ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int n)
    {
        for (var i = 0; i < n; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void SetKey(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key,
            Pressed = pressed
        });
    }

    private async Task ValidateManor()
    {
        Runtime.StartMap("Manor");
        var game = new GameplayRoot
        {
            Name = "Pass77ManorPlayableWorld",
            MapName = "Manor",
            Runtime = Runtime
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var nav = game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if (player is null || nav is null || Runtime.Player?.IsAlive != true ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null ||
            game.Pass77ApproximateStairs.Count != 3 ||
            nav.StairLinkCount77 != 3)
            throw new InvalidOperationException(
                "Pass77 fallback Manor gameplay and three-floor-link metadata absent");

        var stairs = game.Pass77ApproximateStairs;
        foreach (var stair in stairs)
        {
            var ramp = game.GetNodeOrNull<StaticBody3D>(
                stair.Name + "_WalkableRamp77");
            var landing = game.GetNodeOrNull<StaticBody3D>(
                stair.Name + "_Landing77");
            if (ramp is null || landing is null ||
                ramp.CollisionLayer != 1u ||
                ramp.GetChildren().OfType<CollisionShape3D>().Count() != 1 ||
                !ramp.GetChildren().OfType<MeshInstance3D>().Any() ||
                landing.GetChildren().OfType<CollisionShape3D>().Count() != 1)
                throw new InvalidOperationException(
                    "Pass77 walkable stair ramp or connected upper deck missing " +
                    stair.Name);
            var rise = stair.UpperFeet.Y - stair.LowerFeet.Y;
            if (rise < 2.3f || rise > 3.5f)
                throw new InvalidOperationException(
                    "Pass77 illegal fallback stair elevation " + stair.Name);
        }
        await Frames(80);
        if (!nav.IsGraphReady || nav.PointCount < 200)
            throw new InvalidOperationException(
                "Pass77 graph was not populated on real Manor scene");
        game.SetProcess(false); // avoid unrelated wave spawns

        var front = stairs.Single(x => x.Name == "FrontHallUpper");
        // Move ONLY the starting position for the isolated test: ascent
        // itself comes from real W-key input, physics, and collision.
        player.Rotation = Vector3.Zero;
        player.GlobalPosition = front.LowerFeet +
            Vector3.Up*.85f + new Vector3(0,0,1.0f);
        await Frames(10);
        var start = player.GlobalPosition;
        if (!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass77 player was not physically grounded at stair entrance");

        var highest = start.Y;
        SetKey(Key.W,true);
        for (var frame=0;frame<122;frame++)
        {
            await Frames(1);
            highest = Math.Max(highest,player.GlobalPosition.Y);
        }
        SetKey(Key.W,false);
        await Frames(10);
        var final = player.GlobalPosition;
        if (highest-start.Y < 2.0f ||
            final.Z > front.UpperFeet.Z + 1.8f ||
            !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass77 keyboard ascent did not climb physical ramp " +
                "start=" + start + " maxY=" + highest +
                " end=" + final + " floor=" + player.IsOnFloor());
        GD.Print("TWR_PASS77_REAL_PLAYER_STAIRS_OK " +
            "rise=" + (highest-start.Y).ToString("F2") +
            " end=" + final + " grounded=true flights=3");

        player.GlobalPosition = front.UpperFeet +
            Vector3.Up*.87f + Vector3.Back*.15f;
        await Frames(6);
        if (!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass77 balcony standing pose was not solidly supported");

        var below = front.LowerFeet +
            Vector3.Up*.85f + Vector3.Back*.85f;
        var route = nav.GetRoute(below, player.GlobalPosition);
        if (route.Length < 10 || nav.VerifiedElevatedRoutes77 < 1 ||
            route[^1].Y < front.UpperFeet.Y-.12f)
            throw new InvalidOperationException(
                "Pass77 physics-audited lower-to-upper route was rejected " +
                "points=" + route.Length +
                " success=" + nav.VerifiedElevatedRoutes77 +
                " rejects=" + nav.RejectedElevatedRoutes77);
        for (var i=1;i<route.Length;i++)
        {
            if (route[i].Y < route[i-1].Y-.50f ||
                route[i].DistanceTo(route[i-1]) > 6.0f)
                throw new InvalidOperationException(
                    "Pass77 route had a discontinuous vertical jump/teleport");
        }

        // Spawn a genuine collision-driven infected once, on the stair's
        // actual lower approach. No per-frame position/velocity assignment.
        var infected = new InfectedAgent
        {
            Name = "Pass77RealManorStairPursuer",
            InfectedType = "Civilian",
            Target = player,
            Runtime = Runtime,
            GroundNavigator73 = nav,
            Health = 500f,
            Damage = 0f,
            MoveSpeed = 5.0f,
            Position = below
        };
        game.AddChild(infected);
        var infectedStartY = infected.GlobalPosition.Y;
        var infectedMaxY = infectedStartY;
        for (var frame=0;frame<235;frame++)
        {
            await Frames(1);
            infectedMaxY = Math.Max(infectedMaxY, infected.GlobalPosition.Y);
        }
        if (infectedMaxY - infectedStartY < 1.65f ||
            infected.SourceJumpAttempts != 0)
            throw new InvalidOperationException(
                "Pass77 infected failed real slope-physics ascent " +
                "start=" + infectedStartY +
                " max=" + infectedMaxY +
                " location=" + infected.GlobalPosition +
                " jumpAttempts=" + infected.SourceJumpAttempts);
        GD.Print("TWR_PASS77_REAL_INFECTED_STAIRS_OK " +
            "route_points=" + route.Length +
            " actual_rise=" + (infectedMaxY-infectedStartY).ToString("F2") +
            " source_jump_attempts=0");

        Runtime.FailMatch("Pass77 test-only Manor floor traversal");
        if (Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass77 domain results were not saved safely");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass77 failed to restore isolated offline lobby");
    }
}

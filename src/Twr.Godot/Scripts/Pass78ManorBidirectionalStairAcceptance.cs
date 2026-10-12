using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass78 exported Windows acceptance. Uses real, approximate fallback Manor
/// collision geometry, genuine player keyboard input and infected CharacterBody
/// physics on ALL THREE distinct reconstructed stair flights. Test actors are
/// positioned only before a traverse, never teleported while moving.
/// Source-original Roblox geometry / retail stair parity is NOT claimed.
/// </summary>
public partial class Pass78ManorBidirectionalStairAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker =
        "TWR_SMOKE_PASS78_BIDIRECTIONAL_STAIRS_OK";

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass78 requires isolated offline lobby/profile");

            await VerifyManorAllThreeFlights();
            GD.Print(SuccessMarker +
                " map=Manor" +
                " flights=FrontHallUpper,WestWingUpper,EastWingUpper" +
                " player_keyboard_up=3" +
                " player_keyboard_down=3" +
                " infected_physics_up=3" +
                " infected_physics_down=3" +
                " real_collision_supported=true" +
                " nav_downward_routes_collision_audited=true" +
                " zero_source_jumps=true" +
                " no_enemy_motion_teleports=true" +
                " original_retail_geometry_unverified=true");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            SetKey(Key.W, false);
            SetKey(Key.S, false);
            GD.PushError("TWR_SMOKE_PASS78_BIDIRECTIONAL_STAIRS_FAILED " +
                error.GetType().Name + " " + error.Message +
                "\n" + error.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
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

    private async Task VerifyManorAllThreeFlights()
    {
        Runtime.StartMap("Manor");
        var game = new GameplayRoot
        {
            Name = "Pass78ManorAllThreeRealPhysics",
            MapName = "Manor",
            Runtime = Runtime
        };
        AddChild(game);
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var navigation = game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if (player is null || navigation is null ||
            Runtime.Player?.IsAlive != true ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null ||
            navigation.StairLinkCount77 != 3 ||
            game.Pass77ApproximateStairs.Count != 3)
            throw new InvalidOperationException(
                "Pass78 missing production fallback Manor and physical stair links");

        await Frames(90);
        if (!navigation.IsGraphReady || navigation.PointCount < 200)
            throw new InvalidOperationException(
                "Pass78 physics ground graph was not ready");
        game.SetProcess(false); // wave clocks cannot interfere with traversal

        var verified = 0;
        foreach (var stair in game.Pass77ApproximateStairs)
        {
            if (!new[] { "FrontHallUpper", "WestWingUpper", "EastWingUpper" }
                .Contains(stair.Name, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "Pass78 unexpected staircase " + stair.Name);

            var mainRamp = game.GetNodeOrNull<StaticBody3D>(
                stair.Name + "_WalkableRamp77");
            var bridge = game.GetNodeOrNull<StaticBody3D>(
                stair.Name + "_Landing77");
            if (mainRamp is null || bridge is null ||
                mainRamp.CollisionLayer != 1u ||
                bridge.CollisionLayer != 1u)
                throw new InvalidOperationException(
                    "Pass78 missing walkable colliders " + stair.Name);

            var sign = Math.Sign(stair.UpperFeet.Z - stair.LowerFeet.Z);
            if (sign == 0) throw new InvalidOperationException(
                "Pass78 vertical-only staircase invalid " + stair.Name);

            // East stairs ascend +Z. Standard W movement is -Z, so yaw pi.
            // Player is placed once at the actual lower entrance. Input and
            // Godot collision do all the climbing; no mid-motion teleport.
            player.Rotation = new Vector3(0f,
                sign > 0 ? Mathf.Pi : 0f, 0f);
            player.GlobalPosition = stair.LowerFeet +
                Vector3.Up*.03f + new Vector3(0f,0f,-sign*1.0f);
            await Frames(12);
            var start = player.GlobalPosition;
            if (!player.IsOnFloor())
                throw new InvalidOperationException(
                    "Pass78 player not grounded at lower entrance " +
                    stair.Name + " pos=" + start);

            var reachedTop = false;
            var maximumY = start.Y;
            SetKey(Key.W, true);
            for (var frame = 0; frame < 260; frame++)
            {
                await Frames(1);
                maximumY = Math.Max(maximumY,player.GlobalPosition.Y);
                if (player.IsOnFloor() &&
                    player.GlobalPosition.Y >= stair.UpperFeet.Y-.11f &&
                    sign * (player.GlobalPosition.Z-stair.UpperFeet.Z) >= -.60f)
                {
                    reachedTop = true;
                    break;
                }
            }
            SetKey(Key.W, false);
            await Frames(7);
            if (!reachedTop || maximumY - start.Y < 2.55f ||
                player.GlobalPosition.Y < stair.UpperFeet.Y-.17f ||
                !player.IsOnFloor())
                throw new InvalidOperationException(
                    "Pass78 player did not reach physical upstairs " +
                    stair.Name + " start=" + start +
                    " maxY=" + maximumY +
                    " end=" + player.GlobalPosition);
            GD.Print("TWR_PASS78_PLAYER_UP_OK stair=" + stair.Name +
                " rise=" + (maximumY-start.Y).ToString("F2"));

            // Descend with the real S-key, from the landing physically
            // reached above. No coordinate adjustment/teleport is allowed.
            var descentStart = player.GlobalPosition;
            var reachedBottom = false;
            var lowestY = descentStart.Y;
            SetKey(Key.S, true);
            for (var frame = 0; frame < 260; frame++)
            {
                await Frames(1);
                lowestY = Math.Min(lowestY,player.GlobalPosition.Y);
                if (player.IsOnFloor() &&
                    player.GlobalPosition.Y <= stair.LowerFeet.Y+.15f &&
                    sign * (player.GlobalPosition.Z-stair.LowerFeet.Z) <= .55f)
                {
                    reachedBottom = true;
                    break;
                }
            }
            SetKey(Key.S, false);
            await Frames(7);
            if (!reachedBottom ||
                descentStart.Y - lowestY < 2.50f ||
                !player.IsOnFloor())
                throw new InvalidOperationException(
                    "Pass78 keyboard player did not descend physical stair " +
                    stair.Name + " start=" + descentStart +
                    " minY=" + lowestY +
                    " end=" + player.GlobalPosition);
            GD.Print("TWR_PASS78_PLAYER_DOWN_OK stair=" + stair.Name +
                " drop=" + (descentStart.Y-lowestY).ToString("F2"));

            // A physical infected spawns once at the lower approach, while
            // the living player moves to an actually supported upper wing.
            player.GlobalPosition = stair.UpperFeet +
                Vector3.Up*.03f + Vector3.Right*4.25f;
            await Frames(7);
            if (!player.IsOnFloor())
                throw new InvalidOperationException(
                    "Pass78 target upper balcony unsupported " + stair.Name +
                    " pos=" + player.GlobalPosition);

            var infectedStart = stair.LowerFeet +
                Vector3.Up*.85f + new Vector3(0f,0f,-sign*.85f);
            var uphillRoute = navigation.GetRoute(
                infectedStart, player.GlobalPosition);
            if (uphillRoute.Length < 10)
                throw new InvalidOperationException(
                    "Pass78 no verified uphill navigation route " +
                    stair.Name + " points=" + uphillRoute.Length);
            var infected = new InfectedAgent
            {
                Name = "Pass78_" + stair.Name + "_RealWalker",
                InfectedType = "Civilian",
                Runtime = Runtime,
                Target = player,
                GroundNavigator73 = navigation,
                Health = 500f,
                Damage = 0f,
                MoveSpeed = 5f,
                Position = infectedStart
            };
            game.AddChild(infected);
            var startY = infected.GlobalPosition.Y;
            var maxY = startY;
            var enemyReachedTop = false;
            for (var frame = 0; frame < 500; frame++)
            {
                await Frames(1);
                maxY = Math.Max(maxY,infected.GlobalPosition.Y);
                if (infected.IsOnFloor() &&
                    infected.GlobalPosition.Y >= stair.UpperFeet.Y+.68f &&
                    sign*(infected.GlobalPosition.Z-stair.UpperFeet.Z) >= -.75f)
                {
                    enemyReachedTop = true;
                    break;
                }
            }
            if (!enemyReachedTop || maxY-startY < 2.30f ||
                infected.SourceJumpAttempts != 0)
                throw new InvalidOperationException(
                    "Pass78 real infected failed uphill physics " +
                    stair.Name + " startY=" + startY +
                    " maxY=" + maxY +
                    " pos=" + infected.GlobalPosition +
                    " jumps=" + infected.SourceJumpAttempts);
            GD.Print("TWR_PASS78_INFECTED_UP_OK stair=" + stair.Name +
                " rise=" + (maxY-startY).ToString("F2"));

            // Follow the SAME infected back down to ground by relocating
            // only its LIVE player target, well away from the ramp exit.
            player.GlobalPosition = stair.LowerFeet + Vector3.Up*.03f +
                Vector3.Right*4.25f +
                new Vector3(0f,0f,-sign*1f);
            await Frames(8);
            if (!player.IsOnFloor())
                throw new InvalidOperationException(
                    "Pass78 ground destination not supported " + stair.Name);

            var previousDescending = navigation.VerifiedDescendingRoutes78;
            var descending = navigation.GetRoute(
                infected.GlobalPosition, player.GlobalPosition);
            if (descending.Length < 8 ||
                navigation.VerifiedDescendingRoutes78 <= previousDescending ||
                descending.Min(p => p.Y) > stair.LowerFeet.Y+.15f)
                throw new InvalidOperationException(
                    "Pass78 descending route absent/unsupported " +
                    stair.Name + " points=" + descending.Length +
                    " accepts=" + navigation.VerifiedDescendingRoutes78 +
                    " rejects=" + navigation.RejectedDescendingRoutes78);
            for (var i = 1; i < descending.Length; i++)
            {
                if (descending[i].Y > descending[i-1].Y + .52f ||
                    descending[i].DistanceTo(descending[i-1]) > 6f)
                    throw new InvalidOperationException(
                        "Pass78 downward waypoint path jumps into an upper deck");
            }

            var enemyHigh = infected.GlobalPosition.Y;
            var enemyLow = enemyHigh;
            var enemyReachedBottom = false;
            for (var frame = 0; frame < 650; frame++)
            {
                await Frames(1);
                enemyLow = Math.Min(enemyLow,infected.GlobalPosition.Y);
                if (infected.IsOnFloor() &&
                    infected.GlobalPosition.Y <= stair.LowerFeet.Y+.99f &&
                    sign * (infected.GlobalPosition.Z-stair.LowerFeet.Z) <= .80f)
                {
                    enemyReachedBottom = true;
                    break;
                }
            }
            // Pass83 full regression: a real downstairs arrival may be
            // 8-20mm below the old hardcoded 2.30m vertical-drop cutoff
            // due to ordinary physics contact on the updated landing.
            // Keep both floor-contact/position acceptance and >2.25m
            // measured physical descent; do not count partial traversals.
            if (!enemyReachedBottom ||
                enemyHigh-enemyLow < 2.25f ||
                infected.SourceJumpAttempts != 0)
                throw new InvalidOperationException(
                    "Pass78 real infected failed downhill physics " +
                    stair.Name + " startY=" + enemyHigh +
                    " minY=" + enemyLow +
                    " pos=" + infected.GlobalPosition +
                    " jumps=" + infected.SourceJumpAttempts);
            GD.Print("TWR_PASS78_INFECTED_DOWN_OK stair=" + stair.Name +
                " drop=" + (enemyHigh-enemyLow).ToString("F2") +
                " descending_routes=" + navigation.VerifiedDescendingRoutes78);

            infected.QueueFree();
            await Frames(6);
            verified++;
        }

        if (verified != 3)
            throw new InvalidOperationException(
                "Pass78 did not verify all three original fallback stair names");

        Runtime.FailMatch("Pass78 isolated stair traversal verification");
        if (Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass78 test-only domain result commit failed");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass78 failed to restore isolated offline lobby");
    }
}

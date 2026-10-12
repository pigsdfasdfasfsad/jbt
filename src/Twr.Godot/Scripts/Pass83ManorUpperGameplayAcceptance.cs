using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass83: actual exported Windows / Godot physics acceptance for the
/// reconstructed (NOT source-original) upper Manor gameplay. The navigation
/// test verifies obstruction rejection, path construction, and actual enemy
/// collision movement across all three crosswalks. E-key pickup interaction
/// remains under the existing domain authority and wall/vertical gates.
/// </summary>
public partial class Pass83ManorUpperGameplayAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker =
        "TWR_SMOKE_PASS83_MANOR_UPPER_GAMEPLAY_OK";

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass83 requires an isolated offline test profile");

            await TestAllThreeUpperWings();
            GD.Print(SuccessMarker +
                " map=Manor" +
                " wings=3" +
                " collision_rejected_voids=3" +
                " collision_verified_crossings=3" +
                " real_infected_physics_crossings=3" +
                " physically_supported_upper_loot=3" +
                " upstairs_interaction_vertical_guards=3" +
                " retail_mesh_fidelity_unverified=true");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            KeyInput(Key.E,false);
            GD.PushError("TWR_SMOKE_PASS83_MANOR_UPPER_GAMEPLAY_FAILED " +
                error.GetType().Name + " " + error.Message +
                "\n" + error.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static void KeyInput(Key key,bool down)
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode=key,
            PhysicalKeycode=key,
            Pressed=down
        });
    }

    private async Task PressE()
    {
        KeyInput(Key.E,true);
        await Frames(3);
        KeyInput(Key.E,false);
        await Frames(3);
    }

    private static StaticBody3D BlockCrossing(
        GameplayRoot game, Pass80UpperCrossing bridge)
    {
        var obstacle = new StaticBody3D
        {
            Name="Pass83SyntheticCrosswalkBarrier",
            Position=bridge.CentreFeet+Vector3.Up*.88f,
            CollisionLayer=1,
            CollisionMask=0
        };
        obstacle.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D
            {
                Size=new Vector3(1.25f,1.76f,2.15f)
            }
        });
        game.AddChild(obstacle);
        return obstacle;
    }

    private async Task TestAllThreeUpperWings()
    {
        Runtime.StartMap("Manor");
        var game=new GameplayRoot
        {
            Name="Pass83RealFallbackManor",
            MapName="Manor",
            Runtime=Runtime
        };
        AddChild(game);

        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if(player is null || nav is null ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null ||
            Runtime.Player?.IsAlive != true ||
            game.Pass77ApproximateStairs.Count != 3 ||
            nav.CrossingCount80 != 3 ||
            game.Pass82UpperPickupMarkers.Count != 3 ||
            game.Pass82UpperObjectiveMarkers.Count != 3)
            throw new InvalidOperationException(
                "Pass83 expected only three actual fallback Manor upper wings");

        await Frames(90);
        if(!nav.IsGraphReady || nav.PointCount < 200)
            throw new InvalidOperationException(
                "Pass83 collision-derived ground navigation not ready");
        game.SetProcess(false); // no wave clock spawns during physical tests

        // The production opening supply roll is randomized. Remove those
        // opening test-scene pickups so a natural pickup at the exact same
        // marker cannot win the stable nearest-E-key tie-breaker. The
        // actual pickup authority and domain command remain unmodified.
        foreach(var node in GetTree().GetNodesInGroup("twr-pickup"))
            if(node is PickupActor ordinary &&
                ReferenceEquals(ordinary.Player,player))
                ordinary.QueueFree();
        await Frames(3);

        var crossings=game.Pass77ApproximateStairs
            .Select(stair => new
            {
                Stair=stair,
                Crossing=MapCrossing(stair,game)
            }).ToArray();

        for(var index=0;index<crossings.Length;index++)
        {
            var stair=crossings[index].Stair;
            var bridge=crossings[index].Crossing;
            var name=stair.Name;
            var catwalk=game.GetNodeOrNull<StaticBody3D>(
                name+"_UpperCrossing80");
            if(catwalk is null || catwalk.CollisionLayer!=1u ||
                !catwalk.GetChildren().OfType<CollisionShape3D>()
                    .Any(shape => shape.Shape is BoxShape3D))
                throw new InvalidOperationException(
                    "Pass83 missing real upper walkway collider: "+name);

            // Same player and infected physics bodies as normal gameplay.
            // Diagonal approach is out of alignment with the narrow 1.2m
            // crosswalk: direct pursuit must fail the floor support audit.
            var awayFromBridge=-Math.Sign(
                stair.UpperFeet.Z-stair.LowerFeet.Z)*2.0f;
            var leftFeet=bridge.LeftFeet + Vector3.Left*.75f +
                new Vector3(0,.04f,awayFromBridge);
            var rightFeet=bridge.RightFeet + Vector3.Right*.75f +
                new Vector3(0,.04f,awayFromBridge);
            player.GlobalPosition=rightFeet;
            await Frames(12);
            if(!player.IsOnFloor())
                throw new InvalidOperationException(
                    "Pass83 unsupported upper target "+name+
                    " pos="+player.GlobalPosition);
            var infectedCentre=leftFeet + Vector3.Up*.8f;
            if(nav.UpperCorridorSupported79(leftFeet,rightFeet) ||
                !nav.IsManorSameUpperFloor79(
                    infectedCentre,player.GlobalPosition))
                throw new InvalidOperationException(
                    "Pass83 unsupported diagonal crossed upper void "+name);

            var blocker=BlockCrossing(game,bridge);
            await Frames(6);
            var blocked=nav.GetRoute(
                infectedCentre,player.GlobalPosition);
            if(blocked.Length!=0 ||
                nav.RejectedUpperCrossingRoutes80==0)
                throw new InvalidOperationException(
                    "Pass83 physical wall failed to block elevated route "+name);
            GD.Print("TWR_PASS83_BLOCKED_VOID_OK stair="+name);

            blocker.QueueFree();
            await Frames(7);
            var verified=nav.GetRoute(
                infectedCentre,player.GlobalPosition);
            if(verified.Length < 3 ||
                verified.Length > 4 ||
                nav.VerifiedUpperCrossingRoutes80 == 0 ||
                verified[^1].DistanceTo(player.GlobalPosition)>.12f)
                throw new InvalidOperationException(
                    "Pass83 expected audited 4-stage upper catwalk route "+
                    name+" actual="+verified.Length);
            GD.Print("TWR_PASS83_CROSSING_ROUTE_OK stair="+name+
                " waypoints="+verified.Length);

            var enemy=new InfectedAgent
            {
                Name="Pass83RealUpperWalker_"+name,
                InfectedType="Civilian",
                Runtime=Runtime,
                Target=player,
                GroundNavigator73=nav,
                Health=500f,
                Damage=0f,
                MoveSpeed=5f,
                Position=infectedCentre
            };
            game.AddChild(enemy);
            var startX=enemy.GlobalPosition.X;
            var maxX=startX;
            var reachedRightDeck=false;
            for(var frame=0;frame<900;frame++)
            {
                await Frames(1);
                maxX=Math.Max(maxX,enemy.GlobalPosition.X);
                if(frame%150==0)
                {
                    var now=nav.GetRoute(enemy.GlobalPosition,
                        player.GlobalPosition);
                    var towardCentre=bridge.CentreFeet-
                        (enemy.GlobalPosition-Vector3.Up*.8f);
                    GD.Print("TWR_PASS83_NAV_TELEMETRY stair="+name+
                        " frame="+frame+
                        " enemy="+enemy.GlobalPosition+
                        " next="+(now.Length>0 ? now[0].ToString() : "NONE")+
                        " legs="+now.Length+
                        " safe="+nav.SafeUpperMove79(
                            enemy.GlobalPosition,towardCentre));
                }
                if(enemy.IsOnFloor() &&
                    enemy.GlobalPosition.X >= bridge.RightFeet.X+.05f &&
                    Math.Abs(enemy.GlobalPosition.Y-
                        (stair.UpperFeet.Y+.8f)) < .40f)
                {
                    reachedRightDeck=true;
                    break;
                }
            }
            if(!reachedRightDeck ||
                maxX-startX < 6.8f ||
                enemy.SourceJumpAttempts!=0)
                throw new InvalidOperationException(
                    "Pass83 real enemy failed to cross "+name+
                    " x0="+startX+" xMax="+maxX+
                    " pos="+enemy.GlobalPosition+
                    " verified="+nav.VerifiedUpperCrossingRoutes80+
                    " rejects="+nav.RejectedUpperCrossingRoutes80);
            GD.Print("TWR_PASS83_REAL_INFECTED_CROSSING_OK stair="+name+
                " progress="+(maxX-startX).ToString("F2")+
                " source_jumps="+enemy.SourceJumpAttempts);
            enemy.QueueFree();
            await Frames(5);

            var pickupMarker=game.Pass82UpperPickupMarkers[index];
            var objectiveMarker=game.Pass82UpperObjectiveMarkers[index];
            var pickupFeet=pickupMarker-Vector3.Up*.62f;
            var objectiveFeet=objectiveMarker-Vector3.Up*.35f;
            if(!nav.UpperCorridorSupported79(pickupFeet,pickupFeet) ||
                !nav.UpperCorridorSupported79(objectiveFeet,objectiveFeet) ||
                Math.Abs(pickupFeet.Y-stair.UpperFeet.Y)>.10f ||
                Math.Abs(objectiveFeet.Y-stair.UpperFeet.Y)>.10f)
                throw new InvalidOperationException(
                    "Pass83 elevated items/objectives have no physical floor "+
                    name);
            GD.Print("TWR_PASS83_UPPER_MARKERS_OK stair="+name);

            // The same upper pickup may not be taken by a player standing
            // directly below it on the lower floor.
            var medkit=new PickupActor
            {
                Name="Pass83UpperMedkit_"+name,
                PickupType="Medkit",
                Player=player,
                Runtime=Runtime,
                Position=pickupMarker
            };
            game.AddChild(medkit);
            player.GlobalPosition=new Vector3(
                pickupMarker.X,.24f,pickupMarker.Z);
            await Frames(10);
            if(Pass76InteractionSafety.CanReach(
                    player,pickupMarker,2.5f))
                throw new InvalidOperationException(
                    "Pass83 permitted loot through the upstairs floor "+name);

            player.GlobalPosition=pickupFeet+
                Vector3.Back*.75f+Vector3.Up*.05f;
            await Frames(12);
            if(!player.IsOnFloor() ||
                !Pass76InteractionSafety.CanReach(
                    player,pickupMarker,2.5f))
                throw new InvalidOperationException(
                    "Pass83 upper-floor pickup unreachable "+name);
            var previousHealth=Runtime.Player!.Health;
            Runtime.DamagePlayer(25f,"Pass83 test-only controlled damage",
                bypassArmor:true);
            var damagedHealth=Runtime.Player.Health;
            if(damagedHealth >= previousHealth ||
                damagedHealth <= 0)
                throw new InvalidOperationException(
                    "Pass83 unable to prepare authority-only medkit test");
            await PressE();
            if(GodotObject.IsInstanceValid(medkit) &&
                !medkit.IsQueuedForDeletion() ||
               Runtime.Player.Health <= damagedHealth)
                throw new InvalidOperationException(
                    "Pass83 real E-key upper pickup failed "+name);
            GD.Print("TWR_PASS83_PICKUP_FLOOR_GUARD_OK stair="+name+
                " health_before="+damagedHealth+
                " health_after="+Runtime.Player.Health);
        }

        Runtime.FailMatch("Pass83 isolated upper-floor gameplay acceptance");
        if(Runtime.Match?.Phase != MatchPhase.Results ||
            Runtime.Match.SaveCommitted != true)
            throw new InvalidOperationException(
                "Pass83 authoritative result save did not commit");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass83 failed to restore offline lobby");
    }

    private static Pass80UpperCrossing MapCrossing(
        Pass77StairLink stair,GameplayRoot game)
    {
        // The source-oriented geometry and these test coordinates are
        // both derived from the same reconstructed staircase endpoints.
        var sign=Math.Sign(stair.UpperFeet.Z-stair.LowerFeet.Z);
        var mid=stair.UpperFeet+
            new Vector3(0,0f,sign*.70f);
        return new Pass80UpperCrossing(stair.Name,mid,
            mid+Vector3.Left*3.50f,
            mid+Vector3.Right*3.50f);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass89 exported Windows, production Godot survivor movement, isolated
/// profile. One injected Ranch barrier must force a real Escort NPC detour;
/// a second post-graph barrier must invalidate an otherwise cached route.
/// No forced NPC transform or test-only navigation substitute is used.
/// </summary>
public partial class Pass89EscortNavigationAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker=
        "TWR_SMOKE_PASS89_ESCORT_PHYSICAL_NAVIGATION_OK";

    public override async void _Ready()
    {
        try
        {
            if(Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass89 requires isolated local session at lobby");
            await TestRanchRealEscortNavigation();
            GD.Print(SuccessMarker+
                " map=Ranch" +
                " collision_nav=pass73" +
                " physically_supported_waypoints=true" +
                " original_source_geometry_unverified=true" +
                " no_survivor_teleport=true" +
                " no_test_only_reward_grants=true");
            GetTree().Quit(0);
        }
        catch(Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS89_ESCORT_PHYSICAL_NAVIGATION_FAILED "+
                ex.GetType().Name+" "+ex.Message+"\n"+ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int frames)
    {
        for(var i=0;i<frames;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static StaticBody3D Wall(GameplayRoot game,
        string name,Vector3 center,Vector3 size)
    {
        var wall=new StaticBody3D
        {
            Name=name,
            Position=center,
            CollisionLayer=1u,
            CollisionMask=0u
        };
        wall.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D{Size=size}
        });
        game.AddChild(wall);
        return wall;
    }

    private async Task TestRanchRealEscortNavigation()
    {
        Runtime.StartMap("Ranch");
        var game=new GameplayRoot
        {
            Name="Pass89ActualRanchGameplay",
            MapName="Ranch",
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if(player is null || nav is null ||
            game.GetNodeOrNull<GameplayHud>("HUD") is null ||
            Runtime.Player?.IsAlive != true)
            throw new InvalidOperationException(
                "Pass89 missing production gameplay/player/ground navigator");
        game.SetProcess(false); // no wave spawner: physics is still LIVE

        // Real static wall, not a visual mesh. Spawn it BEFORE graph
        // construction so Pass73 learns the physically blocked cells.
        var wall=Wall(game,"Pass89RanchSolidDivider",
            new Vector3(0,1.50f,16f),
            new Vector3(14f,3f,1.10f));
        await Frames(90);
        if(!nav.IsGraphReady || nav.PointCount<150 ||
           !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass89 Ranch physics graph/player not ready");

        var from=new Vector3(0,.05f,27f);
        var goal=new Vector3(0,.15f,3f);
        var excludes=new global::Godot.Collections.Array<Rid>
        {
            player.GetRid()
        };
        if(Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),from,goal,excludes))
            throw new InvalidOperationException(
                "Pass89 synthetic solid wall did not obstruct direct route");
        var valid=nav.GetVerifiedEscortRoute89(from,goal,new Rid());
        if(valid.Length<3 || !valid.Any(
                waypoint=>Math.Abs(waypoint.X)>7.0f))
            throw new InvalidOperationException(
                "Pass89 collision graph did not find around-wall route"+
                " count="+valid.Length+
                " furthestX="+(valid.Length==0 ? 0f :
                    valid.Max(p=>Math.Abs(p.X))));
        GD.Print("TWR_PASS89_SOLID_WALL_REJECT_OK"+
            " direct=false route_waypoints="+valid.Length);

        // New obstacle after static A* graph sampling must NOT silently
        // authorize its previously cached/accepted route through concrete.
        var liveBlocker=Wall(game,"Pass89RuntimeObstruction",
            new Vector3(0,1.50f,16f),
            new Vector3(95f,3f,1.50f));
        await Frames(4);
        var blocked=nav.GetVerifiedEscortRoute89(from,goal,new Rid());
        if(blocked.Length!=0)
            throw new InvalidOperationException(
                "Pass89 graph failed live physics invalidation"+
                " route="+blocked.Length);
        GD.Print("TWR_PASS89_LIVE_ROUTE_INVALIDATED_OK"+
            " rejected_waypoints=true");
        liveBlocker.QueueFree();
        await Frames(4);

        // This is the actual gameplay ObjectiveRuntime and CharacterBody3D,
        // not a simulated position or fake escort rig.
        var objective=new ObjectiveRuntime
        {
            Name="Pass89ProductionEscort",
            ObjectiveId="Ranch:Pass89:EscortAcceptance",
            Family="Escort",
            Runtime=Runtime,
            Player=player,
            UseFallbackEscortNavigation89=true,
            EscortGroundNavigator89=nav,
            Position=from
        };
        game.AddChild(objective);
        var survivor=objective.GetNodeOrNull<CharacterBody3D>(
            "EscortSurvivor");
        var destination=objective.GetNodeOrNull<Node3D>(
            "EscortDestination");
        if(survivor is null || destination is null)
            throw new InvalidOperationException(
                "Pass89 original Escort survivor/destination actor missing");

        await Frames(8);
        if(!survivor.IsOnFloor() ||
            objective.EscortSafeRoutePlans89<1 ||
            objective.EscortDestinationAdjusted89 ||
            Math.Abs(destination.GlobalPosition.Z-goal.Z)>.3f ||
            Math.Abs(destination.GlobalPosition.X)>.3f)
            throw new InvalidOperationException(
                "Pass89 failed to retain physically supported -24Z goal"+
                " routes="+objective.EscortSafeRoutePlans89+
                " adjusted="+objective.EscortDestinationAdjusted89+
                " dest="+destination.GlobalPosition);

        var start=survivor.GlobalPosition;
        var furthestX=Math.Abs(start.X);
        var closestZ=start.Z;
        var clearedWall=false;
        for(var frame=0;frame<450;frame++)
        {
            await Frames(1);
            if(!GodotObject.IsInstanceValid(survivor) ||
               survivor.IsQueuedForDeletion())
                throw new InvalidOperationException(
                    "Pass89 original Escort survivor disposed mid-route");
            var at=survivor.GlobalPosition;
            furthestX=Math.Max(furthestX,Math.Abs(at.X));
            closestZ=Math.Min(closestZ,at.Z);
            // The capsule cannot enter the wall's actual occupied volume.
            if(Math.Abs(at.Z-16f)<.74f && Math.Abs(at.X)<7.18f)
                throw new InvalidOperationException(
                    "Pass89 Escort clipped through real static wall"+
                    " position="+at);
            if(at.Z<14.45f && survivor.IsOnFloor())
            {
                clearedWall=true;
                break;
            }
        }
        if(!clearedWall || furthestX<7.0f ||
           start.Z-closestZ<12f ||
           objective.EscortSafeSteps89<30 ||
           objective.EscortBlockedSteps89>300)
            throw new InvalidOperationException(
                "Pass89 Escort did not physically traverse wall detour"+
                " maxX="+furthestX+" minZ="+closestZ+
                " safeSteps="+objective.EscortSafeSteps89+
                " blockedSteps="+objective.EscortBlockedSteps89+
                " replans="+objective.EscortSafeRoutePlans89);
        GD.Print("TWR_PASS89_REAL_ESCORT_DETOUR_OK"+
            " detour_lateral="+furthestX.ToString("F2")+
            " progress_z="+(start.Z-closestZ).ToString("F2")+
            " safe_steps="+objective.EscortSafeSteps89+
            " physical_body=true");

        Runtime.FailMatch("Pass89 isolated native escort-navigation check");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass89 result-save authority failed");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass89 isolated lobby return failed");
    }
}

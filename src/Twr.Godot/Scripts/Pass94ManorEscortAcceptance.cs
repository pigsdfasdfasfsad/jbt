using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass94 exported Windows production Manor Wave5: a real survivor crosses
/// an upper catwalk, descends the reconstructed physical staircase, then
/// navigates the solid ground route and earns exactly one domain payout.
/// Only public fallback geometry is supported, not owner-original scenes.
/// </summary>
public partial class Pass94ManorEscortAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;}=null!;

    public override async void _Ready()
    {
        try
        {
            await VerifyManorEscort();
            GD.Print("TWR_SMOKE_PASS94_MANOR_STAIR_ESCORT_OK"+
                " map=Manor wave=5 actual_physical_descent=true"+
                " retail_map_and_animation_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS94_MANOR_STAIR_ESCORT_FAILED "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    // Test-only explanation of a failed full-capsule route leg.
    // It NEVER authorizes movement or weakens Pass89's production checks.
    private static string ExplainBlockedLeg94(
        World3D world,Vector3 from,Vector3 to,
        global::Godot.Collections.Array<Rid> exclude)
    {
        var space=world.DirectSpaceState;
        using var capsule=new CapsuleShape3D
        {
            Radius=.42f,Height=1.8f
        };
        using var query=new PhysicsShapeQueryParameters3D
        {
            Shape=capsule,
            Transform=new Transform3D(
                Basis.Identity,from+Vector3.Up*.99f),
            Motion=to-from,
            CollideWithBodies=true,
            CollisionMask=1u,
            Exclude=exclude
        };
        var cast=space.CastMotion(query);
        var safeFraction=cast.Length>0 ? cast[0] : -1f;
        var wallMessage="NONE";
        foreach(var height in new[]{.48f,.96f})
        {
            using var wall=PhysicsRayQueryParameters3D.Create(
                from+Vector3.Up*height,
                to+Vector3.Up*height);
            wall.CollisionMask=1u;
            wall.Exclude=exclude;
            var wallHit=space.IntersectRay(wall);
            if(wallHit.Count>0)
            {
                wallMessage="height="+height+
                    " hit="+((Vector3)wallHit["position"])+
                    " normal="+((Vector3)wallHit["normal"]);
                break;
            }
        }
        var firstFloorFailure="NONE";
        var planar=to-from;
        planar.Y=0f;
        var side=planar.LengthSquared()>.0001f
            ? new Vector3(-planar.Z,0,planar.X).Normalized()
            : Vector3.Right;
        // 50mm samples reveal even sub-cell floor gaps and verify
        // the actual capsule footprint, not just the centreline.
        for(var i=0;i<=28;i++)
        {
            var expected=from.Lerp(to,i/28f);
            foreach(var offset in new[]{0f,-.22f,.22f})
            {
                var at=expected+side*offset;
                using var ray=PhysicsRayQueryParameters3D.Create(
                    at+Vector3.Up*.68f,
                    at+Vector3.Down*.84f);
                ray.CollisionMask=1u;
                ray.Exclude=exclude;
                var hit=space.IntersectRay(ray);
                if(hit.Count==0)
                {
                    firstFloorFailure="missing t="+(i/28f).ToString("F3")+
                        " at="+at;
                    break;
                }
                var floor=(Vector3)hit["position"];
                var normal=(Vector3)hit["normal"];
                if(normal.Y<.72f ||
                   Math.Abs(floor.Y-expected.Y)>.47f)
                {
                    firstFloorFailure="mismatch t="+
                        (i/28f).ToString("F3")+
                        " expected="+expected+
                        " floor="+floor+" normal="+normal;
                    break;
                }
            }
            if(firstFloorFailure!="NONE")break;
        }
        return "capsuleCastSafe="+safeFraction.ToString("F4")+
            " torsoWall="+wallMessage+
            " fineFloor="+firstFloorFailure;
    }

    private async Task VerifyManorEscort()
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby || Runtime.Session is null)
            throw new InvalidOperationException(
                "Pass94 requires an isolated offline profile");
        Runtime.StartMap("Manor");
        var game=new GameplayRoot
        {
            Name="Pass94RealManor",MapName="Manor",Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if(player is null || nav is null ||
           Runtime.Player?.IsAlive!=true ||
           game.Pass77ApproximateStairs.Count!=3 ||
           game.Pass82UpperObjectiveMarkers.Count!=3)
            throw new InvalidOperationException(
                "Pass94 expects original Pass77/82 fallback Manor topology");
        for(var frame=0;frame<360 && !nav.IsGraphReady;frame++)
            await Frames(1);
        for(var frame=0;frame<120 && !player.IsOnFloor();frame++)
            await Frames(1);
        if(!nav.IsGraphReady || !player.IsOnFloor() ||
           nav.EscortSafeEdgeCount91<10)
            throw new InvalidOperationException(
                "Pass94 physics graph not ready nodes="+nav.PointCount+
                " escortEdges="+nav.EscortSafeEdgeCount91);

        // Normal production wave transitions, with only stage timers
        // fast-forwarded by the isolated Windows smoke harness.
        for(var wave=1;wave<5;wave++)
        {
            if(Runtime.Match?.Wave!=wave || game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass94 unexpected countdown wave "+wave);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            game._Process(RegularWaveRules.WaveDurationSeconds+.25);
            game._Process(RegularWaveRules.WaveEndSeconds+.25);
            game._Process(RegularWaveRules.IntermissionSeconds+.25);
            if(Runtime.Match?.Wave!=wave+1 ||
               game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass94 production wave advancement failed "+wave);
            await Frames(2);
        }
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(Runtime.Match?.Wave!=5 || game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass94 could not reach actual Manor Wave5 Escort");

        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Escort");
        if(objective is null || !objective.UseManorElevatedEscort94 ||
           !objective.UseFallbackEscortNavigation89 ||
           !ReferenceEquals(objective.EscortGroundNavigator89,nav) ||
           game.VerifiedFallbackEscortStarts91<1)
            throw new InvalidOperationException(
                "Pass94 did not spawn grounded upper-floor Escort"+
                " verified="+game.VerifiedFallbackEscortStarts91+
                " rejected="+game.RejectedFallbackEscortStarts91);
        var actor=objective.GetNodeOrNull<CharacterBody3D>("EscortSurvivor");
        var goal=objective.GetNodeOrNull<Node3D>("EscortDestination");
        if(actor is null || goal is null)
            throw new InvalidOperationException(
                "Pass94 production NPC/goal not present");

        game.SetProcess(false);
        var initial=actor.GlobalPosition;
        if(initial.Y<2.5f)
            throw new InvalidOperationException(
                "Pass94 Escort did not start on actual upper deck "+initial);
        var stair=game.Pass77ApproximateStairs[2]; // Wave5 front-upper objective
        if(new Vector2(initial.X-stair.UpperFeet.X,
                initial.Z-stair.UpperFeet.Z).Length()>7f)
            throw new InvalidOperationException(
                "Pass94 Wave5 Escort not anchored to front landing");

        // One bounded diagnostic of the unmodified runtime physics map:
        // identifies the *exact* unsupported segment if route generation
        // fails. No collider or NPC position is changed by these probes.
        var bridgeCentre=stair.UpperFeet+
            new Vector3(0f,0f,
                Math.Sign(stair.UpperFeet.Z-stair.LowerFeet.Z)*1.4f);
        var bridgeEntrance=bridgeCentre+Vector3.Left*4.25f;
        var routeExclude=new global::Godot.Collections.Array<Rid>
        {
            actor.GetRid(),player.GetRid()
        };
        var groundedGoal=Pass94ManorEscortRoute.TryGroundGoal(
            game.GetWorld3D(),goal.GlobalPosition,routeExclude);
        var sideLane=stair.UpperFeet+Vector3.Left*3.35f;
        var lowerPath=groundedGoal.HasValue
            ? nav.GetVerifiedEscortRoute89(stair.LowerFeet,
                groundedGoal.Value,actor.GetRid()) : [];
        GD.Print("TWR_PASS94_ROUTE_DIAGNOSTIC"+
            " authoredGoal="+goal.GlobalPosition+
            " groundedGoal="+(groundedGoal.HasValue
                ? groundedGoal.Value.ToString():"NONE")+
            " upperToEntrance="+Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),initial,bridgeEntrance,routeExclude)+
            " entranceToCentre="+Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),bridgeEntrance,bridgeCentre,routeExclude)+
            " centreToStair="+Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),bridgeCentre,stair.UpperFeet,routeExclude)+
            " centreToStairReason="+ExplainBlockedLeg94(
                game.GetWorld3D(),bridgeCentre,stair.UpperFeet,
                routeExclude)+
            " sideEntryToLane="+Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),initial,sideLane,routeExclude)+
            " laneToStair="+Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),sideLane,stair.UpperFeet,routeExclude)+
            " stairFirstStep="+Pass89EscortSurfaceSafety.CanTravel(
                game.GetWorld3D(),stair.UpperFeet,
                stair.LowerFeet.Lerp(stair.UpperFeet,11f/12f),
                routeExclude)+
            " groundRoutePoints="+lowerPath.Length+
            " plannedRoutePoints="+(groundedGoal.HasValue
                ? Pass94ManorEscortRoute.GetVerifiedRoute(
                    nav,initial,groundedGoal.Value,
                    actor.GetRid(),player.GetRid()).Length : 0));

        var creditsBefore=Runtime.Player!.Credits;
        var xpBefore=Runtime.Player.Xp;
        var levelBefore=Runtime.Player.Level;
        var expectedCredits=creditsBefore+4000;
        var expectedXp=xpBefore+3500;
        var expectedLevel=levelBefore;
        while(expectedXp>=ProgressionRules.RequiredForNextLevel(expectedLevel))
        {
            expectedXp-=ProgressionRules.RequiredForNextLevel(expectedLevel);
            expectedLevel++;
            expectedCredits+=ProgressionRules.CreditRewardForLevel(expectedLevel);
        }

        var callbacks=0;
        var objectiveId=objective.ObjectiveId;
        objective.Completed+=_=>callbacks++;
        var minimumFeetY=initial.Y;
        var safeSteps=0;
        var blockedSteps=0;
        var maxTravel=0f;
        var reachedMidRamp=false;
        var last=initial;
        var lastProgressPoint=initial;
        var framesWithoutProgress=0;
        // Real CharacterBody3D physics runs at 60Hz. No NPC teleport,
        // domain state override, objective completion spoof, or wall disabling.
        for(var frame=0;frame<3000;frame++)
        {
            await Frames(1);
            if(callbacks>0)break;
            if(frame==240 && maxTravel<.10f &&
               objective.EscortSafeRoutePlans89==0)
                throw new InvalidOperationException(
                    "Pass94 no viable physical Manor upper route"+
                    " rejectedGoals="+objective.EscortRejectedDestinations90+
                    " blockedPlans="+objective.EscortBlockedRoutePlans89+
                    " actor="+actor.GlobalPosition+
                    " upperRamp="+stair.UpperFeet+
                    " lowerRamp="+stair.LowerFeet);
            if(!GodotObject.IsInstanceValid(actor))
                throw new InvalidOperationException(
                    "Pass94 NPC deleted before real objective completion");
            last=actor.GlobalPosition;
            // Do not mistake a validated A* plan for actual NPC progress.
            // A stalled CharacterBody3D must produce an actionable native
            // failure rather than exiting from Godot's --quit-after timer.
            if(last.DistanceTo(lastProgressPoint)>.60f)
            {
                lastProgressPoint=last;
                framesWithoutProgress=0;
            }
            else
                framesWithoutProgress++;
            if(frame>300 && framesWithoutProgress>300)
                throw new InvalidOperationException(
                    "Pass94 physics survivor motion stalled"+
                    " frame="+frame+
                    " feet="+last+
                    " floor="+actor.IsOnFloor()+
                    " velocity="+actor.Velocity+
                    " waypoint="+objective.EscortNextWaypoint91+
                    " remaining="+objective.EscortRemainingWaypoints91+
                    " routePlans="+objective.EscortSafeRoutePlans89+
                    " routeFailures="+objective.EscortBlockedRoutePlans89+
                    " steps="+objective.EscortSafeSteps89+
                    " blocked="+objective.EscortBlockedSteps89);
            if(frame%180==0)
                GD.Print("TWR_PASS94_MOTION_PROGRESS"+
                    " frame="+frame+
                    " feet="+last+
                    " safeSteps="+objective.EscortSafeSteps89+
                    " blockedSteps="+objective.EscortBlockedSteps89+
                    " next="+objective.EscortNextWaypoint91+
                    " remaining="+objective.EscortRemainingWaypoints91);
            minimumFeetY=Math.Min(minimumFeetY,last.Y);
            maxTravel=Math.Max(maxTravel,last.DistanceTo(initial));
            safeSteps=objective.EscortSafeSteps89;
            blockedSteps=objective.EscortBlockedSteps89;
            if(last.Y>stair.LowerFeet.Y+.65f &&
               last.Y<stair.UpperFeet.Y-.45f &&
               Math.Abs(last.X-stair.LowerFeet.X)<1.5f &&
               last.Z>Math.Min(stair.LowerFeet.Z,stair.UpperFeet.Z)-.6f &&
               last.Z<Math.Max(stair.LowerFeet.Z,stair.UpperFeet.Z)+.6f)
                reachedMidRamp=true;
            // Only test profile player position changes are allowed.
            // Keep a safe follow distance without touching NPC physics.
            if(player.GlobalPosition.DistanceTo(last)>35f ||
               player.GlobalPosition.DistanceTo(last)<2.0f)
                player.GlobalPosition=last+new Vector3(3f,1.12f,7f);
        }
        if(callbacks!=1 ||
           !objective.EscortGroundedCompletion90 ||
           !Runtime.Match.CompletedObjectives.Contains(objectiveId) ||
           !reachedMidRamp || minimumFeetY>1.1f ||
           maxTravel<15f || safeSteps<45)
            throw new InvalidOperationException(
                "Pass94 production Manor Escort failed physically"+
                " callbacks="+callbacks+
                " initial="+initial+
                " last="+last+
                " minY="+minimumFeetY+
                " midRamp="+reachedMidRamp+
                " maxTravel="+maxTravel+
                " safeSteps="+safeSteps+
                " blockedSteps="+blockedSteps+
                " rejectedGoals="+objective.EscortRejectedDestinations90+
                " safePlans="+objective.EscortSafeRoutePlans89+
                " blockedPlans="+objective.EscortBlockedRoutePlans89+
                " invalidations="+objective.EscortRouteInvalidations91+
                " nextWaypoint="+objective.EscortNextWaypoint91+
                " waypointsRemaining="+objective.EscortRemainingWaypoints91+
                " destination="+(GodotObject.IsInstanceValid(goal)
                    ? goal.GlobalPosition.ToString() : "freed")+
                " floor="+actor.IsOnFloor()+
                " velocity="+actor.Velocity);

        if(Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass94 authoritative credit/XP accounting failed");
        Runtime.CompleteObjective(objectiveId,"Escort");
        await Frames(4);
        if(callbacks!=1 ||
           Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass94 repeat objective command paid more than once");

        GD.Print("TWR_PASS94_MANOR_STAIR_ESCORT_REWARD_OK"+
            " credits=4000 xp=3500 repeat_paid=false"+
            " actual_descent="+(initial.Y-minimumFeetY).ToString("F2")+
            " travel="+maxTravel.ToString("F2")+
            " collision_validated_steps="+safeSteps);

        Runtime.FailMatch("Pass94 isolated Manor stair Escort success");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass94 result save failed");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass94 failed to restore offline lobby");
    }
}

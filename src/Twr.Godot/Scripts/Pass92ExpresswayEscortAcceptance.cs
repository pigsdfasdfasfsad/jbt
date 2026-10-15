using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass92: exported Windows real Expressway Wave6 Escort must reach a
/// collision-supported destination and award domain credits/XP once.
/// The original interim highway infected graph must remain present.
/// </summary>
public partial class Pass92ExpresswayEscortAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;

    public override async void _Ready()
    {
        try
        {
            await VerifyRealExpresswayEscort();
            GD.Print("TWR_SMOKE_PASS92_EXPRESSWAY_ESCORT_OK"+
                " map=Expressway wave=6"+
                " original_retail_meshes_unverified=true"+
                " original_roblox_escort_animation_unverified=true");
            // This async _Ready resumes from the PhysicsFrame signal.
            // Exiting synchronously here can tear down managed Godot nodes
            // while the frame event is still being delivered.
            ScheduleQuitOnIdle(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS92_EXPRESSWAY_ESCORT_FAILED "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            ScheduleQuitOnIdle(1);
        }
    }

    private void ScheduleQuitOnIdle(int exitCode)
    {
        // Leave the physics signal dispatcher before Godot performs final
        // managed/native object disposal. Windows CI still requires the
        // actual child-process exit code to be 0 on success.
        GetTree().CallDeferred("quit", exitCode);
    }

    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private async Task VerifyRealExpresswayEscort()
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby || Runtime.Session is null)
            throw new InvalidOperationException(
                "Pass92 requires isolated offline lobby and profile");
        Runtime.StartMap("Expressway");
        var game=new GameplayRoot
        {
            Name="Pass92ProductionExpressway",
            MapName="Expressway",Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var roadNav=game.GetNodeOrNull<ExpresswayNavigationRuntime>(
            "ExpresswayReconstruction/HighwayNavigation");
        var escortNav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass92ExpresswayEscortGroundNavigation");
        if(player is null || roadNav is null || escortNav is null ||
           roadNav.NavigablePoints < 100 ||
           Runtime.Player?.IsAlive != true)
            throw new InvalidOperationException(
                "Pass92 actual Expressway infected-road and Escort graphs missing");

        for(var i=0;i<400 && !escortNav.IsGraphReady;i++)
            await Frames(1);
        for(var i=0;i<120 && !player.IsOnFloor();i++)
            await Frames(1);
        if(!escortNav.IsGraphReady ||
           !player.IsOnFloor() ||
           escortNav.EscortSafeEdgeCount91<50 ||
           !game.Pass92ExpresswayEscortNavReady ||
           game.Pass73FallbackNavReady)
            throw new InvalidOperationException(
                "Pass92 isolated collision map graph was not built"+
                " escortPoints="+escortNav.PointCount+
                " escortEdges="+escortNav.EscortSafeEdgeCount91+
                " legacyRoadPoints="+roadNav.NavigablePoints+
                " player="+player.GlobalPosition);

        // Reach the real catalog's Wave6 Escort via normal stage transitions
        // with TEST-ONLY accelerated clocks; no manual objective or grant.
        for(var wave=1;wave<=5;wave++)
        {
            if(Runtime.Match?.Wave!=wave || game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass92 expected countdown wave="+wave);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            if(game.WaveStage!="WAVE")
                throw new InvalidOperationException(
                    "Pass92 wave start mismatch "+wave);
            game._Process(RegularWaveRules.WaveDurationSeconds+.25);
            game._Process(RegularWaveRules.WaveEndSeconds+.25);
            game._Process(RegularWaveRules.IntermissionSeconds+.25);
            if(Runtime.Match?.Wave!=wave+1 ||
               game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass92 wave progression failed "+wave);
            await Frames(2);
        }

        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(Runtime.Match?.Wave!=6 || game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass92 actual Wave6 failed to start");
        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Escort");
        if(objective is null ||
           !objective.UseFallbackEscortNavigation89 ||
           objective.EscortGroundNavigator89 != escortNav ||
           game.VerifiedFallbackEscortStarts91<1)
            throw new InvalidOperationException(
                "Pass92 production Escort did not use supported road graph"+
                " verifiedStarts="+game.VerifiedFallbackEscortStarts91+
                " rejectedStarts="+game.RejectedFallbackEscortStarts91);
        var survivor=objective.GetNodeOrNull<CharacterBody3D>("EscortSurvivor");
        var target=objective.GetNodeOrNull<Node3D>("EscortDestination");
        if(survivor is null || target is null)
            throw new InvalidOperationException(
                "Pass92 real Escort survivor or destination missing");
        // Original ExpresswayNavigationRuntime still contains the infected
        // routes and must not be replaced with the Escort-only graph.
        if(roadNav.NavigablePoints<100)
            throw new InvalidOperationException(
                "Pass92 accidentally replaced highway infected graph");

        player.GlobalPosition=objective.GlobalPosition+
            new Vector3(2,1.12f,7);
        game.SetProcess(false);
        var beforeCredits=Runtime.Player!.Credits;
        var beforeXp=Runtime.Player.Xp;
        var beforeLevel=Runtime.Player.Level;
        var expectedCredits=beforeCredits+4000;
        var expectedXp=beforeXp+3500;
        var expectedLevel=beforeLevel;
        while(expectedXp>=ProgressionRules.RequiredForNextLevel(expectedLevel))
        {
            expectedXp-=ProgressionRules.RequiredForNextLevel(expectedLevel);
            expectedLevel++;
            expectedCredits+=ProgressionRules.CreditRewardForLevel(expectedLevel);
        }

        var id=objective.ObjectiveId;
        var completionCallbacks=0;
        objective.Completed+=_=>completionCallbacks++;
        var first=survivor.GlobalPosition;
        var last=first;
        var maxTravel=0f;
        var groundedGoal=false;
        var testedSteps=0;
        var blockedSteps=0;
        var goalRejected=0;
        var alternateGoal=false;
        for(var frame=0;frame<2400;frame++)
        {
            await Frames(1);
            if(completionCallbacks>0)break;
            if(!GodotObject.IsInstanceValid(survivor))
                throw new InvalidOperationException(
                    "Pass92 real NPC deleted without completion");
            last=survivor.GlobalPosition;
            maxTravel=Math.Max(maxTravel,last.DistanceTo(first));
            testedSteps=objective.EscortSafeSteps89;
            blockedSteps=objective.EscortBlockedSteps89;
            goalRejected=objective.EscortRejectedDestinations90;
            alternateGoal=objective.EscortDestinationAdjusted89;
            if(objective.EscortGroundedDestinations90>0 &&
               GodotObject.IsInstanceValid(target))
                groundedGoal=true;
            // Player movement is test-only; no survivor teleports.
            if(player.GlobalPosition.DistanceTo(last)>37f)
                player.GlobalPosition=last+new Vector3(2,1.12f,7);
        }
        if(completionCallbacks!=1 ||
           !objective.EscortGroundedCompletion90 ||
           !groundedGoal ||
           !Runtime.Match.CompletedObjectives.Contains(id) ||
           maxTravel<7f)
            throw new InvalidOperationException(
                "Pass92 physical Expressway Escort failed"+
                " callbacks="+completionCallbacks+
                " goalGrounded="+groundedGoal+
                " travel="+maxTravel+
                " actor="+last+
                " next="+objective.EscortNextWaypoint91+
                " remaining="+objective.EscortRemainingWaypoints91+
                " safeSteps="+testedSteps+
                " blockedSteps="+blockedSteps+
                " rejectedGoals="+goalRejected+
                " alternateGoal="+alternateGoal+
                " graphEdges="+escortNav.EscortSafeEdgeCount91+
                " playerDistance="+player.GlobalPosition.DistanceTo(last));

        if(Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass92 inaccurate real domain Escort payout");
        Runtime.CompleteObjective(id,"Escort"); // replay exact same command
        await Frames(3);
        if(completionCallbacks!=1 ||
           Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass92 duplicate objective reward granted");
        GD.Print("TWR_PASS92_EXPRESSWAY_ESCORT_REWARD_OK"+
            " credits=4000 xp=3500 replay_did_not_pay=true"+
            " npc_travel="+maxTravel.ToString("F2")+
            " physics_steps="+testedSteps+
            " legacy_highway_nodes="+roadNav.NavigablePoints+
            " collision_escort_nodes="+escortNav.PointCount);

        Runtime.FailMatch("Pass92 isolated Expressway Escort verification");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass92 did not save authorized results");
        game.QueueFree();
        // A gameplay result is not a clean release while its Godot scene
        // is still queued for deletion. Wait for the native tree to let go
        // before recreating the lobby or requesting engine shutdown.
        for(var frame=0;frame<30 && GodotObject.IsInstanceValid(game);frame++)
            await Frames(1);
        if(GodotObject.IsInstanceValid(game))
            throw new InvalidOperationException(
                "Pass118 Expressway gameplay scene survived QueueFree");
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass92 did not return to isolated lobby");
        // Let an idle frame drain the game's deferred callbacks. The
        // following _Ready continuation schedules quit rather than
        // synchronously unloading inside PhysicsFrame.
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GD.Print("TWR_PASS118_EXPRESSWAY_TEARDOWN_OK" +
            " scene_released=true lobby_restored=true");
    }
}

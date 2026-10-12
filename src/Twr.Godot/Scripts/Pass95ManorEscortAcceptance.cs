using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass95 exported Windows integration: traverse ACTUAL production Manor
/// Wave10 WestWing and Wave15 EastWing Escort missions. Each uses the same
/// full-size survivor capsule, game physics, and domain reward callbacks.
/// Source-authored owner maps are never rewritten by this fallback-only pass.
/// </summary>
public partial class Pass95ManorEscortAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;}=null!;

    public override async void _Ready()
    {
        try
        {
            await VerifyRotatingManorEscort();
            GD.Print("TWR_SMOKE_PASS95_MANOR_WINGS_OK"+
                " waves=10,15 wings=west,east"+
                " real_survivors=true source_retail_meshes_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS95_MANOR_WINGS_FAILED "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private async Task VerifyRotatingManorEscort()
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby || Runtime.Session is null)
            throw new InvalidOperationException(
                "Pass95 requires isolated local offline lobby/profile");
        Runtime.StartMap("Manor");
        var game=new GameplayRoot
        {
            Name="Pass95ProductionManor",
            MapName="Manor",Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if(player is null || nav is null ||
           game.Pass77ApproximateStairs.Count!=3 ||
           game.Pass82UpperObjectiveMarkers.Count!=3 ||
           Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass95 requires three upper decks and a live offline player");
        for(var frame=0;frame<400 && !nav.IsGraphReady;frame++)
            await Frames(1);
        for(var frame=0;frame<120 && !player.IsOnFloor();frame++)
            await Frames(1);
        if(!nav.IsGraphReady || !player.IsOnFloor() ||
           nav.EscortSafeEdgeCount91<10)
            throw new InvalidOperationException(
                "Pass95 no collision-grounded Escort graph");

        if(Pass95ManorEscortRotation.UpperWingForWave(5)!=2 ||
           Pass95ManorEscortRotation.UpperWingForWave(10)!=0 ||
           Pass95ManorEscortRotation.UpperWingForWave(15)!=1)
            throw new InvalidOperationException(
                "Pass95 changed verified front / planned west / east wave cycle");

        // Normal mission stages and objectives. Only TEST clock periods
        // are accelerated; no production wave/loot/reward state is assigned.
        for(var wave=1;wave<=15;wave++)
        {
            if(Runtime.Match?.Wave!=wave || game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass95 wave countdown mismatch wave="+wave+
                    " stage="+game.WaveStage);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            if(Runtime.Match?.Wave!=wave || game.WaveStage!="WAVE")
                throw new InvalidOperationException(
                    "Pass95 real wave did not start "+wave);
            if(wave==10 || wave==15)
                await VerifyActualWingEscort(game,player,nav,wave);
            if(wave==15)break;

            game._Process(RegularWaveRules.WaveDurationSeconds+.25);
            game._Process(RegularWaveRules.WaveEndSeconds+.25);
            game._Process(RegularWaveRules.IntermissionSeconds+.25);
            if(Runtime.Match?.Wave!=wave+1 ||
               game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass95 failed regular wave transition "+wave);
            await Frames(2);
        }

        Runtime.FailMatch("Pass95 isolated real multi-wing objective test");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass95 did not save local result");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass95 failed to return to local lobby");
    }

    private async Task VerifyActualWingEscort(
        GameplayRoot game,FirstPersonPlayer player,
        Pass73GroundNavigationRuntime nav,int wave)
    {
        var wing=Pass95ManorEscortRotation.UpperWingForWave(wave);
        var label=wing==0?"WestWing":wing==1?"EastWing":"FrontHall";
        if((wave==10 && wing!=0) || (wave==15 && wing!=1))
            throw new InvalidOperationException(
                "Pass95 expected west then east only");
        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Escort");
        if(objective is null ||
           !objective.UseManorElevatedEscort94 ||
           !objective.UseFallbackEscortNavigation89 ||
           !ReferenceEquals(objective.EscortGroundNavigator89,nav))
            throw new InvalidOperationException(
                "Pass95 real catalog did not produce routed Escort"+
                " wing="+label+" wave="+wave);
        var actor=objective.GetNodeOrNull<CharacterBody3D>(
            "EscortSurvivor");
        var target=objective.GetNodeOrNull<Node3D>(
            "EscortDestination");
        if(actor is null || target is null)
            throw new InvalidOperationException(
                "Pass95 real survivor/goal nodes missing");
        var intended=game.Pass82UpperObjectiveMarkers[wing];
        var initial=actor.GlobalPosition;
        if(initial.Y<2.5f ||
           new Vector2(initial.X-intended.X,
                       initial.Z-intended.Z).Length()>10f)
            throw new InvalidOperationException(
                "Pass95 survivor was not safely spawned at "+label+
                " intended="+intended+" actual="+initial);

        // The regular wave clock is stopped, not the NPC CharacterBody3D,
        // nav physics, infected agents or reward-domain mutations.
        game.SetProcess(false);
        var startingCredits=Runtime.Player!.Credits;
        var startingXp=Runtime.Player.Xp;
        var startingLevel=Runtime.Player.Level;
        var expectedCredits=startingCredits+4000;
        var expectedXp=startingXp+3500;
        var expectedLevel=startingLevel;
        while(expectedXp>=ProgressionRules.RequiredForNextLevel(expectedLevel))
        {
            expectedXp-=ProgressionRules.RequiredForNextLevel(expectedLevel);
            expectedLevel++;
            expectedCredits+=ProgressionRules.CreditRewardForLevel(expectedLevel);
        }
        var id=objective.ObjectiveId;
        var callbacks=0;
        objective.Completed+=_=>callbacks++;
        var minY=initial.Y;
        var maxTravel=0f;
        var safeSteps=0;
        var noProgress=0;
        var lastProgress=initial;
        var last=initial;
        var reachedRamp=false;
        var stair=game.Pass77ApproximateStairs[wing];
        player.GlobalPosition=initial+new Vector3(2f,1.12f,7f);

        // All NPC motion must come from ObjectiveRuntime.MoveAndSlide.
        // The test may follow by positioning the PLAYER, never the NPC.
        for(var frame=0;frame<4200;frame++)
        {
            await Frames(1);
            if(callbacks!=0)break;
            if(Runtime.Player?.IsAlive!=true ||
               !GodotObject.IsInstanceValid(actor))
                throw new InvalidOperationException(
                    "Pass95 NPC/player invalid during "+label+" wave "+wave);
            last=actor.GlobalPosition;
            minY=Math.Min(minY,last.Y);
            maxTravel=Math.Max(maxTravel,last.DistanceTo(initial));
            safeSteps=objective.EscortSafeSteps89;
            if(last.DistanceTo(lastProgress)>.5f)
            {
                lastProgress=last;
                noProgress=0;
            }
            else noProgress++;
            if(last.Y<stair.UpperFeet.Y-.5f &&
               last.Y>stair.LowerFeet.Y+.5f &&
               Math.Abs(last.X-stair.LowerFeet.X)<1.7f)
                reachedRamp=true;
            if(frame>300 && noProgress>360)
                throw new InvalidOperationException(
                    "Pass95 real survivor stuck wing="+label+
                    " wave="+wave+
                    " feet="+last+
                    " next="+objective.EscortNextWaypoint91+
                    " remaining="+objective.EscortRemainingWaypoints91+
                    " safeSteps="+safeSteps+
                    " blockedSteps="+objective.EscortBlockedSteps89+
                    " rejectedGoals="+objective.EscortRejectedDestinations90+
                    " plans="+objective.EscortSafeRoutePlans89);
            if(frame%300==0)
                GD.Print("TWR_PASS95_WING_PROGRESS"+
                    " wing="+label+" wave="+wave+" frame="+frame+
                    " feet="+last+
                    " steps="+safeSteps+
                    " blocked="+objective.EscortBlockedSteps89);
            if(player.GlobalPosition.DistanceTo(last)>36f ||
               player.GlobalPosition.DistanceTo(last)<1.8f)
                player.GlobalPosition=last+new Vector3(2f,1.12f,7f);
        }

        if(callbacks!=1 ||
           !objective.EscortGroundedCompletion90 ||
           !Runtime.Match!.CompletedObjectives.Contains(id) ||
           maxTravel<8f || safeSteps<20)
            throw new InvalidOperationException(
                "Pass95 failed actual production wing Escort"+
                " wing="+label+" wave="+wave+
                " callbacks="+callbacks+
                " travel="+maxTravel+
                " minY="+minY+
                " actual="+last+
                " target="+(GodotObject.IsInstanceValid(target)
                    ? target.GlobalPosition.ToString():"freed")+
                " next="+objective.EscortNextWaypoint91+
                " remaining="+objective.EscortRemainingWaypoints91+
                " safeSteps="+safeSteps+
                " blockedSteps="+objective.EscortBlockedSteps89);
        // The east objective's catalog destination is beyond the upper
        // floor so it must physically descend; west remains on one deck.
        if(wing==1 && (!reachedRamp || minY>1.1f))
            throw new InvalidOperationException(
                "Pass95 EastWing survivor never physically descended stairs"+
                " minY="+minY+" ramp="+reachedRamp);

        if(Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass95 incorrect authoritative reward wing="+label+
                " beforeCredits="+startingCredits+
                " after="+Runtime.Player.Credits+
                " expected="+expectedCredits);
        Runtime.CompleteObjective(id,"Escort");
        await Frames(4);
        if(callbacks!=1 ||
           Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass95 duplicate objective payout "+label);

        GD.Print("TWR_PASS95_MANOR_WING_ESCORT_OK"+
            " wing="+label+" wave="+wave+
            " credits=4000 xp=3500 repeat_paid=false"+
            " real_travel="+maxTravel.ToString("F2")+
            " minimumY="+minY.ToString("F2")+
            " collision_steps="+safeSteps);
        game.SetProcess(true);
    }
}

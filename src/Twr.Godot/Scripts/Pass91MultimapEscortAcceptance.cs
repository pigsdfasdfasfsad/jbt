using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass91 exported Windows eight-map fallback Escort acceptance: actual
/// Wave5/Wave6 objective, NPC physical movement, collision-verified
/// start and destination, and domain-authoritative idempotent payout.
/// </summary>
public partial class Pass91MultimapEscortAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;}=null!;
    private static readonly string[] Maps =
    [
        "Ranch","Mill","Bypass","Cabin",
        "Cargo","District","Prison","Laboratory"
    ];
    private int _completed;

    public override async void _Ready()
    {
        try
        {
            foreach(var map in Maps)
                await CheckMap(map);
            if(_completed != Maps.Length)
                throw new InvalidOperationException(
                    "Pass91 missing real-map Escort completions");
            GD.Print("TWR_SMOKE_PASS91_MULTIMAP_ESCORT_OK"+
                " maps="+_completed+
                " runtime_objective_waves=5_or_6"+
                " original_retail_geometry_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS91_MULTIMAP_ESCORT_FAILED"+
                " completed_maps="+_completed+" "+
                ex.GetType().Name+" "+ex.Message+"\n"+ex.StackTrace);
            GetTree().Quit(1);
        }
    }
    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private async Task CheckMap(string map)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby ||
           Runtime.Session is null)
            throw new InvalidOperationException(
                "Pass91 isolated lobby not restored for "+map);

        Runtime.StartMap(map);
        var game=new GameplayRoot
        {
            Name="Pass91Production"+map,MapName=map,Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
            "Pass73GroundNavigation");
        if(player is null || nav is null ||
           Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass91 must use actual player and fallback graph "+map);
        for(var frame=0;frame<360 && !nav.IsGraphReady;frame++)
            await Frames(1);
        // Small blockouts (Cabin in particular) complete their Pass73
        // graph before the player has fallen from the authored 1m spawn
        // to the actual ground collider. Wait for real physics contact,
        // rather than interpreting a transient airborne frame as a map
        // with no playable floor.
        for(var frame=0;frame<120 && !player.IsOnFloor();frame++)
            await Frames(1);
        if(!nav.IsGraphReady || !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass91 no usable real ground physics "+map+
                " nodes="+nav.PointCount+
                " player="+player.GlobalPosition+
                " velocity="+player.Velocity+
                " onFloor="+player.IsOnFloor());

        var families=MapCatalogRuntime.Get(map).Objectives;
        var escortIndex=Array.IndexOf(families.ToArray(),"Escort");
        if(escortIndex<0)
            throw new InvalidOperationException(
                "Pass91 original map catalog lacks Escort "+map);
        var escortWave=escortIndex+1;
        if(escortWave!=5 && escortWave!=6)
            throw new InvalidOperationException(
                "Pass91 unexpected Escort wave index "+map+" "+escortWave);

        for(var wave=1;wave<escortWave;wave++)
        {
            if(Runtime.Match?.Wave!=wave || game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass91 unexpected countdown map="+map+" wave="+wave);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            game._Process(RegularWaveRules.WaveDurationSeconds+.25);
            game._Process(RegularWaveRules.WaveEndSeconds+.25);
            game._Process(RegularWaveRules.IntermissionSeconds+.25);
            if(Runtime.Match?.Wave!=wave+1 ||
               game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass91 failed real game wave advancement "+
                    map+" wave="+wave);
            await Frames(2);
        }
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(Runtime.Match?.Wave!=escortWave ||
           game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass91 no Escort target wave "+map);

        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Escort");
        if(objective is null ||
           !objective.UseFallbackEscortNavigation89 ||
           objective.EscortGroundNavigator89!=nav ||
           game.VerifiedFallbackEscortStarts91<1)
            throw new InvalidOperationException(
                "Pass91 no physically verified production Escort actor "+map+
                " accepted="+game.VerifiedFallbackEscortStarts91+
                " rejected="+game.RejectedFallbackEscortStarts91);
        var survivor=objective.GetNodeOrNull<CharacterBody3D>("EscortSurvivor");
        var destination=objective.GetNodeOrNull<Node3D>("EscortDestination");
        if(survivor is null || destination is null)
            throw new InvalidOperationException(
                "Pass91 real Escort body or goal missing "+map);

        // Test-only player positioning, never NPC teleportation. Stay
        // within the source-recovered 45m follow distance without moving
        // the actual NPC through physics obstacles by hand.
        player.GlobalPosition=objective.GlobalPosition+
            new Vector3(2f,1.12f,7f);
        game.SetProcess(false); // actual objectives/physics still run
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
        var completions=0;
        objective.Completed+=_=>completions++;
        var initial=survivor.GlobalPosition;
        var last=initial;
        var groundedGoal=false;
        var maxTravel=0f;
        var safeSteps=0;
        var blockedPlans=0;
        var rejectedDestinations=0;
        var selectedAlternative=false;
        for(var frame=0;frame<2100;frame++)
        {
            await Frames(1);
            if(completions>0)break;
            if(!GodotObject.IsInstanceValid(survivor))
                throw new InvalidOperationException(
                    "Pass91 Escort survivor destroyed before arrival "+map);
            last=survivor.GlobalPosition;
            maxTravel=Math.Max(maxTravel,last.DistanceTo(initial));
            safeSteps=objective.EscortSafeSteps89;
            blockedPlans=objective.EscortBlockedRoutePlans89;
            rejectedDestinations=objective.EscortRejectedDestinations90;
            selectedAlternative=objective.EscortDestinationAdjusted89;
            if(objective.EscortGroundedDestinations90>0 &&
               GodotObject.IsInstanceValid(destination))
            {
                groundedGoal=true;
                if(Math.Abs(last.Y-destination.GlobalPosition.Y)>2.2f)
                    throw new InvalidOperationException(
                        "Pass91 destination floor unsupported "+map+
                        " survivor="+last+
                        " target="+destination.GlobalPosition);
            }
            if(player.GlobalPosition.DistanceTo(last)>37f)
                player.GlobalPosition=last+new Vector3(2f,1.12f,7f);
        }

        if(!groundedGoal || completions!=1 ||
           !objective.EscortGroundedCompletion90 ||
           !Runtime.Match.CompletedObjectives.Contains(id) ||
           maxTravel<7.0f)
            throw new InvalidOperationException(
                "Pass91 physically grounded Escort did not complete "+
                "map="+map+
                " wave="+escortWave+
                " callbacks="+completions+
                " groundedGoal="+groundedGoal+
                " travel="+maxTravel+
                " actor="+last+
                " steps="+safeSteps+
                " blockedReplans="+blockedPlans+
                " rejectedGoals="+rejectedDestinations+
                " alternateGoal="+selectedAlternative+
                " goal="+(GodotObject.IsInstanceValid(destination)
                    ? destination.GlobalPosition.ToString() : "freed")+
                " npcNextWaypoint="+objective.EscortNextWaypoint91+
                " remainingWaypoints="+objective.EscortRemainingWaypoints91+
                " physicsVelocity="+survivor.Velocity+
                " escortGraphEdges="+nav.EscortSafeEdgeCount91);
        if(Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel)
            throw new InvalidOperationException(
                "Pass91 objective payout mismatch map="+map+
                " gotCredits="+Runtime.Player.Credits+
                " expectedCredits="+expectedCredits+
                " gotXp="+Runtime.Player.Xp+
                " expectedXp="+expectedXp);

        Runtime.CompleteObjective(id,"Escort");
        await Frames(4);
        if(Runtime.Player.Credits!=expectedCredits ||
           Runtime.Player.Xp!=expectedXp ||
           Runtime.Player.Level!=expectedLevel ||
           completions!=1)
            throw new InvalidOperationException(
                "Pass91 duplicate Escort reward or completion "+map);
        GD.Print("TWR_PASS91_MAP_ESCORT_OK map="+map+
            " wave="+escortWave+
            " physically_grounded=true"+
            " npc_travel="+maxTravel.ToString("F2")+
            " safe_steps="+safeSteps+
            " alternative_goal="+selectedAlternative+
            " original_retail_layout_unverified=true");

        Runtime.FailMatch("Pass91 isolated multimapped Escort success");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass91 authoritative result save failed "+map);
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass91 failed lobby restore "+map);
        _completed++;
    }
}

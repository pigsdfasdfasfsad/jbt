using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>Native, isolated Wave5 Ranch Escort: survivor must physically
/// reach its floor-checked destination and domain credits/XP pay only once.
/// Test-only stage clocks are accelerated; actor motion is never teleported.
/// </summary>
public partial class Pass90EscortCompletionAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;}=null!;

    public override async void _Ready()
    {
        try
        {
            await Run();
            GD.Print("TWR_SMOKE_PASS90_ESCORT_COMPLETION_OK"+
                " map=Ranch wave=5 authentic_retail_geometry_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS90_ESCORT_COMPLETION_FAILED "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int n)
    {
        for(var i=0;i<n;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private async Task Run()
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby ||
           Runtime.Session is null)
            throw new InvalidOperationException("Pass90 requires isolated lobby");
        Runtime.StartMap("Ranch");
        var game=new GameplayRoot
        {
            Name="Pass90ProductionRanch",
            MapName="Ranch",
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException("Pass90 requires real player");
        await Frames(90);

        // Advance real production wave transitions only by test clocks.
        // The Escort CharacterBody3D will move solely via PhysicsFrame.
        for(var wave=1;wave<=4;wave++)
        {
            if(game.WaveStage!="COUNTDOWN" ||
               Runtime.Match?.Wave!=wave)
                throw new InvalidOperationException("Pass90 countdown "+wave);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            if(game.WaveStage!="WAVE")
                throw new InvalidOperationException("Pass90 wave "+wave);
            game._Process(RegularWaveRules.WaveDurationSeconds+.25);
            if(game.WaveStage!="WAVE END")
                throw new InvalidOperationException("Pass90 wave end "+wave);
            game._Process(RegularWaveRules.WaveEndSeconds+.25);
            game._Process(RegularWaveRules.IntermissionSeconds+.25);
            if(game.WaveStage!="COUNTDOWN" ||
               Runtime.Match?.Wave!=wave+1)
                throw new InvalidOperationException("Pass90 wave transition "+wave);
            await Frames(3);
        }
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(game.WaveStage!="WAVE" || Runtime.Match?.Wave!=5)
            throw new InvalidOperationException("Pass90 did not reach Wave 5");

        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Escort");
        if(objective is null || !objective.UseFallbackEscortNavigation89)
            throw new InvalidOperationException("Pass90 actual Wave5 Escort absent");
        var survivor=objective.GetNodeOrNull<CharacterBody3D>("EscortSurvivor");
        var destination=objective.GetNodeOrNull<Node3D>("EscortDestination");
        if(survivor is null || destination is null)
            throw new InvalidOperationException("Pass90 real survivor missing");

        game.SetProcess(false);
        var initialCredits=Runtime.Player.Credits;
        var initialXp=Runtime.Player.Xp;
        var id=objective.ObjectiveId;
        var marker=destination.GlobalPosition;
        var completions=0;
        objective.Completed+=_=>completions++;
        var starting=survivor.GlobalPosition;
        var lastSurvivor=starting;
        var groundedGoal=marker;
        var grounded=false;
        for(var i=0;i<1800;i++)
        {
            await Frames(1);
            if(completions>0)break; // Completed objective nodes are QueueFree.
            if(GodotObject.IsInstanceValid(survivor))
                lastSurvivor=survivor.GlobalPosition;
            if(objective.EscortGroundedDestinations90>0 &&
               GodotObject.IsInstanceValid(destination))
            {
                grounded=true;
                groundedGoal=destination.GlobalPosition;
                if(Math.Abs(groundedGoal.Y)>.48f)
                    throw new InvalidOperationException(
                        "Pass90 destination not feet-grounded "+groundedGoal);
            }
        }
        if(!grounded)
            throw new InvalidOperationException(
                "Pass90 no grounded goal marker="+marker+
                " rejects="+objective.EscortRejectedDestinations90);
        GD.Print("TWR_PASS90_GROUNDED_GOAL_OK"+
            " original_y="+marker.Y+" grounded_y="+groundedGoal.Y);

        if(completions!=1 || !objective.EscortGroundedCompletion90 ||
           !Runtime.Match.CompletedObjectives.Contains(id) ||
           lastSurvivor.DistanceTo(starting)<10f)
            throw new InvalidOperationException(
                "Pass90 Escort failed complete after physical movement"+
                " callbacks="+completions+
                " steps="+objective.EscortSafeSteps89+
                " survivor="+lastSurvivor+
                " goal="+groundedGoal);
        GD.Print("TWR_PASS90_ESCORT_FINISH_OK"+
            " physics_steps="+objective.EscortSafeSteps89+
            " actor_movement="+lastSurvivor.DistanceTo(starting).ToString("F2"));

        if(Runtime.Player.Credits-initialCredits!=4000 ||
           Runtime.Player.Xp-initialXp!=3500)
            throw new InvalidOperationException("Pass90 reward amounts wrong");
        Runtime.CompleteObjective(id,"Escort");
        await Frames(4);
        if(completions!=1 ||
           Runtime.Player.Credits-initialCredits!=4000 ||
           Runtime.Player.Xp-initialXp!=3500)
            throw new InvalidOperationException("Pass90 duplicate objective reward");
        GD.Print("TWR_PASS90_REWARD_ONCE_OK credits=4000 xp=3500");
        Runtime.FailMatch("Pass90 isolated verification complete");
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException("Pass90 lobby restore failed");
    }
}

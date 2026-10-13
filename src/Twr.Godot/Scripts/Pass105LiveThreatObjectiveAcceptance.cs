using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass105: the exported Windows game must let its NORMAL Wave1 infected
/// spawn, pursue and actually damage a player who is holding a real Load
/// material, then let the same player physically deposit that material.
/// One bounded Ranch pickup/deposit, not a full under-fire survival match.
/// The threat's short approach lane is test-controlled for repeatability;
/// the player's position, health, mission state and inventory are not.
/// </summary>
public partial class Pass105LiveThreatObjectiveAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    private const int MaxWalkFrames = 540;
    private const int MaxThreatFrames = 210;

    public override async void _Ready()
    {
        try
        {
            await VerifyRanchLiveThreat();
            GD.Print("TWR_SMOKE_PASS105_LIVE_OBJECTIVE_OK"+
                " map=Ranch normal_wave_spawn=true"+
                " real_infected_physics_chase=true"+
                " live_melee_damage_while_carrying=true"+
                " actual_W_E_pickup_and_deposit=true"+
                " real_wave_process_active_during_threat=true"+
                " player_teleports=0 player_health_cheats=0"+
                " no_premature_objective_reward=true"+
                " interrupted_result_persisted=true"+
                " threat_approach_repositioned_for_repeatability=true"+
                " full_real_time_survival_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception ex)
        {
            SetKey(Key.W,false);
            SetKey(Key.E,false);
            GD.PushError("TWR_SMOKE_PASS105_LIVE_OBJECTIVE_FAILED "+
                ex.GetType().Name+" "+ex.Message+"\n"+ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task PhysicsFrames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void SetKey(Key key,bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode=key,
            PhysicalKeycode=key,
            Pressed=pressed
        });
    }

    private async Task PressE()
    {
        SetKey(Key.E,true);
        await PhysicsFrames(5);
        SetKey(Key.E,false);
        await PhysicsFrames(5);
    }

    private async Task<float> WalkTo(
        FirstPersonPlayer player,Vector3 goal,string context)
    {
        var travelled=0f;
        var reached=false;
        SetKey(Key.W,true);
        try
        {
            for(var tick=0;tick<MaxWalkFrames;tick++)
            {
                var flat=goal-player.GlobalPosition;
                flat.Y=0f;
                if(flat.Length()<.70f)
                {
                    reached=true;
                    break;
                }
                player.Rotation=new Vector3(
                    0,Mathf.Atan2(-flat.X,-flat.Z),0);
                var before=player.GlobalPosition;
                await PhysicsFrames(1);
                var step=player.GlobalPosition.DistanceTo(before);
                if(step>.55f)
                    throw new InvalidOperationException(
                        "Pass105 player movement discontinuity "+context+
                        " step="+step);
                travelled+=step;
                if(Runtime.Player?.IsAlive!=true)
                    throw new InvalidOperationException(
                        "Pass105 real player died while carrying "+context);
            }
        }
        finally
        {
            SetKey(Key.W,false);
        }
        await PhysicsFrames(3);
        var separation=goal-player.GlobalPosition;
        separation.Y=0f;
        if(!reached || separation.Length()>1.05f ||
           !player.IsOnFloor() ||
           Math.Abs(player.GlobalPosition.Y-goal.Y)>.55f)
            throw new InvalidOperationException(
                "Pass105 real W-key route blocked "+context+
                " current="+player.GlobalPosition+
                " target="+goal);
        return travelled;
    }

    private async Task<float> WalkSafe(
        GameplayRoot game,FirstPersonPlayer player,
        Vector3 target,string context)
    {
        var world=game.GetWorld3D();
        var excluded=new global::Godot.Collections.Array<Rid>{player.GetRid()};
        Vector3[] legs;
        if(Pass89EscortSurfaceSafety.CanTravel(
                world,player.GlobalPosition,target,excluded))
            legs=[target];
        else
        {
            var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
                "Pass73GroundNavigation");
            if(nav is null || !nav.IsGraphReady)
                throw new InvalidOperationException(
                    "Pass105 missing production ground graph "+context);
            legs=nav.GetVerifiedEscortRoute89(
                player.GlobalPosition,target,player.GetRid());
            if(legs.Length==0 || legs.Length>160)
                throw new InvalidOperationException(
                    "Pass105 no verified player route "+context);
        }

        var travelled=0f;
        foreach(var leg in legs)
        {
            if(!Pass89EscortSurfaceSafety.CanTravel(
                    world,player.GlobalPosition,leg,excluded))
                throw new InvalidOperationException(
                    "Pass105 route lost live collision support "+context);
            var planar=leg-player.GlobalPosition;
            planar.Y=0;
            if(planar.Length()>.70f)
                travelled+=await WalkTo(player,leg,context+"/leg");
        }
        var remaining=target-player.GlobalPosition;
        remaining.Y=0f;
        if(remaining.Length()>1.10f)
            throw new InvalidOperationException(
                "Pass105 failed physical destination "+context);
        return travelled;
    }

    private static float FlatDistance(Node3D a,Node3D b)
    {
        var v=a.GlobalPosition-b.GlobalPosition;
        return new Vector2(v.X,v.Z).Length();
    }

    private async Task VerifyRanchLiveThreat()
    {
        if(Runtime.Session is null ||
           Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass105 needs isolated clean offline lobby");

        Runtime.StartMap("Ranch");
        var game=new GameplayRoot
        {
            Name="Pass105ProductionRanch",
            Runtime=Runtime,
            MapName="Ranch"
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass105 genuine production player missing");
        // Avoid unplanned enemy contact while preparing the initial route.
        // The normal Wave1 game loop is explicitly enabled again once the
        // item is being carried; its native timed spawner is used below.
        game.SetProcess(false);
        await PhysicsFrames(90);
        if(!player.IsOnFloor() ||
           game.Pass87FallbackObjectiveMarkers.Count==0)
            throw new InvalidOperationException(
                "Pass105 no grounded public Ranch blockout/player");

        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match.Wave!=1 || game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass105 production Wave1 did not start");
        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Load");
        if(objective is null || !objective.Pass96GroundedMaterialLayout ||
           objective.Pass98RequiredMaterials<10)
            throw new InvalidOperationException(
                "Pass105 missing real Wave1 Load actor/material geometry");

        var material=objective.Pass96MaterialFeet[0];
        var depot=objective.GlobalPosition;
        var originalSpawn=player.GlobalPosition;
        var creditsBefore=Runtime.Player.Credits;
        var xpBefore=Runtime.Player.Xp;
        var travelled=await WalkSafe(
            game,player,depot,"Ranch/spawn-to-Load-depot");
        travelled+=await WalkSafe(
            game,player,material,"Ranch/depot-to-real-item");
        await PressE();
        if(!objective.Pass98CarryingMaterial ||
           objective.Pass98RemainingMaterials!=
                objective.Pass98RequiredMaterials-1 ||
           objective.Pass98DepositedMaterials!=0)
            throw new InvalidOperationException(
                "Pass105 original E-key material pickup failed");

        // The engine's NORMAL wave _Process spawns a real AI actor. No
        // fabricated InfectedAgent or direct Runtime.DamagePlayer call.
        // Test-only enemy placement makes a bounded, unobstructed melee
        // lane near this one item; this does not verify original retail
        // zombie spawn coordinates or distant navigation.
        game.SetProcess(true);
        await PhysicsFrames(8);
        var spawned=game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                               !x.IsQueuedForDeletion() && x.Health>0);
        if(spawned is null || game.ActiveInfectedCount<1 ||
           spawned.Target!=player)
            throw new InvalidOperationException(
                "Pass105 real Wave1 infected spawner did not produce AI");

        var exclude=new global::Godot.Collections.Array<Rid>
        {
            player.GetRid(),spawned.GetRid()
        };
        Vector3? lane=null;
        foreach(var direction in new[]
        {
            Vector3.Right,Vector3.Left,Vector3.Forward,Vector3.Back
        })
        {
            var proposed=player.GlobalPosition+direction*3.0f;
            if(Pass89EscortSurfaceSafety.CanTravel(
                    game.GetWorld3D(),player.GlobalPosition,proposed,exclude))
            {
                lane=proposed;
                break;
            }
        }
        if(!lane.HasValue)
            throw new InvalidOperationException(
                "Pass105 no collision-supported exposed Ranch threat lane");

        // The type/damage/speed and short-lane start are bounded test
        // controls only. The real production melee LOS, cadence, health
        // command and CharacterBody3D motion still execute unchanged.
        spawned.InfectedType="Civilian";
        spawned.SourceNavigator=null;
        spawned.HighwayNavigator=null;
        spawned.GroundNavigator73=null;
        spawned.SpecialAttackRequested=null;
        spawned.Damage=4f;
        spawned.MoveSpeed=2.6f;
        spawned.GlobalPosition=lane.Value+Vector3.Up*.47f;
        await PhysicsFrames(4);
        if(!spawned.IsOnFloor())
            throw new InvalidOperationException(
                "Pass105 placed melee actor lacks physical floor support");

        var startGap=FlatDistance(spawned,player);
        var initialHealth=Runtime.Player.Health;
        var minGap=startGap;
        for(var frame=0;frame<MaxThreatFrames;frame++)
        {
            await PhysicsFrames(1);
            if(!GodotObject.IsInstanceValid(spawned) ||
               Runtime.Player?.IsAlive!=true ||
               !objective.Pass98CarryingMaterial)
                throw new InvalidOperationException(
                    "Pass105 native player died or lost material while attacked");
            minGap=Math.Min(minGap,FlatDistance(spawned,player));
            if(Runtime.Player.Health<initialHealth-.1f)
                break;
        }
        var damageTaken=initialHealth-Runtime.Player.Health;
        if(minGap>startGap-.65f || damageTaken<.5f ||
           !spawned.IsOnFloor() ||
           game.WaveStage!="WAVE" ||
           game.ActiveInfectedCount<1)
            throw new InvalidOperationException(
                "Pass105 no real pursuit and authoritative melee hit"+
                " start_gap="+startGap+" minimum_gap="+minGap+
                " health_damage="+damageTaken);

        // Do not disable the running wave or the enemy while returning
        // with the material. The E interaction must still work under
        // active AI pursuit, and must NOT pay a completion reward early.
        travelled+=await WalkSafe(
            game,player,depot,"Ranch/live-enemy-carry-to-depot");
        if(!objective.Pass98CarryingMaterial ||
           Runtime.Player?.IsAlive!=true ||
           game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass105 carried material lost under live enemy pursuit");
        await PressE();
        if(objective.Pass98CarryingMaterial ||
           objective.Pass98DepositedMaterials!=1 ||
           objective.Pass98RemainingMaterials!=
                objective.Pass98RequiredMaterials-1 ||
           Runtime.Match?.CompletedObjectives.Contains(
                objective.ObjectiveId)==true ||
           Runtime.Player.Credits!=creditsBefore ||
           Runtime.Player.Xp!=xpBefore ||
           game.ActiveInfectedCount<1)
            throw new InvalidOperationException(
                "Pass105 under-fire deposit corrupted objective or rewards");

        GD.Print("TWR_PASS105_RANCH_UNDER_FIRE_DEPOSIT_OK"+
            " original_spawn="+originalSpawn+
            " wave=1 material_pickups=1 material_deposits=1"+
            " player_walked_metres="+travelled.ToString("F1")+
            " native_melee_damage="+damageTaken.ToString("F1")+
            " native_chase_metres="+
                (startGap-minGap).ToString("F1")+
            " native_spawn_active=true player_is_alive=true"+
            " pickup_and_deposit_W_E_inputs=true"+
            " real_wave_clock_enabled_during_hostility=true"+
            " enemy_lane_start_test_controlled=true"+
            " no_player_position_override=true"+
            " rewards_not_issued_prematurely=true");

        Runtime.FailMatch("Pass105 bounded live-hostility material interaction");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           !Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass105 local interrupted-objective result not persisted");
        game.QueueFree();
        await PhysicsFrames(5);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass105 did not restore isolated offline lobby");
    }
}

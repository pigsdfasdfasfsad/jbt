using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass103 native acceptance: entire REAL Wave1 Load objective on Ranch
/// and Manor, with physics walking from production spawn to every item and
/// back. No acceptance actor is teleported or granted inventory/rewards.
/// Headless isolated combat-free mission test, not human 15-wave survival.
/// </summary>
public partial class Pass103PhysicalLoadLifecycleAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    private int _completed;
    private int _deposits;
    private float _walkedMetres;
    // Pass104 extends the existing, fully verified real-input Wave1 Load
    // acceptance through an actual Wave1->Wave2 progression and a complete
    // physical Repair mission. The default Pass103 mode is unchanged.
    public bool ExtendToRepair104 { get; set; }
    private int _completedRepairs104;
    private int _repairDeposits104;
    private float _repairWalkedMetres104;
    // Pass105: production-spawned infected must remain a live physics
    // pursuer while the player physically carries and deposits the last
    // Wave2 Repair material. These optional checks never run in normal
    // gameplay or the already-verified Pass103/104 test modes.
    public bool VerifyPursuit105 { get; set; }
    private int _pursuitMaps105;
    private float _pursuitMetres105;
    private const int MaxWalkFrames=540;

    public override async void _Ready()
    {
        try
        {
            if(Runtime.Session is null ||
               Runtime.Match?.Phase!=MatchPhase.Lobby)
                throw new InvalidOperationException(
                    "Pass103 requires clean offline lobby");
            await VerifyMap("Ranch");
            await VerifyMap("Manor");
            if(_completed!=2 || _deposits<20 ||
               _walkedMetres<80f)
                throw new InvalidOperationException(
                    "Pass103 inadequate full physical mission coverage"+
                    " maps="+_completed+" deposits="+_deposits+
                    " metres="+_walkedMetres);
            GD.Print("TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_OK"+
                " maps=Ranch,Manor completed_production_wave1_loads="+_completed+
                " material_pickup_deposits="+_deposits+
                " walked_metres="+_walkedMetres.ToString("F1")+
                " actual_W_E_keys=true player_teleports=0"+
                " objective_completion_receipts_once=true"+
                " accelerated_wave_start=true"+
                " isolated_enemy_combat=true"+
                " retail_art_fidelity_unverified=true");
            if(VerifyPursuit105 && !ExtendToRepair104)
                throw new InvalidOperationException(
                    "Pass105 requires Pass104 two-wave mission progression");
            if(ExtendToRepair104)
            {
                if(_completedRepairs104!=2 ||
                   _repairDeposits104<20 ||
                   _repairWalkedMetres104<40f)
                    throw new InvalidOperationException(
                        "Pass104 incomplete real Wave2 Repair acceptance"+
                        " maps="+_completedRepairs104+
                        " deposits="+_repairDeposits104+
                        " metres="+_repairWalkedMetres104);
                GD.Print("TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK"+
                    " maps=Ranch,Manor completed_wave1_loads="+_completed+
                    " completed_wave2_repairs="+_completedRepairs104+
                    " wave2_material_deposits="+_repairDeposits104+
                    " wave2_walked_metres="+
                        _repairWalkedMetres104.ToString("F1")+
                    " total_material_deposits="+
                        (_deposits+_repairDeposits104)+
                    " actual_W_E_keys=true player_teleports=0"+
                    " wave1_to_wave2_runtime_progression=true"+
                    " exactly_once_rewards=true persisted_results=true"+
                    " enemy_spawning_frozen_during_missions=true"+
                    " full_realtime_survival_unverified=true");
            }
            if(VerifyPursuit105)
            {
                if(_pursuitMaps105!=2 || _pursuitMetres105<.60f)
                    throw new InvalidOperationException(
                        "Pass105 insufficient grounded live pursuit"+
                        " maps="+_pursuitMaps105+
                        " moved="+_pursuitMetres105.ToString("F2"));
                GD.Print("TWR_SMOKE_PASS105_LIVE_REPAIR_PURSUIT_OK"+
                    " maps=Ranch,Manor full_wave2_repairs="+
                        _completedRepairs104+
                    " materials_deposited="+_repairDeposits104+
                    " live_pursuit_metres="+_pursuitMetres105.ToString("F2")+
                    " real_wave2_spawn=true"+
                    " physics_enemy_alive_at_deposit=true"+
                    " real_W_E_keys=true player_teleports=0"+
                    " no_damage_immunity=true"+
                    " timed_extra_spawns_paused=true"+
                    " player_combat_kill_not_tested=true"+
                    " full_combat_survival_unverified=true");
            }
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            InputEvent(Key.W,false);
            InputEvent(Key.E,false);
            GD.PushError((VerifyPursuit105
                    ? "TWR_SMOKE_PASS105_LIVE_REPAIR_PURSUIT_FAILED "
                    : ExtendToRepair104
                    ? "TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_FAILED "
                    : "TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_FAILED ")+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for(var n=0;n<count;n++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static void InputEvent(Key key,bool pressed)
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
        InputEvent(Key.E,true);
        await Frames(5);
        InputEvent(Key.E,false);
        await Frames(5);
    }

    private async Task<float> WalkTo(
        FirstPersonPlayer player,Vector3 feet,string context)
    {
        var distance=0f;
        var arrived=false;
        InputEvent(Key.W,true);
        try
        {
            for(var n=0;n<MaxWalkFrames;n++)
            {
                var horizontal=feet-player.GlobalPosition;
                horizontal.Y=0;
                if(horizontal.Length()<.70f)
                {
                    arrived=true;
                    break;
                }
                player.Rotation=new Vector3(
                    0,Mathf.Atan2(-horizontal.X,-horizontal.Z),0);
                var before=player.GlobalPosition;
                await Frames(1);
                var step=player.GlobalPosition.DistanceTo(before);
                if(step>.55f)
                    throw new InvalidOperationException(
                        "Pass103 movement discontinuity "+context+
                        " step="+step);
                distance+=step;
                if(Runtime.Player?.IsAlive!=true)
                    throw new InvalidOperationException(
                        "Pass103 player died while walking "+context);
            }
        }
        finally
        {
            InputEvent(Key.W,false);
        }
        await Frames(4);
        var delta=feet-player.GlobalPosition;
        delta.Y=0;
        if(!arrived || delta.Length()>1.05f ||
           !player.IsOnFloor() ||
           Math.Abs(player.GlobalPosition.Y-feet.Y)>.55f)
            throw new InvalidOperationException(
                "Pass103 real player W walk blocked "+context+
                " current="+player.GlobalPosition+" goal="+feet+
                " metres="+distance.ToString("F1"));
        return distance;
    }

    private async Task<float> WalkSafely(
        GameplayRoot game,FirstPersonPlayer player,
        Vector3 destination,string context)
    {
        var world=game.GetWorld3D();
        var exclude=new global::Godot.Collections.Array<Rid>
        {
            player.GetRid()
        };
        Vector3[] legs;
        var from=player.GlobalPosition;
        if(Pass89EscortSurfaceSafety.CanTravel(
                world,from,destination,exclude))
            legs=[destination];
        else
        {
            var nav=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
                "Pass73GroundNavigation");
            if(nav is null || !nav.IsGraphReady)
                throw new InvalidOperationException(
                    "Pass103 no physical ground graph "+context);
            legs=nav.GetVerifiedEscortRoute89(
                from,destination,player.GetRid());
            if(legs.Length==0 || legs.Length>160)
                throw new InvalidOperationException(
                    "Pass103 no body-safe route "+context+
                    " origin="+from+" target="+destination);
        }
        var travelled=0f;
        foreach(var leg in legs)
        {
            if(!Pass89EscortSurfaceSafety.CanTravel(
                    world,player.GlobalPosition,leg,exclude))
                throw new InvalidOperationException(
                    "Pass103 route changed under player "+context+
                    " current="+player.GlobalPosition+" leg="+leg);
            var d=leg-player.GlobalPosition;
            d.Y=0;
            if(d.Length()>.7f)
                travelled+=await WalkTo(player,leg,context+"/route");
        }
        if(new Vector2(player.GlobalPosition.X-destination.X,
                       player.GlobalPosition.Z-destination.Z).Length()>1.10f)
            throw new InvalidOperationException(
                "Pass103 route incomplete "+context);
        return travelled;
    }

    private static (int credits,int xp,int level) RewardAfterObjective(
        PlayerState player)
    {
        var credits=player.Credits+4000;
        var xp=player.Xp+3500;
        var level=player.Level;
        while(xp>=ProgressionRules.RequiredForNextLevel(level))
        {
            xp-=ProgressionRules.RequiredForNextLevel(level);
            level++;
            credits+=ProgressionRules.CreditRewardForLevel(level);
        }
        return (credits,xp,level);
    }

    private void ValidateReward(
        (int credits,int xp,int level) expected,string context)
    {
        if(Runtime.Player?.Credits!=expected.credits ||
           Runtime.Player?.Xp!=expected.xp ||
           Runtime.Player?.Level!=expected.level)
            throw new InvalidOperationException(
                "Pass103 production reward incorrect "+context+
                " expected="+expected+" credits="+Runtime.Player?.Credits+
                " xp="+Runtime.Player?.Xp+
                " level="+Runtime.Player?.Level);
    }

    private async Task<(InfectedAgent enemy,Vector3 start)> SpawnRealPursuer105(
        GameplayRoot game,FirstPersonPlayer player,string map)
    {
        if(game.WaveStage!="WAVE" || Runtime.Match?.Wave!=2 ||
           game.ActiveInfectedCount!=0)
            throw new InvalidOperationException(
                "Pass105 requires clean genuine Wave2 spawner "+map);

        // One ordinary production SpawnInfected call, using the REAL
        // collision-approved map spawn and source/current wave catalog.
        // Stopping only GameplayRoot's future stage clock and spawn timer
        // does not disable the spawned InfectedAgent._PhysicsProcess.
        game._Process(.05);
        game.SetProcess(false);
        var actor=game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Health>0f);
        if(actor is null || actor.Target!=player ||
           game.ActiveInfectedCount!=1 ||
           game.VerifiedFallbackSpawns85<1)
            throw new InvalidOperationException(
                "Pass105 production Wave2 spawn not collision supported "+
                map+" infected="+game.ActiveInfectedCount+
                " verified="+game.VerifiedFallbackSpawns85);

        var initial=actor.GlobalPosition;
        // While W is not pressed, the carried material and player stay
        // in the actual playable world with real pursuit/damage enabled.
        // No enemy or player positions, speed, damage, health or collision
        // properties are modified for this acceptance.
        await Frames(90);
        if(!GodotObject.IsInstanceValid(actor) ||
           actor.IsQueuedForDeletion() || actor.Health<=0f ||
           !actor.IsOnFloor() || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass105 active physics pursuer lost during carried item "+
                map);

        var horizontal=actor.GlobalPosition-initial;
        horizontal.Y=0;
        if(horizontal.Length()<.30f)
            throw new InvalidOperationException(
                "Pass105 wave-spawned infected failed grounded pursuit "+
                map+" start="+initial+
                " actual="+actor.GlobalPosition+
                " displacement="+horizontal.Length().ToString("F2"));
        return (actor,initial);
    }

    /// <summary>
    /// After the complete, real-input Wave1 Load mission, use the normal
    /// stage state machine (not a modified wave number) to start Wave2.
    /// The running stage clock stays frozen during item carrying, as in
    /// Pass103; physically spawned players and objective markers are never
    /// translated or modified by the acceptance harness.
    /// </summary>
    private async Task WalkRepairWave104(
        GameplayRoot game,FirstPersonPlayer player,
        string map,string completedLoadId)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match.Wave!=1 ||
           game.WaveStage!="WAVE" ||
           Runtime.Match.CompletedObjectives.Contains(completedLoadId)!=true)
            throw new InvalidOperationException(
                "Pass104 requires completed real Wave1 Load "+map);

        // These are the exact production Stage.Wave -> WaveEnd ->
        // Intermission -> Countdown transitions used by the game. The
        // regular EndWave teardown clears old objective actors and
        // zombies; no gameplay scene is restarted between the waves.
        game._Process(RegularWaveRules.WaveDurationSeconds+.25);
        game._Process(RegularWaveRules.WaveEndSeconds+.25);
        game._Process(RegularWaveRules.IntermissionSeconds+.25);
        if(Runtime.Match?.Wave!=2 || game.WaveStage!="COUNTDOWN")
            throw new InvalidOperationException(
                "Pass104 actual Wave1->2 state transition failed "+map);
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        game.SetProcess(false);
        await Frames(5);
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match.Wave!=2 || game.WaveStage!="WAVE" ||
           Runtime.Player?.IsAlive!=true ||
           !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass104 no living player in real Wave2 "+map);

        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Repair");
        if(objective is null ||
           !objective.UseFallbackMaterialGrounding96 ||
           !objective.Pass96GroundedMaterialLayout ||
           objective.Pass98RequiredMaterials<10)
            throw new InvalidOperationException(
                "Pass104 actual Wave2 Repair objective not spawned "+map);

        var feet=objective.Pass96MaterialFeet.ToArray();
        var count=feet.Length;
        var depot=objective.GlobalPosition;
        var id=objective.ObjectiveId;
        var callbacks=0;
        objective.Completed+=_=>callbacks++;
        var expected=RewardAfterObjective(Runtime.Player!);
        var wave2Start=player.GlobalPosition;
        var metres=await WalkSafely(
            game,player,depot,map+"/wave2-to-production-Repair");
        InfectedAgent? livePursuer105=null;
        var pursuitOrigin105=Vector3.Zero;

        for(var item=0;item<count;item++)
        {
            if(!GodotObject.IsInstanceValid(objective) ||
               Runtime.Player?.IsAlive!=true ||
               objective.Pass98RemainingMaterials!=count-item ||
               objective.Pass98DepositedMaterials!=item ||
               objective.Pass98CarryingMaterial ||
               callbacks!=0)
                throw new InvalidOperationException(
                    "Pass104 stale real Repair material state "+map+
                    " item="+item);
            metres+=await WalkSafely(
                game,player,feet[item],map+"/wave2-Repair-material-"+item);
            await PressE();
            if(!objective.Pass98CarryingMaterial ||
               objective.Pass98RemainingMaterials!=count-item-1 ||
               objective.Pass98DepositedMaterials!=item)
                throw new InvalidOperationException(
                    "Pass104 failed physical E pickup "+map+
                    " item="+item);
            if(VerifyPursuit105 && item==count-1)
            {
                (livePursuer105,pursuitOrigin105)=
                    await SpawnRealPursuer105(game,player,map);
            }
            metres+=await WalkSafely(
                game,player,depot,map+"/wave2-Repair-deposit-"+item);
            await PressE();
            if(item<count-1 &&
               (!GodotObject.IsInstanceValid(objective) ||
                objective.Pass98CarryingMaterial ||
                objective.Pass98DepositedMaterials!=item+1 ||
                callbacks!=0))
                throw new InvalidOperationException(
                    "Pass104 failed physical E deposit "+map+
                    " item="+item);
            _repairDeposits104++;
        }

        await Frames(5);
        if(VerifyPursuit105)
        {
            if(livePursuer105 is null ||
               !GodotObject.IsInstanceValid(livePursuer105) ||
               livePursuer105.IsQueuedForDeletion() ||
               livePursuer105.Health<=0f ||
               !livePursuer105.IsOnFloor() ||
               Runtime.Player?.IsAlive!=true)
                throw new InvalidOperationException(
                    "Pass105 lost living pursuer at actual final E deposit "+
                    map);
            var distance=livePursuer105.GlobalPosition-pursuitOrigin105;
            distance.Y=0f;
            if(distance.Length()<.30f)
                throw new InvalidOperationException(
                    "Pass105 enemy did not advance during carry "+map);
            _pursuitMaps105++;
            _pursuitMetres105+=distance.Length();
            GD.Print("TWR_PASS105_MAP_REPAIR_PURSUIT_OK map="+map+
                " production_wave=2"+
                " material_carried_while_pursued=true"+
                " final_material_deposited_with_enemy_alive=true"+
                " infected_grounded=true"+
                " player_alive=true"+
                " pursuit_metres="+distance.Length().ToString("F2")+
                " real_spawn_and_collision=true"+
                " player_teleports=0 enemy_test_relocation=0");
        }
        if(callbacks!=1 ||
           Runtime.Match?.CompletedObjectives.Contains(id)!=true ||
           Runtime.Match.CompletedObjectives.Contains(completedLoadId)!=true)
            throw new InvalidOperationException(
                "Pass104 domain did not complete both waves' missions "+
                map+" repair_callbacks="+callbacks);
        ValidateReward(expected,map+"/wave2-Repair-completed");
        Runtime.CompleteObjective(id,"Repair");
        await Frames(3);
        ValidateReward(expected,map+"/wave2-Repair-replayed-receipt");
        if(callbacks!=1)
            throw new InvalidOperationException(
                "Pass104 duplicate Repair callback "+map);

        _completedRepairs104++;
        _repairWalkedMetres104+=metres;
        GD.Print("TWR_PASS104_MAP_FULL_REPAIR_OK map="+map+
            " production_wave=2 family=Repair"+
            " material_items="+count+
            " pickups="+count+" deposits="+count+
            " walked_metres="+metres.ToString("F1")+
            " wave2_start="+wave2Start+
            " no_position_assignment=true"+
            " actual_W_E_keys=true"+
            " callbacks_once=true rewards_once=true"+
            " previous_load_completion_retained=true");
    }

    private async Task VerifyMap(string map)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass103 previous map did not restore lobby");
        Runtime.StartMap(map);
        var game=new GameplayRoot
        {
            Name="Pass103Production"+map,
            MapName=map,
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass103 no physical production player "+map);
        await Frames(90);
        if(!player.IsOnFloor() ||
           game.Pass87FallbackObjectiveMarkers.Count==0)
            throw new InvalidOperationException(
                "Pass103 missing supported fallback "+map);

        // Start the REAL first production wave and then hold its stage
        // clock. No time-based new infected spawns during this isolated
        // mission acceptance; normal objective/player PhysicsProcess runs.
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(Runtime.Match?.Phase!=MatchPhase.Wave ||
           Runtime.Match?.Wave!=1 || game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass103 real wave did not begin "+map);
        game.SetProcess(false);
        foreach(var pickup in game.GetChildren().OfType<PickupActor>())
            pickup.QueueFree();
        await Frames(5);
        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion() && x.Family=="Load");
        if(objective is null || !objective.UseFallbackMaterialGrounding96 ||
           !objective.Pass96GroundedMaterialLayout ||
           objective.Pass98RequiredMaterials<10)
            throw new InvalidOperationException(
                "Pass103 real Wave1 Load mission absent "+map);

        var feet=objective.Pass96MaterialFeet.ToArray();
        var count=feet.Length;
        var depot=objective.GlobalPosition;
        var id=objective.ObjectiveId;
        var completedCallbacks=0;
        objective.Completed+=_=>completedCallbacks++;
        var expected=RewardAfterObjective(Runtime.Player!);
        var spawn=player.GlobalPosition;
        var metres=await WalkSafely(
            game,player,depot,map+"/spawn-to-production-Load");

        for(var item=0;item<count;item++)
        {
            if(!GodotObject.IsInstanceValid(objective) ||
               Runtime.Player?.IsAlive!=true ||
               objective.Pass98RemainingMaterials!=count-item ||
               objective.Pass98DepositedMaterials!=item ||
               objective.Pass98CarryingMaterial)
                throw new InvalidOperationException(
                    "Pass103 stale production materials before item="+item+
                    " map="+map);
            var target=feet[item];
            metres+=await WalkSafely(
                game,player,target,map+"/material-"+item);
            await PressE();
            if(!objective.Pass98CarryingMaterial ||
               objective.Pass98RemainingMaterials!=count-item-1 ||
               objective.Pass98DepositedMaterials!=item)
                throw new InvalidOperationException(
                    "Pass103 actual E pickup failed "+map+
                    " item="+item);
            metres+=await WalkSafely(
                game,player,depot,map+"/return-to-depot-"+item);
            await PressE();
            if(item<count-1)
            {
                if(!GodotObject.IsInstanceValid(objective) ||
                   objective.Pass98CarryingMaterial ||
                   objective.Pass98DepositedMaterials!=item+1 ||
                   completedCallbacks!=0)
                    throw new InvalidOperationException(
                        "Pass103 premature/failed material deposit "+
                        map+" item="+item);
            }
            _deposits++;
        }
        await Frames(5);
        if(completedCallbacks!=1 ||
           Runtime.Match?.CompletedObjectives.Contains(id)!=true)
            throw new InvalidOperationException(
                "Pass103 production objective not fully completed "+map+
                " callbacks="+completedCallbacks);
        ValidateReward(expected,map+"/complete");
        Runtime.CompleteObjective(id,"Load");
        await Frames(3);
        ValidateReward(expected,map+"/replayed-receipt");
        if(completedCallbacks!=1)
            throw new InvalidOperationException(
                "Pass103 duplicate completion callback "+map);
        _completed++;
        _walkedMetres+=metres;
        GD.Print("TWR_PASS103_MAP_FULL_LOAD_OK map="+map+
            " production_wave=1 material_items="+count+
            " pickups="+count+" deposits="+count+
            " walked_metres="+metres.ToString("F1")+
            " original_spawn="+spawn+
            " no_position_assignment=true"+
            " actual_W_E_keys=true"+
            " callbacks_once=true rewards_once=true");
        if(ExtendToRepair104)
            await WalkRepairWave104(game,player,map,id);
        Runtime.FailMatch(ExtendToRepair104
            ? "Pass104 isolated physical Load and Repair missions"
            : "Pass103 isolated physical Load mission");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           !Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass103 offline result did not persist "+map);
        game.QueueFree();
        await Frames(5);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass103 could not restore local offline lobby "+map);
    }
}

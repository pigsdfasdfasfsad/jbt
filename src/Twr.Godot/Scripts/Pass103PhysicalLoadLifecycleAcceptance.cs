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
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            InputEvent(Key.W,false);
            InputEvent(Key.E,false);
            GD.PushError("TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_FAILED "+
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
        Runtime.FailMatch("Pass103 isolated physical Load mission");
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

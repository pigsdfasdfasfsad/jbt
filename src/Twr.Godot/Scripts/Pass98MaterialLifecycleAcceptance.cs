using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass98 exported Windows acceptance: full physical E-key carry/deposit
/// loop for production wave Load and Repair objectives in Ranch and Manor.
/// Only the test player's position is controlled; the actual ObjectiveRuntime,
/// domain receipts, collision, inventory state, and completion callbacks run
/// unchanged. This is not a human-input movement-path acceptance.
/// </summary>
public partial class Pass98MaterialLifecycleAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;}=null!;
    private const int PressFrames=4;
    private int _completedMissions;
    private int _carriedPieces;

    public override async void _Ready()
    {
        try
        {
            if(Runtime.Match?.Phase!=MatchPhase.Lobby ||
               Runtime.Session is null)
                throw new InvalidOperationException(
                    "Pass98 requires isolated local offline lobby");
            await VerifyMap("Ranch");
            await VerifyMap("Manor");
            if(_completedMissions!=4 || _carriedPieces<40)
                throw new InvalidOperationException(
                    "Pass98 did not complete four production material missions"+
                    " missions="+_completedMissions+
                    " material_interactions="+_carriedPieces);
            GD.Print("TWR_SMOKE_PASS98_MATERIAL_LIFECYCLE_OK"+
                " maps=Ranch,Manor production_wave_objectives=4"+
                " carried_pieces="+_carriedPieces+
                " real_E_key=true shared_press_budget=true"+
                " exactly_once_rewards=true"+
                " player_positions_test_controlled=true"+
                " original_retail_coordinates_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS98_MATERIAL_LIFECYCLE_FAILED "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int n)
    {
        for(var i=0;i<n;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static void E(bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode=Key.E,
            PhysicalKeycode=Key.E,
            Pressed=pressed
        });
    }

    private async Task PressE()
    {
        E(true);
        await Frames(PressFrames);
        E(false);
        await Frames(PressFrames);
    }

    private async Task MoveTestPlayer(
        FirstPersonPlayer player,Vector3 groundedFeet)
    {
        // The previous Pass96 native test proved these target feet are
        // solid-collider grounded. Only the acceptance player is placed
        // nearby: no objective, physical item, wall or reward is moved.
        player.GlobalPosition=groundedFeet+Vector3.Up*1.1f;
        await Frames(22);
        if(!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass98 player unable to stand at verified point "+
                groundedFeet+" actual="+player.GlobalPosition);
    }

    private static (int credits,int xp,int level) ExpectedReward(
        int beforeCredits,int beforeXp,int beforeLevel)
    {
        var credits=beforeCredits+4000;
        var xp=beforeXp+3500;
        var level=beforeLevel;
        while(xp>=ProgressionRules.RequiredForNextLevel(level))
        {
            xp-=ProgressionRules.RequiredForNextLevel(level);
            level++;
            credits+=ProgressionRules.CreditRewardForLevel(level);
        }
        return (credits,xp,level);
    }

    private void CheckReward(
        (int credits,int xp,int level) expected,string context)
    {
        if(Runtime.Player?.Credits!=expected.credits ||
           Runtime.Player.Xp!=expected.xp ||
           Runtime.Player.Level!=expected.level)
            throw new InvalidOperationException(
                "Pass98 authoritative objective reward mismatch "+context+
                " credits="+Runtime.Player?.Credits+"/"+expected.credits+
                " xp="+Runtime.Player?.Xp+"/"+expected.xp+
                " level="+Runtime.Player?.Level+"/"+expected.level);
    }

    private async Task VerifyContendedPress(
        GameplayRoot game,FirstPersonPlayer player)
    {
        // This isolated setup confirms two physically co-located mission
        // actors cannot take the SAME held key press. Remove unrelated
        // natural loot from only this test fixture so its input receiver
        // does not vary with randomized supply placement.
        foreach(var pickup in game.GetChildren().OfType<PickupActor>())
            pickup.QueueFree();
        await Frames(4);
        var marker=game.Pass87FallbackObjectiveMarkers[0];
        var first=new ObjectiveRuntime
        {
            Name="Pass98OverlapA",ObjectiveId="Ranch:Pass98:OverlapA",
            Family="Load",Runtime=Runtime,Player=player,
            UseFallbackMaterialGrounding96=true,Position=marker
        };
        var second=new ObjectiveRuntime
        {
            Name="Pass98OverlapB",ObjectiveId="Ranch:Pass98:OverlapB",
            Family="Load",Runtime=Runtime,Player=player,
            UseFallbackMaterialGrounding96=true,Position=marker
        };
        game.AddChild(first);
        game.AddChild(second);
        if(!first.Pass96GroundedMaterialLayout ||
           !second.Pass96GroundedMaterialLayout ||
           first.Pass96MaterialFeet.Count!=10 ||
           second.Pass96MaterialFeet.Count!=10 ||
           first.Pass96MaterialFeet[0].DistanceTo(
               second.Pass96MaterialFeet[0])>.15f)
            throw new InvalidOperationException(
                "Pass98 overlapping objectives not physically co-located");
        await MoveTestPlayer(player,first.Pass96MaterialFeet[0]);
        E(true);
        await Frames(12);
        E(false);
        await Frames(PressFrames);
        var carriers=(first.Pass98CarryingMaterial?1:0)+
            (second.Pass98CarryingMaterial?1:0);
        if(carriers!=1 ||
           first.Pass98RemainingMaterials+
               second.Pass98RemainingMaterials!=19)
            throw new InvalidOperationException(
                "Pass98 shared held E consumed multiple overlapping props"+
                " carriers="+carriers+" first="+
                first.Pass98RemainingMaterials+
                " second="+second.Pass98RemainingMaterials);
        var carried=first.Pass98CarryingMaterial?first:second;
        var idle=ReferenceEquals(carried,first)?second:first;
        // Pass99 regression: releasing E must NOT permit a player to
        // hold material from both overlapping objectives at once.
        await PressE();
        if(!carried.Pass98CarryingMaterial ||
           idle.Pass98CarryingMaterial ||
           first.Pass98RemainingMaterials+
               second.Pass98RemainingMaterials!=19)
            throw new InvalidOperationException(
                "Pass99 second E acquired an additional carried material");
        GD.Print("TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK"+
            " overlapping_objectives=2 concurrent_carried=1"+
            " second_press_blocked=true");
        await MoveTestPlayer(player,carried.GlobalPosition);
        await PressE();
        if(carried.Pass98DepositedMaterials!=1 ||
           idle.Pass98DepositedMaterials!=0 ||
           idle.Pass98CarryingMaterial ||
           carried.Pass98CarryingMaterial)
            throw new InvalidOperationException(
                "Pass98 one E deposit simultaneously interacted with"+
                " another physically overlapping objective");
        GD.Print("TWR_PASS98_SHARED_E_PRESS_OK"+
            " overlapping_objectives=2"+
            " held_pickup_total=1 deposit_total=1"+
            " world_actors_single_press=true");
        first.QueueFree();
        second.QueueFree();
        await Frames(4);
    }

    private async Task VerifyMap(string map)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass98 previous map was not returned to offline lobby");
        Runtime.StartMap(map);
        var game=new GameplayRoot
        {
            Name="Pass98Production"+map,
            MapName=map,
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || Runtime.Player?.IsAlive!=true ||
           game.GetNodeOrNull<GameplayHud>("HUD") is null ||
           game.Pass87FallbackObjectiveMarkers.Count<1)
            throw new InvalidOperationException(
                "Pass98 needs actual playable fallback "+map);
        for(var n=0;n<90 && !player.IsOnFloor();n++)
            await Frames(1);
        if(!player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass98 player not on physical map floor "+map);
        game.SetProcess(false);
        if(map=="Ranch")
            await VerifyContendedPress(game,player);

        // Ranch/Manor production wave catalogs select Load on Wave1 and
        // Repair on Wave2. Only the stage clock is accelerated.
        for(var wave=1;wave<=2;wave++)
        {
            if(Runtime.Match?.Wave!=wave ||
               game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass98 unexpected production countdown "+map+"/"+wave);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            if(Runtime.Match?.Phase!=MatchPhase.Wave ||
               game.WaveStage!="WAVE")
                throw new InvalidOperationException(
                    "Pass98 did not enter real Wave "+wave+" "+map);
            var family=wave==1?"Load":"Repair";
            var objective=game.GetChildren().OfType<ObjectiveRuntime>()
                .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                    !x.IsQueuedForDeletion() && x.Family==family);
            if(objective is null || !objective.UseFallbackMaterialGrounding96 ||
               !objective.Pass96GroundedMaterialLayout ||
               objective.Pass98RemainingMaterials<10)
                throw new InvalidOperationException(
                    "Pass98 production material objective missing "+map+
                    " wave="+wave+" family="+family);
            var positions=objective.Pass96MaterialFeet.ToArray();
            var count=positions.Length;
            var deposit=objective.GlobalPosition;
            var id=objective.ObjectiveId;
            var completed=0;
            objective.Completed+=_=>completed++;
            if(objective.Pass98RequiredMaterials!=count)
                throw new InvalidOperationException(
                    "Pass98 wrong required material count "+family);
            var expected=ExpectedReward(
                Runtime.Player!.Credits,Runtime.Player.Xp,Runtime.Player.Level);

            for(var item=0;item<count;item++)
            {
                if(Runtime.Player?.IsAlive!=true ||
                   !GodotObject.IsInstanceValid(objective))
                    throw new InvalidOperationException(
                        "Pass98 objective/player vanished during carry loop");

                await MoveTestPlayer(player,positions[item]);
                if(objective.Pass98CarryingMaterial ||
                   objective.Pass98DepositedMaterials!=item ||
                   objective.Pass98RemainingMaterials!=count-item)
                    throw new InvalidOperationException(
                        "Pass98 stale material inventory before pickup"+
                        " map="+map+" family="+family+
                        " item="+item);

                // Hold E through multiple render/physics updates:
                // the same press must never remove two items.
                E(true);
                await Frames(12);
                if(!objective.Pass98CarryingMaterial ||
                   objective.Pass98RemainingMaterials!=count-item-1 ||
                   objective.Pass98DepositedMaterials!=item)
                    throw new InvalidOperationException(
                        "Pass98 held-E pickup removed more than one item"+
                        " map="+map+" family="+family+" item="+item+
                        " remain="+objective.Pass98RemainingMaterials);
                E(false);
                await Frames(PressFrames);

                await MoveTestPlayer(player,deposit);
                await PressE();
                if(completed==0 &&
                   (objective.Pass98CarryingMaterial ||
                    objective.Pass98DepositedMaterials!=item+1 ||
                    objective.Pass98RemainingMaterials!=count-item-1))
                    throw new InvalidOperationException(
                        "Pass98 real E-deposit failed "+map+
                        " family="+family+" item="+item);
                if(item<count-1 && completed!=0)
                    throw new InvalidOperationException(
                        "Pass98 objective completed before all items deposited"+
                        " map="+map+" family="+family+" item="+item);
                _carriedPieces++;
            }

            await Frames(3);
            if(completed!=1 ||
               Runtime.Match?.CompletedObjectives.Contains(id)!=true)
                throw new InvalidOperationException(
                    "Pass98 objective failed domain completion "+
                    map+"/"+family+" callbacks="+completed);
            CheckReward(expected,map+"/"+family+" first completion");
            Runtime.CompleteObjective(id,family);
            await Frames(2);
            if(completed!=1)
                throw new InvalidOperationException(
                    "Pass98 completion callback fired twice "+id);
            CheckReward(expected,map+"/"+family+" replayed receipt");
            GD.Print("TWR_PASS98_OBJECTIVE_E_COMPLETE_OK"+
                " map="+map+" wave="+wave+
                " family="+family+
                " items="+count+
                " pickup_deposit_E_presses="+(2*count)+
                " domain_rewards_once=true"+
                " real_floor_and_occlusion=true");
            _completedMissions++;

            // Continue the original WaveEnd/Intermission transitions, not
            // by changing the domain's wave number or granting wave XP.
            if(wave==1)
            {
                game._Process(RegularWaveRules.WaveDurationSeconds+.25);
                game._Process(RegularWaveRules.WaveEndSeconds+.25);
                game._Process(RegularWaveRules.IntermissionSeconds+.25);
                if(Runtime.Match?.Wave!=2 ||
                   game.WaveStage!="COUNTDOWN")
                    throw new InvalidOperationException(
                        "Pass98 real Wave1->2 advancement failed "+map);
                await Frames(3);
            }
        }
        Runtime.FailMatch("Pass98 isolated material-interaction acceptance");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           !Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass98 offline result did not save "+map);
        game.QueueFree();
        await Frames(4);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass98 did not restore local lobby "+map);
    }
}

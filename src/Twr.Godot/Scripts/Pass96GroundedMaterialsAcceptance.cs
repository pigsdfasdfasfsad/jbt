using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Native exported Windows contract on the ten real PUBLIC fallback maps.
/// Verifies grounded Load/Repair geometry before doing an actual Ranch
/// E-key pickup/deposit. Does not invent retail map topology or place private
/// source packs in CI.
/// </summary>
public partial class Pass96GroundedMaterialsAcceptance : Node
{
    public LocalSessionNode Runtime {get;set;}=null!;
    private static readonly string[] Maps =
    [
        "Ranch","Mill","Bypass","Cabin","Cargo",
        "District","Prison","Laboratory","Expressway","Manor"
    ];
    private int _completedMaps;
    private int _checkedItems;
    private int _overlapFallbacks;

    public override async void _Ready()
    {
        try
        {
            if(Runtime.Match?.Phase!=MatchPhase.Lobby ||
               Runtime.Session is null)
                throw new InvalidOperationException(
                    "Pass96 needs isolated offline profile");
            foreach(var map in Maps)
                await CheckMap(map);
            if(_completedMaps!=Maps.Length || _checkedItems<200)
                throw new InvalidOperationException(
                    "Pass96 failed ten-map material totals maps="+
                    _completedMaps+" items="+_checkedItems);
            GD.Print("TWR_SMOKE_PASS96_GROUNDED_MATERIALS_OK"+
                " maps=10 families=Load,Repair"+
                " real_material_points="+_checkedItems+
                " same_floor=true solid_capsule_routes=true"+
                " overlap_fallbacks="+_overlapFallbacks+
                " retail_source_positions_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS96_GROUNDED_MATERIALS_FAILED "+
                " maps="+_completedMaps+
                " items="+_checkedItems+" "+
                e.GetType().Name+" "+e.Message+"\n"+e.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for(var frame=0;frame<count;frame++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static void KeyInput(bool pressed)
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
        KeyInput(true);
        await Frames(3);
        KeyInput(false);
        await Frames(3);
    }

    private async Task CheckMap(string map)
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass96 leaked previous map into "+map);
        Runtime.StartMap(map);
        var game=new GameplayRoot
        {
            Name="Pass96"+map,
            MapName=map,
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass96 no playable production map/player "+map);
        game.SetProcess(false);
        await Frames(50);
        if(game.Pass87FallbackObjectiveMarkers.Count==0)
            throw new InvalidOperationException(
                "Pass96 private or empty map; expected fallback "+map);

        var marker=game.Pass87FallbackObjectiveMarkers[0];
        if(map=="Manor")
        {
            if(game.Pass82UpperObjectiveMarkers.Count!=3)
                throw new InvalidOperationException(
                    "Pass96 three upper Manor objectives missing");
            marker=game.Pass82UpperObjectiveMarkers[0];
        }
        foreach(var family in new[]{"Load","Repair"})
        {
            var objective=new ObjectiveRuntime
            {
                Name="Pass96"+family,
                ObjectiveId=map+":Pass96:"+family,
                Family=family,
                Player=player,
                Runtime=Runtime,
                UseFallbackMaterialGrounding96=true,
                Position=marker
            };
            var status="";
            objective.StatusChanged=value=>status=value;
            game.AddChild(objective);
            if(!objective.Pass96GroundedMaterialLayout)
                throw new InvalidOperationException(
                    "Pass96 no fully supported material plan"+
                    " map="+map+" family="+family+
                    " marker="+marker+
                    " rejected="+objective.Pass96RejectedMaterialCandidates);
            var count=objective.Pass96MaterialFeet.Count;
            if(count<10 || count>12)
                throw new InvalidOperationException(
                    "Pass96 lost required material count "+family+
                    " map="+map+" count="+count);
            var exclude=new global::Godot.Collections.Array<Rid>
            {
                player.GetRid()
            };
            var deposit=objective.GlobalPosition;
            foreach(var itemFeet in objective.Pass96MaterialFeet)
            {
                if(Math.Abs(itemFeet.Y-deposit.Y)>.35f ||
                   !Pass89EscortSurfaceSafety.CanTravel(
                       game.GetWorld3D(),deposit,itemFeet,exclude))
                    throw new InvalidOperationException(
                        "Pass96 unreachable real material"+
                        " map="+map+" family="+family+
                        " deposit="+deposit+" material="+itemFeet);
                _checkedItems++;
            }
            _overlapFallbacks+=
                objective.Pass96OverlappingMaterialFallbacks;

            // Real keyboard interaction on the real Ranch material pickup.
            // The world geometry and objective count remain unchanged.
            if(map=="Ranch" && family=="Load")
            {
                player.GlobalPosition=objective.Pass96MaterialFeet[0]+
                    Vector3.Up*1.1f;
                await Frames(24);
                await PressE();
                if(!status.Contains("CARRYING ITEM",
                    StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Pass96 physical E pickup was not carried"+
                        " player="+player.GlobalPosition+
                        " item="+objective.Pass96MaterialFeet[0]);
                player.GlobalPosition=deposit+Vector3.Up*1.1f;
                await Frames(24);
                await PressE();
                if(!status.Contains("1/10 INSERTED",
                    StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Pass96 actual player E deposit failed status="+status);
            }
            GD.Print("TWR_PASS96_MAP_MATERIALS_OK"+
                " map="+map+
                " family="+family+
                " count="+count+
                " depositY="+deposit.Y.ToString("F2")+
                " overlap_fallbacks="+
                    objective.Pass96OverlappingMaterialFallbacks+
                " rejected_candidates="+
                    objective.Pass96RejectedMaterialCandidates);
            objective.QueueFree();
            await Frames(3);
        }
        Runtime.FailMatch("Pass96 isolated grounded-materials acceptance");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass96 offline match result did not commit "+map);
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass96 could not restore local lobby "+map);
        _completedMaps++;
    }
}

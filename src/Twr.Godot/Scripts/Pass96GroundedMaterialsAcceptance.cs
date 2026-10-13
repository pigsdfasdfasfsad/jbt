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
    // Pass100 measures genuine CharacterBody3D locomotion, not teleport
    // between an item's position and its deposit point.
    private int _walkedMaterialRoundTrips100;
    private float _walkedMetres100;
    // Start these open-corridor fallback missions at the real map player
    // spawn; keep other maps' Pass100 isolated depot-entry gate intact.
    private static readonly string[] SpawnIngressMaps101 =
        ["Ranch","Mill","Bypass","District","Prison"];
    private int _spawnToDepotWalks101;
    private float _spawnToDepotMetres101;

    // Pass102 adds collision-audited physical navigation from the untouched
    // initial spawn on the remaining four ground-level fallback maps and
    // through Manor's actual reconstructed WestWing staircase.
    private static readonly string[] SpawnIngressMaps102 =
        ["Cabin","Cargo","Laboratory","Expressway","Manor"];
    private int _spawnToDepotWalks102;
    private float _spawnToDepotMetres102;
    private int _verifiedManorStairClimbs102;

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
            if(_completedMaps!=Maps.Length || _checkedItems<200 ||
               _walkedMaterialRoundTrips100!=Maps.Length*2 ||
               _walkedMetres100<25f ||
               _spawnToDepotWalks101!=SpawnIngressMaps101.Length ||
               _spawnToDepotMetres101<20f ||
               _spawnToDepotWalks102!=SpawnIngressMaps102.Length ||
               _spawnToDepotMetres102<20f ||
               _verifiedManorStairClimbs102!=1)
                throw new InvalidOperationException(
                    "Pass96 failed ten-map material totals maps="+
                    _completedMaps+" items="+_checkedItems);
            GD.Print("TWR_SMOKE_PASS96_GROUNDED_MATERIALS_OK"+
                " maps=10 families=Load,Repair"+
                " real_material_points="+_checkedItems+
                " same_floor=true solid_capsule_routes=true"+
                " overlap_fallbacks="+_overlapFallbacks+
                " retail_source_positions_unverified=true");
            GD.Print("TWR_SMOKE_PASS100_MATERIAL_WALK_OK"+
                " maps=10 objective_families=Load,Repair"+
                " material_roundtrips="+_walkedMaterialRoundTrips100+
                " walked_metres="+_walkedMetres100.ToString("F1")+
                " actual_forward_key=true entry_setup_only=true"+
                " human_operated_waves_unverified=true");
            GD.Print("TWR_SMOKE_PASS101_SPAWN_OBJECTIVE_INGRESS_OK"+
                " maps="+_spawnToDepotWalks101+
                " distance_metres="+_spawnToDepotMetres101.ToString("F1")+
                " actual_spawn_positions=true"+
                " forward_key_movement=true"+
                " remaining_maps_depot_entry_test_controlled=true"+
                " retail_source_positions_unverified=true");
            GD.Print("TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK"+
                " newly_verified_maps="+_spawnToDepotWalks102+
                " total_verified_maps="+
                    (_spawnToDepotWalks101+_spawnToDepotWalks102)+
                " new_walked_metres="+_spawnToDepotMetres102.ToString("F1")+
                " manor_stair_climbs="+_verifiedManorStairClimbs102+
                " W_key=true no_spawn_teleports=true"+
                " actual_reconstructed_collision=true"+
                " owner_original_map_fidelity_unverified=true");
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

    private static void Forward100(bool pressed)
    {
        // Godot's actual player physics reads W. No physics actor,
        // objective, collider or material is moved by this input helper.
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode=Key.W,
            PhysicalKeycode=Key.W,
            Pressed=pressed
        });
    }

    private async Task<float> WalkTo100(
        FirstPersonPlayer player,Vector3 targetFeet,string context)
    {
        // Test operator adjusts only facing, analogous to moving the
        // mouse; horizontal motion comes solely from normal W input,
        // FirstPersonPlayer._PhysicsProcess and MoveAndSlide.
        var travelled=0f;
        var reached=false;
        Forward100(true);
        try
        {
            for(var frame=0;frame<360;frame++)
            {
                var direction=targetFeet-player.GlobalPosition;
                direction.Y=0;
                if(direction.Length()<=.80f)
                {
                    reached=true;
                    break;
                }
                player.Rotation=new Vector3(
                    0,Mathf.Atan2(-direction.X,-direction.Z),0);
                var prior=player.GlobalPosition;
                await Frames(1);
                var step=player.GlobalPosition.DistanceTo(prior);
                if(step>.55f)
                    throw new InvalidOperationException(
                        "Pass100 discontinuous player movement "+
                        context+" step="+step);
                travelled+=step;
                if(Runtime.Player?.IsAlive!=true)
                    throw new InvalidOperationException(
                        "Pass100 player died during movement "+context);
            }
        }
        finally
        {
            Forward100(false);
        }
        await Frames(4);
        var separation=targetFeet-player.GlobalPosition;
        separation.Y=0;
        if(!reached || separation.Length()>1.15f ||
           !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass100 forward-key traversal blocked "+
                context+" final="+player.GlobalPosition+
                " target="+targetFeet+" travelled="+travelled);
        return travelled;
    }


    // A walk plan only controls facing and ordinary keyboard input.
    // It never mutates the player's position/velocity, objective transform,
    // floor geometry, or NPC navigation. Every graph segment is audited
    // against live body-size collision before its W-key traversal.
    private async Task<float> WalkGroundIngress102(
        GameplayRoot game,FirstPersonPlayer player,
        Vector3 destination,string context)
    {
        var world=game.GetWorld3D();
        var exclude=new global::Godot.Collections.Array<Rid>{player.GetRid()};
        var current=player.GlobalPosition;
        Vector3[] route;
        if(Pass89EscortSurfaceSafety.CanTravel(
                world,current,destination,exclude))
            route=[destination];
        else
        {
            var navigator=game.GetNodeOrNull<Pass73GroundNavigationRuntime>(
                game.MapName=="Expressway"
                    ? "Pass92ExpresswayEscortGroundNavigation"
                    : "Pass73GroundNavigation");
            if(navigator is null || !navigator.IsGraphReady)
                throw new InvalidOperationException(
                    "Pass102 no ready collision graph "+context);
            route=navigator.GetVerifiedEscortRoute89(
                current,destination,player.GetRid());
            if(route.Length==0 || route.Length>160)
                throw new InvalidOperationException(
                    "Pass102 no verified walkable objective ingress route "+
                    context+" from="+current+" to="+destination);
        }

        var metres=0f;
        var last=current;
        foreach(var waypoint in route)
        {
            var d=waypoint-last;
            d.Y=0;
            if(d.Length()>.65f)
            {
                if(!Pass89EscortSurfaceSafety.CanTravel(
                        world,last,waypoint,exclude))
                    throw new InvalidOperationException(
                        "Pass102 physics route invalidated "+context+
                        " previous="+last+" next="+waypoint);
                metres+=await WalkTo100(
                    player,waypoint,context+"/waypoint");
            }
            last=player.GlobalPosition;
        }
        var separation=destination-player.GlobalPosition;
        separation.Y=0;
        if(separation.Length()>1.15f ||
           Math.Abs(player.GlobalPosition.Y-destination.Y)>.60f ||
           !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass102 player missed grounded objective "+context+
                " at="+player.GlobalPosition+
                " destination="+destination);
        return metres;
    }

    private async Task<float> WalkManorUpperIngress102(
        GameplayRoot game, FirstPersonPlayer player,
        Vector3 objectiveDepot)
    {
        var stairs=game.Pass77ApproximateStairs;
        var stair=stairs.FirstOrDefault(
            item=>item.Name=="WestWingUpper");
        if(stair is null || stairs.Count!=3)
            throw new InvalidOperationException(
                "Pass102 three real reconstructed Manor ramps unavailable");
        var sign=Math.Sign(stair.UpperFeet.Z-stair.LowerFeet.Z);
        if(sign==0)throw new InvalidOperationException(
            "Pass102 invalid Manor staircase direction");
        var approach=stair.LowerFeet+
            new Vector3(0f,0f,-sign*.75f);
        var walked=await WalkGroundIngress102(
            game,player,approach,"Manor/spawn-to-WestWing-lower");

        // The supported stair itself must be climbed continuously. The
        // ramp and deck are production physics meshes, not a teleport.
        // Walk beyond the landing to require full upstairs elevation.
        var upstairs=stair.UpperFeet+
            new Vector3(0f,0f,sign*1.0f);
        walked+=await WalkTo100(
            player,upstairs,"Manor/WestWing/ramp-climb");
        if(!player.IsOnFloor() ||
           player.GlobalPosition.Y<stair.UpperFeet.Y-.18f)
            throw new InvalidOperationException(
                "Pass102 actual Manor player failed to climb stairs"+
                " at="+player.GlobalPosition+" top="+stair.UpperFeet);

        // Walk laterally onto the landing's connected upper deck, then
        // to the authored fallback mission marker on that same deck.
        var side=stair.UpperFeet+
            Vector3.Left*4.25f+
            new Vector3(0f,0f,sign*1.0f);
        walked+=await WalkTo100(
            player,side,"Manor/WestWing/upper-side-deck");
        walked+=await WalkTo100(
            player,objectiveDepot,"Manor/WestWing/upper-Load-depot");
        if(Math.Abs(player.GlobalPosition.Y-objectiveDepot.Y)>.50f ||
           !player.IsOnFloor())
            throw new InvalidOperationException(
                "Pass102 upper Manor objective not reached on its floor");
        _verifiedManorStairClimbs102++;
        return walked;
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
        // Pass73 samples its ground graph during physics frames;
        // Pass102 must not consume an incomplete route cache.
        await Frames(90);
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

            // Pass100: one entry-only test positioning at the actual
            // collision-supported Load depot, then entirely physical W
            // movement to/from each family's first grounded material.
            // The same on-foot player is reused for Repair. Only natural
            // loot actors are removed from this isolated test scene so
            // randomized unrelated E-key recipients cannot steal a press.
            if(family=="Load")
            {
                foreach(var pickup in game.GetChildren().OfType<PickupActor>())
                    pickup.QueueFree();
                if (SpawnIngressMaps101.Contains(map))
                {
                    // Unlike the previous depot-only acceptance, this
                    // player begins at GameplayRoot's real PlayerSpawn.
                    // Walk toward the live Load objective on the actual
                    // built map; do not set position, velocity or collision.
                    if(!player.IsOnFloor())
                        throw new InvalidOperationException(
                            "Pass101 player spawn unsupported "+map+
                            " spawn="+player.GlobalPosition);
                    var initial=player.GlobalPosition;
                    var walked=await WalkTo100(
                        player,deposit,map+"/spawn-to-Load-depot");
                    var travelled=new Vector2(
                        player.GlobalPosition.X-initial.X,
                        player.GlobalPosition.Z-initial.Z).Length();
                    if(walked<2f || travelled<2f)
                        throw new InvalidOperationException(
                            "Pass101 player did not traverse actual spawn"+
                            " map="+map+" walked="+walked);
                    _spawnToDepotWalks101++;
                    _spawnToDepotMetres101+=walked;
                    GD.Print("TWR_PASS101_MAP_SPAWN_INGRESS_OK map="+map+
                        " player_spawn="+initial+
                        " objective_depot="+deposit+
                        " walked_metres="+walked.ToString("F1")+
                        " player_w_key=true"+
                        " teleport_after_spawn=false");
                }
                else if (SpawnIngressMaps102.Contains(map))
                {
                    // The test player is still at the normal map spawn.
                    // Only its facing and W key are controlled from here.
                    if(!player.IsOnFloor())
                        throw new InvalidOperationException(
                            "Pass102 player is not grounded at map spawn "+map);
                    var spawn=player.GlobalPosition;
                    var walked=map=="Manor"
                        ? await WalkManorUpperIngress102(
                            game,player,deposit)
                        : await WalkGroundIngress102(
                            game,player,deposit,
                            map+"/spawn-to-Load-depot");
                    if(walked<2f)
                        throw new InvalidOperationException(
                            "Pass102 no meaningful physical spawn movement "+
                            map+" metres="+walked);
                    _spawnToDepotWalks102++;
                    _spawnToDepotMetres102+=walked;
                    GD.Print("TWR_PASS102_MAP_SPAWN_INGRESS_OK map="+map+
                        " player_spawn="+spawn+
                        " objective_depot="+deposit+
                        " walked_metres="+walked.ToString("F1")+
                        " W_key=true no_route_teleport=true"+
                        " grounded_arrival=true");
                }
                else
                    throw new InvalidOperationException(
                        "Pass102 unclassified fallback map "+map);
            }
            else
            {
                // Repair's depot is planned against the same actual map
                // colliders. Walk any slight shift; never teleport again.
                var shift=deposit-player.GlobalPosition;
                shift.Y=0;
                if(shift.Length()>1f)
                    _walkedMetres100+=await WalkTo100(
                        player,deposit,map+"/Repair/entry");
            }

            var initialRemaining=objective.Pass98RemainingMaterials;
            var routeContext=map+"/"+family;
            var forward=await WalkTo100(
                player,objective.Pass96MaterialFeet[0],
                routeContext+"/to material");
            await PressE();
            if(!status.Contains("CARRYING ITEM",
                StringComparison.Ordinal) ||
               !objective.Pass98CarryingMaterial ||
               objective.Pass98RemainingMaterials!=initialRemaining-1)
                throw new InvalidOperationException(
                    "Pass100 walked E pickup was not actually carried"+
                    " context="+routeContext+
                    " player="+player.GlobalPosition+
                    " item="+objective.Pass96MaterialFeet[0]);

            var backward=await WalkTo100(
                player,deposit,routeContext+"/return to depot");
            await PressE();
            if(!status.Contains($"1/{count} INSERTED",
                StringComparison.Ordinal) ||
               objective.Pass98CarryingMaterial ||
               objective.Pass98DepositedMaterials!=1)
                throw new InvalidOperationException(
                    "Pass100 walked E deposit failed"+
                    " context="+routeContext+" status="+status);
            _walkedMaterialRoundTrips100++;
            _walkedMetres100+=forward+backward;
            GD.Print("TWR_PASS100_MAP_WALK_OK map="+map+
                " family="+family+
                " material_pickups=1 deposits=1"+
                " walked_metres="+(forward+backward).ToString("F1")+
                " no_route_teleport=true"+
                " player_physics=MoveAndSlide");
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

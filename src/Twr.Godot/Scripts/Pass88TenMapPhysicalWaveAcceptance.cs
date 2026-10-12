using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass88 exported Windows gameplay acceptance: real production map, enemy,
/// pickup and Wave 1/2 actors for all ten maps, followed by a REAL Manor
/// Wave 3 Radio objective verifying the downstairs/upper-floor secure guard.
/// Stage clocks are fast-forwarded only in this isolated native test; this
/// is not proof of fifteen manually survived waves.
/// </summary>
public partial class Pass88TenMapPhysicalWaveAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker =
        "TWR_SMOKE_PASS88_TEN_MAP_PHYSICAL_WAVES_OK";
    private int _verified;

    public override async void _Ready()
    {
        try
        {
            if(Runtime.Match?.Phase != MatchPhase.Lobby ||
               Runtime.Session is null ||
               ReleaseRules.Maps.Length != 10 ||
               MapCatalogRuntime.All().Count != 10)
                throw new InvalidOperationException(
                    "Pass88 requires isolated offline ten-map lobby");
            foreach(var map in ReleaseRules.Maps)
                await TestLiveMap(map);
            if(_verified != 10)
                throw new InvalidOperationException(
                    "Pass88 failed ten native scenes");
            GD.Print(SuccessMarker+
                " maps=10" +
                " real_first_wave_infected=10" +
                " real_second_wave_infected=10" +
                " grounded_fallback_loot=10" +
                " validated_fallback_spawns=10" +
                " manor_third_wave_secure_floor_guard=verified" +
                " isolated_authoritative_profile=true" +
                " retail_mesh_fidelity_unverified=true" +
                " no_full_fifteen_wave_playthrough_claim=true");
            GetTree().Quit(0);
        }
        catch(Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS88_TEN_MAP_PHYSICAL_WAVES_FAILED "+
                "completed_maps="+_verified+" "+ex.GetType().Name+
                " "+ex.Message+"\n"+ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static ObjectiveRuntime FirstObjective(GameplayRoot game)
    {
        var objective=game.GetChildren()
            .OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion());
        return objective ?? throw new InvalidOperationException(
            "Missing authentic wave objective actor");
    }

    private static void FinishWaveAndStartNext(
        GameplayRoot game, int nextWave)
    {
        // These are TEST-ONLY simulated stage clock increments. The wave
        // engine still constructs real enemies/pickups/objective actors,
        // grants rewards and calls match progression through production
        // commands. Physical enemy walking is tested between clock skips.
        game._Process(RegularWaveRules.WaveDurationSeconds+.25);
        if(game.WaveStage != "WAVE END" ||
           game.ActiveInfectedCount!=0)
            throw new InvalidOperationException(
                "Pass88 wave survivor cleanup failed");
        game._Process(RegularWaveRules.WaveEndSeconds+.25);
        if(game.WaveStage!="INTERMISSION")
            throw new InvalidOperationException(
                "Pass88 intermission did not begin");
        game._Process(RegularWaveRules.IntermissionSeconds+.25);
        if(game.WaveStage!="COUNTDOWN")
            throw new InvalidOperationException(
                "Pass88 next-wave countdown missing");
        game._Process(RegularWaveRules.CountdownSeconds+.25);
        if(game.WaveStage!="WAVE")
            throw new InvalidOperationException(
                "Pass88 next wave not started");
    }

    private async Task TestLiveMap(string map)
    {
        if(Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass88 preceding map session did not return to lobby");
        Runtime.StartMap(map);
        var game=new GameplayRoot
        {
            Name="Pass88Actual"+map,
            MapName=map,
            Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null || player.CaptureCamera is null ||
           game.GetNodeOrNull<GameplayHud>("HUD") is null ||
           Runtime.Player?.IsAlive!=true ||
           Runtime.Match?.MapName!=map ||
           game.ActivePickupCount<1 ||
           game.VerifiedFallbackNaturalPickups86<1)
            throw new InvalidOperationException(
                "Pass88 real map/HUD/validated loot missing on "+map+
                " available="+game.ActivePickupCount+
                " accepted="+game.VerifiedFallbackNaturalPickups86+
                " rejected="+game.RejectedFallbackNaturalPickups86);

        await Frames(75);
        if(!player.IsOnFloor() || Runtime.Player?.IsAlive!=true)
            throw new InvalidOperationException(
                "Pass88 player has no real walkable collision at "+map);

        // Assert the actual initially spawned natural pickup actors are
        // still above fully supported collision surfaces. Source-original
        // map items would not be moved or culled by Pass86.
        foreach(var pickup in game.GetChildren().OfType<PickupActor>())
        {
            if(!GodotObject.IsInstanceValid(pickup) ||
               pickup.IsQueuedForDeletion()) continue;
            var supported=Pass86NaturalLootPlacement.TryGround(
                game.GetWorld3D(),pickup.GlobalPosition);
            if(!supported.HasValue ||
               pickup.GlobalPosition.DistanceTo(supported.Value)>.13f)
                throw new InvalidOperationException(
                    "Pass88 natural loot buried/unsupported map="+map+
                    " pickup="+pickup.Name+
                    " world="+pickup.GlobalPosition);
        }

        game._Process(RegularWaveRules.CountdownSeconds+.10);
        game._Process(.05);
        if(game.WaveStage!="WAVE" || Runtime.Match?.Wave!=1)
            throw new InvalidOperationException(
                "Pass88 first wave failed to start "+map);
        var objective1=FirstObjective(game);
        CheckFallbackAnchor(game,map,1,objective1);

        // The real wave engine must spawn at least one physics-driven
        // infected with a guaranteed 11m non-embedded fallback placement.
        for(var attempt=0;attempt<5 && game.ActiveInfectedCount==0;attempt++)
            game._Process(RegularWaveRules.SpawnIntervalSeconds(1)+.1);
        var enemy=game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(GodotObject.IsInstanceValid);
        if(enemy is null || enemy.Target!=player ||
           game.VerifiedFallbackSpawns85<1)
            throw new InvalidOperationException(
                "Pass88 real first-wave infected not safely spawned "+map+
                " refused="+game.RejectedFallbackSpawns85);
        var from=new Vector2(
            enemy.GlobalPosition.X-player.GlobalPosition.X,
            enemy.GlobalPosition.Z-player.GlobalPosition.Z).Length();
        if(from<10.95f)
            throw new InvalidOperationException(
                "Pass88 infected spawned unacceptably near player map="+map+
                " distance="+from.ToString("F2"));
        await Frames(16);
        if(!GodotObject.IsInstanceValid(enemy) ||
           !enemy.IsOnFloor() ||
           enemy.SourceJumpAttempts!=0)
            throw new InvalidOperationException(
                "Pass88 first-wave infected did not use real floor movement "+map);

        FinishWaveAndStartNext(game,2);
        if(Runtime.Match?.Wave!=2 ||
           game.ActiveInfectedCount!=0)
            throw new InvalidOperationException(
                "Pass88 first wave rewards/teardown missing map="+map);
        var objective2=FirstObjective(game);
        CheckFallbackAnchor(game,map,2,objective2);
        game._Process(.05);
        await Frames(3); // retire QueueFree actors from the previous wave
        var wave2=game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                !x.IsQueuedForDeletion());
        if(wave2 is null || wave2.Target!=player)
            throw new InvalidOperationException(
                "Pass88 live Wave 2 infected missing map="+map);
        await Frames(9);
        if(!GodotObject.IsInstanceValid(wave2) ||
           wave2.IsQueuedForDeletion())
            throw new InvalidOperationException(
                "Pass88 real second-wave enemy vanished map="+map);

        if(map=="Manor")
        {
            FinishWaveAndStartNext(game,3);
            if(Runtime.Match?.Wave!=3)
                throw new InvalidOperationException(
                    "Pass88 Manor expected Wave 3");
            await VerifyManorSecureFloor(game,player);
        }

        GD.Print("TWR_PASS88_MAP_OK map="+map+
            " real_infected=true" +
            " natural_loot="+game.VerifiedFallbackNaturalPickups86+
            " spawn_approved="+game.VerifiedFallbackSpawns85+
            " rejected_spawns="+game.RejectedFallbackSpawns85+
            " objective_wave2=true");

        Runtime.FailMatch("Pass88 isolated native physical-wave acceptance");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           Runtime.Match.SaveCommitted!=true)
            throw new InvalidOperationException(
                "Pass88 failed authoritative results save map="+map);
        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass88 failed isolated lobby roundtrip on "+map);
        _verified++;
    }

    private static void CheckFallbackAnchor(
        GameplayRoot game,string map,int wave,ObjectiveRuntime objective)
    {
        if(map=="Manor") return; // preserve Pass82 three-floor cadence.
        var markers=game.Pass87FallbackObjectiveMarkers;
        if(markers.Count<1)
            throw new InvalidOperationException(
                "Pass88 no fallback objective markers "+map);
        var expected=markers[Pass87FallbackObjectiveRotation.GetIndex(
            markers.Count,wave,0)];
        if(expected.DistanceTo(objective.GlobalPosition)>.02f)
            throw new InvalidOperationException(
                "Pass88 objective did not rotate to physical marker "+
                map+" wave="+wave);
    }

    private async Task VerifyManorSecureFloor(
        GameplayRoot game,FirstPersonPlayer player)
    {
        // Actual Wave 3 source-catalog Radio objective at the first
        // reconstructed upper-floor objective marker, not a fake test
        // actor or a direct health/credit/wave state mutation.
        var objective=game.GetChildren().OfType<ObjectiveRuntime>()
            .FirstOrDefault(x=>GodotObject.IsInstanceValid(x) &&
                x.Family=="Radio" && x.GlobalPosition.Y>2.5f);
        if(objective is null || !objective.RequireSameFloorSecure84 ||
           game.Pass82UpperObjectiveMarkers.Count!=3)
            throw new InvalidOperationException(
                "Pass88 missing production Wave 3 upper Radio objective");

        var completed=0;
        objective.Completed+=_=>completed++;
        var target=objective.GlobalPosition;
        // Teleport only the TEST PLAYER once to establish the two
        // separate initial test positions, never teleport any enemy.
        player.GlobalPosition=new Vector3(target.X,.05f,target.Z);
        await Frames(8);
        if(Pass84SecureZoneFloorSafety.CanAccumulate(
                player.GlobalPosition,target))
            throw new InvalidOperationException(
                "Pass88 rejected lower-floor precondition not satisfied");
        objective._Process(RegularWaveRules.WaveDurationSeconds/2.0);
        if(completed!=0 || objective.IsQueuedForDeletion())
            throw new InvalidOperationException(
                "Pass88 upper Radio objective completed from below floor");

        player.GlobalPosition=target+Vector3.Down*.30f+
            Vector3.Forward*.20f;
        await Frames(12);
        if(!player.IsOnFloor() ||
           !Pass84SecureZoneFloorSafety.CanAccumulate(
                player.GlobalPosition,target))
            throw new InvalidOperationException(
                "Pass88 player not physically on upper objective floor");
        objective._Process(RegularWaveRules.WaveDurationSeconds/2.0);
        if(completed!=1)
            throw new InvalidOperationException(
                "Pass88 upper Radio objective did not complete above floor");
        GD.Print("TWR_PASS88_MANOR_FLOOR_OBJECTIVE_OK"+
            " family=Radio wave=3" +
            " denied_downstairs=true" +
            " completed_upstairs=true");
    }
}

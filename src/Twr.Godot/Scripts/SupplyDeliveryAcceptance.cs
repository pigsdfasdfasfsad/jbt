using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Executes the real gameplay supply entry point, pallet, landing callback,
/// and eight spawned pickup actors. Radio transmission timing has separate
/// inherited coverage. Only the descent time is accelerated in this fixture.
/// </summary>
public static class SupplyDeliveryAcceptance
{
    private static async Task Frames(Node node,int count)
    {
        for(var i=0;i<count;i++) await node.ToSignal(node.GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    public static async Task Verify(GameplayRoot game,FirstPersonPlayer player)
    {
        var previousControls=player.GameplayControlsEnabled;
        player.SetGameplayControlsEnabled(true);
        try
        {
            var before=game.GetChildren().OfType<PickupActor>().ToArray();
            var trigger=typeof(GameplayRoot).GetMethod("SpawnSupplyDrop",BindingFlags.Instance|BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Production supply entry point missing");
            trigger.Invoke(game,null);
            var pallet=game.GetChildren().OfType<SupplyDropRuntime>().Single();
            for(var waiting=0; waiting<40 && !pallet.Visible && !pallet.FailedDelivery; waiting++)
                await Frames(game,1);
            pallet.SetPhysicsProcess(false);
            if(pallet.FailedDelivery || !pallet.Visible)
                throw new InvalidOperationException("Ranch has no valid supply landing plan");
            pallet._PhysicsProcess(6.0);
            await Frames(game,3);
            var spawned=game.GetChildren().OfType<PickupActor>()
                .Where(p=>!before.Contains(p)).ToArray();
            if(spawned.Length!=8 || game.GetChildren().OfType<SupplyDropRuntime>().Any())
                throw new InvalidOperationException("Actual supply callback did not create exactly eight pickups and retire its pallet");
            var regular=spawned.Count(p=>p.PickupType is "Medkit" or "Ammo" or "Body Armor");
            var forts=spawned.Count(p=>p.PickupType is "50 Cal" or "Barbed Wire" or "Clap Bomb" or "Jack");
            if(regular!=4 || forts!=4)
                throw new InvalidOperationException("Recovered four-regular/four-fort supply contract changed");
            var excluded=new global::Godot.Collections.Array<Rid>{player.GetRid()};
            foreach(var pickup in spawned)
            {
                var feet=pickup.GlobalPosition-Vector3.Up*.48f;
                if(!Pass89EscortSurfaceSafety.CanTravel(game.GetWorld3D(),player.GlobalPosition,feet,excluded))
                    throw new InvalidOperationException("Landed supply spawned an inaccessible pickup");
            }
            // Calling a retired managed actor cannot create duplicate payloads.
            pallet._PhysicsProcess(20);
            await Frames(game,2);
            if(game.GetChildren().OfType<PickupActor>().Count(p=>!before.Contains(p))!=8)
                throw new InvalidOperationException("Supply landing paid its payload twice");
            foreach(var pickup in spawned) pickup.QueueFree();

            var failed=0;
            var falseLandings=0;
            var unsupported=new SupplyDropRuntime
            {
                Name="MissingFloorSupply",Position=new Vector3(5000,20,5000),GroundY=.3f,
                Landed=_=>falseLandings++,Failed=_=>failed++
            };
            game.AddChild(unsupported);
            unsupported.SetPhysicsProcess(false);
            unsupported._PhysicsProcess(20);
            await Frames(game,3);
            if(falseLandings!=0 || failed!=1 || !unsupported.FailedDelivery || unsupported.Landed76)
                throw new InvalidOperationException("Configured Y was still treated as an invented supply floor");
            unsupported._PhysicsProcess(20);
            if(failed!=1) throw new InvalidOperationException("Missing-floor cancellation fired twice");

            var blocked=new SupplyDropRuntime
            {
                Name="BlockedAirDrop",SourcePlayer=player,PlanNearSourcePlayer=true,
                Position=player.GlobalPosition+Vector3.Up*15f,
                Landed=_=>falseLandings++,Failed=_=>failed++
            };
            game.AddChild(blocked);
            for(var waiting=0; waiting<40 && !blocked.Visible && !blocked.FailedDelivery; waiting++)
                await Frames(game,1);
            blocked.SetPhysicsProcess(false);
            if(blocked.FailedDelivery) throw new InvalidOperationException("Could not create obstruction fixture");
            var obstacle=new StaticBody3D
            {
                Name="TemporarySupplyObstruction",CollisionLayer=1u,CollisionMask=0u,
                Position=new Vector3(blocked.GlobalPosition.X,blocked.GroundY+4f,blocked.GlobalPosition.Z)
            };
            game.AddChild(obstacle);
            obstacle.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=new Vector3(3,.5f,3) } });
            await Frames(game,3);
            blocked._PhysicsProcess(6);
            await Frames(game,3);
            if(falseLandings!=0 || failed!=2 || !blocked.FailedDelivery)
                throw new InvalidOperationException("Mid-flight obstacle produced an invalid successful landing");
            obstacle.QueueFree();
            GD.Print("TWR_PHYSICAL_SUPPLY_LIFECYCLE_OK actual_gameplay_request=true"+
                " valid_drop_payload=8 regular_slots=4 fort_slots=4"+
                " grounded_reachable_pickups=true landing_once=true"+
                " no_floor_cancels=true obstacle_cancels=true"+
                " guessed_height_never_lands=true descent_time_test_accelerated=true"+
                " original_helicopter_route_unverified=true");
        }
        finally { player.SetGameplayControlsEnabled(previousControls); }
        await Frames(game,3);
    }
}

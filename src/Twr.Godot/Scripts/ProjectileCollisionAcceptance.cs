using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Synthetic collision fixtures, actual projectile/player handlers.
/// Does not load or claim to validate private original map geometry.</summary>
public partial class ProjectileCollisionAcceptance : Node3D
{
    public LocalSessionNode Runtime { get; set; } = null!;
    private async Task Frames(int n)
    {
        for (var i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    private StaticBody3D Box(string name, Vector3 p, Vector3 size, uint layer=1u)
    {
        var body = new StaticBody3D { Name=name, Position=p, CollisionLayer=layer, CollisionMask=0u };
        AddChild(body);
        body.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=size } });
        return body;
    }
    private static void Require(bool ok,string why)
    {
        if (!ok) throw new InvalidOperationException(why);
    }
    public override async void _Ready()
    {
        try
        {
            Position = new Vector3(0,300,0);
            await SweepCases();
            await ActualThrow();
            await RestingFloorRemoval();
            GD.Print("TWR_PROJECTILE_COLLISION_OK sphere_edge=true thin_wall=true initial_overlap_rejected=true"+
                " owner_excluded=true actual_throw_input=true inventory_once=true fuse_preserved=true"+
                " floor_removal=true private_source_unverified=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_PROJECTILE_COLLISION_FAILED "+e);
            RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }
    private async Task SweepCases()
    {
        var wall=Box("ThinWall",new Vector3(0,1,-2),new Vector3(1,3,.025f));
        await Frames(4);
        using var sweep=new ProjectileSweep(.18f);
        var from=ToGlobal(new Vector3(.62f,1,0));
        using var ray=PhysicsRayQueryParameters3D.Create(from,from+Vector3.Forward*8f);
        Require(GetWorld3D().DirectSpaceState.IntersectRay(ray).Count==0,"Fixture centre ray unexpectedly hit wall");
        var edge=sweep.Move(GetWorld3D(),from,Vector3.Forward*8f,1u);
        Require(edge.Valid && edge.Hit && !edge.InitialOverlap && edge.Centre.Z> -2,
            "Sphere edge or fast thin-wall sweep tunneled");
        var embedded=sweep.Move(GetWorld3D(),wall.GlobalPosition,Vector3.Forward*8f,1u);
        Require(embedded.InitialOverlap,"Initial overlap was ignored by shape motion");
        var invalid=sweep.Move(GetWorld3D(),new Vector3(float.NaN,0,0),Vector3.Forward,1u);
        Require(!invalid.Valid,"Nonfinite physics origin accepted");
        var rocket=new ExplosiveProjectileRuntime { Position=new Vector3(0,1,0),Damage=1,Speed=600 };
        AddChild(rocket); rocket.SetPhysicsProcess(false);
        rocket._PhysicsProcess(.016);
        Require(rocket.Resolved && !rocket.CancelledInvalidOrigin && rocket.ResolutionPosition.Z> -2,
            "Actual fast rocket crossed thin wall");
        var invalidRocket=new ExplosiveProjectileRuntime { Position=wall.Position,Damage=1000 };
        AddChild(invalidRocket); invalidRocket.SetPhysicsProcess(false);
        invalidRocket._PhysicsProcess(.016);
        Require(invalidRocket.CancelledInvalidOrigin,"Embedded rocket invented an exit/explosion point");
        wall.QueueFree(); await Frames(3);
        // The same collision policy must retain infected and source-server layers.
        foreach(var layer in new[]{2u,Pass36SourceFragmentsRuntime.SourceServerCollisionLayer})
        {
            var collider=Box("LayerFixture",new Vector3(0,1,-2),new Vector3(3,3,.03f),layer);
            await Frames(2);
            var hit=sweep.Move(GetWorld3D(),ToGlobal(new Vector3(0,1,0)),Vector3.Forward*8,
                1u|2u|Pass36SourceFragmentsRuntime.SourceServerCollisionLayer);
            Require(hit.Hit && !hit.InitialOverlap,"Projectile lost existing collision layer "+layer);
            collider.QueueFree(); await Frames(2);
        }
        GD.Print("TWR_PROJECTILE_SWEEP_CASES_OK edge_centre_ray_misses=true real_rocket_stops=true layers_preserved=true");
    }
    private async Task ActualThrow()
    {
        Runtime.StartMap("Ranch");
        var floor=Box("ThrowFloor",new Vector3(0,-.25f,0),new Vector3(20,.5f,20));
        var wall=Box("MuzzleWall",new Vector3(0,1.6f,-.43f),new Vector3(12,5,.04f));
        var player=new FirstPersonPlayer { Runtime=Runtime,Name="ActualPlayer",Position=Vector3.Zero };
        AddChild(player); player.SetPhysicsProcess(false); player.SetProcess(false);
        player.SetGameplayControlsEnabled(true);
        await Frames(3);
        Require(Runtime.GrantItem("Frag",1,99),"Cannot grant isolated inventory fixture");
        var count=Runtime.Player!.Inventory.GetValueOrDefault("Frag");
        using var select=new InputEventKey { Keycode=Key.Key5,Pressed=true };
        using var fire=new InputEventMouseButton { ButtonIndex=MouseButton.Left,Pressed=true };
        player._UnhandledInput(select); player._UnhandledInput(fire);
        var grenade=GetChildren().OfType<ThrowableProjectileRuntime>().Single();
        Require(grenade.GlobalPosition.DistanceTo(player.AimOrigin)<.001f,
            "Throw teleported past wall before its physics launch sweep");
        Require(Runtime.Player.Inventory.GetValueOrDefault("Frag")==count-1,"Throw inventory not consumed exactly once");
        await Frames(5);
        Require(GodotObject.IsInstanceValid(grenade) && !grenade.CancelledInvalidOrigin &&
            grenade.GlobalPosition.Z>-.43f && grenade.ContactCount>0,
            "Actual input throw crossed muzzle wall or collided with owner");
        // Wait for the genuine recovered fuse, rather than forcing detonation.
        await Frames(190);
        Require(!GodotObject.IsInstanceValid(grenade),"Real grenade fuse failed to complete");
        player.QueueFree(); floor.QueueFree(); wall.QueueFree();
        Runtime.FailMatch("Projectile fixture finished"); Runtime.ReturnToLobby();
        await Frames(3);
        GD.Print("TWR_PROJECTILE_INPUT_OK real_throw=true wall_safe=true inventory_decrement=1 genuine_fuse=true");
    }
    private async Task RestingFloorRemoval()
    {
        var floor=Box("RemovableFloor",new Vector3(0,-.25f,0),new Vector3(12,.5f,12));
        var grenade=new ThrowableProjectileRuntime { Position=new Vector3(0,.3f,0),Direction=Vector3.Zero };
        AddChild(grenade);
        for(var i=0;i<145 && !grenade.RestingOnGround75;i++) await Frames(1);
        Require(GodotObject.IsInstanceValid(grenade) && grenade.RestingOnGround75,"Grenade never rested on sphere support");
        var y=grenade.GlobalPosition.Y;
        Require(y>=300+ThrowableProjectileRuntime.CollisionRadius-.01f,"Grenade radius embedded in floor");
        floor.QueueFree(); await Frames(10);
        Require(GodotObject.IsInstanceValid(grenade) && !grenade.RestingOnGround75 && grenade.GlobalPosition.Y<y-.08f,
            "Grenade remained suspended after supporting floor disappeared");
        grenade.QueueFree(); await Frames(2);
    }
}

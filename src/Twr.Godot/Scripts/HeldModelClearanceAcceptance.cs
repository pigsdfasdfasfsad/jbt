using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Actual held model with W/S walking, real renderer and static wall.
/// Captures are produced by Godot; source assets are not used by this fixture.</summary>
public partial class HeldModelClearanceAcceptance : Node3D
{
    public LocalSessionNode Runtime {get;set;}=null!;
    private async Task Frames(int n){for(var i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
    public override async void _Ready()
    {
        try
        {
            // Physics assertions execute in the native Windows headless game too.
            // Actual screenshot requests separately require a real renderer.
            if(DisplayServer.GetName()=="headless" && OS.GetCmdlineUserArgs().Any(
                arg => arg.StartsWith("--clearance-captures=")))
                throw new InvalidOperationException("Captures require a renderer");
            Runtime.StartMap("Ranch");
            Box("Floor",new(0,-.25f,0),new(20,.5f,20),new(.2f,.22f,.25f));
            Box("Wall",new(0,2,0),new(8,4,.06f),new(.36f,.39f,.43f));
            AddChild(new WorldEnvironment {Environment=new global::Godot.Environment {
                BackgroundMode=global::Godot.Environment.BGMode.Color,BackgroundColor=new(.08f,.11f,.15f),
                AmbientLightSource=global::Godot.Environment.AmbientSource.Color,AmbientLightEnergy=.7f}});
            AddChild(new DirectionalLight3D {RotationDegrees=new(-45,-20,0),LightEnergy=1.1f});
            var audio=new OfflineAudioRuntime();AddChild(audio);
            var player=new FirstPersonPlayer {Runtime=Runtime,Audio=audio,Position=new(0,.08f,2f)};AddChild(player);
            await Frames(45);
            var name=player.EquippedWeaponName;var ammo=Runtime.Player!.Ammo.GetValueOrDefault(name);
            var fov=player.CaptureCamera.Fov;var aim=player.AimDirection;
            var nearPlane=player.CaptureCamera.Near;
            var model=player.CaptureCamera.GetNode<WeaponViewModelRuntime>("WeaponViewModel");
            if(player.HeldModelFitScale<.999f)throw new InvalidOperationException("Clear weapon is unexpectedly scaled");
            await Capture("held-weapon-clear-of-wall.png");
            await Walk(Key.W,35);await Frames(8);
            if(player.GlobalPosition.Z>.4f || player.HeldModelFitScale>=.9f)
                throw new InvalidOperationException("Actual walking did not trigger held-model clearance");
            if(!HeldModelClearance.TryCameraBounds(model,out var bounds) ||
                player.CaptureCamera.GlobalPosition.Z+bounds.Position.Z<.03f)
                throw new InvalidOperationException("Visible held geometry still crosses the blocking wall");
            if(player.CaptureCamera.Fov!=fov || player.AimDirection.DistanceTo(aim)>.0001f ||
                Runtime.Player.Ammo.GetValueOrDefault(name)!=ammo ||
                player.GetChildren().OfType<CollisionShape3D>().Count()!=1)
                throw new InvalidOperationException("Visual clearance changed firing or physics authority");
            var near=player.HeldModelFitScale;
            await Capture("held-weapon-near-wall.png");
            using(var equip=new InputEventKey {Keycode=Key.Key1,Pressed=true})
                player._UnhandledInput(equip);
            await Frames(70);
            var second=player.EquippedWeaponName;
            var secondAmmo=Runtime.Player.Ammo.GetValueOrDefault(second);
            if(second==name || !HeldModelClearance.TryCameraBounds(model,out bounds) ||
                player.CaptureCamera.GlobalPosition.Z+bounds.Position.Z<.03f)
                throw new InvalidOperationException("Weapon switch lost held-model wall clearance");
            await Capture("held-shotgun-near-wall.png");
            using(var aimDown=new InputEventMouseButton {ButtonIndex=MouseButton.Right,Pressed=true})
                Input.ParseInputEvent(aimDown);
            try
            {
                await Frames(60);
                if(!HeldModelClearance.TryCameraBounds(model,out bounds) ||
                    player.CaptureCamera.GlobalPosition.Z+bounds.Position.Z<.03f ||
                    player.AimDirection.DistanceTo(aim)>.0001f ||
                    Runtime.Player.Ammo.GetValueOrDefault(second)!=secondAmmo)
                    throw new InvalidOperationException("Aiming near a wall changed shot authority or clipped the model");
                await Capture("held-shotgun-aim-near-wall.png");
            }
            finally
            {
                using var aimUp=new InputEventMouseButton {ButtonIndex=MouseButton.Right,Pressed=false};
                Input.ParseInputEvent(aimUp);
            }
            await Frames(60);
            await Walk(Key.S,35);await Frames(35);
            if(player.HeldModelFitScale<.999f || Math.Abs(player.CaptureCamera.Near-nearPlane)>.000001f)
                throw new InvalidOperationException("Clearance or near clipping persisted away from the wall");
            GD.Print($"TWR_HELD_MODEL_CLEARANCE_OK weapon={name} near_fit_scale={near:F3} far_fit_scale={player.HeldModelFitScale:F3} real_walk=true aim_unchanged=true ammo_unchanged=true collider_unchanged=true weapon_switch=true shotgun_ads=true near_plane_restored=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e){GD.PushError("TWR_HELD_MODEL_CLEARANCE_FAILED "+e);RuntimeShutdownCoordinator.Request(GetTree(),1);}
    }
    private async Task Walk(Key key,int frames)
    {
        using var down=new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=true};Input.ParseInputEvent(down);
        try{await Frames(frames);}finally{using var up=new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=false};Input.ParseInputEvent(up);}
    }
    private async Task Capture(string name)
    {
        var arg=OS.GetCmdlineUserArgs().FirstOrDefault(x=>x.StartsWith("--clearance-captures="));
        if(arg is null)return;
        var folder=Path.GetFullPath(arg["--clearance-captures=".Length..]);Directory.CreateDirectory(folder);
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        using var image=GetViewport().GetTexture().GetImage();
        if(image.SavePng(Path.Combine(folder,name))!=Error.Ok)throw new IOException("Capture failed");
    }
    private void Box(string name,Vector3 pos,Vector3 size,Color color)
    {
        var body=new StaticBody3D {Name=name,Position=pos,CollisionLayer=1,CollisionMask=0};
        body.AddChild(new CollisionShape3D {Shape=new BoxShape3D {Size=size}});
        body.AddChild(new MeshInstance3D {Mesh=new BoxMesh {Size=size,Material=new StandardMaterial3D {AlbedoColor=color}}});AddChild(body);
    }
}

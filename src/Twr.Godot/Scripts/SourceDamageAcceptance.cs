using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Godot;

namespace Twr.Godot;

/// <summary>Complete tanker lifecycle on a synthetic floor: real player walking,
/// ammo pickup, input-driven shooting/reload, objective award and results save.
/// No health edits, forced completion or installed private map data.</summary>
public partial class SourceDamageAcceptance : Node3D
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public override async void _Ready()
    {
        try
        {
            Runtime.StartMap("Ranch");
            Box("SyntheticTankerFloor",new(0,-.25f,0),new(32,.5f,32));
            Box("InsufficientPlatform",new(25,-.25f,0),new(1,.5f,1));
            var audio=new OfflineAudioRuntime(); AddChild(audio);
            var player=new FirstPersonPlayer {Name="Player",Runtime=Runtime,Audio=audio,Position=new(-4,.08f,-6)};
            AddChild(player);
            await Frames(10);
            GD.Print("TWR_TANKER_STAGE player_ready");
            var preferred=new Vector3(7,0,0);
            if(SourceDamageLayout.TryGroundFootprint(GetWorld3D(),new(25,0,0),player.GetRid()).HasValue ||
               SourceDamageLayout.TryGroundFootprint(GetWorld3D(),new(40,0,0),player.GetRid()).HasValue)
                throw new InvalidOperationException("Tanker admitted overhanging or missing floor");
            var blocker=Box("BlockingTankerVolume",new(7,1.5f,0),new(.5f,3,.5f));
            await Frames(3);
            if(SourceDamageLayout.TryPlan(GetWorld3D(),player.GlobalPosition,player.GetRid(),new[]{preferred},Array.Empty<Vector3>()) is not null)
                throw new InvalidOperationException("Tanker admitted overlapping solid wall");
            blocker.QueueFree(); await Frames(3);
            var plan=SourceDamageLayout.TryPlan(GetWorld3D(),player.GlobalPosition,player.GetRid(),new[]{preferred},Array.Empty<Vector3>());
            if(plan is null || plan.PlayerRoute.Length<2)
                throw new InvalidOperationException("Supported tanker firing layout was rejected");
            if(!SourceDamageLayout.RouteCrossesTarget(new[]{new Vector3(0,0,0),new Vector3(14,0,0)},plan.Position) ||
                SourceDamageLayout.RouteCrossesTarget(plan.PlayerRoute,plan.Position))
                throw new InvalidOperationException("Future tanker footprint sweep is incorrect");
            GD.Print("TWR_TANKER_STAGE layout_verified");
            var receipts=0;var credits=Runtime.Player!.Credits;var xp=Runtime.Player.Xp;var level=Runtime.Player.Level;
            var mission=new ObjectiveRuntime {Name="PlannedDamage",ObjectiveId="Ranch:SourceDamageAcceptance",Family="Damage",
                Runtime=Runtime,Player=player,Position=plan.Position,SourceDamagePlan=plan,Completed=_=>receipts++};
            AddChild(mission); await Frames(4);
            var target=mission.GetNode<DamageObjectiveTarget>("FuelTanker");
            var visualBounds=ReconstructedPickupVisuals.Measure(target.GetNode<Node3D>("ReconstructedFuelTanker"));
            if (visualBounds.Position.X < -2.4f || visualBounds.End.X > 2.4f ||
                visualBounds.Position.Y < 0 || visualBounds.End.Y > 2.2f ||
                visualBounds.Position.Z < -.95f || visualBounds.End.Z > .95f)
                throw new InvalidOperationException("Tanker mesh exceeds its existing collider");
            if(target.GetChildren().OfType<CollisionShape3D>().Count()!=1 ||
                target.GetNodeOrNull<Node3D>("ReconstructedFuelTanker") is null)
                throw new InvalidOperationException("Tanker presentation changed collision authority");
            // Invalid scalar inputs must not poison the health or award state.
            var initialHealth=target.Health;
            target.ApplyDamage(float.NaN,"Bullet"); target.ApplyDamage(float.PositiveInfinity,"Bullet");
            target.ApplyDamage(100,"Melee"); target.ApplyDamage(100,"Fire");
            if(target.Health!=initialHealth || receipts!=0)
                throw new InvalidOperationException("Invalid or immune damage changed tanker state");
            var guide=new ObjectiveRouteGuidance {Player=player,Runtime=Runtime};AddChild(guide);
            using(var toggle=new InputEventKey {Keycode=Key.H,Pressed=true})guide._UnhandledInput(toggle);
            await Frames(34);
            if(!guide.DisplayText.StartsWith("DAMAGE |") || guide.VisibleSegments==0)
                throw new InvalidOperationException("Route guidance does not reach the firing station");
            GD.Print("TWR_TANKER_STAGE starting_walk");
            var walked=await AcceptanceKeyboardDriver.Walk(this,player,plan.PlayerRoute);
            if(walked<5 || !player.IsOnFloor())throw new InvalidOperationException("Player did not physically approach firing station");
            // Add light only in this scene for a genuine engine capture.
            AddChild(new WorldEnvironment {Environment=new global::Godot.Environment {
                BackgroundMode=global::Godot.Environment.BGMode.Color,BackgroundColor=new(.10f,.13f,.17f),
                AmbientLightSource=global::Godot.Environment.AmbientSource.Color,AmbientLightEnergy=.7f}});
            AddChild(new DirectionalLight3D {RotationDegrees=new(-45,-25,0),LightEnergy=1.2f});
            player.CaptureCamera.LookAt(plan.Position+Vector3.Up*1.1f,Vector3.Up);
            var capture=OS.GetCmdlineUserArgs().FirstOrDefault(x=>x.StartsWith("--tanker-capture="));
            if(capture is not null)
            {
                if(DisplayServer.GetName()=="headless")throw new InvalidOperationException("Capture requires a real renderer");
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                var path=Path.GetFullPath(capture["--tanker-capture=".Length..]);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var image=GetViewport().GetTexture().GetImage();
                if(image.IsEmpty() || image.SavePng(path)!=Error.Ok)throw new IOException("Tanker capture failed");
            }
            GD.Print("TWR_TANKER_STAGE starting_fire");
            var weapon=player.EquippedWeaponName;var spec=RuntimeWeaponCatalog.Get(weapon);
            var shots=0;var reloads=0;var supplies=0;
            for(var attempt=0;attempt<220 && !mission.IsCompleted;attempt++)
            {
                if (attempt%20==0) GD.Print($"TWR_TANKER_STAGE fire attempt={attempt} health={target.Health} ammo={Runtime.Player.Ammo.GetValueOrDefault(weapon)}");
                if(Runtime.Player.Ammo.GetValueOrDefault(weapon)==0)
                {
                    if(Runtime.Player.ReserveAmmo.GetValueOrDefault(weapon)<spec.Magazine)
                    {
                        var collected=false;
                        var ammo=new PickupActor {PickupType="Ammo",Player=player,Runtime=Runtime,
                            Position=player.GlobalPosition+new Vector3(.7f,.55f,0),Collected=_=>collected=true};
                        AddChild(ammo);await Frames(3);await AcceptanceKeyboardDriver.PressE(this);
                        if(!collected)throw new InvalidOperationException("Ammo supply did not use normal E pickup");
                        supplies++;
                    }
                    using var reload=new InputEventKey {Keycode=Key.R,Pressed=true};player._UnhandledInput(reload);
                    if(player.ReloadRemainingSeconds70<=0)throw new InvalidOperationException("Empty magazine failed to begin reload");
                    await Frames((int)Math.Ceiling(spec.ReloadSeconds*60)+5);reloads++;
                    if (attempt%20==0) GD.Print($"TWR_TANKER_STAGE fire attempt={attempt} health={target.Health} ammo={Runtime.Player.Ammo.GetValueOrDefault(weapon)}");
                if(Runtime.Player.Ammo.GetValueOrDefault(weapon)==0)throw new InvalidOperationException("Reload granted no ammunition");
                }
                Input.MouseMode=Input.MouseModeEnum.Captured;
                player.CaptureCamera.LookAt(plan.Position+Vector3.Up*1.1f,Vector3.Up);
                var before=Runtime.Player.Ammo.GetValueOrDefault(weapon);
                using var fire=new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=true};player._UnhandledInput(fire);
                using var up=new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=false};player._UnhandledInput(up);
                if(Runtime.Player.Ammo.GetValueOrDefault(weapon)==before-1)shots++;
                await Frames((int)Math.Ceiling(spec.ActionSeconds*60)+3);
            }
            var reward=new Twr.Domain.Policies.ObjectiveRewardPolicy().Reward("Damage");
            var expected=new Twr.Domain.Model.PlayerState {Credits=credits+reward.Credits,Xp=xp+reward.Xp,Level=level};
            new Twr.Domain.Services.ProgressionService(new Twr.Domain.Core.EventStream()).Apply(expected,DateTimeOffset.UtcNow);
            if(!mission.IsCompleted || receipts!=1 || shots<20 || reloads<1 ||
                Runtime.Match!.CompletedObjectives.Count!=1 || !Runtime.Match.CompletedObjectives.Contains(mission.ObjectiveId) ||
                Runtime.Player.Credits!=expected.Credits || Runtime.Player.Xp!=expected.Xp || Runtime.Player.Level!=expected.Level)
                throw new InvalidOperationException($"Incomplete tanker loop: completed={mission.IsCompleted} receipts={receipts} shots={shots} reloads={reloads} credits={Runtime.Player.Credits-credits} xp={Runtime.Player.Xp-xp}");
            var earned=Runtime.Player.Credits;Runtime.CompleteObjective(mission.ObjectiveId,"Damage");
            if(Runtime.Player.Credits!=earned)throw new InvalidOperationException("Tanker replay duplicated award");
            await Frames(34);
            if(guide.VisibleSegments!=0)throw new InvalidOperationException("Finished tanker retained walking guide");
            Runtime.FailMatch("Tanker lifecycle acceptance completed");
            if(Runtime.Match?.SaveCommitted!=true)throw new InvalidOperationException("Tanker result was not saved");
            GD.Print($"TWR_SOURCE_DAMAGE_OK real_walk_metres={walked:F2} shots={shots} reloads={reloads} ammo_pickups={supplies} rewards=1 result_saved=true footprint=true future_collider_avoided=true synthetic_world=true original_private_scene_unverified=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e)
        {
            AcceptanceKeyboardDriver.SendKey(Key.W,false);AcceptanceKeyboardDriver.SendKey(Key.E,false);
            GD.PushError("TWR_SOURCE_DAMAGE_FAILED "+e);RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }
    private StaticBody3D Box(string name,Vector3 at,Vector3 size)
    {
        var body=new StaticBody3D {Name=name,Position=at,CollisionLayer=1,CollisionMask=0};AddChild(body);
        body.AddChild(new CollisionShape3D {Shape=new BoxShape3D {Size=size}});
        body.AddChild(new MeshInstance3D {Mesh=new BoxMesh {Size=size},MaterialOverride=new StandardMaterial3D {AlbedoColor=new(.22f,.24f,.26f)}});
        return body;
    }
    private async Task Frames(int count){for(var i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
}

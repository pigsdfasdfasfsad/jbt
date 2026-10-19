using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Full ten-item loading mission driven by real keyboard movement
/// and E presses, with assistance visible. Synthetic world, production actors.</summary>
public partial class ObjectiveGuidanceAcceptance : Node3D
{
    public LocalSessionNode Runtime {get;set;}=null!;
    public override async void _Ready()
    {
        try
        {
            Runtime.StartMap("Ranch");
            var floor=new StaticBody3D { Position=new(0,-.25f,0) };AddChild(floor);
            floor.AddChild(new CollisionShape3D {Shape=new BoxShape3D {Size=new(32,.5f,32)}});
            floor.AddChild(new MeshInstance3D {Mesh=new BoxMesh {Size=new(32,.5f,32)},
                MaterialOverride=new StandardMaterial3D {AlbedoColor=new(.21f,.24f,.25f)}});
            AddChild(new WorldEnvironment {Environment=new global::Godot.Environment {
                BackgroundMode=global::Godot.Environment.BGMode.Color,BackgroundColor=new(.12f,.15f,.18f),
                AmbientLightSource=global::Godot.Environment.AmbientSource.Color,AmbientLightEnergy=.7f}});
            AddChild(new DirectionalLight3D { RotationDegrees=new(-45,-25,0),LightEnergy=1.2f });
            var player=new FirstPersonPlayer { Name="Player",Runtime=Runtime,Position=new(0,.08f,0) };AddChild(player);
            await Frames(8);
            var plan=SourceObjectiveLayout.TryPlan(GetWorld3D(),player.GlobalPosition,player.GetRid(),
                "Load",new[]{new Vector3(5,0,0)},Array.Empty<Vector3>(),null);
            if(plan is null || plan.MaterialFeet.Length!=10)throw new InvalidOperationException("Full load circuit not found");
            var receipts=0;
            var mission=new ObjectiveRuntime { Name="GuidedLoad",Family="Load",Runtime=Runtime,Player=player,
                SourceMaterialLayout=plan,Position=plan.Position,ObjectiveId="Ranch:GuidedLoad",Completed=_=>receipts++ };
            AddChild(mission);
            var guide=new ObjectiveRouteGuidance { Name="RouteGuide",Runtime=Runtime,Player=player };AddChild(guide);
            await Frames(6);
            if(guide.GuideEnabled || guide.VisibleSegments!=0)throw new InvalidOperationException("Guide was not opt-in");
            await PressKey(Key.F4);
            if(guide.GuideEnabled)throw new InvalidOperationException("F4 floorplan binding was replaced");
            var credits=Runtime.Profile!.Credits;
            await PressKey(Key.H);await Frames(36);
            if(!guide.GuideEnabled || !guide.DisplayText.StartsWith("COLLECT") || guide.VisibleSegments<1 ||
                mission.Pass98DepositedMaterials!=0 || mission.Pass98CarryingMaterial || Runtime.Profile.Credits!=credits)
                throw new InvalidOperationException("Guide changed authority or failed to show a verified route");
            player.Rotation=new(0,-Mathf.Pi/2,0);player.CaptureCamera.RotationDegrees=new(-20,0,0);
            var capture=OS.GetCmdlineUserArgs().FirstOrDefault(s=>s.StartsWith("--guidance-capture="));
            if(capture is not null)
            {
                if(DisplayServer.GetName()=="headless")throw new InvalidOperationException("Real renderer required for capture");
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                var path=Path.GetFullPath(capture["--guidance-capture=".Length..]);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var image=GetViewport().GetTexture().GetImage();
                if(image.IsEmpty() || image.SavePng(path)!=Error.Ok)throw new IOException("Guidance screenshot failed");
            }
            float walked=0;
            for(var i=0;i<10;i++)
            {
                var target=mission.RouteGuideTargets().OrderBy(t=>player.GlobalPosition.DistanceSquaredTo(t.Position)).First();
                if(!SourceTraversalRoutes.TryPlan(GetWorld3D(),player.GlobalPosition,target.Position,player.GetRid(),null,out var collect))
                    throw new InvalidOperationException("Remaining material is not reachable");
                walked+=await AcceptanceKeyboardDriver.Walk(this,player,collect);
                await AcceptanceKeyboardDriver.PressE(this);await Frames(32);await IdleFrames();
                if(!mission.Pass98CarryingMaterial || !guide.DisplayText.StartsWith("RETURN MATERIAL") || mission.Pass98DepositedMaterials!=i)
                    throw new InvalidOperationException($"Guide carry transition failed: item={i} carrying={mission.Pass98CarryingMaterial} deposited={mission.Pass98DepositedMaterials} text={guide.DisplayText}");
                if(!SourceTraversalRoutes.TryPlan(GetWorld3D(),player.GlobalPosition,mission.GlobalPosition,player.GetRid(),null,out var deposit))
                    throw new InvalidOperationException("Deposit route is not reachable");
                walked+=await AcceptanceKeyboardDriver.Walk(this,player,deposit);
                await AcceptanceKeyboardDriver.PressE(this);
                if(mission.Pass98DepositedMaterials!=i+1 || mission.Pass98CarryingMaterial)
                    throw new InvalidOperationException("E interaction failed to deposit exactly one material");
                await Frames(32);await IdleFrames();
            }
            if(!mission.IsCompleted || receipts!=1 || guide.VisibleSegments!=0 || !guide.DisplayText.StartsWith("NO ACTIVE"))
                throw new InvalidOperationException("Guide persisted after mission completion");
            var rewarded=Runtime.Profile.Credits;var xp=Runtime.Profile.Xp;Runtime.CompleteObjective(mission.ObjectiveId,"Load");
            if(rewarded!=Runtime.Profile.Credits || xp!=Runtime.Profile.Xp)throw new InvalidOperationException("Repeated completion duplicated rewards");
            await PressKey(Key.H);await Frames(3);
            if(guide.GuideEnabled || guide.VisibleSegments!=0)throw new InvalidOperationException("Guide toggle did not hide markers");
            GD.Print($"TWR_OBJECTIVE_GUIDANCE_OK deposits=10 player_walked={walked:F2} real_keyboard=true opt_in=true carrying_and_completion_followed=true no_auto_interaction=true reward_idempotent=true f4_preserved=true synthetic_world=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e){GD.PushError("TWR_OBJECTIVE_GUIDANCE_FAILED "+e);RuntimeShutdownCoordinator.Request(GetTree(),1);}
    }
    private async Task IdleFrames()
    {await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task PressKey(Key key){AcceptanceKeyboardDriver.SendKey(key,true);await Frames(2);AcceptanceKeyboardDriver.SendKey(key,false);await Frames(2);}
    private async Task Frames(int count){for(var i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
}

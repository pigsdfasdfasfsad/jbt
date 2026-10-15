using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Actual rendered engine captures, never reference photographs or generated
/// illustrations. Uses public reconstructed content and an isolated profile.
/// </summary>
public partial class RuntimePresentationCapture : Node3D
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public override async void _Ready()
    {
        try
        {
            if (DisplayServer.GetName()=="headless")
                throw new InvalidOperationException("A real graphics renderer is required for captures");
            var outputArg=OS.GetCmdlineUserArgs().FirstOrDefault(x=>x.StartsWith("--capture-output=",StringComparison.Ordinal));
            if(outputArg is null) throw new InvalidOperationException("Capture output directory was not specified");
            var folder=Path.GetFullPath(outputArg["--capture-output=".Length..]);
            Directory.CreateDirectory(folder);
            GetWindow().Size=new Vector2I(1280,720);
            Runtime.StartMap("Laboratory");
            var game=new GameplayRoot { MapName="Laboratory",Runtime=Runtime,Name="CapturedGameplay" };
            AddChild(game);
            game.SetProcess(false);
            var player=game.GetNode<FirstPersonPlayer>("Player");
            await Frames(90);
            // Let the actual gameplay HUD read authoritative ammo,
            // health and match state before capturing its view.
            game.SetProcess(true);
            await Frames(3);
            game.SetProcess(false);
            player.SetGameplayControlsEnabled(false);
            await Frames(5);
            Save(folder,"laboratory-fallback-first-person.png");
            Runtime.FailMatch("Isolated graphics capture completed");
            game.QueueFree();
            await Frames(12);
            Runtime.ReturnToLobby();
            var gallery=new Node3D { Name="ActualProductionFortificationGallery" };
            AddChild(gallery);
            gallery.AddChild(new WorldEnvironment
            {
                Environment=new global::Godot.Environment
                {
                    BackgroundMode=global::Godot.Environment.BGMode.Color,
                    BackgroundColor=new Color(.14f,.16f,.18f),
                    AmbientLightSource=global::Godot.Environment.AmbientSource.Color,
                    AmbientLightColor=new Color(.85f,.87f,.90f),AmbientLightEnergy=.65f
                }
            });
            gallery.AddChild(new DirectionalLight3D
            {
                Name="GalleryKey",RotationDegrees=new Vector3(-50,-30,0),
                LightEnergy=1.4f,ShadowEnabled=true
            });
            gallery.AddChild(new MeshInstance3D
            {
                Name="GalleryFloor",Position=new Vector3(0,-.06f,0),
                Mesh=new BoxMesh { Size=new Vector3(13f,.1f,6f) },
                MaterialOverride=new StandardMaterial3D
                {
                    AlbedoColor=new Color(.24f,.26f,.28f),Roughness=.95f
                }
            });
            var types=new[]{"Barbed Wire","Clap Bomb","Jack","50 Cal"};
            var xs=new[]{-4.1f,-1.2f,1f,3.4f};
            var definitions=FortificationCatalogRuntime.Load();
            for(var i=0;i<types.Length;i++)
                gallery.AddChild(new FortificationActor
                {
                    Name="Rendered"+i,Definition=definitions.First(x=>x.Name==types[i]),
                    Position=new Vector3(xs[i],0,0)
                });
            var camera=new Camera3D { Current=true,Fov=46,Position=new Vector3(0,3.2f,9.0f) };
            gallery.AddChild(camera);
            camera.LookAt(new Vector3(0,.50f,0),Vector3.Up);
            var canvas=new CanvasLayer();
            gallery.AddChild(canvas);
            var caption=new Label
            {
                Text="TWR OFFLINE | ACTUAL ENGINE RENDER\nDeployed fortifications - locally reconstructed models, not original mesh assets",
                Position=new Vector2(30,22)
            };
            caption.AddThemeFontSizeOverride("font_size",24);
            canvas.AddChild(caption);
            await Frames(20);
            Save(folder,"deployed-fortifications-engine-render.png");
            var report=new
            {
                actual_engine_capture=true,renderer=RenderingServer.GetCurrentRenderingMethod().ToString(),
                platform=OS.GetName(),private_original_assets_loaded=false,
                captures=new[]{"laboratory-fallback-first-person.png","deployed-fortifications-engine-render.png"},
                original_visual_parity_verified=false
            };
            File.WriteAllText(Path.Combine(folder,"capture-receipt.json"),JsonSerializer.Serialize(report));
            GD.Print("TWR_PRESENTATION_CAPTURE_OK "+JsonSerializer.Serialize(report));
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_PRESENTATION_CAPTURE_FAILED "+e);
            RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }
    private async Task Frames(int count)
    {
        for(var i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    private void Save(string folder,string name)
    {
        using var image=GetViewport().GetTexture().GetImage();
        if(image.IsEmpty() || image.GetWidth()<640 || image.GetHeight()<360)
            throw new InvalidOperationException("Renderer returned an empty or undersized capture");
        if(image.SavePng(Path.Combine(folder,name))!=Error.Ok)
            throw new IOException("Could not save actual engine capture");
    }
}

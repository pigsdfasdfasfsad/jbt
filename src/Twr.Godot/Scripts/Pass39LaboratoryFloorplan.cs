using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Optional three-level Laboratory map plan generated from exact owner
/// source transforms and bounding colliders; not recovered custom mesh art.
/// Press F4 to show/hide and left/right to select the source height band.
/// </summary>
public partial class Pass39LaboratoryFloorplan : CanvasLayer
{
    private const string SceneSha = "35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74";
    private static readonly string[] PlanShas =
    [
        "ee07c7c550e605902fdc448e44d516867a67a7e3f86a3f16008a15cf5dc08ffb",
        "68c7ede384828f0e3e5c56db54ca34edccb030de22c25de79e70393306c089f4",
        "176aa6bc6956751e8914e2c659f90e4c9b35bee1dbfe0ed55503c8f1dd73c60d"
    ];
    private static readonly string[] LevelLabels =
    [
        "LOWER  |  source Y -10 to 16 studs",
        "MAIN  |  source Y 16 to 32 studs",
        "UPPER  |  source Y 32+ studs"
    ];

    private readonly List<Texture2D> _plans = [];
    private readonly List<Texture2D> _navigationPlans = [];
    private const string Pass40NavSha =
        "b021b905a0a87619997a0e2da08a4c7b2db18ecd2a5250f93dfd84d08cb26bdc";
    private static readonly string[] Pass40PlanShas =
    [
        "ad256d57589d505a39050458ae67df516c5760ba6a913560d1fa01d151784f81",
        "e996c2ddb86f90e0a90b71435c7ff81c3af702c799195149fe1e6d39b10e3d50",
        "a7dd824fa7daa355fa68ed3e5d5e5546049384e043f1be69b7fc2f0d60fc4078"
    ];
    private const string Pass41NavSha =
        "12ac9bae602cbdea3ae6a6789289d1068f1c8efd5d603de34d6186698c375dd0";
    private static readonly string[] Pass41PlanShas =
    [
        "22c8dd0029d9dced051371e1af06041d87729f83fe249ffd0b0e61358715027d",
        "bf90e00db93bc9fa208d10d4c1094edf0f0282d65a0cc0be0213b0ebf671c3e1",
        "65a3f37130b898a82d3de602344ccef31e219efd006179f9fce735ac8f57158b"
    ];
    private const string Pass42NavSha =
        "c9f03bfe0c5da13972d5c149f88c4084da61b5804501419aebb6cddac51be4fd";
    private static readonly string[] Pass42PlanShas =
    [
        "0fb83b5e262ca846f31862c7cc9b5097d3ae3e9833ede87e0c25f15bcbbb3169",
        "27bdc7961e973476816c875e5cb927e1b946af9bafef9ed6b36ed81e975c28d5",
        "d42d1e479a4dc2ae6569a675a7c78abac118f1471cbdeaed97b6321a982a5f24"
    ];
    public bool HasPass42JumpDiagnostic { get; private set; }
    public bool HasPass41RepairDiagnostic { get; private set; }
    private Control _panel = null!;
    private TextureRect _image = null!;
    private Label _caption = null!;
    public int ActiveLevel { get; private set; }
    public bool IsOpen => _panel is not null && _panel.Visible;
    public bool VerifiedOriginalScene { get; private set; }
    public int LevelCount => _plans.Count;
    public bool HasNavigationDiagnostic => _navigationPlans.Count == 3;
    public bool IsShowingNavigation { get; private set; }

    public static Pass39LaboratoryFloorplan? TryBuild(Node3D owner, string map)
    {
        if (map != "Laboratory" ||
            owner.GetNodeOrNull<Node3D>("RecoveredLaboratory") is null)
            return null;
        var directory = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        string? Find(string subdir, string filename) => new[]
        {
            Path.Combine(directory,"Content",subdir,filename),
            Path.Combine(Directory.GetCurrentDirectory(),"Content",subdir,filename),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content",subdir,filename)
        }.FirstOrDefault(File.Exists);

        try
        {
            var original = Find("Maps","Laboratory.scene.jsonl.gz")
                ?? throw new InvalidDataException("No installed loaded Laboratory source map");
            var simulated = OS.GetCmdlineUserArgs().Any(arg =>
                arg is "--smoke-pass39" or "--smoke-pass40" or "--smoke-pass41" or "--smoke-pass42");
            using var stream = File.OpenRead(original);
            var sha = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!simulated && sha != SceneSha)
                throw new InvalidDataException("Original Laboratory source scene SHA mismatch");
            var overlay = new Pass39LaboratoryFloorplan
            {
                Name="Pass39LaboratoryFloorplan", VerifiedOriginalScene=!simulated
            };
            for (var index=0;index<PlanShas.Length;index++)
            {
                var path=Find("MapPlans",$"Laboratory.sourceplan39-{index}.png")
                    ?? throw new InvalidDataException("Original Laboratory plan layer missing");
                var data=File.ReadAllBytes(path);
                if (data.Length is < 128 or > 5_000_000)
                    throw new InvalidDataException("Original Laboratory PNG outside file budget");
                var sum=Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
                if (!simulated && sum!=PlanShas[index])
                    throw new InvalidDataException("Original Laboratory floor plan SHA mismatch");
                var bitmap=Image.LoadFromFile(path);
                if (bitmap is null || bitmap.IsEmpty() ||
                    bitmap.GetWidth() is < 16 or > 2048 ||
                    bitmap.GetHeight() is < 16 or > 2048)
                    throw new InvalidDataException("Invalid source Laboratory PNG dimensions");
                overlay._plans.Add(ImageTexture.CreateFromImage(bitmap));
            }
            // Only accept a complete, SHA-bound diagnostic set matching the
            // exact installed Laboratory navigation pack. Missing or stale
            // PNGs never remove the original geometry floorplan viewer.
            var navVersions = new[]
            {
                (Source:"Laboratory.nav42.gz",Prefix:"Laboratory.navgraph42-",
                    Digest:Pass42NavSha,Images:Pass42PlanShas,Is41:true,Is42:true),
                (Source:"Laboratory.nav41.gz",Prefix:"Laboratory.navgraph41-",
                    Digest:Pass41NavSha,Images:Pass41PlanShas,Is41:true,Is42:false),
                (Source:"Laboratory.nav31.gz",Prefix:"Laboratory.navgraph40-",
                    Digest:Pass40NavSha,Images:Pass40PlanShas,Is41:false,Is42:false)
            };
            foreach(var navVersion in navVersions)
            {
                var navFile = Find("Navigation",navVersion.Source);
                if(navFile is null)continue;
                if(!simulated &&
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(navFile)))
                        .ToLowerInvariant()!=navVersion.Digest)
                    continue;
                var images = new List<Texture2D>();
                for(var i=0;i<3;i++)
                {
                    var navPng = Find("MapPlans",$"{navVersion.Prefix}{i}.png");
                    if(navPng is null)break;
                    var png=File.ReadAllBytes(navPng);
                    if(png.Length<128 || png.Length>5_000_000 ||
                        (!simulated && Convert.ToHexString(SHA256.HashData(png))
                            .ToLowerInvariant()!=navVersion.Images[i]))
                        break;
                    var decoded=Image.LoadFromFile(navPng);
                    if(decoded is null || decoded.IsEmpty() ||
                        decoded.GetWidth() is <16 or >2048 ||
                        decoded.GetHeight() is <16 or >2048)
                        break;
                    images.Add(ImageTexture.CreateFromImage(decoded));
                }
                if(images.Count!=3)continue;
                overlay._navigationPlans.AddRange(images);
                overlay.HasPass41RepairDiagnostic=navVersion.Is41;
                overlay.HasPass42JumpDiagnostic=navVersion.Is42;
                break;
            }
            owner.AddChild(overlay);
            GD.Print("TWR_PASS39_LAB_PLANS_READY floor_levels=3 " +
                $"verified_original_scene={!simulated} default=OFF missing_source_meshes=true " +
                $"nav_diagrams={overlay.HasNavigationDiagnostic} " +
                $"pass41_native_bridges_visualized={overlay.HasPass41RepairDiagnostic} " +
                $"pass42_jump_links_visualized={overlay.HasPass42JumpDiagnostic}");
            return overlay;
        }
        catch(Exception error)
        {
            GD.PushWarning("TWR_PASS39_LAB_PLANS_REJECTED: "+error.Message);
            return null;
        }
    }

    public override void _Ready()
    {
        Layer=42;
        _panel=new Control
        {
            Name="SourceLabFloorPanel",
            AnchorLeft=.5f,AnchorRight=.5f,AnchorTop=.5f,AnchorBottom=.5f,
            OffsetLeft=-470,OffsetRight=470,OffsetTop=-443,OffsetBottom=443,
            MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false
        };
        AddChild(_panel);
        _panel.AddChild(new ColorRect
        {
            OffsetRight=940,OffsetBottom=886,
            Color=new Color(.018f,.028f,.041f,.97f),
            MouseFilter=Control.MouseFilterEnum.Ignore
        });
        _caption=new Label
        {
            Name="SourceFloorLabel",OffsetLeft=26,OffsetRight=914,
            OffsetTop=12,OffsetBottom=50,
            MouseFilter=Control.MouseFilterEnum.Ignore
        };
        _caption.AddThemeFontSizeOverride("font_size",20);
        _panel.AddChild(_caption);
        _image=new TextureRect
        {
            Name="OriginalLaboratoryFloorImage",
            OffsetLeft=21,OffsetRight=920,OffsetTop=54,OffsetBottom=848,
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter=Control.MouseFilterEnum.Ignore
        };
        _panel.AddChild(_image);
        _panel.AddChild(new Label
        {
            Text="F4 CLOSE     LEFT / RIGHT: FLOOR     N: NAV GRAPH     SOURCE MESH DATA INCOMPLETE",
            OffsetLeft=26,OffsetRight=925,OffsetTop=852,OffsetBottom=878,
            MouseFilter=Control.MouseFilterEnum.Ignore
        });
        SetLevel(0);
    }

    public void SetOpen(bool open)
    {
        if (_panel is not null)_panel.Visible=open;
    }

    public void SetLevel(int level)
    {
        if (_plans.Count==0)return;
        ActiveLevel=(level%_plans.Count+_plans.Count)%_plans.Count;
        if (_image is not null)
            _image.Texture=IsShowingNavigation && HasNavigationDiagnostic
                ? _navigationPlans[ActiveLevel] : _plans[ActiveLevel];
        if (_caption is not null)
            _caption.Text=(IsShowingNavigation
                ? (HasPass42JumpDiagnostic ? "PASS42 JUMP LINKS | " :
                    HasPass41RepairDiagnostic ? "PASS41 NATIVE BRIDGES | " :
                    "PASS40 APPROXIMATE GRAPH | ")
                : "SOURCE BOUNDS | ")+LevelLabels[ActiveLevel];
    }

    public void ToggleNavigation()
    {
        if (!HasNavigationDiagnostic)return;
        IsShowingNavigation=!IsShowingNavigation;
        SetLevel(ActiveLevel);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)return;
        if(key.Keycode==Key.F4)
        {
            SetOpen(!IsOpen);
            GetViewport().SetInputAsHandled();
        }
        else if(IsOpen && key.Keycode==Key.N)
        {
            ToggleNavigation();
            GetViewport().SetInputAsHandled();
        }
        else if(IsOpen && key.Keycode is Key.Right or Key.Left)
        {
            SetLevel(ActiveLevel+(key.Keycode==Key.Right?1:-1));
            GetViewport().SetInputAsHandled();
        }
    }
}

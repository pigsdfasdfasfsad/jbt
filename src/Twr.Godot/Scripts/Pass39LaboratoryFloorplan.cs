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
    private Control _panel = null!;
    private TextureRect _image = null!;
    private Label _caption = null!;
    public int ActiveLevel { get; private set; }
    public bool IsOpen => _panel is not null && _panel.Visible;
    public bool VerifiedOriginalScene { get; private set; }
    public int LevelCount => _plans.Count;

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
            var simulated = OS.GetCmdlineUserArgs().Contains("--smoke-pass39",StringComparer.Ordinal);
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
            owner.AddChild(overlay);
            GD.Print("TWR_PASS39_LAB_PLANS_READY floor_levels=3 " +
                $"verified_original_scene={!simulated} default=OFF missing_source_meshes=true");
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
            Text="F4 CLOSE    LEFT / RIGHT CHANGE FLOOR    SOURCE BOUNDS, NOT ORIGINAL CUSTOM MESHES",
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
        if (_image is not null)_image.Texture=_plans[ActiveLevel];
        if (_caption is not null)_caption.Text=LevelLabels[ActiveLevel];
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)return;
        if(key.Keycode==Key.F4)
        {
            SetOpen(!IsOpen);
            GetViewport().SetInputAsHandled();
        }
        else if(IsOpen && key.Keycode is Key.Right or Key.Left)
        {
            SetLevel(ActiveLevel+(key.Keycode==Key.Right?1:-1));
            GetViewport().SetInputAsHandled();
        }
    }
}

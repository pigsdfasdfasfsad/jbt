using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;

namespace Twr.Godot;

/// <summary>
/// F4 opens a bird's-eye visualization of only the original CMaps Server Walls
/// fragments. NEVER claims to be a full original playable map or terrain.
/// Original client/runtime geometry is unchanged until F5/F6 opt-in.
/// </summary>
public partial class Pass36MapPlanOverlay : CanvasLayer
{
    private static readonly Dictionary<string,string> KnownMapPlanSha=new(StringComparer.Ordinal)
    {
        ["Bypass"] = "fe1d53192a09b7f76ad596f6bb5a6f3e121512ac66b5dc8299e2638f66896e71",
        ["Cabin"] = "d60c543c801f722c7487b8ee4dfe95786b2457e7ccc4a81c39a3e2fa9cbe6097",
        ["Cargo"] = "ea47c2c85cd0e254f53939001c9b65eb8d933f42645da66cceb1d92ee65b0e60",
        ["District"] = "205fdbe0f93c16ec6af26ee0a16d6263c467755f2428d57cc0a18d1d06f811cb",
        ["Expressway"] = "b904c3b14aed46d9221033ec7662c22f59b97e67c0d0e29c2f2af4602887c5f8",
        ["Manor"] = "6787a445a381a2edc8d2e1ee55eed2c675c84e2cf6589519f4d1320c3a5e329d",
        ["Mill"] = "d5a2d3032848ceca8328ea86ac6066707946d66ddfe19c088abac46ae67a21f8",
        ["Prison"] = "1173f475e56f28d9ed6483a06c453bea4a94bc2996afa34ff0c3166c51eb4c70",
        ["Ranch"] = "b4f3f3c469577f59842cbc5178b50d99328e1d06eb60623b3dc83dd1ec0cf65e",
    };
    private Control _panel=null!;
    private Texture2D _texture=null!;
    public bool HasPlan => _texture is not null;
    public bool IsOpen => _panel is not null && _panel.Visible;

    public static Pass36MapPlanOverlay? TryBuild(Node3D owner,string mapName)
    {
        if (!KnownMapPlanSha.TryGetValue(mapName,out var knownSha))return null;
        var directory=Path.GetDirectoryName(OS.GetExecutablePath()) ?? Directory.GetCurrentDirectory();
        var filename=mapName+".sourcefragment36.png";
        var path=new[] {
            Path.Combine(directory,"Content","MapPlans",filename),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","MapPlans",filename),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","MapPlans",filename)
        }.FirstOrDefault(File.Exists);
        if(path is null)return null;
        try
        {
            var bytes=File.ReadAllBytes(path);
            if(bytes.Length<64 || bytes.Length>3_000_000)
                throw new InvalidDataException("Offline original map outline size invalid");
            var sha=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if(!OS.GetCmdlineUserArgs().Contains("--smoke-pass36",StringComparer.Ordinal) || mapName!="Manor")
                if(sha!=knownSha)throw new InvalidDataException("Original source map outline SHA mismatch");
            var image=Image.LoadFromFile(path);
            if(image is null || image.IsEmpty() || image.GetWidth()>2048 || image.GetHeight()>2048)
                throw new InvalidDataException("Offline original map outline PNG invalid");
            var overlay=new Pass36MapPlanOverlay
            {
                Name="Pass36SourceMapPlan", _texture=ImageTexture.CreateFromImage(image)
            };
            owner.AddChild(overlay);
            return overlay;
        }
        catch(Exception error)
        {
            GD.PushWarning("TWR_PASS36_MAP_PLAN_REJECTED map="+mapName+": "+error.Message);
            return null;
        }
    }

    public override void _Ready()
    {
        Layer=39;
        _panel=new Control
        {
            Name="OriginalSourceFootprints",AnchorLeft=.5f,AnchorRight=.5f,
            AnchorTop=.5f,AnchorBottom=.5f,
            OffsetLeft=-560,OffsetRight=560,OffsetTop=-405,OffsetBottom=405,
            MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false
        };
        AddChild(_panel);
        _panel.AddChild(new ColorRect
        {
            Name="DarkBackdrop",OffsetRight=1120,OffsetBottom=810,
            Color=new Color(.024f,.03f,.042f,.97f),
            MouseFilter=Control.MouseFilterEnum.Ignore
        });
        _panel.AddChild(new TextureRect
        {
            Name="OwnerSourceServerWallPlan",Texture=_texture,
            OffsetLeft=12,OffsetRight=1108,OffsetTop=12,OffsetBottom=798,
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter=Control.MouseFilterEnum.Ignore
        });
    }

    public void SetOpen(bool open)
    {
        if(_panel is not null)_panel.Visible=open;
    }

    public override void _Input(InputEvent @event)
    {
        if(@event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode!=Key.F4)
            return;
        SetOpen(!IsOpen);
        GetViewport().SetInputAsHandled();
    }
}
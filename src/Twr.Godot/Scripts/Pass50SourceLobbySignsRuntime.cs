using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass 50 — static labels from the owner's Workspace/Lobby/BulletinBoard
/// SurfaceGuis, anchored to exact source Adornee CFrames and CanvasSize.
/// Never displays historic leaderboard players/scores as live account data.
/// All remote image/mesh triangles and animated Roblox GUI remain missing.
/// </summary>
public partial class Pass50SourceLobbySignsRuntime : Node3D
{
    public const string PackName = "LobbySigns50.json.gz";
    public const string OriginalXmlSha =
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    public const string OriginalPackSha =
        "6858501862b3b3344e13bdb91b002979592225722073e52e1848a2abb9ace732";
    public const string OriginalLobby49Sha =
        "98a8bf71c0561be814b2de01831b48e413e349ed4925fab3c0b8a403d7845b49";
    private const int MaxCompressedBytes = 256 * 1024;
    private const int MaxExpandedBytes = 768 * 1024;
    private const float Stud = RobloxUnits.MetersPerStud;
    private static readonly string[] Pages =
        ["level","kills","wavesSurvived","donatedRobux"];

    private readonly List<(Node3D Node,string Page,int Headings)> _panels = [];

    public bool OwnerSourceVerified { get; private set; }
    public bool OriginalHistoricalValuesExcluded { get; private set; }
    public int SourceSurfaceGuiCount { get; private set; }
    public int SourceTextLabelCount { get; private set; }
    public int PanelCount => _panels.Count;
    public int HeadingCount { get; private set; }
    public int ActiveHeadingCount { get; private set; }
    public string ActivePage { get; private set; } = "level";
    public string ActivePageTitle => ActivePage switch
    {
        "level" => "HIGHEST LEVELS",
        "kills" => "MOST KILLS",
        "wavesSurvived" => "WAVES SURVIVED",
        "donatedRobux" => "AMOUNT DONATED",
        _ => "SOURCE BULLETIN BOARD"
    };

    public static Pass50SourceLobbySignsRuntime? TryBuild(Pass49OriginalLobbyRuntime lobby)
    {
        var path = FindPack();
        if (path is null) return null;
        var stage = new Pass50SourceLobbySignsRuntime {
            Name = "Pass50SourceLobbySigns"
        };
        try
        {
            stage.Load(path,lobby.OwnerSourceVerified);
            lobby.AddChild(stage);
            stage.SetPage("level");
            GD.Print($"TWR_PASS50_BULLETIN_READY panels={stage.PanelCount} " +
                $"static_headings={stage.HeadingCount} excluded_old_values=" +
                $"{stage.SourceTextLabelCount - stage.HeadingCount} " +
                $"source_verified={stage.OwnerSourceVerified}");
            return stage;
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS50_BULLETIN_FALLBACK: " + ex.Message);
            if (stage.GetParent() is not null)
                stage.GetParent()!.RemoveChild(stage);
            stage.Free();
            return null;
        }
    }

    public bool SetPage(string name)
    {
        if (!Pages.Contains(name,StringComparer.Ordinal))
            return false;
        ActivePage = name;
        var count = 0;
        foreach (var panel in _panels)
        {
            var shown = panel.Page is "always" || panel.Page == name;
            panel.Node.Visible = shown;
            if(shown) count += panel.Headings;
        }
        ActiveHeadingCount = count;
        return true;
    }

    public string CyclePage()
    {
        var index=Array.IndexOf(Pages,ActivePage);
        SetPage(Pages[(index + 1) % Pages.Length]);
        return ActivePageTitle;
    }

    private static string? FindPack()
    {
        var exe=Path.GetDirectoryName(OS.GetExecutablePath()) ??
                Directory.GetCurrentDirectory();
        foreach (var folder in new[]
        {
            exe, Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot")
        })
        {
            var p=Path.Combine(folder,"Content","Lobby",PackName);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private void Load(string path,bool ownerLobbyVerified)
    {
        var info=new FileInfo(path);
        if(!info.Exists || info.Length<=0 || info.Length>MaxCompressedBytes)
            throw new InvalidDataException("Source bulletin file size invalid");

        var gz=File.ReadAllBytes(path);
        var packedSha=Convert.ToHexString(SHA256.HashData(gz)).ToLowerInvariant();
        using var input=new MemoryStream(gz);
        using var decompressor=new GZipStream(input,CompressionMode.Decompress);
        using var raw=new MemoryStream();
        var buffer=new byte[32768];
        int read;
        while((read=decompressor.Read(buffer))>0)
        {
            if(raw.Length+read>MaxExpandedBytes)
                throw new InvalidDataException("Unbounded source bulletin decompression");
            raw.Write(buffer,0,read);
        }

        using var doc=JsonDocument.Parse(raw.ToArray());
        var root=doc.RootElement;
        if(root.GetProperty("format").GetString()!="twr-pass50-source-lobby-signs-v1")
            throw new InvalidDataException("Unknown source bulletin format");
        var synthetic=root.GetProperty("synthetic").GetBoolean();
        var args=OS.GetCmdlineUserArgs();
        var smoke=args.Contains("--smoke-pass50",StringComparer.Ordinal) ||
            args.Contains("--smoke-pass51",StringComparer.Ordinal) ||
            args.Contains("--smoke-pass52",StringComparer.Ordinal) ||
            args.Contains("--smoke-pass53",StringComparer.Ordinal);
        if (synthetic && !smoke)
            throw new InvalidDataException("Synthetic bulletin used in normal gameplay");
        if(synthetic == ownerLobbyVerified)
            throw new InvalidDataException("Source lobby and bulletin provenance mismatch");
        if(!synthetic &&
           (packedSha!=OriginalPackSha ||
            root.GetProperty("original_rbxlx_sha256").GetString()!=OriginalXmlSha ||
            root.GetProperty("lobby49_sha256").GetString()!=OriginalLobby49Sha))
            throw new InvalidDataException("Original source bulletin SHA mismatch");
        SourceSurfaceGuiCount=root.GetProperty("source_surfacegui_count").GetInt32();
        SourceTextLabelCount=root.GetProperty("source_textlabel_count").GetInt32();
        OriginalHistoricalValuesExcluded =
            root.GetProperty("historic_snapshots_intentionally_excluded").GetBoolean();
        if (!OriginalHistoricalValuesExcluded)
            throw new InvalidDataException("Historical scoreboard values must not be presented as current");

        var panels=root.GetProperty("panels");
        if(panels.GetArrayLength() is <1 or >26)
            throw new InvalidDataException("Invalid source bulletin panel budget");
        foreach(var src in panels.EnumerateArray())
            BuildPanel(src);

        if(HeadingCount is <1 or >96 ||
            (!synthetic && (PanelCount!=9 || HeadingCount!=34 ||
                SourceSurfaceGuiCount!=26 || SourceTextLabelCount!=872)))
            throw new InvalidDataException("Source bulletin heading counts changed");
        OwnerSourceVerified=!synthetic && ownerLobbyVerified;
    }

    private void BuildPanel(JsonElement src)
    {
        var page=src.GetProperty("page").GetString() ?? "";
        if (page!="always" && !Pages.Contains(page,StringComparer.Ordinal))
            throw new InvalidDataException("Unsupported original leaderboard page");
        var sourceFrame=src.GetProperty("source_part_frame");
        var position=Vector(sourceFrame,"t",3);
        var rotation=Vector(sourceFrame,"r",9);
        var size=Vector(sourceFrame,"s",3);
        var canvas=Vector(src,"canvas",2);
        if(size.Any(v=>v<=.001f || v>100) ||
            canvas.Any(v=>v<50 || v>2000))
            throw new InvalidDataException("Bad original billboard size");
        var holder=new Node3D {
            Name="OriginalBulletinSurface",
            Transform=new Transform3D(
                SourceBasis(rotation),
                new Vector3(position[0],position[1],-position[2])*Stud)
        };
        AddChild(holder);

        var count=0;
        var letters=src.GetProperty("headings");
        if(letters.GetArrayLength() is <1 or >40)
            throw new InvalidDataException("Heading limit exceeded");
        foreach(var item in letters.EnumerateArray())
        {
            var text=item.GetProperty("text").GetString() ?? "";
            if(text.Length is <1 or >64 || text.Any(char.IsControl))
                throw new InvalidDataException("Invalid source authored title");
            var uv=Vector(item,"uv",2);
            var rgb=Vector(item,"rgb",3);
            if(uv.Any(x=>x< -1.5f || x>1.5f) ||
               rgb.Any(x=>x<0 || x>255))
                throw new InvalidDataException("Invalid source title layout/color");
            var originalFont=(int)MathF.Round(item.GetProperty("font_px").GetSingle());
            if(originalFont is <8 or >100)
                throw new InvalidDataException("Invalid source title font size");
            var transparency=item.GetProperty("transparency").GetSingle();
            if(!float.IsFinite(transparency) || transparency<0 || transparency>1)
                throw new InvalidDataException("Invalid original title alpha");
            var fontWorldScale=Stud *
                MathF.Min(size[0]/canvas[0],size[1]/canvas[1]);

            // Roblox SurfaceGui.Face=Front: author local -Z maps to Godot
            // local +Z after the inherited Roblox->Godot handedness flip.
            // Label3D fronts therefore align with the physical board paper.
            var label=new Label3D {
                Name="OriginalHeading",
                Text=text,
                FontSize=originalFont,
                PixelSize=fontWorldScale,
                Modulate=new Color(rgb[0]/255f,rgb[1]/255f,rgb[2]/255f,
                    1f-transparency),
                Position=new Vector3(uv[0]*size[0]*Stud,
                    uv[1]*size[1]*Stud,
                    (.5f*size[2]+.025f)*Stud)
            };
            holder.AddChild(label);
            count++;
        }
        HeadingCount+=count;
        _panels.Add((holder,page,count));
    }

    private static float[] Vector(JsonElement row,string name,int length)
    {
        var values=row.GetProperty(name);
        if(values.ValueKind!=JsonValueKind.Array || values.GetArrayLength()!=length)
            throw new InvalidDataException("Unexpected original source billboard vector");
        var result=new float[length];
        var i=0;
        foreach(var x in values.EnumerateArray())
        {
            var v=x.GetSingle();
            if(!float.IsFinite(v) || Math.Abs(v)>100000)
                throw new InvalidDataException("Nonfinite source heading frame");
            result[i++]=v;
        }
        return result;
    }

    private static Basis SourceBasis(float[] r) => new(
        new Vector3(r[0],r[3],-r[6]),
        new Vector3(r[1],r[4],-r[7]),
        new Vector3(-r[2],-r[5],r[8]));
}

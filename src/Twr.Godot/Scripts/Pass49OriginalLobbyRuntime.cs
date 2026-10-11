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
/// The owner's exact Workspace/Lobby authored geometry, light fixtures and
/// eight camera CFrames. External MeshPart/CSG triangle data is absent from
/// the original RBXLX, so those parts use honest sized/rotated bound proxies.
/// UI remains the previous functional offline Windows menu and armory.
/// </summary>
public partial class Pass49OriginalLobbyRuntime : Node3D
{
    public const string PackName = "SourceLobby49.json.gz";
    public const string OriginalXmlSha =
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    public const string OriginalPackSha =
        "98a8bf71c0561be814b2de01831b48e413e349ed4925fab3c0b8a403d7845b49";
    private const float Stud = RobloxUnits.MetersPerStud;
    private const long MaxPackedBytes = 1024 * 1024;
    private const int MaxUnpackedBytes = 6 * 1024 * 1024;

    private sealed class RenderGroup
    {
        public Mesh Mesh = null!;
        public StandardMaterial3D Material = null!;
        public bool Shadow;
        public readonly List<Transform3D> Instances = [];
    }

    private readonly Dictionary<string, Transform3D> _cameras =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Transform3D> _loadoutPoints =
        new(StringComparer.Ordinal);
    private Camera3D? _camera;

    public bool OwnerSourceVerified { get; private set; }
    public int SourceGeometryParts { get; private set; }
    public int VisibleSourceParts { get; private set; }
    public int UnresolvedMeshProxyParts { get; private set; }
    public int SourceLights { get; private set; }
    public int VisibleLightEmitters { get; private set; }
    public int SourceCameraCount => _cameras.Count;
    public int SourceLoadoutPointCount => _loadoutPoints.Count;
    public int RenderBatchCount { get; private set; }
    public string ActiveCameraName { get; private set; } = "";
    public bool HasCamera(string name) => _cameras.ContainsKey(name);
    public bool HasLoadoutPoint(string name) => _loadoutPoints.ContainsKey(name);

    public static Pass49OriginalLobbyRuntime? TryBuild(Node parent)
    {
        var pack = FindPack();
        if (pack is null) return null; // no change to normal text menus
        var stage = new Pass49OriginalLobbyRuntime { Name="Pass49OriginalLobby" };
        try
        {
            stage.Load(pack);
            parent.AddChild(stage);
            if (!stage.SetCamera("Start"))
                throw new InvalidDataException("Original lobby Start camera missing");
            // Optional Pass50 authentic static bulletin board headings. A
            // missing/corrupt owner-specific sign pack must never break the
            // actual map/armory/perks menu or the Pass49 3D lobby itself.
            Pass50SourceLobbySignsRuntime.TryBuild(stage);
            GD.Print($"TWR_PASS49_LOBBY_READY source_parts={stage.SourceGeometryParts} " +
                $"visible={stage.VisibleSourceParts} proxy_meshes={stage.UnresolvedMeshProxyParts} " +
                $"render_batches={stage.RenderBatchCount} cameras={stage.SourceCameraCount} " +
                $"lights={stage.SourceLights} source_sha_verified={stage.OwnerSourceVerified}");
            return stage;
        }
        catch(Exception error)
        {
            GD.PushWarning("TWR_PASS49_LOBBY_FALLBACK " + error.Message);
            if (stage.GetParent() is not null) stage.GetParent()!.RemoveChild(stage);
            stage.Free();
            return null;
        }
    }

    public bool SetCamera(string name)
    {
        if (_camera is null || !_cameras.TryGetValue(name,out var frame))
            return false;
        _camera.Transform=frame;
        _camera.Current=true;
        ActiveCameraName=name;
        return true;
    }

    private static string? FindPack()
    {
        var executable=Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        foreach(var dir in new[] {
            executable,Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot")
        })
        {
            var path=Path.Combine(dir,"Content","Lobby",PackName);
            if(File.Exists(path))return path;
        }
        return null;
    }

    private void Load(string filename)
    {
        var info=new FileInfo(filename);
        if(!info.Exists || info.Length<=0 || info.Length>MaxPackedBytes)
            throw new InvalidDataException("Lobby original data outside compressed size cap");
        var bytes=File.ReadAllBytes(filename);
        var actualSha=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        using var input=new MemoryStream(bytes);
        using var gzip=new GZipStream(input,CompressionMode.Decompress);
        using var uncompressed=new MemoryStream();
        var buffer=new byte[65536];
        int read;
        while((read=gzip.Read(buffer))>0)
        {
            if (uncompressed.Length+read>MaxUnpackedBytes)
                throw new InvalidDataException("Lobby decompressed size cap exceeded");
            uncompressed.Write(buffer,0,read);
        }
        using var document=JsonDocument.Parse(uncompressed.ToArray());
        var root=document.RootElement;
        if(root.GetProperty("format").GetString()!="twr-pass49-original-lobby-v1")
            throw new InvalidDataException("Wrong source lobby pack format");
        var synthetic=root.TryGetProperty("synthetic",out var flag) &&
            flag.ValueKind==JsonValueKind.True;
        // Pass50's native test composes a fabricated source-sign pack with
        // the existing fabricated Pass49 3D lobby. Ordinary game startup
        // still rejects all synthetic packs; only explicit CI smoke flags
        // can enable this fixture.
        var smoke=OS.GetCmdlineUserArgs().Any(arg =>
            arg is "--smoke-pass49" or "--smoke-pass50");
        if(synthetic && !smoke)
            throw new InvalidDataException("Synthetic lobby used outside smoke test");
        if(!synthetic &&
            (actualSha!=OriginalPackSha ||
             root.GetProperty("owner_place_sha256").GetString()!=OriginalXmlSha))
            throw new InvalidDataException("Original lobby source SHA mismatch");

        var geometry=root.GetProperty("geometry");
        var cameras=root.GetProperty("cameras");
        var loadouts=root.GetProperty("loadout_points");
        var lights=root.GetProperty("lights");
        if(geometry.GetArrayLength() is <2 or >1500 ||
           lights.GetArrayLength()>80 ||
           cameras.EnumerateObject().Count() is <1 or >16 ||
           loadouts.EnumerateObject().Count()>12)
            throw new InvalidDataException("Source lobby content budgets exceeded");
        if(!synthetic &&
            (geometry.GetArrayLength()!=818 || lights.GetArrayLength()!=18 ||
             cameras.EnumerateObject().Count()!=8 ||
             loadouts.EnumerateObject().Count()!=5))
            throw new InvalidDataException("Original source lobby record counts changed");

        foreach(var cam in cameras.EnumerateObject())
        {
            if(!AllowedCamera(cam.Name))throw new InvalidDataException("Unknown source camera");
            _cameras.Add(cam.Name,Frame(cam.Value));
        }
        foreach(var anchor in loadouts.EnumerateObject())
        {
            if(!AllowedLoadout(anchor.Name))
                throw new InvalidDataException("Unknown source loadout point");
            _loadoutPoints.Add(anchor.Name,Frame(anchor.Value));
        }

        // Render only authored part positions/appearance. Native ball/cylinder
        // and WedgePart shapes have geometric primitives. External MeshPart
        // and CSG files are not in the source and stay visible as bounding proxies.
        var batches=new Dictionary<string,RenderGroup>(StringComparer.Ordinal);
        foreach(var part in geometry.EnumerateArray())
        {
            SourceGeometryParts++;
            var opacity=ReadFloat(part,"opacity",1);
            if(opacity<=.001f)continue;
            if(opacity>1.0001f)throw new InvalidDataException("Bad lobby opacity");
            var cls=part.GetProperty("class").GetString() ?? "";
            if(cls is not ("Part" or "MeshPart" or "UnionOperation" or
                          "WedgePart" or "CornerWedgePart"))
                throw new InvalidDataException("Unexpected original lobby part class");
            var proxy=cls is "MeshPart" or "UnionOperation";
            if(proxy)UnresolvedMeshProxyParts++;
            VisibleSourceParts++;

            var rgb=ReadNumbers(part,"rgb",3);
            var materialCode=GetString(part,"mat","272");
            var materialKey=string.Join(",",rgb.Select(x=>Math.Clamp((int)x,0,255))) +
                "|"+materialCode+"|"+Math.Round(opacity,3);
            var shape=GetString(part,"shape","1");
            var special=GetString(part,"specialMeshType","");
            var meshKind=cls=="WedgePart" ? "wedge" :
                cls=="Part" && (shape=="0" || special is "3" or "0") ? "sphere" :
                cls=="Part" && (shape=="2" || special=="4") ? "cylinder" :
                "box";
            var shadow=part.TryGetProperty("shadow",out var cast) &&
                cast.ValueKind==JsonValueKind.True;
            var key=meshKind+"|"+materialKey+"|"+shadow;
            if(!batches.TryGetValue(key,out var batch))
            {
                var mesh=meshKind switch {
                    "wedge" => RobloxPrimitiveGeometry.WedgeMesh(),
                    "sphere" => new SphereMesh { Radius=.5f,Height=1f },
                    "cylinder" => new CylinderMesh {
                        TopRadius=.5f,BottomRadius=.5f,Height=1f },
                    _ => new BoxMesh { Size=Vector3.One }
                };
                var surface=new StandardMaterial3D {
                    AlbedoColor=new Color(rgb[0]/255f,rgb[1]/255f,
                        rgb[2]/255f,opacity),
                    Roughness=.84f,
                    Metallic=.05f,
                    Transparency=opacity<.995f
                        ? BaseMaterial3D.TransparencyEnum.Alpha
                        : BaseMaterial3D.TransparencyEnum.Disabled
                };
                RobloxMaterialSurface.Apply(surface,materialCode,null);
                batch=new RenderGroup {Mesh=mesh,Material=surface,Shadow=shadow};
                batches.Add(key,batch);
            }
            batch.Instances.Add(PartFrame(part));
        }
        if (!synthetic && (VisibleSourceParts!=774 ||
                            UnresolvedMeshProxyParts!=505))
            throw new InvalidDataException("Original visible lobby geometry changed");
        foreach(var item in batches.Values)
        {
            var mm=new MultiMesh {
                TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,
                Mesh=item.Mesh,
                InstanceCount=item.Instances.Count
            };
            var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,
                float.PositiveInfinity);
            var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,
                float.NegativeInfinity);
            var bounds=item.Mesh.GetAabb();
            for(var i=0;i<item.Instances.Count;i++)
            {
                var xf=item.Instances[i];
                mm.SetInstanceTransform(i,xf);
                var box=Pass45FallbackProxyStreamer.WorldBounds(xf,bounds);
                min=min.Min(box.Position);
                max=max.Max(box.End);
            }
            mm.CustomAabb=new Aabb(min,max-min);
            AddChild(new MultiMeshInstance3D {
                Name="OriginalLobbyBatch",
                Multimesh=mm,
                MaterialOverride=item.Material,
                CastShadow=item.Shadow
                    ? GeometryInstance3D.ShadowCastingSetting.On
                    : GeometryInstance3D.ShadowCastingSetting.Off
            });
            RenderBatchCount++;
        }

        foreach(var light in lights.EnumerateArray())
        {
            SourceLights++;
            if(light.TryGetProperty("enabled",out var enabled) &&
               enabled.ValueKind==JsonValueKind.False)
                continue;
            var color=ReadNumbers(light,"rgb",3);
            var energy=Math.Clamp(ReadFloat(light,"energy",1),0f,5f);
            var range=Math.Clamp(ReadFloat(light,"range",16)*Stud,1f,55f);
            var position=SourcePosition(light);
            var name=GetString(light,"class","");
            Light3D emitter;
            if(name=="PointLight")
                emitter=new OmniLight3D {OmniRange=range};
            else if(name=="SpotLight")
                emitter=new SpotLight3D {
                    SpotRange=range,
                    SpotAngle=Math.Clamp(ReadFloat(light,"angle",45),1f,130f)
                };
            else throw new InvalidDataException("Unsupported source lobby light");
            emitter.LightColor=new Color(color[0]/255f,
                color[1]/255f,color[2]/255f);
            emitter.LightEnergy=energy;
            emitter.ShadowEnabled=false;
            emitter.Transform=new Transform3D(SourceBasis(light),position);
            AddChild(emitter);
            VisibleLightEmitters++;
        }
        // Avoid an entirely dark scene in the absence of original skybox
        // textures. This fill light is explicitly approximate; the 18 source
        // Point/Spot emitters above retain their original authored positions.
        AddChild(new DirectionalLight3D {
            Name="ApproximateAmbientFill",
            LightEnergy=.52f,
            ShadowEnabled=false,
            RotationDegrees=new Vector3(-55,20,0)
        });
        _camera=new Camera3D {
            Name="OriginalSourceLobbyCamera",
            Fov=68,
            Near=.03f,
            Far=250f,
            Current=true
        };
        AddChild(_camera);
        OwnerSourceVerified=!synthetic;
    }

    private static bool AllowedCamera(string n) =>
        n is "Start" or "Shop" or "Loadout" or "Perks" or
             "Options" or "Gifts" or "Leaderboards" or "WeaponNode";
    private static bool AllowedLoadout(string n) =>
        n is "Primary" or "Secondary" or "Melee" or "Utility" or "View";
    private static string GetString(JsonElement x,string name,string fallback="") =>
        x.TryGetProperty(name,out var v) && v.ValueKind==JsonValueKind.String
            ? v.GetString() ?? fallback : fallback;
    private static float ReadFloat(JsonElement x,string name,float fallback)
    {
        var n=x.TryGetProperty(name,out var value) ? value.GetSingle() : fallback;
        if(!float.IsFinite(n) || Math.Abs(n)>100000f)
            throw new InvalidDataException("Nonfinite original lobby number");
        return n;
    }
    private static float[] ReadNumbers(JsonElement row,string name,int count)
    {
        var data=row.GetProperty(name);
        if(data.GetArrayLength()!=count)
            throw new InvalidDataException("Unexpected source vector dimensions");
        var result=new float[count];
        var i=0;
        foreach(var v in data.EnumerateArray())
        {
            var f=v.GetSingle();
            if(!float.IsFinite(f) || Math.Abs(f)>100000f)
                throw new InvalidDataException("Nonfinite original lobby coordinates");
            result[i++]=f;
        }
        return result;
    }

    private static Vector3 SourcePosition(JsonElement source)
    {
        var t=ReadNumbers(source,"t",3);
        return new Vector3(t[0],t[1],-t[2])*Stud;
    }
    private static Basis SourceBasis(JsonElement source)
    {
        var r=ReadNumbers(source,"r",9);
        return new Basis(
            new Vector3(r[0],r[3],-r[6]),
            new Vector3(r[1],r[4],-r[7]),
            new Vector3(-r[2],-r[5],r[8]));
    }
    private static Transform3D Frame(JsonElement source) =>
        new(SourceBasis(source),SourcePosition(source));

    private static Transform3D PartFrame(JsonElement source)
    {
        var pos=SourcePosition(source);
        var basis=SourceBasis(source);
        var dimensions=ReadNumbers(source,"s",3);
        if(dimensions.Any(v=>v<=0 || v>5000))
            throw new InvalidDataException("Invalid original lobby source dimensions");
        var size=new Vector3(dimensions[0],dimensions[1],dimensions[2])*Stud;
        if(source.TryGetProperty("specialMeshScale",out var special) &&
           special.ValueKind==JsonValueKind.Array)
        {
            var s=ReadNumbers(source,"specialMeshScale",3);
            if(s.Any(v=>v<=0 || v>5000))
                throw new InvalidDataException("Bad original SpecialMesh scale");
            // Primitive SpecialMesh.Scale multiplies its carrier Part.Size.
            // Treating Scale=(1,1,1) as an absolute one-stud mesh would
            // incorrectly shrink the authored lobby shelf wires/windows.
            size=new Vector3(size.X*s[0],size.Y*s[1],size.Z*s[2]);
        }
        if(source.TryGetProperty("specialMeshOffset",out var offset) &&
           offset.ValueKind==JsonValueKind.Array)
        {
            var o=ReadNumbers(source,"specialMeshOffset",3);
            pos+=basis * (new Vector3(o[0],o[1],-o[2])*Stud);
        }
        return new Transform3D(new Basis(
            basis.X*size.X,basis.Y*size.Y,basis.Z*size.Z),pos);
    }
}

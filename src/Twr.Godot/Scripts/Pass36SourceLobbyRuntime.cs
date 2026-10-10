using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Recreates source-positioned 3D front-end geometry and eight real CamPoints
/// from the owner's TestPlace.rbxlx Workspace.Lobby. None of the 518 missing
/// custom MeshPart/CSG binaries is invented: those pieces remain box proxies.
/// The existing functional 2D menu stays authoritative and remains available
/// when this optional owner-held source pack is absent.
/// </summary>
public partial class Pass36SourceLobbyRuntime : Node3D
{
    private const string PackName = "OriginalLobby.lobby36.jsonl.gz";
    private const string OriginalSourceSha = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    private const string OriginalPackSha = "484ca68c8113b7e41a3024b570f9480474f13cbb56b576ea2a6c5a827c1b36c5";
    private const int MaxPackedBytes = 256 * 1024;
    private const int MaxRecords = 1024;
    private const float Stud = RobloxUnits.MetersPerStud;
    private static readonly Vector3 OriginStuds = new(290f, -2945f, 0f);
    private readonly Dictionary<string, Camera3D> _cameras = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Batch> _batches = new(StringComparer.Ordinal);
    private Camera3D? _currentCamera;
    private WorldEnvironment? _worldEnvironment;
    private global::Godot.Environment? _sourceEnvironment;

    private sealed class Batch
    {
        public Mesh Mesh = null!;
        public StandardMaterial3D Material = null!;
        public List<Transform3D> Instances = new();
        public Vector3 Minimum = new(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        public Vector3 Maximum = new(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
    }

    public int RecoveredObjectCount { get; private set; }
    public int VisualProxyCount { get; private set; }
    public int RecoveredCameraCount => _cameras.Count;
    public int RenderBatchCount => _batches.Count;
    public string CurrentCameraName { get; private set; } = "Start";
    public bool Active { get; private set; }
    public bool IsSyntheticSmoke { get; private set; }

    public static Pass36SourceLobbyRuntime? TryBuild(Node parent)
    {
        var root = Path.GetDirectoryName(OS.GetExecutablePath()) ?? Directory.GetCurrentDirectory();
        var path = new[]
        {
            Path.Combine(root,"Content","Lobby",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Lobby",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","Lobby",PackName)
        }.FirstOrDefault(File.Exists);
        if (path is null)
        {
            GD.Print("TWR_PASS36_LOBBY_ABSENT fallback=flat_menu");
            return null;
        }
        var instance = new Pass36SourceLobbyRuntime { Name = "RecoveredSourceLobby" };
        try
        {
            instance.ReadPack(path);
            parent.AddChild(instance);
            instance.SetActive(true);
            GD.Print($"TWR_PASS36_LOBBY_READY geometry={instance.RecoveredObjectCount} " +
                $"cameras={instance.RecoveredCameraCount} batches={instance.RenderBatchCount} " +
                $"missing_mesh_proxies={instance.VisualProxyCount} synthetic={instance.IsSyntheticSmoke}");
            return instance;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS36_LOBBY_REJECTED fallback=flat_menu: " + error.Message);
            instance.Free();
            return null;
        }
    }

    public bool SwitchView(string cameraName)
    {
        if (!_cameras.TryGetValue(cameraName,out var next)) return false;
        CurrentCameraName=cameraName;
        _currentCamera=next;
        if (Active)
        {
            foreach (var camera in _cameras.Values) camera.Current=false;
            next.Current=true;
        }
        return true;
    }

    public void SetActive(bool enabled)
    {
        Active=enabled;
        Visible=enabled;
        // WorldEnvironment is not a VisualInstance3D: merely hiding this
        // parent DOES NOT deactivate its environment. Release ownership
        // before the first-person gameplay map sets its own source sky/fog.
        if (_worldEnvironment is not null)
            _worldEnvironment.Environment=enabled ? _sourceEnvironment : null;
        foreach (var camera in _cameras.Values) camera.Current=false;
        if (enabled) SwitchView(CurrentCameraName);
    }

    private static JsonElement[] Array(JsonElement parent,string key,int required)
    {
        if (!parent.TryGetProperty(key,out var prop) || prop.ValueKind!=JsonValueKind.Array ||
            prop.GetArrayLength()!=required) throw new InvalidDataException("Invalid lobby "+key);
        return prop.EnumerateArray().ToArray();
    }
    private static float[] Numbers(JsonElement parent,string key,int required)
    {
        var result=Array(parent,key,required).Select(x=>x.GetSingle()).ToArray();
        if (result.Any(x=>!float.IsFinite(x)||Math.Abs(x)>100000f))
            throw new InvalidDataException("Out-of-range lobby "+key);
        return result;
    }
    private static Vector3 WorldPosition(JsonElement row)
    {
        var p=Numbers(row,"t",3);
        return new Vector3((p[0]-OriginStuds.X)*Stud,
            (p[1]-OriginStuds.Y)*Stud,
            -(p[2]-OriginStuds.Z)*Stud);
    }
    private static Basis WorldBasis(JsonElement row)
    {
        var r=Numbers(row,"r",9);
        return new Basis(new Vector3(r[0],r[3],-r[6]),
            new Vector3(r[1],r[4],-r[7]),
            new Vector3(-r[2],-r[5],r[8]));
    }
    private static Vector3 ScaledSize(JsonElement row)
    {
        var size=Numbers(row,"s",3);
        if (size.Any(x=>x<.0001f))throw new InvalidDataException("Degenerate lobby geometry");
        return new Vector3(size[0],size[1],size[2])*Stud;
    }
    private static Color SourceColor(JsonElement row,float alpha=1f)
    {
        var rgb=Numbers(row,"rgb",3);
        if (rgb.Any(x=>x<0||x>255))throw new InvalidDataException("Bad source lobby color");
        return new Color(rgb[0]/255f,rgb[1]/255f,rgb[2]/255f,alpha);
    }
    private static string String(JsonElement row,string key,string fallback="") =>
        row.TryGetProperty(key,out var x) && x.ValueKind==JsonValueKind.String
            ? x.GetString() ?? fallback : fallback;
    private static bool Enabled(JsonElement row,string key,bool fallback) =>
        !row.TryGetProperty(key,out var val) ? fallback : val.ValueKind==JsonValueKind.True;

    private void ReadPack(string file)
    {
        var info=new FileInfo(file);
        if (info.Length < 250 || info.Length>MaxPackedBytes)
            throw new InvalidDataException("Lobby source pack violates byte bound");
        var packed=File.ReadAllBytes(file);
        var checksum=Convert.ToHexString(SHA256.HashData(packed)).ToLowerInvariant();
        var smokeOnly=OS.GetCmdlineUserArgs().Contains("--smoke-pass36",StringComparer.Ordinal);
        if (checksum != OriginalPackSha && !smokeOnly)
            throw new InvalidDataException("Lobby source pack differs from verified original SHA");
        using var compressed=new MemoryStream(packed);
        using var gz=new GZipStream(compressed,CompressionMode.Decompress);
        using var reader=new StreamReader(gz,Encoding.UTF8);
        var text=ReadLineBounded(reader,8192) ??
            throw new InvalidDataException("Missing original Lobby header");
        using var header=JsonDocument.Parse(text);
        var h=header.RootElement;
        if (String(h,"format")!="twr-pass36-source-lobby-v1" ||
            String(h,"source_path")!="Workspace.Lobby")
            throw new InvalidDataException("Unknown lobby source format");
        IsSyntheticSmoke=Enabled(h,"synthetic",false);
        if (IsSyntheticSmoke ? !smokeOnly :
            String(h,"source_rbxlx_sha256")!=OriginalSourceSha || checksum!=OriginalPackSha)
            throw new InvalidDataException("Unverified source lobby origin or checksum");
        var origin=Numbers(h,"origin_studs",3);
        if (Math.Abs(origin[0]-290)>0.01 || Math.Abs(origin[1]+2945)>0.01 ||
            Math.Abs(origin[2])>0.01) throw new InvalidDataException("Source lobby origin changed");
        var counts=h.GetProperty("counts");
        var expectedObjects=counts.GetProperty("geometry").GetInt32();
        var expectedCameras=counts.GetProperty("cameras").GetInt32();
        var expectedLights=counts.GetProperty("lights").GetInt32();
        if (expectedObjects < (IsSyntheticSmoke?10:800) || expectedObjects>900 ||
            expectedCameras!=8 || expectedLights<0 || expectedLights>20)
            throw new InvalidDataException("Source lobby count mismatch");
        var cameraRows=0;var lightingRows=0;var total=0;
        while ((text=ReadLineBounded(reader,4096)) is not null)
        {
            if (++total>MaxRecords)throw new InvalidDataException("Lobby record count exceeds bound");
            using var doc=JsonDocument.Parse(text);
            var row=doc.RootElement;
            switch (String(row,"kind"))
            {
                case "geometry":AddGeometry(row);break;
                case "camera":AddSourceCamera(row);cameraRows++;break;
                case "light":AddSourceLight(row,lightingRows++);break;
                default:throw new InvalidDataException("Unrecognized source lobby row");
            }
        }
        if (RecoveredObjectCount!=expectedObjects || cameraRows!=expectedCameras ||
            lightingRows!=expectedLights || _cameras.Count!=8)
            throw new InvalidDataException("Truncated lobby source pack");
        var batchIndex = 0;
        foreach (var (_,batch) in _batches)
        {
            var multim=new MultiMesh { Mesh=batch.Mesh,
                TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,
                InstanceCount=batch.Instances.Count };
            for (var i=0;i<batch.Instances.Count;i++)
                multim.SetInstanceTransform(i,batch.Instances[i]);
            AddChild(new MultiMeshInstance3D
            {
                Name="LobbyBatch_"+batchIndex++,
                Multimesh=multim,
                MaterialOverride=batch.Material,
                CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
                CustomAabb=new Aabb(batch.Minimum-Vector3.One,
                    batch.Maximum-batch.Minimum+Vector3.One*2f)
            });
        }
        _sourceEnvironment=new global::Godot.Environment
        {
            BackgroundMode=global::Godot.Environment.BGMode.Color,
            BackgroundColor=new Color(.045f,.045f,.048f),
            AmbientLightSource=global::Godot.Environment.AmbientSource.Color,
            AmbientLightColor=new Color(.70f,.67f,.61f),
            AmbientLightEnergy=.65f
        };
        _worldEnvironment=new WorldEnvironment { Environment=_sourceEnvironment };
        AddChild(_worldEnvironment);
    }

    private static string? ReadLineBounded(StreamReader reader,int max)
    {
        var line=new StringBuilder();int c;
        while ((c=reader.Read())!=-1)
        {
            if (c=='\n')return line.ToString().TrimEnd('\r');
            if (line.Length>=max)throw new InvalidDataException("Oversized Lobby JSON row");
            line.Append((char)c);
        }
        return line.Length>0?line.ToString():null;
    }

    private void AddSourceCamera(JsonElement row)
    {
        var name=String(row,"name");
        if (!_cameras.TryAdd(name,new Camera3D
        {
            Name="SourceCam_"+name,
            Transform=new Transform3D(WorldBasis(row),WorldPosition(row)),
            Fov=60f, Current=false,
            Far=400f
        })) throw new InvalidDataException("Duplicate original lobby camera");
        AddChild(_cameras[name]);
    }

    private void AddSourceLight(JsonElement row,int index)
    {
        // Camera-lit menu: recreate limited Roblox source light positions.
        // Actual global illumination and material light responses differ.
        if (index>=12 || !Enabled(row,"enabled",true))return;
        var dist=row.GetProperty("range").GetSingle();
        var energy=row.GetProperty("brightness").GetSingle();
        var source=SourceColor(row);
        if (dist<=0 || energy<=0)return;
        AddChild(new OmniLight3D
        {
            Name="SourceLobbyLight_"+index,
            Position=WorldPosition(row),
            LightColor=source,
            LightEnergy=Math.Min(1.5f,Math.Max(.1f,energy*.45f)),
            OmniRange=Math.Min(28f,dist*Stud),
            ShadowEnabled=false
        });
    }

    private void AddGeometry(JsonElement row)
    {
        RecoveredObjectCount++;
        var cls=String(row,"class");
        if (cls is not ("Part" or "WedgePart" or "MeshPart" or "UnionOperation"))
            throw new InvalidDataException("Unknown original Lobby primitive class");
        if (cls is "MeshPart" or "UnionOperation")VisualProxyCount++;
        var size=ScaledSize(row);
        var alpha=row.GetProperty("opacity").GetSingle();
        if (alpha<0 || alpha>1)throw new InvalidDataException("Bad lobby opacity");
        if (alpha<.04f)return; // Original fully invisible support Part.
        var rgb=Numbers(row,"rgb",3);
        var mat=String(row,"material");
        var shape=String(row,"shape","1");
        var color=SourceColor(row,alpha);
        var meshKey=cls=="WedgePart" ? "wedge" :
            cls=="Part" && shape=="0" ? "sphere" :
            cls=="Part" && shape=="2" ? "cylinder" : "box";
        var key=meshKey+"/"+mat+"/"+string.Join(',',rgb)+"/"+alpha.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
        if (!_batches.TryGetValue(key,out var batch))
        {
            Mesh mesh=meshKey switch
            {
                "wedge"=>RobloxPrimitiveGeometry.WedgeMesh(),
                "sphere"=>new SphereMesh { Radius=.5f,Height=1f },
                "cylinder"=>new CylinderMesh { BottomRadius=.5f,TopRadius=.5f,Height=1f },
                _=>new BoxMesh { Size=Vector3.One }
            };
            var metal=mat is "1088" or "1056" or "1040";
            var m=new StandardMaterial3D
            {
                AlbedoColor=color,Metallic=metal?.65f:.04f,
                Roughness=metal?.36f:.84f,
                Transparency=alpha<.995f ? BaseMaterial3D.TransparencyEnum.Alpha :
                    BaseMaterial3D.TransparencyEnum.Disabled
            };
            RobloxMaterialSurface.Apply(m,mat,null);
            batch=new Batch { Mesh=mesh,Material=m };
            _batches[key]=batch;
        }
        var basis=WorldBasis(row);
        var t=new Transform3D(new Basis(basis.X*size.X,basis.Y*size.Y,basis.Z*size.Z),WorldPosition(row));
        batch.Instances.Add(t);
        // Expanded axis-aligned oriented box extents (conservative culling).
        var e=(basis.X.Abs()*size.X+basis.Y.Abs()*size.Y+basis.Z.Abs()*size.Z)*.5f;
        var position=t.Origin;
        batch.Minimum=batch.Minimum.Min(position-e);
        batch.Maximum=batch.Maximum.Max(position+e);
    }
}
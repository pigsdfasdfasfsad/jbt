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
/// Loads ORIGINAL CMaps / Walls / Server Walls collision pieces and CMaps / Map
/// / Objects source positions from the user's TestPlace.rbxlx. Neither set is
/// a full map. F6 collision/F5 object display default to OFF on legacy blockouts.
/// Missing Roblox MeshPart/CSG geometry is never faked as authentic art.
/// </summary>
public partial class Pass36SourceFragmentsRuntime : Node3D
{
    public const uint SourceServerCollisionLayer = 16;
    private const string OwnerSourceSha = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    private const long MaxFileBytes = 2 * 1024 * 1024;
    private const int MaxRows = 2000;
    private const float Stud = RobloxUnits.MetersPerStud;

    private sealed record Expected(string WallSha, int WallCount, int WallProxies,
                                   string ObjectSha, int ObjectCount, int NativeObjects);
    private static readonly Dictionary<string,Expected> Maps = new(StringComparer.Ordinal)
    {
        ["Bypass"] = new("1c28d0963f761ad8e447da4b4d218fbc6c7cc6770e21d27736f46fe893bb3299",768,0,"eca918d4283260da82b2190ff0f4f6dc311b0ff874677fef32e1addd7b6b2a97",852,135),
        ["Cabin"] = new("0979253f14a2d9fc322503b1248b0e9c88a9b81ec7b042b0f5f9921191c971c5",871,0,"a75ffe943241f65f8a243e0a7a0b9e1f6dd3d494705eb14b42eaea7fe4ca5ef8",36,5),
        ["Cargo"] = new("519df9651d8bac4573be485fd208012eb390dac2b8ac3701da4b7bfc571e2d44",321,0,"86c371c90dd8d16118c8578212a4a356e60679060cace0d5679f7f0bc06b2abb",27,0),
        ["District"] = new("024561f2adbbece6e4caa7cd9910240cc7349287c2dd0a8aaa5c247588dec1d4",346,54,"7cecfe17a99791fab76f080c3b9369e259bbb856b5a591fe32a5e7d8ee18c529",195,6),
        ["Expressway"] = new("c51dd9cc6505908b9d245614419c287ec4c67374cb6eba223a4e766ea2a35077",274,0,"f0e4002b810040b53ba5177e34e016b3a7b259e9b52b771f95b0c8baca9f8d3b",22,0),
        ["Manor"] = new("cd27526b16db2b9ef0795ea320ea031c6782c64c30768c5229b6b8d1bee4c16a",785,0,"3037c7beac5f8b6e0768175294900ea96b5b3ac25bfceb7d908c3ec121d0d33c",1633,7),
        ["Mill"] = new("b390448f386488378f8a769ea96ed009a33ec7d69310a02534bab38a32d1e30f",1055,0,"8a0d4cc2b09c68eba95378705079bd79b6e043ec88038f8623ae8a583450a17c",267,33),
        ["Prison"] = new("18eba0c763ec1d0e4b2ceacf59b0e5ebdbf915f8a82198378ab4980594e0197a",1234,0,"285bb765a46a24a5d4deec951b3ffd2146daf78eae0bcb57c8f4f989399be27e",372,90),
        ["Ranch"] = new("984a2ac91f4f7c1b24a2062279f08f17a2ed24a7c47e75530732b3152cee06e6",1266,0,"75644405766728147369591cec31282d9756253663c15ff08dca273b8ac708c9",152,29),
    };

    private readonly List<StaticBody3D> _walls = new();
    private readonly List<MeshInstance3D> _objects = new();
    private readonly Dictionary<string,StandardMaterial3D> _materials = new(StringComparer.Ordinal);
    private Node3D? _objectsRoot;
    public int WallCount => _walls.Count;
    public int NativeObjectCount => _objects.Count;
    public int MissingOriginalCustomObjectCount { get; private set; }
    public int ApproximateServerMeshWallCount { get; private set; }
    public bool ServerCollisionEnabled { get; private set; }
    public bool NativeObjectPreviewEnabled { get; private set; }

    public static Pass36SourceFragmentsRuntime? TryBuild(Node3D owner,string mapName)
    {
        if (!Maps.TryGetValue(mapName,out var expected)) return null;
        var folder=Path.GetDirectoryName(OS.GetExecutablePath()) ?? Directory.GetCurrentDirectory();
        var wfile=Find("ServerWalls",mapName+".serverwalls36.jsonl.gz",folder);
        var ofile=Find("MapObjects",mapName+".objects36.jsonl.gz",folder);
        if (wfile is null || ofile is null)
        {
            GD.Print("TWR_PASS36_SOURCE_FRAGMENTS_ABSENT map="+mapName+" fallback=original_blockout");
            return null;
        }
        var pack=new Pass36SourceFragmentsRuntime { Name="Pass36SourceFragments" };
        try
        {
            pack.ReadSource(wfile,ofile,mapName,expected);
            owner.AddChild(pack);
            pack.SetServerCollision(false);
            pack.SetNativeObjectPreview(false);
            GD.Print($"TWR_PASS36_SOURCE_FRAGMENTS_READY map={mapName} walls={pack.WallCount} " +
                $"native_objects={pack.NativeObjectCount} missing_meshes={pack.MissingOriginalCustomObjectCount} " +
                $"mesh_wall_proxies={pack.ApproximateServerMeshWallCount} default=OFF");
            return pack;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS36_SOURCE_FRAGMENTS_REJECTED map="+mapName+
                " fallback=legacy: "+error.Message);
            pack.Free();
            return null;
        }
    }

    public void SetServerCollision(bool enabled)
    {
        ServerCollisionEnabled=enabled;
        foreach (var body in _walls)
            body.CollisionLayer=enabled ? SourceServerCollisionLayer : 0;
    }

    public void SetNativeObjectPreview(bool enabled)
    {
        NativeObjectPreviewEnabled=enabled;
        if (_objectsRoot is not null) _objectsRoot.Visible=enabled;
    }

    private static string? Find(string folder,string file,string executable)
    {
        var options=new[]
        {
            Path.Combine(executable,"Content",folder,file),
            Path.Combine(Directory.GetCurrentDirectory(),"Content",folder,file),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content",folder,file)
        };
        return options.FirstOrDefault(File.Exists);
    }

    private static string? Line(StreamReader reader,int limit)
    {
        var chars=new StringBuilder();int ch;
        while ((ch=reader.Read())!=-1)
        {
            if(ch=='\n')return chars.ToString().TrimEnd('\r');
            if(chars.Length>=limit)throw new InvalidDataException("Original source row over length budget");
            chars.Append((char)ch);
        }
        return chars.Length>0 ? chars.ToString() : null;
    }

    private static StreamReader OpenValidated(string file,string expectedSha,bool synthetic)
    {
        var bytes=File.ReadAllBytes(file);
        if(bytes.Length<32 || bytes.Length>MaxFileBytes)
            throw new InvalidDataException("Source fragment compressed size out of bounds");
        var sha=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if(!synthetic && sha!=expectedSha)
            throw new InvalidDataException("Source fragment pack SHA-256 mismatch");
        return new StreamReader(new GZipStream(new MemoryStream(bytes),
            CompressionMode.Decompress),Encoding.UTF8,false,8192);
    }

    private static float[] Values(JsonElement row,string key,int count)
    {
        var values=row.GetProperty(key);
        if(values.ValueKind!=JsonValueKind.Array || values.GetArrayLength()!=count)
            throw new InvalidDataException("Malformed original transform "+key);
        var output=new float[count];
        for(var i=0;i<count;i++)
        {
            var v=values[i].GetSingle();
            if(!float.IsFinite(v) || Math.Abs(v)>100000)
                throw new InvalidDataException("Unbounded source coordinate");
            output[i]=v;
        }
        return output;
    }

    private static (Transform3D Pose,Vector3 Size) Transform(JsonElement row)
    {
        var translation=Values(row,"t",3);
        var r=Values(row,"r",9);
        var size=Values(row,"s",3);
        if(size.Any(v=>v<=0.001f || v>50000f))
            throw new InvalidDataException("Original part size invalid");
        var basis=new Basis(new Vector3(r[0],r[3],-r[6]),
            new Vector3(r[1],r[4],-r[7]),
            new Vector3(-r[2],-r[5],r[8]));
        if(Math.Abs(basis.X.Length()-1)>.03f || Math.Abs(basis.Y.Length()-1)>.03f ||
           Math.Abs(basis.Z.Length()-1)>.03f || Math.Abs(basis.X.Dot(basis.Y))>.03f ||
           Math.Abs(basis.X.Dot(basis.Z))>.03f || Math.Abs(basis.Y.Dot(basis.Z))>.03f)
            throw new InvalidDataException("Original CFrame not orthonormal");
        return (new Transform3D(basis,new Vector3(translation[0],translation[1],-translation[2])*Stud),
            new Vector3(size[0],size[1],size[2])*Stud);
    }

    private static bool OriginalHeader(JsonElement h,string type,string map,
        string sourcePath,int expectedCount,bool synthetic)
    {
        return h.GetProperty("format").GetString()==type &&
            h.GetProperty("map").GetString()==map &&
            h.GetProperty("source_path").GetString()==sourcePath &&
            h.GetProperty("owner_rbxlx_sha256").GetString()==(synthetic ? new string('0',64) : OwnerSourceSha) &&
            expectedCount>0 && expectedCount<=MaxRows;
    }

    private void ReadSource(string wallsFile,string objectsFile,string map,Expected expected)
    {
        // --smoke-pass36 is exclusively for synthetic CI fixtures. Normal
        // source files require exact content-addressed expected hashes.
        var synthetic=OS.GetCmdlineUserArgs().Contains("--smoke-pass36",StringComparer.Ordinal) && map=="Manor";
        using (var input=OpenValidated(wallsFile,expected.WallSha,synthetic))
        {
            var heading=Line(input,8192) ?? throw new InvalidDataException("Missing server wall header");
            using var doc=JsonDocument.Parse(heading);var h=doc.RootElement;
            var count=h.GetProperty("wall_count").GetInt32();
            var proxies=h.GetProperty("approximated_collision_meshes").GetInt32();
            if(!OriginalHeader(h,"twr-serverwalls36-v1",map,
                   $"ReplicatedStorage/CMaps/{map}/Walls/Server Walls",count,synthetic) ||
               h.GetProperty("wall_layer").GetInt32()!=SourceServerCollisionLayer ||
               h.GetProperty("default_collision_enabled").GetBoolean() ||
               (!synthetic && (count!=expected.WallCount || proxies!=expected.WallProxies)) ||
               proxies<0 || proxies>count)
                throw new InvalidDataException("Original source server-wall header rejected");
            var proxySeen=0;
            for(var i=0;i<count;i++)
            {
                var text=Line(input,4096) ?? throw new InvalidDataException("Truncated source wall pack");
                using var obj=JsonDocument.Parse(text);var row=obj.RootElement;
                var cls=row.GetProperty("class").GetString();
                if(cls is not ("Part" or "WedgePart" or "MeshPart"))
                    throw new InvalidDataException("Unsupported original server-wall class");
                var approx=cls=="MeshPart";
                if(approx!=row.GetProperty("approximated_mesh").GetBoolean())
                    throw new InvalidDataException("Mesh proxy status must be explicit");
                if(approx)proxySeen++;
                var (pose,size)=Transform(row);
                Shape3D shape=cls=="WedgePart" ? RobloxPrimitiveGeometry.WedgeCollision(size)
                    : new BoxShape3D { Size=size };
                var wall=new StaticBody3D
                {
                    Name="OriginalServerWall"+(i+1),Transform=pose,
                    CollisionLayer=0,CollisionMask=0
                };
                wall.AddChild(new CollisionShape3D {Shape=shape});
                AddChild(wall);_walls.Add(wall);
            }
            if(proxySeen!=proxies || Line(input,64)!=null)
                throw new InvalidDataException("Source wall row count/proxy count mismatch");
            ApproximateServerMeshWallCount=proxySeen;
        }

        _objectsRoot=new Node3D {Name="SourceObjectPreview",Visible=false};
        AddChild(_objectsRoot);
        using(var input=OpenValidated(objectsFile,expected.ObjectSha,synthetic))
        {
            var headline=Line(input,8192) ?? throw new InvalidDataException("Missing source object header");
            using var doc=JsonDocument.Parse(headline);var h=doc.RootElement;
            var count=h.GetProperty("object_count").GetInt32();
            var native=h.GetProperty("native_primitives").GetInt32();
            var missing=h.GetProperty("missing_mesh_or_csg").GetInt32();
            if(!OriginalHeader(h,"twr-source-objects36-v1",map,
                    $"ReplicatedStorage/CMaps/{map}/Map/Objects",count,synthetic) ||
                !h.GetProperty("visual_geometry_incomplete").GetBoolean() ||
                count>MaxRows || native<0 || missing<0 || native+missing!=count ||
                (!synthetic && (count!=expected.ObjectCount || native!=expected.NativeObjects)))
                throw new InvalidDataException("Original source object manifest rejected");
            for(var i=0;i<count;i++)
            {
                var line=Line(input,8192) ?? throw new InvalidDataException("Truncated source objects");
                using var rec=JsonDocument.Parse(line);var row=rec.RootElement;
                var cls=row.GetProperty("class").GetString();
                if(cls is not ("Part" or "WedgePart" or "MeshPart" or "UnionOperation"))
                    throw new InvalidDataException("Unsupported source Object class");
                var real=cls is "Part" or "WedgePart";
                if((row.GetProperty("geometry_status").GetString()=="native_primitive") !=real)
                    throw new InvalidDataException("Source mesh recovery label invalid");
                var (pose,size)=Transform(row);
                if(!real)continue; // NEVER fill missing custom meshes with fake boxes.
                var rgb=Values(row,"rgb",3);
                var transparency=row.GetProperty("transparency").GetSingle();
                if(!float.IsFinite(transparency) || transparency is <0 or >1)
                    throw new InvalidDataException("Invalid original transparency");
                var materialName=row.GetProperty("material").GetString() ?? "";
                var key=materialName+":"+string.Join(",",rgb)+":"+transparency;
                if(!_materials.TryGetValue(key,out var material))
                {
                    material=new StandardMaterial3D
                    {
                        AlbedoColor=new Color(rgb[0]/255f,rgb[1]/255f,rgb[2]/255f,1f-transparency),
                        Roughness=0.9f,
                        Transparency=transparency>0 ? BaseMaterial3D.TransparencyEnum.Alpha
                            : BaseMaterial3D.TransparencyEnum.Disabled
                    };
                    _materials[key]=material;
                }
                var visual=new MeshInstance3D
                {
                    Name="SourceNativeObject"+(i+1),
                    Mesh=cls=="WedgePart" ? RobloxPrimitiveGeometry.WedgeMesh() : new BoxMesh{Size=Vector3.One},
                    MaterialOverride=material,
                    Transform=new Transform3D(new Basis(pose.Basis.X*size.X,
                        pose.Basis.Y*size.Y,pose.Basis.Z*size.Z),pose.Origin)
                };
                _objectsRoot.AddChild(visual);_objects.Add(visual);
            }
            if(Line(input,64)!=null || _objects.Count!=native)
                throw new InvalidDataException("Source object row count does not match actual native primitive instances");
            MissingOriginalCustomObjectCount=missing;
        }
    }
}
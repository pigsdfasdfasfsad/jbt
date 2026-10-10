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
/// Original source-placed CMaps/{map}/Walls/Infected Walls, recovered directly
/// from the user's RBXLX. These belong to DIFFERENT map geometry than the
/// fallback blockouts: collision is OFF until the user opts in with F7.
/// Unknown retail Roblox collision matrix, so treat the infected-only layer
/// as a transparent experimental approximation, not an original game claim.
/// </summary>
public partial class Pass35InfectedWallsRuntime : Node3D
{
    public const uint InfectedWallCollisionLayer = 8;
    private const float MetresPerStud = RobloxUnits.MetersPerStud;
    private const int MaximumWalls = 400;
    private const long MaxCompressedBytes = 512 * 1024;
    private const string OwnerSourceSha = "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    private static readonly Dictionary<string,(string Sha,int Count,int Approximated)> Expected = new(StringComparer.Ordinal)
    {
        ["Ranch"] = ("5a9d549069e743e88b2d4a2e5f02b87ac374970f9d37eedc4a8b994d28f85525",80,0),
        ["Mill"] = ("bc013f95fc0061ef31908a78722b5c87f069e412e3eaa966cf21c4bc3ff226f8",37,0),
        ["Bypass"] = ("87ac6add129d502b2e0d8ae936805ad44e26a5237aaac8caf81998e3cd27dad7",55,0),
        ["District"] = ("524c27501e7a22912e0469754f94697bce4da3fcc03bcea0e3af7dab1db8d612",82,3),
        ["Prison"] = ("b254ec2cc6603a3a39d0eabd6b1566d3c0937410754d1051dd226300bd1ccb95",5,0),
        ["Manor"] = ("ea634fd3b107107418a0606a89e61df430a4fa0f78a49be80a8eec1d66a8ab6f",26,0)
    };
    private readonly List<StaticBody3D> _bodies = new();
    public int WallCount => _bodies.Count;
    public int ApproximateMeshProxyCount { get; private set; }
    public bool Enabled { get; private set; }

    public static Pass35InfectedWallsRuntime? TryBuild(Node3D root, string mapName)
    {
        if (!Expected.TryGetValue(mapName,out var exact)) return null;
        var executableFolder = Path.GetDirectoryName(OS.GetExecutablePath()) ?? Directory.GetCurrentDirectory();
        var filename = mapName + ".infectedwalls35.jsonl.gz";
        var packed = new[]
        {
            Path.Combine(executableFolder,"Content","Walls",filename),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Walls",filename),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","Walls",filename)
        }.FirstOrDefault(File.Exists);
        if (packed is null) return null;

        var wallGroup = new Pass35InfectedWallsRuntime { Name = "Pass35InfectedWalls" };
        try
        {
            wallGroup.Load(packed,mapName,exact);
            root.AddChild(wallGroup);
            wallGroup.SetEnabled(false);
            GD.Print($"TWR_PASS35_INFECTED_WALLS_READY map={mapName} count={wallGroup.WallCount} " +
                $"mesh_proxies={wallGroup.ApproximateMeshProxyCount} default=OFF layer={InfectedWallCollisionLayer}");
            return wallGroup;
        }
        catch (Exception error)
        {
            GD.PushWarning($"TWR_PASS35_INFECTED_WALLS_REJECTED map={mapName} " +
                $"fallback=legacy: {error.Message}");
            wallGroup.Free();
            return null;
        }
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        foreach (var body in _bodies)
            body.CollisionLayer = enabled ? InfectedWallCollisionLayer : 0;
    }

    private static string? ReadLineBounded(StreamReader reader,int limit)
    {
        var text = new StringBuilder();
        int ch;
        while ((ch=reader.Read()) != -1)
        {
            if (ch=='\n') return text.ToString().TrimEnd('\r');
            if (text.Length >= limit)
                throw new InvalidDataException("Infected Wall row exceeds safety limit");
            text.Append((char)ch);
        }
        return text.Length>0 ? text.ToString() : null;
    }

    private void Load(string packed,string mapName,(string Sha,int Count,int Approximated) expected)
    {
        var bytes = File.ReadAllBytes(packed);
        if (bytes.Length < 32 || bytes.Length > MaxCompressedBytes)
            throw new InvalidDataException("Infected Walls compressed pack outside safe byte budget");
        var sha=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        // Native CI is allowed an isolated synthetic map; normal gameplay
        // accepts only the EXACT original-owner derived pack hash per map.
        bool synthetic = OS.GetCmdlineUserArgs().Contains("--smoke-pass35",StringComparer.Ordinal)
            && mapName=="Manor";
        if (!synthetic && sha!=expected.Sha)
            throw new InvalidDataException("Original owner source wall SHA256 mismatch");

        using var mem=new MemoryStream(bytes);
        using var gz=new GZipStream(mem,CompressionMode.Decompress);
        using var reader=new StreamReader(gz,Encoding.UTF8,false,8192);
        var headline=ReadLineBounded(reader,8192)
            ?? throw new InvalidDataException("Original infected walls header missing");
        using var doc=JsonDocument.Parse(headline);
        var header=doc.RootElement;
        var count=header.GetProperty("wall_count").GetInt32();
        var approximated=header.GetProperty("approximated_meshparts").GetInt32();
        var sourceSha=header.GetProperty("source_rbxlx_sha256").GetString();
        if (header.GetProperty("format").GetString()!="twr-infected-walls35-v1" ||
            header.GetProperty("map").GetString()!=mapName ||
            header.GetProperty("collision_layer").GetInt32()!=InfectedWallCollisionLayer ||
            header.GetProperty("default_enabled").GetBoolean() ||
            count < 1 || count > MaximumWalls || approximated < 0 || approximated > count ||
            count != expected.Count && !synthetic ||
            approximated != expected.Approximated && !synthetic ||
            sourceSha != (synthetic ? new string('0',64) : OwnerSourceSha))
            throw new InvalidDataException("Original infected-wall format, source provenance, or counts differ");
        var approximatedSeen=0;
        for (var i=0; i<count; i++)
        {
            var line=ReadLineBounded(reader,4096)
                ?? throw new InvalidDataException("Truncated original infected-wall pack");
            using var record=JsonDocument.Parse(line);
            var row=record.RootElement;
            var shapeClass=row.GetProperty("class").GetString();
            if (shapeClass is not ("Part" or "WedgePart" or "MeshPart"))
                throw new InvalidDataException("Unsupported original infected-wall class");
            var proxy=shapeClass=="MeshPart";
            if (row.GetProperty("approximated_mesh").GetBoolean()!=proxy)
                throw new InvalidDataException("Missing explicit original mesh approximation label");
            if (proxy) approximatedSeen++;
            var location=ReadVector(row,"t",3);
            var matrix=ReadVector(row,"r",9);
            var sourceSize=ReadVector(row,"s",3);
            if (sourceSize.Any(x=>x<=.001f || x>10000))
                throw new InvalidDataException("Invalid original infected-wall size");
            var position=new Vector3(location[0],location[1],-location[2])*MetresPerStud;
            var rotation=new Basis(
                new Vector3(matrix[0],matrix[3],-matrix[6]),
                new Vector3(matrix[1],matrix[4],-matrix[7]),
                new Vector3(-matrix[2],-matrix[5],matrix[8]));
            if (Math.Abs(rotation.X.Length()-1)>.025 ||
                Math.Abs(rotation.Y.Length()-1)>.025 ||
                Math.Abs(rotation.Z.Length()-1)>.025 ||
                Math.Abs(rotation.X.Dot(rotation.Y))>.025)
                throw new InvalidDataException("Nonorthonormal original infected-wall CFrame");
            var size=new Vector3(sourceSize[0],sourceSize[1],sourceSize[2])*MetresPerStud;
            Shape3D shape = shapeClass=="WedgePart"
                ? RobloxPrimitiveGeometry.WedgeCollision(size)
                : new BoxShape3D{Size=size};
            var wall=new StaticBody3D
            {
                Name=$"SourceInfectedWall_{i+1}",
                Transform=new Transform3D(rotation,position),
                CollisionLayer=0,CollisionMask=0
            };
            wall.AddChild(new CollisionShape3D{Shape=shape});
            AddChild(wall);
            _bodies.Add(wall);
        }
        if (approximateCountMismatch(approximatedSeen,approximated))
            throw new InvalidDataException("Infected Wall mesh-proxy count does not match manifest");
        if (ReadLineBounded(reader,8)!=null)
            throw new InvalidDataException("Unexpected trailing infected-wall rows");
        ApproximateMeshProxyCount=approximatedSeen;
    }

    private static bool approximateCountMismatch(int actual,int expected) => actual!=expected;

    private static float[] ReadVector(JsonElement row,string key,int count)
    {
        var values=row.GetProperty(key);
        if (values.ValueKind!=JsonValueKind.Array || values.GetArrayLength()!=count)
            throw new InvalidDataException("Original infected-wall vector shape invalid: "+key);
        var result=new float[count];
        for (var i=0;i<count;i++)
        {
            var v=values[i].GetSingle();
            if (!float.IsFinite(v) || Math.Abs(v)>100000f)
                throw new InvalidDataException("Original infected-wall transform not finite");
            result[i]=v;
        }
        return result;
    }
}
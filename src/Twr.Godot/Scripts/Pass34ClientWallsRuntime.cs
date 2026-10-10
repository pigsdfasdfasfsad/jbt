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
/// Recovers original CMaps/Laboratory/Walls/Client Walls positions and sizes
/// from the owner's RBXLX. Roblox source places them in collision group 8,
/// whose collision matrix is not included. To avoid false physical behavior,
/// this independent player-only layer is OFF until explicitly toggled with F8.
/// Does not alter source Server Walls, infected routing or world geometry.
/// </summary>
public partial class Pass34ClientWallsRuntime : Node3D
{
    public const uint ClientWallCollisionLayer = 4;
    private const int MaximumWalls = 2000;
    private const long MaximumPackedBytes = 2 * 1024 * 1024;
    private const float Stud = RobloxUnits.MetersPerStud;
    private readonly List<StaticBody3D> _bodies = new();

    public int WallCount => _bodies.Count;
    public bool Enabled { get; private set; }

    public static Pass34ClientWallsRuntime? TryBuild(Node3D owner, string mapName)
    {
        // No complete owner-side reference scene has been validated yet for
        // other maps, although their original CMaps wall fragments are saved.
        if (mapName != "Laboratory") return null;
        var root = Path.GetDirectoryName(OS.GetExecutablePath()) ??
            Directory.GetCurrentDirectory();
        var packed = new[]
        {
            Path.Combine(root,"Content","Walls",mapName+".clientwalls34.jsonl.gz"),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Walls",mapName+".clientwalls34.jsonl.gz"),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","Walls",mapName+".clientwalls34.jsonl.gz")
        }.FirstOrDefault(File.Exists);
        var scene = new[]
        {
            Path.Combine(root,"Content","Maps",mapName+".scene.jsonl.gz"),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Maps",mapName+".scene.jsonl.gz"),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","Maps",mapName+".scene.jsonl.gz")
        }.FirstOrDefault(File.Exists);
        if (packed is null || scene is null)
        {
            GD.Print("TWR_PASS34_CLIENT_WALLS_ABSENT map=Laboratory fallback=legacy");
            return null;
        }
        var walls = new Pass34ClientWallsRuntime { Name="Pass34ClientWalls" };
        try
        {
            walls.ReadSourceWalls(packed,scene,mapName);
            owner.AddChild(walls);
            // Original collision group 8 is documented but its collision
            // matrix isn't: F8 must opt into the player-only interpretation.
            walls.SetEnabled(false);
            GD.Print($"TWR_PASS34_CLIENT_WALLS_READY map={mapName} " +
                $"source_walls={walls.WallCount} layer={ClientWallCollisionLayer} default=OFF");
            return walls;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS34_CLIENT_WALLS_REJECTED map=" + mapName +
                " fallback=legacy: " + error.Message);
            walls.Free();
            return null;
        }
    }

    public void SetEnabled(bool enabled)
    {
        Enabled=enabled;
        foreach (var body in _bodies)
            body.CollisionLayer = enabled ? ClientWallCollisionLayer : 0;
    }

    private static string? ReadLineBounded(StreamReader input,int maxCharacters)
    {
        var builder=new StringBuilder();
        int code;
        while ((code=input.Read())!=-1)
        {
            if (code=='\n') return builder.ToString().TrimEnd('\r');
            if (builder.Length >= maxCharacters)
                throw new InvalidDataException("Source client wall JSON line oversized");
            builder.Append((char)code);
        }
        return builder.Length>0 ? builder.ToString() : null;
    }

    private void ReadSourceWalls(string packed,string scene,string mapName)
    {
        if (new FileInfo(packed).Length is < 32 or > MaximumPackedBytes)
            throw new InvalidDataException("Client wall source pack outside safe byte limit");
        var sceneSha=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(scene)))
            .ToLowerInvariant();
        using var inputFile=File.OpenRead(packed);
        using var gz=new GZipStream(inputFile,CompressionMode.Decompress);
        using var reader=new StreamReader(gz,Encoding.UTF8,false,8192);
        var headerText=ReadLineBounded(reader,16000) ??
            throw new InvalidDataException("Missing client wall source header");
        using var headerDoc=JsonDocument.Parse(headerText);
        var h=headerDoc.RootElement;
        if (h.GetProperty("format").GetString()!="twr-client-walls34-v1" ||
            h.GetProperty("map").GetString()!=mapName ||
            h.GetProperty("scene_sha256").GetString()!=sceneSha ||
            h.GetProperty("collision_group_id").GetInt32()!=8 ||
            h.GetProperty("default_enabled").GetBoolean() ||
            h.GetProperty("wall_count").GetInt32() is < 1 or > MaximumWalls)
            throw new InvalidDataException("Client wall source SHA/format/mapping rejected");
        var count=h.GetProperty("wall_count").GetInt32();
        for (var i=0;i<count;i++)
        {
            var line=ReadLineBounded(reader,4096) ??
                throw new InvalidDataException("Truncated source client walls");
            using var record=JsonDocument.Parse(line);
            var e=record.RootElement;
            var cls=e.GetProperty("class").GetString();
            if (cls!="Part" && cls!="WedgePart")
                throw new InvalidDataException("Unsupported original client-wall shape");
            var translation=ReadValues(e,"t",3);
            var matrix=ReadValues(e,"r",9);
            var extent=ReadValues(e,"s",3);
            if (extent.Any(v=>v<=0.001f || v>10000f))
                throw new InvalidDataException("Invalid source wall extents");
            // CFrame reflection exactly matches LaboratorySourceLoader.
            var position=new Vector3(translation[0],translation[1],-translation[2])*Stud;
            var rotation=new Basis(
                new Vector3(matrix[0],matrix[3],-matrix[6]),
                new Vector3(matrix[1],matrix[4],-matrix[7]),
                new Vector3(-matrix[2],-matrix[5],matrix[8]));
            if (Math.Abs(rotation.X.Length()-1f)>.025f ||
                Math.Abs(rotation.Y.Length()-1f)>.025f ||
                Math.Abs(rotation.Z.Length()-1f)>.025f ||
                Math.Abs(rotation.X.Dot(rotation.Y))>.025f)
                throw new InvalidDataException("Original wall CFrame not orthonormal");
            var size=new Vector3(extent[0],extent[1],extent[2])*Stud;
            Shape3D shape=cls=="WedgePart"
                ? RobloxPrimitiveGeometry.WedgeCollision(size)
                : new BoxShape3D { Size=size };
            var body=new StaticBody3D
            {
                Name=$"SourceClientWall_{i+1}",
                Transform=new Transform3D(rotation,position),
                CollisionLayer=0,
                CollisionMask=0
            };
            body.AddChild(new CollisionShape3D { Shape=shape });
            AddChild(body);
            _bodies.Add(body);
        }
        if (ReadLineBounded(reader,8)!=null)
            throw new InvalidDataException("Unexpected client walls after declared count");
    }

    private static float[] ReadValues(JsonElement record,string key,int count)
    {
        var e=record.GetProperty(key);
        if (e.ValueKind!=JsonValueKind.Array || e.GetArrayLength()!=count)
            throw new InvalidDataException("Invalid source wall vector "+key);
        var result=new float[count];
        for (var i=0;i<count;i++)
        {
            var v=e[i].GetSingle();
            if (!float.IsFinite(v) || Math.Abs(v)>100000f)
                throw new InvalidDataException("Nonfinite or excessive source wall vector");
            result[i]=v;
        }
        return result;
    }
}
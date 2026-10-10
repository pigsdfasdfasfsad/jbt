using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Optional source-bound static collision cache. Packs multiple original
/// oriented box/wedge physics surfaces into one ConcavePolygonShape3D per
/// 64-stud XZ tile; MeshPart/UnionOperation shapes remain clearly marked
/// oriented source-bounds approximations, NOT recovered source collision.
/// The existing per-Part fallback is retained if this pack is unavailable,
/// invalid or mapped to a different exact original scene file.
/// </summary>
public static class Pass26CollisionRuntime
{
    private const float Stud = 0.28f;
    private const float TileStuds = 64f;
    private const uint Version = 1;
    private const uint MaxChunks = 2048;
    private const uint MaxShapes = 100_000;
    private const uint MaxTriangles = 350_000;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TWRCOL26");

    public static bool TryBuild(Node3D parent, string map, string originalScenePath)
    {
        if (map != "Laboratory" || !File.Exists(originalScenePath))
            return false;
        var cache = FindPack(map);
        if (cache is null)
        {
            GD.Print("TWR_PASS26_COLLISION_MISSING map=" + map + "; original collision fallback");
            return false;
        }
        var stage = new Node3D { Name = "Pass26Collision" };
        try
        {
            // SHA-256 of EXACT compressed source scene bytes. The cache is
            // never trusted based on a map name or header-reported source ID.
            byte[] sceneSha;
            using (var source = File.OpenRead(originalScenePath))
                sceneSha = SHA256.HashData(source);

            using var compressedFile = File.OpenRead(cache);
            using var gz = new GZipStream(compressedFile, CompressionMode.Decompress);
            using var reader = new BinaryReader(gz);
            var headerMagic = reader.ReadBytes(Magic.Length);
            if (!headerMagic.SequenceEqual(Magic) || reader.ReadUInt32() != Version)
                throw new InvalidDataException("Invalid Pass 26 collision header");
            var chunkCount = reader.ReadUInt32();
            var sourceShapeCount = reader.ReadUInt32();
            var proxyCount = reader.ReadUInt32();
            var declaredSha = reader.ReadBytes(32);
            if (chunkCount is < 1 or > MaxChunks ||
                sourceShapeCount is < 1 or > MaxShapes ||
                proxyCount > sourceShapeCount ||
                !declaredSha.SequenceEqual(sceneSha))
                throw new InvalidDataException("Collision counts/source SHA do not match");

            var seen = new HashSet<(int X,int Z)>();
            var totalFaces = 0L;
            for (var i = 0u; i < chunkCount; i++)
            {
                var tx = reader.ReadInt32();
                var tz = reader.ReadInt32();
                var faces = reader.ReadUInt32();
                totalFaces += faces;
                if (Math.Abs((long)tx) > 16000 || Math.Abs((long)tz) > 16000 ||
                    faces == 0 || totalFaces > MaxTriangles || !seen.Add((tx,tz)))
                    throw new InvalidDataException("Collision tile/triangle budget violation");

                var points = new Vector3[checked((int)faces * 3)];
                for (var v = 0; v < points.Length; v++)
                {
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    var z = reader.ReadSingle();
                    if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) ||
                        Math.Abs(x) >= 100000 || Math.Abs(y) >= 100000 || Math.Abs(z) >= 100000)
                        throw new InvalidDataException("Nonfinite or excessive collision coordinates");
                    points[v] = new Vector3(x,y,z);
                }

                var shape = new ConcavePolygonShape3D
                {
                    Data = points,
                    // Closed source static solids should obstruct both sides.
                    BackfaceCollision = true
                };
                var body = new StaticBody3D
                {
                    Name = $"SourceCollision_{tx}_{tz}",
                    CollisionLayer = 1,
                    // Positions are metre-scaled and reflected only once;
                    // triangle vertices are pre-baked in tile-local space.
                    Position = new Vector3(tx * TileStuds * Stud, 0,
                        -tz * TileStuds * Stud)
                };
                body.AddChild(new CollisionShape3D { Shape = shape });
                stage.AddChild(body);
            }
            if (reader.BaseStream.ReadByte() != -1)
                throw new InvalidDataException("Unexpected collision cache trailing bytes");
            parent.AddChild(stage);
            GD.Print($"TWR_PASS26_COLLISION_LOADED map={map} " +
                $"chunks={chunkCount} source_shapes={sourceShapeCount} " +
                $"approximate_bounds={proxyCount} triangles={totalFaces}");
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS26_COLLISION_FAILED map=" + map + ": " + error.Message);
            stage.Free();
            return false;
        }
    }

    private static string? FindPack(string map)
    {
        var exe = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var name = map + ".col26.gz";
        return new[]
        {
            Path.Combine(exe,"Content","Collision",name),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Collision",name),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                "Content","Collision",name)
        }.FirstOrDefault(File.Exists);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Source-derived Roblox SmoothGrid terrain meshes converted privately into
/// compact greedy-face chunks. No Roblox network or Studio requirement when
/// running the standalone Windows executable. This first terrain renderer is
/// block-faced rather than a clone of the original smooth-surface mesher.
/// </summary>
public static class RecoveredTerrainRuntime
{
    private const float VoxelMetres = 4f * RobloxUnits.MetersPerStud;
    private const int ChunkVoxels = 32;
    private const int MaxChunks = 3000;
    private const int MaxFacesPerChunk = 120000;
    private const int MaxTotalFaces = 500000;
    private static readonly byte[] HeaderMagic = Encoding.ASCII.GetBytes("TWRTERR1");

    public static bool TryBuild(Node3D root, string mapName)
    {
        if (mapName.Length > 32 || !mapName.All(char.IsLetter))
            return false;
        var fileName = mapName + ".terrainmesh.gz";
        var exeDirectory = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var path = new[]
        {
            Path.Combine(exeDirectory, "Content", "Terrain", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Terrain", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot",
                "Content", "Terrain", fileName)
        }.FirstOrDefault(File.Exists);
        if (path is null) return false;

        var stage = new Node3D { Name = "RecoveredTerrain" };
        var chunkCount = 0;
        var faceCount = 0;
        var waterCount = 0;
        try
        {
            using var file = File.OpenRead(path);
            using var stream = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new BinaryReader(stream);
            var magic = reader.ReadBytes(HeaderMagic.Length);
            if (!magic.SequenceEqual(HeaderMagic))
                throw new InvalidDataException("Unsupported source terrain mesh header");
            var totalChunks = reader.ReadUInt32();
            if (totalChunks > MaxChunks)
                throw new InvalidDataException("Terrain chunk budget exceeded");
            var palette = reader.ReadBytes(69);
            if (palette.Length != 69)
                throw new InvalidDataException("Terrain material palette truncated");

            for (var chunk = 0; chunk < totalChunks; chunk++)
            {
                var cx = reader.ReadInt32();
                var cy = reader.ReadInt32();
                var cz = reader.ReadInt32();
                if (Math.Abs((long)cx) > 100000 || Math.Abs((long)cy) > 100000 ||
                    Math.Abs((long)cz) > 100000)
                    throw new InvalidDataException("Terrain coordinate exceeds safe bounds");
                var faces = reader.ReadUInt32();
                if (faces > MaxFacesPerChunk || faceCount + (long)faces > MaxTotalFaces)
                    throw new InvalidDataException("Terrain face budget exceeded");

                var chunkNode = new Node3D
                {
                    Name = $"VoxelChunk_{cx}_{cy}_{cz}",
                    Position = new Vector3(cx,cy,-cz) * (ChunkVoxels * VoxelMetres)
                };
                var solid = new SurfaceTool();
                var water = new SurfaceTool();
                solid.Begin(global::Godot.Mesh.PrimitiveType.Triangles);
                water.Begin(global::Godot.Mesh.PrimitiveType.Triangles);
                var collider = new List<Vector3>();
                var solidFaces = 0;
                var waterFaces = 0;
                for (var i = 0; i < faces; i++)
                {
                    var dir = reader.ReadByte();
                    var x = reader.ReadByte();
                    var y = reader.ReadByte();
                    var z = reader.ReadByte();
                    var w = reader.ReadByte();
                    var h = reader.ReadByte();
                    var material = reader.ReadByte();
                    if (dir > 5 || x >= 32 || y >= 32 || z >= 32 ||
                        w is < 1 or > 32 || h is < 1 or > 32 ||
                        material is < 1 or > 22)
                        throw new InvalidDataException("Invalid decoded SmoothGrid terrain face");
                    var tool = material == 1 ? water : solid;
                    var color = material == 1
                        ? new Color(.17f,.36f,.48f,.63f)
                        : new Color(palette[material * 3] / 255f,
                            palette[material * 3 + 1] / 255f,
                            palette[material * 3 + 2] / 255f);
                    TerrainFace(dir,x,y,z,w,h,
                        out var a,out var b,out var c,out var d,out var normal);
                    DrawTriangle(tool,a,b,c,normal,color);
                    DrawTriangle(tool,a,c,d,normal,color);
                    if (material != 1)
                    {
                        collider.Add(a);collider.Add(b);collider.Add(c);
                        collider.Add(a);collider.Add(c);collider.Add(d);
                        solidFaces++;
                    }
                    else waterFaces++;
                }
                if (solidFaces > 0)
                {
                    chunkNode.AddChild(new MeshInstance3D
                    {
                        Name = "SolidTerrain",
                        Mesh = solid.Commit(),
                        MaterialOverride = TerrainMaterial(false)
                    });
                    var shape = new ConcavePolygonShape3D
                    {
                        Data = new PackedVector3Array(collider.ToArray())
                    };
                    var body = new StaticBody3D
                    {
                        Name = "TerrainCollision",
                        CollisionLayer = 1
                    };
                    body.AddChild(new CollisionShape3D { Shape = shape });
                    chunkNode.AddChild(body);
                }
                if (waterFaces > 0)
                {
                    chunkNode.AddChild(new MeshInstance3D
                    {
                        Name = "SourceWater",
                        Mesh = water.Commit(),
                        MaterialOverride = TerrainMaterial(true),
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                    });
                }
                stage.AddChild(chunkNode);
                chunkCount++;
                faceCount += (int)faces;
                waterCount += waterFaces;
            }
            // Enforce a clean end-of-stream; corrupted or appended junk must
            // not silently be accepted as a valid original-source mesh.
            if (reader.BaseStream.ReadByte() != -1)
                throw new InvalidDataException("Extra terrain mesh trailing bytes");

            root.AddChild(stage);
            GD.Print($"TWR_TERRAIN_LOADED map={mapName} chunks={chunkCount} " +
                $"faces={faceCount} water_faces={waterCount}");
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_TERRAIN_FAILED map=" + mapName + ": " + error.Message);
            stage.Free();
            return false;
        }
    }

    private static StandardMaterial3D TerrainMaterial(bool water) => new()
    {
        VertexColorUseAsAlbedo = true,
        AlbedoColor = Colors.White,
        Roughness = water ? .11f : .93f,
        Metallic = water ? .28f : 0f,
        Transparency = water ? BaseMaterial3D.TransparencyEnum.Alpha :
            BaseMaterial3D.TransparencyEnum.Disabled,
        CullMode = BaseMaterial3D.CullModeEnum.Back
    };

    private static void DrawTriangle(
        SurfaceTool tool,Vector3 a,Vector3 b,Vector3 c,Vector3 normal,Color color)
    {
        tool.SetNormal(normal);tool.SetColor(color);tool.AddVertex(a);
        tool.SetNormal(normal);tool.SetColor(color);tool.AddVertex(b);
        tool.SetNormal(normal);tool.SetColor(color);tool.AddVertex(c);
    }

    private static void TerrainFace(
        byte dir,byte x,byte y,byte z,byte w,byte h,
        out Vector3 a,out Vector3 b,out Vector3 c,out Vector3 d,
        out Vector3 normal)
    {
        // Source Roblox voxel coordinates (X,Y,Z) are right-handed with
        // Godot Z reversed. A fixed 4-stud voxel size is 1.12 metres.
        var x0=(float)x;var y0=(float)y;var z0=(float)z;
        var u=(float)w;var v=(float)h;
        normal = dir switch
        {
            0 => Vector3.Right,
            1 => Vector3.Left,
            2 => Vector3.Up,
            3 => Vector3.Down,
            4 => Vector3.Forward,
            _ => Vector3.Back
        };
        if (dir < 2)
        {
            var plane=x0+(dir==0?1:0);
            a = new Vector3(plane,y0,-z0);
            b = new Vector3(plane,y0+u,-z0);
            c = new Vector3(plane,y0+u,-(z0+v));
            d = new Vector3(plane,y0,-(z0+v));
        }
        else if (dir < 4)
        {
            var plane=y0+(dir==2?1:0);
            a = new Vector3(x0,plane,-z0);
            b = new Vector3(x0+u,plane,-z0);
            c = new Vector3(x0+u,plane,-(z0+v));
            d = new Vector3(x0,plane,-(z0+v));
        }
        else
        {
            var plane=z0+(dir==4?1:0);
            a = new Vector3(x0,y0,-plane);
            b = new Vector3(x0+u,y0,-plane);
            c = new Vector3(x0+u,y0+v,-plane);
            d = new Vector3(x0,y0+v,-plane);
        }
        if ((b-a).Cross(c-a).Dot(normal) < 0f)
            (b,d) = (d,b);
        a*=VoxelMetres;b*=VoxelMetres;c*=VoxelMetres;d*=VoxelMetres;
    }
}

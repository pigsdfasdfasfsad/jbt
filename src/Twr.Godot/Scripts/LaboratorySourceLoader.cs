using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Loads source-positioned Laboratory geometry from the owner's private offline
/// evidence pack. Roblox asset IDs are never fetched from the network; missing
/// mesh/CSG binaries use source-dimension proxies, not fabricated source art.
/// </summary>
public static class LaboratorySourceLoader
{
    private const string PackName = "Laboratory.scene.jsonl.gz";
    private const float Stud = 0.28f;

    private sealed class RenderBatch
    {
        public Mesh Mesh = null!;
        public bool CastShadow;
        public readonly List<Transform3D> Instances = [];
    }

    public static bool TryBuild(Node3D root, out RuntimeMapLayout layout)
    {
        layout = null!;
        var filePath = CandidatePaths().FirstOrDefault(File.Exists);
        if (filePath is null)
        {
            GD.Print("TWR_LAB_SOURCE_PACK_MISSING: using blockout");
            return false;
        }

        var stage = new Node3D { Name = "RecoveredLaboratory" };
        var batches = new Dictionary<string, RenderBatch>(StringComparer.Ordinal);
        var collisions = new List<(Transform3D Transform, Vector3 Size)>();
        var emitters = new List<LaboratorySourceLight>();
        var infected = new List<Vector3>();
        var players = new List<(string Name, Vector3 Position)>();
        var count = new Dictionary<string, int>(StringComparer.Ordinal);

        try
        {
            using var input = File.OpenRead(filePath);
            using var unpack = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(unpack);
            var headerText = reader.ReadLine()
                ?? throw new InvalidDataException("empty Laboratory scene");
            using (var doc = JsonDocument.Parse(headerText))
            {
                var header = doc.RootElement;
                if (Str(header, "format") != "twr-laboratory-scene-v1" ||
                    Str(header, "map") != "Laboratory" ||
                    Math.Abs(Num(header, "scale") - Stud) > 0.00001f)
                    throw new InvalidDataException("wrong Laboratory scene format");
            }
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                using var doc = JsonDocument.Parse(line);
                var record = doc.RootElement;
                var kind = Str(record, "kind");
                count[kind] = count.GetValueOrDefault(kind) + 1;
                switch (kind)
                {
                    case "geometry":
                        AddGeometry(record, batches, collisions);
                        break;
                    case "collision":
                    {
                        var size = Extents(record);
                        collisions.Add((ToTransform(record, size, false), size));
                        break;
                    }
                    case "light":
                        emitters.Add(ToEmitter(record));
                        break;
                    case "spawn":
                    {
                        var position = Position(record);
                        if (Str(record, "side") == "infected") infected.Add(position);
                        else if (Str(record, "side") == "player")
                            players.Add((Str(record, "name"), position));
                        break;
                    }
                    default:
                        throw new InvalidDataException("unknown source record: " + kind);
                }
            }
            if (count.GetValueOrDefault("geometry") < 30000 ||
                count.GetValueOrDefault("collision") < 1000 ||
                count.GetValueOrDefault("light") < 800 ||
                infected.Count != 15 || players.Count != 8)
                throw new InvalidDataException("incomplete Laboratory source pack");

            foreach (var batch in batches.Values)
            {
                var mm = new MultiMesh
                {
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                    Mesh = batch.Mesh,
                    InstanceCount = batch.Instances.Count
                };
                for (var index = 0; index < batch.Instances.Count; index++)
                    mm.SetInstanceTransform(index, batch.Instances[index]);
                stage.AddChild(new MultiMeshInstance3D
                {
                    Multimesh = mm,
                    CastShadow = batch.CastShadow
                        ? GeometryInstance3D.ShadowCastingSetting.On
                        : GeometryInstance3D.ShadowCastingSetting.Off
                });
            }

            // One static body; original per-instance wall shapes remain in their
            // exact source transforms. Floor collision is additionally restored.
            var worldBody = new StaticBody3D
            {
                Name = "LaboratoryCollision",
                CollisionLayer = 1
            };
            foreach (var (t, size) in collisions)
                worldBody.AddChild(new CollisionShape3D
                {
                    Shape = new BoxShape3D { Size = size },
                    Transform = t
                });
            stage.AddChild(worldBody);
            AddNightEnvironment(stage);
            // SpotLight3D.LookAt requires its node to be inside the scene tree.
            // Mount the map before initializing source-facing spotlights.
            root.AddChild(stage);

            var playerSpawn = players
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .First().Position + Vector3.Up * 0.5f;
            var lightStreamer = new LaboratoryLightStreamer { Name = "LaboratoryLights" };
            lightStreamer.Configure(emitters, playerSpawn);
            stage.AddChild(lightStreamer);
            layout = new RuntimeMapLayout(
                playerSpawn,
                infected,
                new[]
                {
                    playerSpawn + new Vector3(3, 0, 3),
                    playerSpawn + new Vector3(-3, 0, 5)
                },
                new[]
                {
                    playerSpawn + new Vector3(7, 0, -5),
                    playerSpawn + new Vector3(-7, 0, -5)
                })
            {
                UseExactInfectedSpawns = true
            };
            GD.Print($"TWR_LAB_SOURCE_LOADED geometry={count["geometry"]} " +
                $"server_walls={count["collision"]} physical_shapes={collisions.Count} " +
                $"source_lights={emitters.Count} active_lights={Math.Min(LaboratoryLightStreamer.ActiveLimit, emitters.Count)} " +
                $"infected_spawns={infected.Count} player_spawns={players.Count}");
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_LAB_SOURCE_FAILED: " + error.Message);
            stage.Free();
            return false;
        }
    }

    private static IEnumerable<string> CandidatePaths()
    {
        var exeDirectory = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(exeDirectory, "Content", "Maps", PackName);
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Content", "Maps", PackName);
        yield return Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot",
            "Content", "Maps", PackName);
    }

    private static void AddGeometry(
        JsonElement r, Dictionary<string, RenderBatch> batches,
        List<(Transform3D Transform, Vector3 Size)> colliders)
    {
        var opacity = Num(r, "opacity", 1);
        if (opacity < 0.001f) return;
        var size = Extents(r);
        var cls = Str(r, "class");
        var rgb = Vec(r, "rgb");
        var color = new Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f, opacity);
        var id = Str(r, "meshId", Str(r, "specialMeshId"));
        var shadow = Flag(r, "shadow");
        var key = $"{cls}|{Str(r, "shape")}|{Str(r, "mat")}|{id}|" +
                  $"{rgb[0]},{rgb[1]},{rgb[2]}|{opacity:F3}|{shadow}";
        if (!batches.TryGetValue(key, out var batch))
        {
            var mesh = LoadPreparedMesh(id) ?? ProxyMesh(cls, Str(r, "shape"));
            mesh = (Mesh)mesh.Duplicate();
            if (mesh is PrimitiveMesh primitive)
                primitive.Material = new StandardMaterial3D
                {
                    AlbedoColor = color,
                    Roughness = 0.88f,
                    Transparency = opacity < 0.995f
                        ? BaseMaterial3D.TransparencyEnum.Alpha
                        : BaseMaterial3D.TransparencyEnum.Disabled
                };
            batch = new RenderBatch { Mesh = mesh, CastShadow = shadow };
            batches.Add(key, batch);
        }
        batch.Instances.Add(ToTransform(r, size, true));

        var name = Str(r, "name");
        if (Flag(r, "collidable") && cls == "Part" &&
            (name.Contains("Floor", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Stair", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Ramp", StringComparison.OrdinalIgnoreCase)))
            colliders.Add((ToTransform(r, size, false), size));
    }

    private static Mesh? LoadPreparedMesh(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.All(char.IsDigit)) return null;
        var path = $"res://Content/Assets/Meshes/{id}.res";
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Mesh>(path) : null;
    }

    private static Mesh ProxyMesh(string cls, string shape)
    {
        if (cls == "Part" && shape == "0")
            return new SphereMesh { Radius = 0.5f, Height = 1f };
        if (cls == "Part" && shape == "2")
            return new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f };
        return new BoxMesh { Size = Vector3.One };
    }

    private static LaboratorySourceLight ToEmitter(JsonElement r)
    {
        var rgb = Vec(r, "rgb");
        return new LaboratorySourceLight(Str(r, "class"), Position(r), Rotation(r),
            new Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f),
            Num(r, "energy", 1), Num(r, "range", 16) * Stud,
            Num(r, "angle", 90), (int)Num(r, "face", 5));
    }

    private static void AddNightEnvironment(Node3D root)
    {
        // Ambient atmosphere is an APPROXIMATED TestPlace reconstruction.
        // The source light positions/parameters are preserved in the private pack.
        root.AddChild(new WorldEnvironment
        {
            Environment = new global::Godot.Environment
            {
                BackgroundMode = global::Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.039f, 0.043f, 0.071f),
                AmbientLightSource = global::Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.12f, 0.12f, 0.16f),
                AmbientLightEnergy = 0.6f
            }
        });
    }

    private static Transform3D ToTransform(JsonElement r, Vector3 size, bool scaled)
    {
        var rotation = Rotation(r);
        if (scaled)
            rotation = new Basis(
                rotation.X * size.X, rotation.Y * size.Y, rotation.Z * size.Z);
        return new Transform3D(rotation, Position(r));
    }

    private static Vector3 Extents(JsonElement r)
    {
        var p = Vec(r, "s");
        return new Vector3(
            Math.Max(0.001f, p[0] * Stud),
            Math.Max(0.001f, p[1] * Stud),
            Math.Max(0.001f, p[2] * Stud));
    }

    private static Vector3 Position(JsonElement r)
    {
        var p = Vec(r, "t");
        return new Vector3(p[0], p[1], -p[2]) * Stud;
    }

    private static Basis Rotation(JsonElement r)
    {
        var p = Vec(r, "r");
        // Convert reflected Roblox CFrame matrix to Godot's column basis.
        return new Basis(
            new Vector3(p[0], p[3], -p[6]),
            new Vector3(p[1], p[4], -p[7]),
            new Vector3(-p[2], -p[5], p[8]));
    }

    private static float[] Vec(JsonElement r, string key) =>
        r.GetProperty(key).EnumerateArray().Select(v => v.GetSingle()).ToArray();

    private static string Str(JsonElement r, string key, string fallback = "") =>
        r.TryGetProperty(key, out var x) && x.ValueKind == JsonValueKind.String
            ? x.GetString() ?? fallback : fallback;

    private static float Num(JsonElement r, string key, float fallback = 0f) =>
        r.TryGetProperty(key, out var x) && x.ValueKind == JsonValueKind.Number
            ? x.GetSingle() : fallback;

    private static bool Flag(JsonElement r, string key) =>
        r.TryGetProperty(key, out var x) && x.ValueKind == JsonValueKind.True;
}

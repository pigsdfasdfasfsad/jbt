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
    private const float Stud = RobloxUnits.MetersPerStud;

    private sealed class RenderBatch
    {
        public Mesh Mesh = null!;
        public StandardMaterial3D Material = null!;
        public bool CastShadow;
        public readonly List<Transform3D> Instances = [];
    }

    public static bool TryBuild(Node3D root, out RuntimeMapLayout layout)
        => TryBuild(root, "Laboratory", out layout);

    /// <summary>
    /// Load any original in-game map snapshot packaged privately in the
    /// twr-source-map-v2 format. The older Laboratory-only format remains
    /// accepted by its legacy smoke test and existing owner-side packs.
    /// </summary>
    public static bool TryBuild(Node3D root, string mapName, out RuntimeMapLayout layout)
    {
        layout = null!;
        if (!MapCatalogRuntime.All().Any(map => map.Name == mapName))
            throw new ArgumentException("Unsupported original map: " + mapName, nameof(mapName));
        var filePath = CandidatePaths(mapName).FirstOrDefault(File.Exists);
        if (filePath is null)
        {
            GD.Print("TWR_SOURCE_MAP_MISSING map=" + mapName + ": using blockout");
            return false;
        }

        var stage = new Node3D { Name = "Recovered" + mapName };
        var batches = new Dictionary<string, RenderBatch>(StringComparer.Ordinal);
        var meshCache = new Dictionary<string, Mesh?>(StringComparer.Ordinal);
        var textureCache = new Dictionary<string, Texture2D?>(StringComparer.Ordinal);
        var collisions = new List<(Transform3D Transform, Vector3 Size, bool Wedge)>();
        var emitters = new List<LaboratorySourceLight>();
        var infected = new List<Vector3>();
        var players = new List<(string Name, Vector3 Position)>();
        var itemMarkers = new List<Vector3>();
        var fortificationMarkers = new List<Vector3>();
        var count = new Dictionary<string, int>(StringComparer.Ordinal);

        try
        {
            using var input = File.OpenRead(filePath);
            using var unpack = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(unpack);
            var headerText = reader.ReadLine()
                ?? throw new InvalidDataException("empty Laboratory scene");
            var isLegacyLab = false;
            JsonElement? originalLighting = null;
            var expectedCounts = new Dictionary<string, int>();
            using (var doc = JsonDocument.Parse(headerText))
            {
                var header = doc.RootElement;
                isLegacyLab = mapName == "Laboratory" &&
                    Str(header, "format") == "twr-laboratory-scene-v1";
                var isFullSource = Str(header, "format") == "twr-source-map-v2";
                if ((!isLegacyLab && !isFullSource) ||
                    Str(header, "map") != mapName ||
                    Math.Abs(Num(header, "scale") - Stud) > 0.00001f)
                    throw new InvalidDataException("wrong source map scene format: " + mapName);
                if (isFullSource)
                {
                    if (header.TryGetProperty("lighting", out var sourceLighting))
                        originalLighting = sourceLighting.Clone();
                    var srcCounts = header.GetProperty("counts");
                    foreach (var pair in new[] { "geometry", "collision", "lights",
                        "infected_spawns", "player_spawns",
                        "item_markers", "fortification_markers" })
                        expectedCounts[pair] = srcCounts.GetProperty(pair).GetInt32();
                }
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
                        AddGeometry(record, batches, collisions, meshCache, textureCache);
                        break;
                    case "collision":
                    {
                        var size = Extents(record);
                        collisions.Add((ToTransform(record, size, false), size, false));
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
                    case "pickup_spawn":
                    {
                        var group = Str(record, "group");
                        if (group == "Item") itemMarkers.Add(Position(record));
                        else if (group == "Fortification")
                            fortificationMarkers.Add(Position(record));
                        else throw new InvalidDataException("unknown pickup group: " + group);
                        break;
                    }
                    default:
                        throw new InvalidDataException("unknown source record: " + kind);
                }
            }
            if (isLegacyLab)
            {
                if (count.GetValueOrDefault("geometry") < 30000 ||
                    count.GetValueOrDefault("collision") < 1000 ||
                    count.GetValueOrDefault("light") < 800 ||
                    infected.Count != 15 || players.Count != 8)
                    throw new InvalidDataException("incomplete Laboratory source pack");
                if (count.GetValueOrDefault("pickup_spawn") > 0 &&
                    (itemMarkers.Count != 127 || fortificationMarkers.Count != 47))
                    throw new InvalidDataException("incomplete Laboratory pickup marker set");
            }
            else
            {
                if (expectedCounts["geometry"] < 10 ||
                    expectedCounts["infected_spawns"] < 1 ||
                    expectedCounts["player_spawns"] < 1 ||
                    count.GetValueOrDefault("geometry") != expectedCounts["geometry"] ||
                    count.GetValueOrDefault("collision") != expectedCounts["collision"] ||
                    count.GetValueOrDefault("light") != expectedCounts["lights"] ||
                    infected.Count != expectedCounts["infected_spawns"] ||
                    players.Count != expectedCounts["player_spawns"] ||
                    itemMarkers.Count != expectedCounts["item_markers"] ||
                    fortificationMarkers.Count != expectedCounts["fortification_markers"])
                    throw new InvalidDataException("incomplete or corrupted original scene pack: " + mapName);
            }

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
                    MaterialOverride = batch.Material,
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
            var shapeCache = new Dictionary<(Vector3, bool), Shape3D>();
            foreach (var (t, size, wedge) in collisions)
            {
                if (!shapeCache.TryGetValue((size, wedge), out var shape))
                {
                    shape = wedge
                        ? RobloxPrimitiveGeometry.WedgeCollision(size)
                        : new BoxShape3D { Size = size };
                    shapeCache[(size, wedge)] = shape;
                }
                worldBody.AddChild(new CollisionShape3D
                {
                    Shape = shape,
                    Transform = t
                });
            }
            stage.AddChild(worldBody);
            AddNightEnvironment(stage, originalLighting);
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
                itemMarkers.Count > 0 ? itemMarkers : new[]
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
                UseExactInfectedSpawns = true,
                FortificationPoints = fortificationMarkers
            };
            GD.Print($"TWR_LAB_SOURCE_LOADED map={mapName} geometry={count["geometry"]} " +
                $"server_walls={count["collision"]} physical_shapes={collisions.Count} " +
                $"source_lights={emitters.Count} active_lights={Math.Min(LaboratoryLightStreamer.ActiveLimit, emitters.Count)} " +
                $"infected_spawns={infected.Count} player_spawns={players.Count} " +
                $"item_markers={itemMarkers.Count} fortification_markers={fortificationMarkers.Count} " +
                $"mesh_ids_loaded={meshCache.Count(item => item.Value is not null)} " +
                $"mesh_ids_missing={meshCache.Count(item => item.Value is null)} " +
                $"texture_ids_loaded={textureCache.Count(item => item.Value is not null)} " +
                $"texture_ids_missing={textureCache.Count(item => item.Value is null)}");
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_SOURCE_MAP_FAILED map=" + mapName + ": " + error.Message);
            stage.Free();
            return false;
        }
    }

    private static IEnumerable<string> CandidatePaths(string mapName)
    {
        var packName = mapName + ".scene.jsonl.gz";
        var exeDirectory = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(exeDirectory, "Content", "Maps", packName);
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Content", "Maps", packName);
        yield return Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot",
            "Content", "Maps", packName);
    }

    private static void AddGeometry(
        JsonElement r, Dictionary<string, RenderBatch> batches,
        List<(Transform3D Transform, Vector3 Size, bool Wedge)> colliders,
        Dictionary<string, Mesh?> meshCache,
        Dictionary<string, Texture2D?> textureCache)
    {
        var opacity = Num(r, "opacity", 1);
        var size = Extents(r);
        var cls = Str(r, "class");
        // A source-visible mesh may be fully transparent while retaining its
        // collision. Physics must not be gated on opacity or drawing.
        if (Flag(r, "collidable"))
            colliders.Add((ToTransform(r, size, false), size,
                cls == "WedgePart"));
        if (opacity < 0.001f) return;
        var rgb = Vec(r, "rgb");
        var color = new Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f, opacity);
        var id = Str(r, "meshId", Str(r, "specialMeshId"));
        Mesh? prepared = null;
        if (!string.IsNullOrEmpty(id) && !meshCache.TryGetValue(id, out prepared))
        {
            prepared = LoadPreparedMesh(id);
            meshCache[id] = prepared;
        }
        var textureId = Str(r, "textureId");
        Texture2D? preparedTexture = null;
        if (!string.IsNullOrEmpty(textureId) &&
            !textureCache.TryGetValue(textureId, out preparedTexture))
        {
            preparedTexture = LoadPreparedTexture(textureId);
            textureCache[textureId] = preparedTexture;
        }
        var shadow = Flag(r, "shadow");
        // Missing mesh binaries all use the same geometry proxy; retaining
        // their unrelated asset IDs in batch keys creates excess draw calls.
        var batchMeshKey = prepared is null ? "" : id;
        var batchTextureKey = preparedTexture is null ? "" : textureId;
        var key = $"{cls}|{Str(r, "shape")}|{Str(r, "mat")}|{batchMeshKey}|{batchTextureKey}|" +
                  $"{rgb[0]},{rgb[1]},{rgb[2]}|{opacity:F3}|{shadow}";
        if (!batches.TryGetValue(key, out var batch))
        {
            var mesh = prepared ?? ProxyMesh(cls, Str(r, "shape"));
            var materialCode = Str(r, "mat");
            var metallic = materialCode is "1088" or "1056" or "1040";
            var neon = materialCode == "288";
            var glass = materialCode == "1568";
            // MaterialOverride also works on imported ArrayMesh resources,
            // unlike setting PrimitiveMesh.Material only for fallback boxes.
            var material = new StandardMaterial3D
            {
                AlbedoColor = color,
                AlbedoTexture = preparedTexture,
                Metallic = metallic ? 0.75f : 0.02f,
                Roughness = metallic ? 0.42f : glass ? 0.11f : 0.88f,
                EmissionEnabled = neon,
                Emission = color,
                Transparency = opacity < 0.995f || preparedTexture is not null
                    ? BaseMaterial3D.TransparencyEnum.Alpha
                    : BaseMaterial3D.TransparencyEnum.Disabled
            };
            RobloxMaterialSurface.Apply(material, materialCode, preparedTexture);
            batch = new RenderBatch
            {
                Mesh = mesh,
                Material = material,
                CastShadow = shadow
            };
            batches.Add(key, batch);
        }
        var visualSize = VisualExtents(r, size);
        batch.Instances.Add(ToTransform(r, visualSize, true));
    }

    private static Mesh? LoadPreparedMesh(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.All(char.IsDigit)) return null;
        var path = $"res://Content/Assets/Meshes/{id}.res";
        return (ResourceLoader.Exists(path) ? ResourceLoader.Load<Mesh>(path) : null)
            ?? OfflineAssetResolver.Mesh(id);
    }

    private static Texture2D? LoadPreparedTexture(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.All(char.IsDigit)) return null;
        var path = $"res://Content/Assets/Textures/{id}.png";
        return (ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null)
            ?? OfflineAssetResolver.Texture(id);
    }

    private static Mesh ProxyMesh(string cls, string shape)
    {
        if (cls == "WedgePart")
            return RobloxPrimitiveGeometry.WedgeMesh();
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

    private static void AddNightEnvironment(Node3D root, JsonElement? lighting)
    {
        // Original Roblox place Lighting service: ambient/outdoor RGB,
        // brightness and fog color/end. Godot atmospheric scattering and
        // skybox materials differ; those properties are still approximated.
        var ambient = new Color(.12f,.12f,.16f);
        var outdoors = new Color(.10f,.11f,.14f);
        var fog = new Color(.22f,.24f,.27f);
        var energy = .60f;
        var density = .006f;
        if (lighting.HasValue && lighting.Value.ValueKind == JsonValueKind.Object)
        {
            var settings = lighting.Value;
            ambient = SourceColor(settings, "Ambient", ambient);
            outdoors = SourceColor(settings, "OutdoorAmbient", outdoors);
            fog = SourceColor(settings, "FogColor", fog);
            var brightness = Num(settings,"Brightness",.60f);
            energy = Math.Clamp(.30f + brightness * .35f,.28f,1.50f);
            var fogEnd = Num(settings,"FogEnd",600f);
            // Convert source studs into metres before approximating density.
            density = Math.Clamp(.70f / (fogEnd * Stud),.0005f,.035f);
        }
        root.AddChild(new WorldEnvironment
        {
            Environment = new global::Godot.Environment
            {
                BackgroundMode = global::Godot.Environment.BGMode.Color,
                BackgroundColor = outdoors,
                AmbientLightSource = global::Godot.Environment.AmbientSource.Color,
                AmbientLightColor = ambient,
                AmbientLightEnergy = energy,
                FogEnabled = true,
                FogDensity = density,
                FogLightColor = fog
            }
        });
    }

    private static Color SourceColor(JsonElement values, string key, Color fallback)
    {
        if (!values.TryGetProperty(key, out var rgb) ||
            rgb.ValueKind != JsonValueKind.Array)
            return fallback;
        var channels = rgb.EnumerateArray().Select(v => v.GetSingle()).ToArray();
        return channels.Length == 3
            ? new Color(Math.Clamp(channels[0],0f,1f),
                Math.Clamp(channels[1],0f,1f),
                Math.Clamp(channels[2],0f,1f))
            : fallback;
    }

    private static Transform3D ToTransform(JsonElement r, Vector3 size, bool scaled)
    {
        var rotation = Rotation(r);
        if (scaled)
            rotation = new Basis(
                rotation.X * size.X, rotation.Y * size.Y, rotation.Z * size.Z);
        return new Transform3D(rotation, Position(r));
    }

    private static Vector3 VisualExtents(JsonElement r, Vector3 physicalPartSize)
    {
        // Roblox SpecialMesh.Scale is a mesh-local size/scale distinct from
        // the parent Part.Size. In the recovered Laboratory source the large
        // MountainsFar SpecialMeshes sit inside 0.2-stud carrier Parts yet
        // specify hundreds of studs of visible mesh dimensions. Physics uses
        // the parent Part.Size, visual geometry uses SpecialMesh.Scale.
        if (!r.TryGetProperty("specialMeshScale", out var scale) ||
            scale.ValueKind != JsonValueKind.Array)
            return physicalPartSize;
        var values = Vec(r, "specialMeshScale");
        if (values.Length != 3) return physicalPartSize;
        return new Vector3(
            Math.Max(0.001f, values[0] * Stud),
            Math.Max(0.001f, values[1] * Stud),
            Math.Max(0.001f, values[2] * Stud));
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

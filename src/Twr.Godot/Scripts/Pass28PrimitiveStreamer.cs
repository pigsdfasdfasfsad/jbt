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
/// Pass 28: original-positioned *native Roblox Part and WedgePart* visuals,
/// grouped by material and 96-stud tiles. This does not claim to reconstruct
/// arbitrary MeshPart, UnionOperation or FileMesh vertex buffers. Fail closed
/// on corrupt/stale data: the legacy source renderer stays available.
/// </summary>
public partial class Pass28PrimitiveStreamer : Node3D
{
    private const string Format = "TWRINS28";
    private const uint Version = 1;
    private const uint TileStuds = 96;
    private const uint MaxTiles = 512;
    private const uint MaxBatches = 8000;
    private const uint MaxInstances = 100000;
    private const uint MaxPerBatch = 20000;
    private const long MaxFileBytes = 32L * 1024 * 1024;
    private const float Stud = RobloxUnits.MetersPerStud;
    private const float DrawRadius = 145f;
    private const float HideRadius = 166f; // Hysteresis against tile popping.
    private readonly List<(MultiMeshInstance3D Instance, Vector2 Center)> _drawGroups = [];
    private readonly Dictionary<(byte Kind, ushort Material, byte Alpha,
        byte R, byte G, byte B, bool Shadow), (Mesh Mesh, StandardMaterial3D Material)> _resources = [];
    private Node3D? _trackedPlayer;
    private Vector2 _lastPlayerPosition = new(float.NaN, float.NaN);
    private double _refresh;

    public int SourceInstanceCount { get; private set; }
    public int BatchCount => _drawGroups.Count;

    public static bool TryBuild(Node3D parent, string mapName, string scenePath)
    {
        if (mapName != "Laboratory" || !File.Exists(scenePath)) return false;
        var pack = FindPack(mapName);
        if (pack is null) return false;

        var streamer = new Pass28PrimitiveStreamer { Name = "Pass28PrimitiveStream" };
        try
        {
            var expectedSource = SHA256.HashData(File.ReadAllBytes(scenePath));
            streamer.LoadPack(pack, expectedSource);
            parent.AddChild(streamer);
            GD.Print($"TWR_PASS28_PRIMITIVES_LOADED map={mapName} " +
                $"instances={streamer.SourceInstanceCount} batches={streamer.BatchCount}");
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS28_PRIMITIVES_FALLBACK map=" + mapName +
                ": " + error.Message);
            streamer.Free();
            return false;
        }
    }

    /// <summary>
    /// Attach only after a player has entered the tree. Rendering is retained
    /// without tracking (e.g. editor/fidelity-capture diagnostics).
    /// </summary>
    public void Track(Node3D player)
    {
        _trackedPlayer = player;
        _lastPlayerPosition = new Vector2(float.NaN, float.NaN);
        UpdateVisibility(true);
    }

    public override void _Process(double delta)
    {
        if (_trackedPlayer is null || !GodotObject.IsInstanceValid(_trackedPlayer))
            return;
        _refresh -= delta;
        if (_refresh > 0) return;
        _refresh = 0.32;
        UpdateVisibility(false);
    }

    private void UpdateVisibility(bool force)
    {
        if (_trackedPlayer is null || !GodotObject.IsInstanceValid(_trackedPlayer))
            return;
        var p = _trackedPlayer.GlobalPosition;
        var center = new Vector2(p.X, p.Z);
        if (!force && _lastPlayerPosition.DistanceSquaredTo(center) < 25f)
            return;
        _lastPlayerPosition = center;
        foreach (var (renderNode, tileCenter) in _drawGroups)
        {
            var squared = tileCenter.DistanceSquaredTo(center);
            var radius = renderNode.Visible ? HideRadius : DrawRadius;
            renderNode.Visible = squared <= radius * radius;
        }
    }

    private void LoadPack(string filePath, byte[] expectedSource)
    {
        if (new FileInfo(filePath).Length is <= 0 or > MaxFileBytes)
            throw new InvalidDataException("Primitive cache missing or oversized");
        using var file = File.OpenRead(filePath);
        using var unpack = new GZipStream(file, CompressionMode.Decompress);
        using var input = new BinaryReader(unpack);
        var magic = Encoding.ASCII.GetString(input.ReadBytes(8));
        var version = input.ReadUInt32();
        var tileSize = input.ReadUInt32();
        var count = input.ReadUInt32();
        var sourceHash = input.ReadBytes(32);
        var tileCount = input.ReadUInt32();
        if (magic != Format || version != Version || tileSize != TileStuds ||
            count is < 1 or > MaxInstances || tileCount is < 1 or > MaxTiles ||
            !sourceHash.AsSpan().SequenceEqual(expectedSource))
            throw new InvalidDataException("Outdated or invalid source primitive pack");

        var seenTiles = new HashSet<(int X, int Z)>();
        uint instancesSeen = 0;
        uint batchesSeen = 0;
        for (var index = 0u; index < tileCount; index++)
        {
            var x = input.ReadInt32();
            var z = input.ReadInt32();
            var batches = input.ReadUInt32();
            if (Math.Abs((long)x) > 16000 || Math.Abs((long)z) > 16000 ||
                batches is < 1 or > MaxBatches || !seenTiles.Add((x,z)) ||
                batchesSeen + batches > MaxBatches)
                throw new InvalidDataException("Bad primitive tile/batch budget");

            // Tile center is world-local and source Z axis is reflected.
            var center = new Vector2((x + .5f) * TileStuds * Stud,
                -(z + .5f) * TileStuds * Stud);
            for (var batchIndex = 0u; batchIndex < batches; batchIndex++)
            {
                var kind = input.ReadByte();
                var material = input.ReadUInt16();
                var alpha = input.ReadByte();
                var rgb = input.ReadBytes(3);
                var shadow = input.ReadByte();
                var size = input.ReadUInt32();
                if (kind > 1 || alpha == 0 || rgb.Length != 3 || shadow > 1 ||
                    size is < 1 or > MaxPerBatch || instancesSeen + size > count)
                    throw new InvalidDataException("Bad primitive source batch header");

                var key = (kind, material, alpha, rgb[0], rgb[1], rgb[2], shadow == 1);
                if (!_resources.TryGetValue(key, out var resource))
                {
                    Mesh mesh = kind == 1
                        ? RobloxPrimitiveGeometry.WedgeMesh()
                        : new BoxMesh { Size = Vector3.One };
                    var opacity = alpha / 255f;
                    var color = new Color(rgb[0] / 255f, rgb[1] / 255f,
                        rgb[2] / 255f, opacity);
                    var matCode = material.ToString();
                    var surface = new StandardMaterial3D
                    {
                        AlbedoColor = color,
                        Transparency = opacity < .995f
                            ? BaseMaterial3D.TransparencyEnum.Alpha
                            : BaseMaterial3D.TransparencyEnum.Disabled,
                        CullMode = BaseMaterial3D.CullModeEnum.Back
                    };
                    RobloxMaterialSurface.Apply(surface, matCode, null);
                    resource = (mesh, surface);
                    _resources.Add(key, resource);
                }

                var multi = new MultiMesh
                {
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                    Mesh = resource.Mesh,
                    InstanceCount = (int)size
                };
                var boundsMin = new Vector3(float.PositiveInfinity,
                    float.PositiveInfinity, float.PositiveInfinity);
                var boundsMax = new Vector3(float.NegativeInfinity,
                    float.NegativeInfinity, float.NegativeInfinity);
                for (var obj = 0u; obj < size; obj++)
                {
                    // Compressed native original Part CFrame, reflected to
                    // Godot world, including rotated SpecialMesh brick offset.
                    var v = new float[12];
                    for (var axis = 0; axis < 12; axis++)
                    {
                        v[axis] = input.ReadSingle();
                        if (!float.IsFinite(v[axis]) || Math.Abs(v[axis]) > 100000f)
                            throw new InvalidDataException("Nonfinite primitive transform");
                    }
                    var transform = new Transform3D(
                        new Basis(new Vector3(v[0],v[1],v[2]),
                                  new Vector3(v[3],v[4],v[5]),
                                  new Vector3(v[6],v[7],v[8])),
                        new Vector3(v[9],v[10],v[11]));
                    multi.SetInstanceTransform((int)obj, transform);
                    // Godot frustum culling must include source-transformed
                    // MultiMesh instances, not just the unscaled unit cube.
                    // Otherwise entire source tiles can vanish at distance.
                    var bx = transform.Basis.X;
                    var by = transform.Basis.Y;
                    var bz = transform.Basis.Z;
                    var extents = new Vector3(
                        (Math.Abs(bx.X) + Math.Abs(by.X) + Math.Abs(bz.X)) * .5f,
                        (Math.Abs(bx.Y) + Math.Abs(by.Y) + Math.Abs(bz.Y)) * .5f,
                        (Math.Abs(bx.Z) + Math.Abs(by.Z) + Math.Abs(bz.Z)) * .5f);
                    var lo = transform.Origin - extents;
                    var hi = transform.Origin + extents;
                    boundsMin = new Vector3(
                        Math.Min(boundsMin.X, lo.X), Math.Min(boundsMin.Y, lo.Y),
                        Math.Min(boundsMin.Z, lo.Z));
                    boundsMax = new Vector3(
                        Math.Max(boundsMax.X, hi.X), Math.Max(boundsMax.Y, hi.Y),
                        Math.Max(boundsMax.Z, hi.Z));
                }
                multi.CustomAabb = new Aabb(boundsMin, boundsMax - boundsMin);
                var draw = new MultiMeshInstance3D
                {
                    Name = $"PrimitiveTile_{x}_{z}_{batchIndex}",
                    Multimesh = multi,
                    MaterialOverride = resource.Material,
                    CastShadow = shadow == 1
                        ? GeometryInstance3D.ShadowCastingSetting.On
                        : GeometryInstance3D.ShadowCastingSetting.Off,
                    // No player is available until GameplayRoot enters the
                    // tree. An optional test can still render the full scene.
                    Visible = true
                };
                AddChild(draw);
                _drawGroups.Add((draw, center));
                instancesSeen += size;
                batchesSeen++;
            }
        }
        if (instancesSeen != count || input.BaseStream.ReadByte() != -1)
            throw new InvalidDataException("Truncated or trailing primitive pack data");
        SourceInstanceCount = (int)instancesSeen;
    }

    private static string? FindPack(string map)
    {
        var name = map + ".native28.gz";
        var exe = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        return new[]
        {
            Path.Combine(exe, "Content", "Geometry", name),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Geometry", name),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot",
                "Content", "Geometry", name)
        }.FirstOrDefault(File.Exists);
    }
}

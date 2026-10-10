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
/// Opt-in, precomputed Laboratory source waypoint graph. It uses original
/// source-positioned floors and collision bounding-box approximations.
/// Route lookup is conservative: when a path cannot be trusted, return empty
/// and let InfectedAgent's existing obstacle-steering behavior take over.
/// This is not the original Roblox navmesh or original zombie pathfinding.
/// </summary>
public partial class Pass25SourceNavigationRuntime : Node3D
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TWRNAV25");
    private const int MaximumNodes = 125_000;
    private const int MaximumEdges = 350_000;
    private const float Stud = RobloxUnits.MetersPerStud;
    private const float MaximumAnchorDistance = 5.0f;
    private readonly AStar3D _graph = new();
    private int _edgeCount;
    public int PointCount => (int)_graph.GetPointCount();
    public int EdgeCount => _edgeCount;

    public static Pass25SourceNavigationRuntime? TryBuild(Node3D owner, string mapName)
    {
        if (mapName != "Laboratory") return null;
        var navFile = CandidatePaths("Navigation", "Laboratory.nav25.gz")
            .FirstOrDefault(File.Exists);
        var sceneFile = CandidatePaths("Maps", "Laboratory.scene.jsonl.gz")
            .FirstOrDefault(File.Exists);
        if (navFile is null || sceneFile is null)
        {
            GD.Print("TWR_PASS25_NAV_MISSING map=Laboratory fallback=local_steering");
            return null;
        }
        var navigator = new Pass25SourceNavigationRuntime { Name = "Pass25LaboratoryNavigation" };
        try
        {
            navigator.ReadGraph(navFile, sceneFile);
            owner.AddChild(navigator);
            GD.Print($"TWR_PASS25_NAV_READY map=Laboratory " +
                $"nodes={navigator.PointCount} edges={navigator.EdgeCount}");
            return navigator;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS25_NAV_REJECTED map=Laboratory: " +
                error.Message + "; fallback=local_steering");
            navigator.Free();
            return null;
        }
    }

    public Vector3[] GetRoute(Vector3 from, Vector3 to)
    {
        if (_graph.GetPointCount() == 0) return [];
        var start = _graph.GetClosestPoint(from);
        var end = _graph.GetClosestPoint(to);
        if (start < 0 || end < 0 || start == end) return [];
        // Do not route actors from unsupported, distant or wrong-elevation
        // positions. An empty route preserves existing collision steering.
        if (from.DistanceTo(_graph.GetPointPosition(start)) > MaximumAnchorDistance ||
            to.DistanceTo(_graph.GetPointPosition(end)) > MaximumAnchorDistance)
            return [];
        var route = _graph.GetPointPath(start, end);
        if (route.Length == 0) return []; // Disconnected floors / components.
        // The agent replans periodically. Cap the returned waypoint window
        // rather than creating unbounded per-enemy path arrays.
        return route.Length <= 256 ? route : route[..256];
    }

    private static IEnumerable<string> CandidatePaths(string contentType, string filename)
    {
        var exeDirectory = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(exeDirectory, "Content", contentType, filename);
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Content", contentType, filename);
        yield return Path.Combine(Directory.GetCurrentDirectory(),
            "src", "Twr.Godot", "Content", contentType, filename);
    }

    private void ReadGraph(string navFile, string sceneFile)
    {
        // A SHA-256 digest binds this generated waypoint graph to the exact
        // private source scene that was used for offline collision sampling.
        // A source-map replacement cannot accidentally load a stale graph.
        byte[] sceneDigest;
        using (var source = File.OpenRead(sceneFile))
            sceneDigest = SHA256.HashData(source);

        using var archive = File.OpenRead(navFile);
        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip);
        if (!reader.ReadBytes(8).SequenceEqual(Magic))
            throw new InvalidDataException("wrong navigation format");
        if (reader.ReadUInt32() != 1)
            throw new InvalidDataException("unsupported navigation version");
        var pointCount = reader.ReadInt32();
        var edgeCount = reader.ReadInt32();
        if (pointCount is < 1 or > MaximumNodes ||
            edgeCount is < 0 or > MaximumEdges)
            throw new InvalidDataException("navigation graph exceeds safe limits");
        if (!reader.ReadBytes(32).SequenceEqual(sceneDigest))
            throw new InvalidDataException("navigation does not match the original scene pack");
        for (var index = 0; index < pointCount; index++)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) ||
                Math.Abs(x) > 100_000f || Math.Abs(y) > 100_000f || Math.Abs(z) > 100_000f)
                throw new InvalidDataException("invalid waypoint coordinate");
            _graph.AddPoint(index, new Vector3(x, y, -z) * Stud);
        }
        var knownEdges = new HashSet<ulong>();
        for (var index = 0; index < edgeCount; index++)
        {
            var a = reader.ReadInt32();
            var b = reader.ReadInt32();
            if (a < 0 || b < 0 || a >= pointCount || b >= pointCount || a == b)
                throw new InvalidDataException("navigation edge contains invalid node");
            var first = Math.Min(a,b);
            var last = Math.Max(a,b);
            if (!knownEdges.Add(((ulong)(uint)first << 32) | (uint)last))
                throw new InvalidDataException("duplicate navigation edge");
            var delta = _graph.GetPointPosition(a) - _graph.GetPointPosition(b);
            if (delta.Length() > 1.5f || Math.Abs(delta.Y) > .64f)
                throw new InvalidDataException("navigation edge jumps unsupported distance");
            _graph.ConnectPoints(a, b, true);
        }
        if (reader.BaseStream.ReadByte() != -1)
            throw new InvalidDataException("extra data after navigation graph");
        _edgeCount = edgeCount;
    }
}

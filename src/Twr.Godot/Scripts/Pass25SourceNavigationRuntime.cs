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
    private bool _bridgePackActive;
    private int[] _componentRoot = Array.Empty<int>();
    private readonly Dictionary<int, List<int>> _componentNodes = new();
    public bool IsBridgePackActive => _bridgePackActive;
    // The new approximation is deterministically sampled from 18,130
    // exact-positioned owner collider bounds, not Roblox's real navmesh.
    private const string Pass40SceneSha =
        "35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74";
    private const string Pass40NavigationSha =
        "b021b905a0a87619997a0e2da08a4c7b2db18ecd2a5250f93dfd84d08cb26bdc";
    public bool IsPass40GroundedGraph { get; private set; }

    // Most infected replan toward the SAME player anchor every 0.75-1.15s.
    // A* on 19,355 waypoints per infected costs CPU and creates garbage.
    // Cache immutable graph paths by start/end waypoint with bounded LRU.
    // Never cache arbitrary coordinates; anchors and collision must still
    // be checked for EVERY request.
    private const int MaximumCachedRoutes = 512;
    private sealed record CachedRoute((long From,long To) Key, Vector3[] Path);
    private readonly Dictionary<(long From,long To), LinkedListNode<CachedRoute>> _cachedRoutes = new();
    private readonly LinkedList<CachedRoute> _recentRoutes = new();
    public long RouteRequests { get; private set; }
    public long RouteCacheHits { get; private set; }
    public long ActualPathSearches { get; private set; }
    public long DisconnectedRouteRejects { get; private set; }
    public long DistantAnchorRejects { get; private set; }
    public int CachedRouteCount => _cachedRoutes.Count;

    private void RememberRoute((long From,long To) key, Vector3[] path)
    {
        if (_cachedRoutes.TryGetValue(key, out var old))
        {
            _recentRoutes.Remove(old);
            _cachedRoutes.Remove(key);
        }
        var entry = _recentRoutes.AddFirst(new CachedRoute(key,path));
        _cachedRoutes.Add(key,entry);
        while (_cachedRoutes.Count > MaximumCachedRoutes)
        {
            var tail = _recentRoutes.Last;
            if (tail is null) break;
            _recentRoutes.RemoveLast();
            _cachedRoutes.Remove(tail.Value.Key);
        }
    }
    public int PointCount => (int)_graph.GetPointCount();
    public int EdgeCount => _edgeCount;

    public static Pass25SourceNavigationRuntime? TryBuild(Node3D owner, string mapName)
    {
        if (mapName != "Laboratory") return null;
        var navFiles = CandidatePaths("Navigation", "Laboratory.nav31.gz")
            .Concat(CandidatePaths("Navigation", "Laboratory.nav25.gz"))
            .Where(File.Exists).Distinct(StringComparer.Ordinal).ToArray();
        var sceneFile = CandidatePaths("Maps", "Laboratory.scene.jsonl.gz")
            .FirstOrDefault(File.Exists);
        if (navFiles.Length == 0 || sceneFile is null)
        {
            GD.Print("TWR_PASS25_NAV_MISSING map=Laboratory fallback=local_steering");
            return null;
        }
        foreach (var navFile in navFiles)
        {
            var navigator = new Pass25SourceNavigationRuntime { Name = "Pass25LaboratoryNavigation" };
            try
            {
                navigator.ReadGraph(navFile, sceneFile);
                owner.AddChild(navigator);
                GD.Print($"TWR_PASS31_NAV_READY map=Laboratory nodes={navigator.PointCount} " +
                    $"edges={navigator.EdgeCount} bridge_pack={navigator.IsBridgePackActive}");
                return navigator;
            }
            catch (Exception error)
            {
                GD.PushWarning("TWR_PASS25_NAV_REJECTED map=Laboratory: " +
                    error.Message + "; fallback=local_steering");
                navigator.Free();
            }
        }
        return null;
    }

    /// <summary>
    /// Opt-in accessibility only. If an original infected spawn is outside
    /// the player's connected floor region, return a distant supported graph
    /// waypoint instead. This approximates original exterior horde entries.
    /// The main game leaves this mode OFF unless the player presses F9.
    /// </summary>
    public Vector3? FindAssistedInfectedSpawn(Vector3 originalSpawn,
        Vector3 playerPosition, uint variation)
    {
        var candidates = GetAssistedInfectedSpawnCandidates(
            originalSpawn, playerPosition, variation);
        return candidates.Length == 0 ? null : candidates[0];
    }

    /// <summary>
    /// Ordered same-component alternatives for physics validation. Graph
    /// connectivity alone does not prove a safe capsule-ground placement,
    /// especially while original SmoothGrid terrain is unavailable.
    /// </summary>
    public Vector3[] GetAssistedInfectedSpawnCandidates(Vector3 originalSpawn,
        Vector3 playerPosition, uint variation,
        float minimumPlayerDistance = Pass32SpawnSafety.MinimumPlayerDistance)
    {
        // Only explicitly enabled F9 uses this; even a closer candidate must
        // still be physics validated by Pass32SpawnSafety before spawning.
        if (!_bridgePackActive || _componentRoot.Length == 0 ||
            !float.IsFinite(minimumPlayerDistance) ||
            minimumPlayerDistance < Pass40AdaptiveEntry.MinimumFallbackDistance ||
            minimumPlayerDistance > Pass32SpawnSafety.MinimumPlayerDistance)
            return [];
        var target = _graph.GetClosestPoint(playerPosition);
        var origin = _graph.GetClosestPoint(originalSpawn);
        if (target < 0 ||
            playerPosition.DistanceTo(_graph.GetPointPosition(target)) > MaximumAnchorDistance)
            return [];
        var playerComponent = _componentRoot[(int)target];
        if (origin >= 0 &&
            originalSpawn.DistanceTo(_graph.GetPointPosition(origin)) <= MaximumAnchorDistance &&
            _componentRoot[(int)origin] == playerComponent)
            return []; // Source-connected original positions are preserved.
        if (!_componentNodes.TryGetValue(playerComponent, out var nodes))
            return [];

        var options = nodes.Select(id => new
        {
            Id = id,
            Position = _graph.GetPointPosition(id)
        })
        .Where(row => row.Position.DistanceSquaredTo(playerPosition) >=
            minimumPlayerDistance * minimumPlayerDistance &&
            Math.Abs(row.Position.Y - playerPosition.Y) <= 1.6f)
        .OrderBy(row => row.Position.DistanceSquaredTo(originalSpawn))
        .ThenBy(row => row.Id)
        .Take(32).ToArray();
        if (options.Length == 0) return [];
        var reordered = new Vector3[options.Length];
        var start = (int)(variation % (uint)options.Length);
        for (var i = 0; i < reordered.Length; i++)
            reordered[i] = options[(start + i) % options.Length].Position +
                Vector3.Up * .8f;
        return reordered;
    }

    public Vector3[] GetRoute(Vector3 from, Vector3 to)
    {
        RouteRequests++;
        if (_graph.GetPointCount() == 0) return [];
        var start = _graph.GetClosestPoint(from);
        var end = _graph.GetClosestPoint(to);
        if (start < 0 || end < 0 || start == end) return [];

        // Critical: A* must NOT explore an entire disconnected wing.
        // Also avoid accepting world points on another floor via a nearby
        // XZ waypoint; the three-dimensional anchor distance is retained.
        if (from.DistanceTo(_graph.GetPointPosition(start)) > MaximumAnchorDistance ||
            to.DistanceTo(_graph.GetPointPosition(end)) > MaximumAnchorDistance)
        {
            DistantAnchorRejects++;
            return [];
        }
        if (_componentRoot[(int)start] != _componentRoot[(int)end])
        {
            DisconnectedRouteRejects++;
            return [];
        }

        var key = (From:start,To:end);
        if (_cachedRoutes.TryGetValue(key,out var cached))
        {
            RouteCacheHits++;
            _recentRoutes.Remove(cached);
            _recentRoutes.AddFirst(cached);
            // Preserve read-only ownership: callers cannot mutate cached
            // waypoints and silently corrupt other zombie routes.
            return cached.Value.Path.ToArray();
        }

        ActualPathSearches++;
        var route = _graph.GetPointPath(start,end);
        // The source graph remains static for the lifetime of the match.
        // Cache negative routes too, but never a disconnected-component
        // result (already rejected before running A*).
        var bounded = route.Length <= 256 ? route : route[..256];
        RememberRoute(key,bounded);
        return bounded.ToArray();
    }

    private static int FindRoot(int[] parents, int i)
    {
        while (parents[i] != i)
        {
            parents[i] = parents[parents[i]];
            i = parents[i];
        }
        return i;
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

        var packedHash = Convert.ToHexString(SHA256.HashData(
            File.ReadAllBytes(navFile))).ToLowerInvariant();
        IsPass40GroundedGraph =
            Convert.ToHexString(sceneDigest).ToLowerInvariant() == Pass40SceneSha &&
            packedHash == Pass40NavigationSha;
        using var archive = File.OpenRead(navFile);
        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip);
        var marker = reader.ReadBytes(8);
        var nav31 = marker.SequenceEqual(Encoding.ASCII.GetBytes("TWRNAV31"));
        if (!nav31 && !marker.SequenceEqual(Magic))
            throw new InvalidDataException("wrong navigation format");
        if (reader.ReadUInt32() != (nav31 ? 2u : 1u))
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
        var parents = Enumerable.Range(0, pointCount).ToArray();
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
            if (delta.Length() > (nav31 ? 2.46f : 1.5f) || Math.Abs(delta.Y) > .64f)
                throw new InvalidDataException("navigation edge jumps unsupported distance");
            _graph.ConnectPoints(a, b, true);
            var rootA = FindRoot(parents, a);
            var rootB = FindRoot(parents, b);
            if (rootA != rootB) parents[rootB] = rootA;
        }
        if (reader.BaseStream.ReadByte() != -1)
            throw new InvalidDataException("extra data after navigation graph");
        _edgeCount = edgeCount;
        _bridgePackActive = nav31;
        _componentRoot = new int[pointCount];
        for (var i = 0; i < pointCount; i++)
        {
            var root = FindRoot(parents, i);
            _componentRoot[i] = root;
            if (!_componentNodes.TryGetValue(root, out var nodes))
            {
                nodes = new List<int>();
                _componentNodes[root] = nodes;
            }
            nodes.Add(i);
        }
    }
}

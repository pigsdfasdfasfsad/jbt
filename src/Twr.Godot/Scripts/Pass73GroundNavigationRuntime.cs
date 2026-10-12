using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass73: bounded, collision-sampled ground-floor routes for APPROXIMATE
/// fallback maps only. Never substitutes for private original Laboratory
/// source geometry/navigation or the Expressway's existing road graph.
/// Rigid-body physics decides which cells and edges are walkable; a shared
/// AStar3D graph avoids each infected building its own navigation topology.
/// Upper floors, stairs, temporary fortifications and missing retail mesh
/// triangles still require dedicated fidelity work.
/// </summary>
public partial class Pass73GroundNavigationRuntime : Node3D
{
    public RuntimeMapLayout Layout { get; set; } = null!;
    public string MapName { get; set; } = "";
    public const float CellSizeMetres = 3f;
    private const float MaxGroundStep = .62f;
    private const float MinimumWalkableNormal = .71f;
    private const float MaximumAnchorHorizontal = 6.0f;
    private const float MaximumAnchorVertical = 2.0f;
    private const int SampleBudgetPerPhysicsFrame = 120;
    private const int LinkBudgetPerPhysicsFrame = 90;
    private const int MaximumGridCells = 2_800;
    private const int MaximumRouteCache = 192;
    private const uint WorldCollisionMask = 1u;

    private static readonly (int Col, int Row)[] Neighbors =
        [(1,0), (0,1), (1,1), (-1,1)];

    private readonly AStar3D _graph = new();
    // Pass91: the original Pass73 graph samples a smaller zombie body.
    // Escort survivors have a real .42m radius; their edges must be
    // constructed with full-capsule clearance, without changing infected AI.
    private readonly AStar3D _escortGraph91 = new();
    private readonly CapsuleShape3D _standingCapsule =
        new() { Radius = .30f, Height = 1.60f };
    private readonly global::Godot.Collections.Array<Rid> _exclude = new();
    private readonly Dictionary<(long From, long To), Vector3[]> _routes = new();
    private readonly Queue<(long From, long To)> _routeInsertionOrder = new();

    private float _firstX;
    private float _firstZ;
    private float _spacing = CellSizeMetres;
    private int _columns;
    private int _rows;
    private int _nextSample;
    private int _nextLink;
    private bool _connected;
    private bool _ready;
    private int _edges;

    public int PointCount => (int)_graph.GetPointCount();
    public int EdgeCount => _edges;
    public int EscortSafeEdgeCount91 { get; private set; }
    public int CandidateCount => _columns * _rows;
    public bool IsGraphReady => _ready && PointCount >= 20;
    public int RouteCacheCount => _routes.Count;
    public long RouteRequests { get; private set; }
    public long CacheHits { get; private set; }
    public long FallbackPathSearches { get; private set; }
    public long VerifiedElevatedRoutes77 { get; private set; }
    public long RejectedElevatedRoutes77 { get; private set; }
    public long VerifiedDescendingRoutes78 { get; private set; }
    public long RejectedDescendingRoutes78 { get; private set; }
    public long VerifiedUpperDirectRoutes79 { get; private set; }
    public long RejectedUpperDirectRoutes79 { get; private set; }
    public long VerifiedUpperCrossingRoutes80 { get; private set; }
    public long RejectedUpperCrossingRoutes80 { get; private set; }
    public long VerifiedUpperDetours81 { get; private set; }
    public long RejectedUpperDetours81 { get; private set; }
    public int CrossingCount80 => Layout?.ApproximateCrossings80.Count ?? 0;
    public int StairLinkCount77 => Layout?.ApproximateStairs77.Count ?? 0;

    public static Pass73GroundNavigationRuntime Attach(
        Node3D owner, string mapName, RuntimeMapLayout layout)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(layout);
        var nav = new Pass73GroundNavigationRuntime
        {
            Name = "Pass73GroundNavigation",
            MapName = mapName,
            Layout = layout
        };
        owner.AddChild(nav);
        return nav;
    }

    public override void _ExitTree()
    {
        // Dispose native graph and floor-probe shape while Godot's physics
        // server still exists. Late C# GodotObject finalizers are unsafe.
        _standingCapsule.Dispose();
        _escortGraph91.Dispose();
        _graph.Dispose();
        base._ExitTree();
    }

    public override void _Ready()
    {
        var points = new[] { Layout.PlayerSpawn }
            .Concat(Layout.InfectedSpawns)
            .Concat(Layout.PickupPoints)
            .Concat(Layout.ObjectivePoints)
            .ToArray();
        if (points.Length == 0)
        {
            SetPhysicsProcess(false);
            return;
        }

        var minX = points.Min(p => p.X) - 4f;
        var maxX = points.Max(p => p.X) + 4f;
        var minZ = points.Min(p => p.Z) - 4f;
        var maxZ = points.Max(p => p.Z) + 4f;
        do
        {
            _firstX = Mathf.Floor(minX / _spacing) * _spacing;
            _firstZ = Mathf.Floor(minZ / _spacing) * _spacing;
            _columns = (int)Mathf.Ceil((maxX - _firstX) / _spacing) + 1;
            _rows = (int)Mathf.Ceil((maxZ - _firstZ) / _spacing) + 1;
            if ((long)_columns * _rows <= MaximumGridCells) break;
            _spacing += .5f;
        } while (_spacing <= 8f);

        if ((long)_columns * _rows > MaximumGridCells)
            throw new InvalidOperationException(
                "Pass73 bounded ground navigation grid budget exceeded");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_ready) return;
        // Build in bounded slices to avoid a one-frame hitch whenever a map
        // starts. Existing first-wave countdown gives the graph ample time.
        if (_exclude.Count == 0)
        {
            var player = GetParent()?.GetNodeOrNull<FirstPersonPlayer>("Player");
            if (player is not null)
                _exclude.Add(player.GetRid());
        }

        var total = CandidateCount;
        var end = Math.Min(total, _nextSample + SampleBudgetPerPhysicsFrame);
        for (; _nextSample < end; _nextSample++)
        {
            var column = _nextSample % _columns;
            var row = _nextSample / _columns;
            var x = _firstX + column * _spacing;
            var z = _firstZ + row * _spacing;
            if (TryFindGround(x, z, out var ground))
            {
                _graph.AddPoint(_nextSample, ground);
                _escortGraph91.AddPoint(_nextSample, ground);
            }
        }
        if (_nextSample < total) return;

        var linkEnd = Math.Min(total, _nextLink + LinkBudgetPerPhysicsFrame);
        for (; _nextLink < linkEnd; _nextLink++)
        {
            if (!_graph.HasPoint(_nextLink)) continue;
            var from = _graph.GetPointPosition(_nextLink);
            var col = _nextLink % _columns;
            var row = _nextLink / _columns;
            foreach (var (dc, dr) in Neighbors)
            {
                var otherCol = col + dc;
                var otherRow = row + dr;
                if (otherCol < 0 || otherCol >= _columns ||
                    otherRow < 0 || otherRow >= _rows)
                    continue;
                var id = otherRow * _columns + otherCol;
                if (!_graph.HasPoint(id)) continue;
                var to = _graph.GetPointPosition(id);
                if (Math.Abs(to.Y - from.Y) > MaxGroundStep ||
                    !CorridorClear(from, to))
                    continue;
                _graph.ConnectPoints(_nextLink, id);
                _edges++;
                // Pass91 separately audit the actual Escort collision
                // capsule on each traversable physical A* connection.
                // A thin zombie may fit where a .42m survivor cannot.
                if(Pass89EscortSurfaceSafety.CanTravel(
                    GetWorld3D(),from,to,_exclude))
                {
                    _escortGraph91.ConnectPoints(_nextLink,id);
                    EscortSafeEdgeCount91++;
                }
            }
        }
        if (_nextLink < total) return;
        _connected = _edges > 0;
        _ready = true;
        SetPhysicsProcess(false);
        GD.Print($"TWR_PASS73_GROUND_NAV_READY map={MapName} " +
            $"nodes={PointCount} edges={EdgeCount} samples={CandidateCount} " +
            $"spacing={_spacing:F1} active={IsGraphReady && _connected}");
    }

    private bool TryFindGround(float x, float z, out Vector3 feet)
    {
        feet = Vector3.Zero;
        // Fallback map primary floors are at 0m; elevated docks are below
        // 2.4m. Start BELOW Manor's upper deck, deliberately excluding it
        // rather than incorrectly connecting the two floors across walls.
        using var ray = PhysicsRayQueryParameters3D.Create(
            new Vector3(x, 2.4f, z), new Vector3(x, -1.4f, z));
        ray.CollisionMask = WorldCollisionMask;
        ray.Exclude = _exclude;
        var space = GetWorld3D().DirectSpaceState;
        var groundHit = space.IntersectRay(ray);
        if (groundHit.Count == 0 ||
            ((Vector3)groundHit["normal"]).Y < MinimumWalkableNormal)
            return false;

        var floor = (Vector3)groundHit["position"];
        var stand = new Vector3(floor.X, floor.Y + .86f, floor.Z);
        using var capsuleQuery = new PhysicsShapeQueryParameters3D
        {
            Shape = _standingCapsule,
            Transform = new Transform3D(Basis.Identity, stand),
            CollisionMask = WorldCollisionMask,
            CollideWithBodies = true,
            Exclude = _exclude
        };
        if (space.IntersectShape(capsuleQuery, 1).Count != 0)
            return false;
        feet = floor;
        return true;
    }

    private bool CorridorClear(Vector3 aFeet, Vector3 bFeet)
    {
        var d = bFeet - aFeet;
        d.Y = 0;
        if (d.LengthSquared() <= .01f) return true;
        var side = new Vector3(-d.Z, 0, d.X).Normalized();
        foreach (var offset in new[] { 0f, -.23f, .23f })
        {
            var shift = side * offset + Vector3.Up * .82f;
            using var ray = PhysicsRayQueryParameters3D.Create(
                aFeet + shift, bFeet + shift);
            ray.CollisionMask = WorldCollisionMask;
            ray.Exclude = _exclude;
            if (GetWorld3D().DirectSpaceState.IntersectRay(ray).Count > 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns feet-level graph waypoints, matching the existing infected
    /// waypoint +0.8m body-centre convention. Empty means local steering;
    /// this never teleports a zombie or changes physics collision authority.
    /// </summary>
    public Vector3[] GetRoute(Vector3 infectedCentre, Vector3 playerCentre)
    {
        RouteRequests++;
        if (!IsGraphReady || !_connected ||
            !IsFinite(infectedCentre) || !IsFinite(playerCentre))
            return [];

        // Pass79: an infected already upstairs must not use the ground-floor
        // graph or direct steering across missing balcony support.
        if (IsManorSameUpperFloor79(infectedCentre, playerCentre))
        {
            if (UpperCorridorSupported79(
                    infectedCentre - Vector3.Up * .8f, playerCentre))
                VerifiedUpperDirectRoutes79++;
            else
            {
                RejectedUpperDirectRoutes79++;
                if (TryGetVerifiedUpperCrossing80(
                        infectedCentre,playerCentre,out var crossing))
                {
                    VerifiedUpperCrossingRoutes80++;
                    return crossing;
                }
                RejectedUpperCrossingRoutes80++;
                if (TryGetSupportedUpperDetour81(
                        infectedCentre,playerCentre,out var detour))
                {
                    VerifiedUpperDetours81++;
                    return detour;
                }
                RejectedUpperDetours81++;
            }
            return []; // An unsafe unsupported target receives no shortcut.
        }

        // Pass77 supplements ONLY the approximate Manor blockout when a
        // player is above a ground-floor infected. It never overwrites the
        // owner-recovered Laboratory graph or invents retail stair routes.
        if (MapName == "Manor" && StairLinkCount77 == 3 &&
            playerCentre.Y >= 2.2f &&
            infectedCentre.Y - .8f < playerCentre.Y - .01f)
        {
            if (TryGetVerifiedManorAscent77(
                    infectedCentre,playerCentre,out var ascending))
            {
                VerifiedElevatedRoutes77++;
                return ascending;
            }
            RejectedElevatedRoutes77++;
        }

        // Pass78: conservative bidirectional traversal is restricted to
        // the fallback Manor stair geometry. No source-original map graph,
        // jump path, other map or downstairs animation is synthesized.
        if (MapName == "Manor" && StairLinkCount77 == 3 &&
            playerCentre.Y <= 1.5f &&
            infectedCentre.Y - .8f > playerCentre.Y + .45f)
        {
            if (TryGetVerifiedManorDescent78(
                    infectedCentre, playerCentre, out var descending))
            {
                VerifiedDescendingRoutes78++;
                return descending;
            }
            RejectedDescendingRoutes78++;
        }

        // Do not force agents through grid cells if ordinary direct pursuit
        // has a clear corridor; source enemy movement retains priority.
        if (Math.Abs(infectedCentre.Y - playerCentre.Y) <= 1.2f &&
            CorridorClear(infectedCentre - Vector3.Up * .8f,
                playerCentre - Vector3.Up * .8f))
            return [];

        var start = _graph.GetClosestPoint(infectedCentre);
        var end = _graph.GetClosestPoint(playerCentre);
        if (start < 0 || end < 0)
            return [];
        var a = _graph.GetPointPosition(start);
        var b = _graph.GetPointPosition(end);
        if (!AnchorMatches(infectedCentre, a) ||
            !AnchorMatches(playerCentre, b))
            return [];

        var key = (start, end);
        if (_routes.TryGetValue(key, out var cached))
        {
            CacheHits++;
            return cached;
        }
        FallbackPathSearches++;
        var path = _graph.GetPointPath(start, end);
        if (path.Length <= 1) return [];

        // Bounded FIFO cache; no map-specific original source data enters it.
        if (_routes.Count >= MaximumRouteCache)
        {
            var expired = _routeInsertionOrder.Dequeue();
            _routes.Remove(expired);
        }
        _routes.Add(key, path);
        _routeInsertionOrder.Enqueue(key);
        return path;
    }


    /// <summary>
    /// Pass89 route for the feet-origin Escort CharacterBody3D. Unlike
    /// zombie GetRoute, this ALWAYS returns an explicit destination when
    /// the direct corridor is physically supported; [] means stop rather
    /// than blindly steering through solid walls or an unsupported void.
    /// The ground graph is reused but its edges are re-audited against
    /// live Godot collision before each escort plan is accepted.
    /// </summary>
    public Vector3[] GetVerifiedEscortRoute89(
        Vector3 escortFeet, Vector3 destinationFeet, Rid escortRid)
    {
        if (!IsGraphReady || !_connected ||
            EscortSafeEdgeCount91<10 ||
            !IsFinite(escortFeet) || !IsFinite(destinationFeet))
            return [];
        var exclude = new global::Godot.Collections.Array<Rid>();
        foreach (var rid in _exclude) exclude.Add(rid);
        if (escortRid.IsValid) exclude.Add(escortRid);
        var world = GetWorld3D();

        if (Pass89EscortSurfaceSafety.CanTravel(
                world,escortFeet,destinationFeet,exclude))
            return [destinationFeet];

        // Fallback Pass73 covers ground-level surfaces; never anchor a
        // balcony escort to a downstairs vertex and let it fall.
        if (escortFeet.Y > 2.2f || destinationFeet.Y > 2.2f)
            return [];
        var fromId=_escortGraph91.GetClosestPoint(escortFeet);
        var toId=_escortGraph91.GetClosestPoint(destinationFeet);
        if (fromId < 0 || toId < 0 || fromId == toId)
            return [];
        var fromAnchor=_escortGraph91.GetPointPosition(fromId);
        var toAnchor=_escortGraph91.GetPointPosition(toId);
        if (!EscortAnchorMatches89(escortFeet,fromAnchor) ||
            !EscortAnchorMatches89(destinationFeet,toAnchor))
            return [];

        var path=_escortGraph91.GetPointPath(fromId,toId);
        if (path.Length < 2 || path.Length > 160) return [];
        var route=new List<Vector3>(path.Length+1);
        var last=escortFeet;
        foreach(var waypoint in path)
        {
            // Live world rechecks reject synthetic walls/fortifications
            // introduced after Pass73 sampled its original graph.
            if(!Pass89EscortSurfaceSafety.CanTravel(
                    world,last,waypoint,exclude))
                return [];
            route.Add(waypoint);
            last=waypoint;
        }
        if(!Pass89EscortSurfaceSafety.CanTravel(
                world,last,destinationFeet,exclude))
            return [];
        route.Add(destinationFeet);
        return route.ToArray();
    }

    private static bool EscortAnchorMatches89(Vector3 feet,Vector3 anchor)
    {
        var dx=feet.X-anchor.X;
        var dz=feet.Z-anchor.Z;
        return dx*dx+dz*dz <= 36f &&
            Math.Abs(feet.Y-anchor.Y) <= .90f;
    }

    private bool TryGetVerifiedManorAscent77(
        Vector3 infectedCentre, Vector3 playerCentre, out Vector3[] result)
    {
        result = [];
        // InfectedAgent's capsule is centred at its node origin, ~0.8m
        // above its feet. FirstPersonPlayer's collision capsule is offset
        // +0.9m locally, so its GlobalPosition IS already feet-level.
        var infectedFeet = infectedCentre - Vector3.Up * .8f;
        var playerFeet = playerCentre;
        // Keep only the supported up-ramp transition. Downstairs chase and
        // stair routes between disconnected wings are NOT yet verified.
        if (infectedFeet.Y > playerFeet.Y + .1f ||
            playerFeet.Y < 2.2f ||
            Layout.ApproximateStairs77.Count != 3)
            return false;

        Pass77StairLink? selected = null;
        Vector3[] approach = [];
        var startSegment = 1;
        var alreadyOnRamp = false;
        var smallest = float.MaxValue;
        foreach (var stair in Layout.ApproximateStairs77)
        {
            var groundDistance = new Vector2(
                infectedFeet.X - stair.LowerFeet.X,
                infectedFeet.Z - stair.LowerFeet.Z).Length();
            var upperDistance = new Vector2(
                playerFeet.X - stair.UpperFeet.X,
                playerFeet.Z - stair.UpperFeet.Z).Length();
            if (upperDistance > 11f ||
                Math.Abs(playerFeet.Y - stair.UpperFeet.Y) > .90f ||
                !CorridorClear(stair.UpperFeet, playerFeet) ||
                !VerifiedPhysicalStair77(stair))
                continue;

            // Never send an infected BACK DOWN its ramp every time the
            // 0.75s route timer refreshes. Project its physically grounded
            // current position onto this same collision-audited stair and
            // resume at the next uphill sample, if it is truly on the ramp.
            var ramp = stair.UpperFeet - stair.LowerFeet;
            var progress = Mathf.Clamp(
                (infectedFeet - stair.LowerFeet).Dot(ramp) /
                    ramp.LengthSquared(), 0f, 1f);
            var expectedOnRamp = stair.LowerFeet.Lerp(
                stair.UpperFeet, progress);
            var onRamp = progress > .04f && progress < 1f &&
                Math.Abs(infectedFeet.X - expectedOnRamp.X) <= 1.5f &&
                Math.Abs(infectedFeet.Z - expectedOnRamp.Z) <= .65f &&
                Math.Abs(infectedFeet.Y - expectedOnRamp.Y) <= .52f;

            Vector3[] groundPath;
            var begin = 1;
            if (onRamp)
            {
                groundPath = [];
                begin = Math.Clamp((int)Math.Floor(progress*12f)+1,1,12);
            }
            else if (groundDistance <= 4f &&
                CorridorClear(infectedFeet, stair.LowerFeet))
                groundPath = [];
            else
            {
                var start = _graph.GetClosestPoint(infectedCentre);
                var end = _graph.GetClosestPoint(
                    stair.LowerFeet + Vector3.Up * .8f);
                if (start < 0 || end < 0 ||
                    !AnchorMatches(infectedCentre,_graph.GetPointPosition(start)) ||
                    !AnchorMatches(stair.LowerFeet + Vector3.Up * .8f,
                        _graph.GetPointPosition(end)))
                    continue;
                groundPath = _graph.GetPointPath(start,end);
                if (groundPath.Length < 2 ||
                    !CorridorClear(groundPath[^1],stair.LowerFeet))
                    continue;
            }

            var score = (onRamp ? (1f-progress)*6f : groundDistance) +
                upperDistance;
            if (score >= smallest) continue;
            smallest = score;
            selected = stair;
            approach = groundPath;
            startSegment = begin;
            alreadyOnRamp = onRamp;
        }

        if (selected is null) return false;
        // ≤0.75m planar samples from the real sloped collider. Each
        // waypoint height is taken from the actual reconstructed collision
        // ramp, not from an enemy position teleport or a phantom jump.
        var points = new List<Vector3>(approach.Length + 14);
        points.AddRange(approach);
        if (!alreadyOnRamp) points.Add(selected.LowerFeet);
        for (var i = startSegment; i <= 12; i++)
            points.Add(selected.LowerFeet.Lerp(selected.UpperFeet, i/12f));
        points.Add(playerFeet);
        result = points.ToArray();
        return true;
    }

    /// <summary>
    /// Pass78: downward traversal of already-verified, reconstructed
    /// Manor stairs only. Returns feet-level physics waypoints descending
    /// from a supported upstairs ramp/landing to a physically clear lower
    /// floor target. No original source geometry or pathfinding is claimed.
    /// </summary>
    private bool TryGetVerifiedManorDescent78(
        Vector3 infectedCentre, Vector3 playerCentre, out Vector3[] result)
    {
        result = [];
        var infectedFeet = infectedCentre - Vector3.Up * .8f;
        var playerFeet = playerCentre; // FirstPersonPlayer node is feet-level.
        if (playerFeet.Y > 1.5f ||
            infectedFeet.Y < playerFeet.Y + .25f ||
            Layout.ApproximateStairs77.Count != 3)
            return false;

        Pass77StairLink? chosen = null;
        var startSegment = 11;
        var alreadyOnRamp = false;
        var bestScore = float.MaxValue;
        foreach (var stair in Layout.ApproximateStairs77)
        {
            var upperOffset = new Vector2(
                infectedFeet.X - stair.UpperFeet.X,
                infectedFeet.Z - stair.UpperFeet.Z).Length();
            var lowerOffset = new Vector2(
                playerFeet.X - stair.LowerFeet.X,
                playerFeet.Z - stair.LowerFeet.Z).Length();
            if (upperOffset > 6f || lowerOffset > 10f ||
                Math.Abs(playerFeet.Y - stair.LowerFeet.Y) > .9f ||
                !VerifiedPhysicalStair77(stair) ||
                !CorridorClear(stair.LowerFeet, playerFeet))
                continue;

            var ramp = stair.UpperFeet - stair.LowerFeet;
            var progress = Mathf.Clamp(
                (infectedFeet - stair.LowerFeet).Dot(ramp) /
                    ramp.LengthSquared(), 0f, 1f);
            var expected = stair.LowerFeet.Lerp(stair.UpperFeet,progress);
            var onRamp = progress > .02f && progress < .98f &&
                Math.Abs(infectedFeet.X - expected.X) <= 1.5f &&
                Math.Abs(infectedFeet.Z - expected.Z) <= .65f &&
                Math.Abs(infectedFeet.Y - expected.Y) <= .52f;

            // Upper corridor uses real wall visibility. Unlike arbitrary
            // straight-line steering, it will not jump off a balcony edge.
            if (!onRamp &&
                (Math.Abs(infectedFeet.Y - stair.UpperFeet.Y) > .75f ||
                 !CorridorClear(infectedFeet, stair.UpperFeet)))
                continue;

            var score = (onRamp ? progress*6f : upperOffset) + lowerOffset;
            if (score >= bestScore) continue;
            bestScore = score;
            chosen = stair;
            alreadyOnRamp = onRamp;
            // Replanning DURING descent must not send an enemy back up the
            // ramp. Start at the preceding downhill section instead.
            startSegment = onRamp
                ? Math.Clamp((int)Math.Ceiling(progress*12f)-1,0,11)
                : 11;
        }

        if (chosen is null) return false;
        var points = new List<Vector3>(16);
        if (!alreadyOnRamp) points.Add(chosen.UpperFeet);
        for (var i = startSegment; i >= 0; i--)
            points.Add(chosen.LowerFeet.Lerp(chosen.UpperFeet,i/12f));
        points.Add(playerFeet);
        result = points.ToArray();
        return true;
    }


    /// <summary>
    /// Pass79 only applies to the three fallback Manor decks. Player
    /// GlobalPosition is feet-level; infected GlobalPosition is body-center.
    /// In particular this cannot route owner-original source geometry.
    /// </summary>
    public bool IsManorSameUpperFloor79(
        Vector3 infectedCentre, Vector3 playerFeet)
    {
        var infectedFeet = infectedCentre - Vector3.Up * .8f;
        return MapName == "Manor" && StairLinkCount77 == 3 &&
            IsFinite(infectedCentre) && IsFinite(playerFeet) &&
            infectedFeet.Y >= 2.70f && playerFeet.Y >= 2.70f &&
            Math.Abs(infectedFeet.Y - playerFeet.Y) <= .50f;
    }

    public bool UpperCorridorSupported79(Vector3 fromFeet, Vector3 toFeet) =>
        MapName == "Manor" && StairLinkCount77 == 3 &&
        Pass79UpperFloorSafety.IsSupported(
            this, fromFeet, toFeet, _exclude, CorridorClear);

    public bool SafeUpperMove79(
        Vector3 infectedCentre, Vector3 horizontalDirection)
    {
        if (MapName != "Manor" || StairLinkCount77 != 3 ||
            !IsFinite(infectedCentre) || !IsFinite(horizontalDirection))
            return false;
        var direction = new Vector3(
            horizontalDirection.X, 0, horizontalDirection.Z);
        if (direction.LengthSquared() < .0001f) return true;
        var feet = infectedCentre - Vector3.Up * .8f;
        return UpperCorridorSupported79(
            feet, feet + direction.Normalized() * .55f);
    }


    /// <summary>
    /// Pass80: only a verified actual fallback crossing may connect the
    /// left and right balcony halves. Every foot-level section is independently
    /// checked against Godot static collision, including the full bridge.
    /// </summary>
    private bool TryGetVerifiedUpperCrossing80(
        Vector3 infectedCentre, Vector3 playerFeet, out Vector3[] result)
    {
        result = [];
        var from = infectedCentre - Vector3.Up*.8f;
        foreach (var bridge in Layout.ApproximateCrossings80)
        {
            var mid=bridge.CentreFeet;
            if (Math.Abs(from.X-mid.X)>13.8f ||
                Math.Abs(playerFeet.X-mid.X)>13.8f ||
                Math.Abs(from.Z-mid.Z)>38f ||
                Math.Abs(playerFeet.Z-mid.Z)>38f ||
                Math.Abs(from.Y-mid.Y)>.45f ||
                Math.Abs(playerFeet.Y-mid.Y)>.45f)
                continue;
            var firstSide=Math.Sign(from.X-mid.X);
            var lastSide=Math.Sign(playerFeet.X-mid.X);
            if (firstSide == 0 || lastSide == 0 ||
                firstSide == lastSide)
                continue;
            var incoming=firstSide<0 ? bridge.LeftFeet : bridge.RightFeet;
            var outgoing=lastSide<0 ? bridge.LeftFeet : bridge.RightFeet;
            // Pass83 prevents the agent from returning to the *entrance*
            // gate on every 0.75s replan after it has already reached the
            // actual walkable catwalk.
            var onBridge=Math.Abs(from.Z-mid.Z)<.28f &&
                Math.Abs(from.X-mid.X)<4.45f &&
                UpperCorridorSupported79(from,mid);
            if (onBridge)
            {
                if (!UpperCorridorSupported79(mid,outgoing) ||
                    !UpperCorridorSupported79(outgoing,playerFeet))
                    continue;
                result = Math.Abs(from.X-mid.X)<.28f
                    ? [outgoing,playerFeet]
                    : [mid,outgoing,playerFeet];
                return true;
            }
            var entranceSupported=UpperCorridorSupported79(from,incoming);
            if (!UpperCorridorSupported79(incoming,mid) ||
                !UpperCorridorSupported79(mid,outgoing) ||
                !UpperCorridorSupported79(outgoing,playerFeet))
                continue;
            if (entranceSupported)
            {
                result = [incoming,mid,outgoing,playerFeet];
                return true;
            }
            // Pass83 recovery: if the agent wandered near a side-deck
            // edge, first retreat on continuous solid balcony geometry.
            // No unsupported shortcut, jump or position teleport occurs.
            var retreat = new Vector3(
                mid.X+firstSide*5.2f,from.Y,from.Z);
            if (!UpperCorridorSupported79(from,retreat) ||
                !UpperCorridorSupported79(retreat,incoming))
                continue;
            result = [retreat,incoming,mid,outgoing,playerFeet];
            return true;
        }
        return false;
    }


    /// <summary>
    /// Pass81: search only short, pre-bounded corridor L-shaped routes
    /// inside the SAME upper wing. Candidate legs are audited against actual
    /// floor/wall collision. This is not a replacement for retail navigation.
    /// </summary>
    private bool TryGetSupportedUpperDetour81(
        Vector3 infectedCentre, Vector3 playerFeet, out Vector3[] result)
    {
        result = [];
        var from=infectedCentre-Vector3.Up*.8f;
        var bestLength=float.MaxValue;
        foreach (var crossing in Layout.ApproximateCrossings80)
        {
            var mid=crossing.CentreFeet;
            if(Math.Abs(from.X-mid.X)>13.8f ||
               Math.Abs(playerFeet.X-mid.X)>13.8f ||
               Math.Abs(from.Z-mid.Z)>38f ||
               Math.Abs(playerFeet.Z-mid.Z)>38f ||
               Math.Abs(from.Y-mid.Y)>.45f ||
               Math.Abs(playerFeet.Y-mid.Y)>.45f)
                continue;
            // Preserve distinct wings. Opposite-side pursuits must use the
            // physically supported Pass80 bridge, never a phantom corridor.
            var fromSide=Math.Sign(from.X-mid.X);
            var targetSide=Math.Sign(playerFeet.X-mid.X);
            if(fromSide == 0 || fromSide != targetSide) continue;
            var corridorXs=new[]
            {
                mid.X + fromSide*3.2f,
                mid.X + fromSide*4.3f,
                mid.X + fromSide*6.2f,
                from.X,
                playerFeet.X
            };
            foreach(var laneX in corridorXs)
            {
                var a=new Vector3(laneX,from.Y,from.Z);
                var b=new Vector3(laneX,playerFeet.Y,playerFeet.Z);
                if (!UpperCorridorSupported79(from,a) ||
                    !UpperCorridorSupported79(a,b) ||
                    !UpperCorridorSupported79(b,playerFeet))
                    continue;
                var distance=from.DistanceTo(a)+a.DistanceTo(b)+
                    b.DistanceTo(playerFeet);
                if(distance >= bestLength) continue;
                bestLength=distance;
                result=[a,b,playerFeet];
            }
        }
        return result.Length > 0;
    }

    private bool VerifiedPhysicalStair77(Pass77StairLink stair)
    {
        // Bounded (7-ray) physical support audit before issuing a raised
        // route. If a roof, player collider or other wall blocks the ramp,
        // the original ground steering remains authoritative instead.
        var space = GetWorld3D().DirectSpaceState;
        for (var i = 0; i <= 6; i++)
        {
            var expected = stair.LowerFeet.Lerp(stair.UpperFeet, i/6f);
            using var ray = PhysicsRayQueryParameters3D.Create(
                expected + Vector3.Up*.85f,
                expected + Vector3.Down*.70f);
            ray.CollisionMask = WorldCollisionMask;
            ray.Exclude = _exclude;
            var hit = space.IntersectRay(ray);
            if (hit.Count == 0 ||
                ((Vector3)hit["normal"]).Y < .74f ||
                Math.Abs(((Vector3)hit["position"]).Y - expected.Y) > .24f)
                return false;
        }
        return CorridorClear(stair.LowerFeet, stair.UpperFeet);
    }

    private static bool AnchorMatches(Vector3 body, Vector3 feet)
    {
        var dx = body.X - feet.X;
        var dz = body.Z - feet.Z;
        return dx * dx + dz * dz <=
            MaximumAnchorHorizontal * MaximumAnchorHorizontal &&
            Math.Abs(body.Y - (feet.Y + .8f)) <= MaximumAnchorVertical;
    }

    private static bool IsFinite(Vector3 x) =>
        float.IsFinite(x.X) && float.IsFinite(x.Y) &&
        float.IsFinite(x.Z);
}

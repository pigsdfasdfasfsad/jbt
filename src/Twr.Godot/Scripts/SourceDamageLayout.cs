using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Provisional tanker placement, not recovered original objective coordinates.
/// The entire existing 4.8 x 2.2 x 1.9m target must fit, stand on a continuous
/// floor and have a reachable firing position. Query only on a physics tick.
/// No new map floors, remote assets, rewards or player movement are created.
/// </summary>
public sealed record SourceDamageLayout(Vector3 Position, Vector3 FiringPosition,
    Vector3[] PlayerRoute)
{
    public static readonly Vector3 TargetSize = new(4.8f, 2.2f, 1.9f);
    public const int MaximumCandidates = 48;
    public const float MaximumFloorDifference = .18f;
    public const float Clearance = .08f;

    public static SourceDamageLayout? TryPlan(World3D world, Vector3 playerFeet,
        Rid playerRid, IReadOnlyList<Vector3> preferred, IReadOnlyList<Vector3> reserved,
        Pass25SourceNavigationRuntime? navigator = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(preferred);
        ArgumentNullException.ThrowIfNull(reserved);
        if (!playerFeet.IsFinite()) return null;
        var candidates = new List<Vector3>();
        foreach (var point in preferred)
        {
            if (candidates.Count == MaximumCandidates) break;
            candidates.Add(point);
        }
        if (navigator is not null && candidates.Count < MaximumCandidates)
            candidates.AddRange(navigator.GetProvisionalCandidates(playerFeet,
                playerFeet, MaximumCandidates - candidates.Count, 5f, 18f));
        Func<Vector3,Vector3,Vector3[]>? graph = navigator is null ? null : navigator.GetRoute;
        var tried = new List<Vector3>();
        foreach (var candidate in candidates)
        {
            if (!candidate.IsFinite() || tried.Exists(p => p.DistanceSquaredTo(candidate) < .25f)) continue;
            tried.Add(candidate);
            var target = TryGroundFootprint(world, candidate, playerRid);
            if (!target.HasValue || target.Value.DistanceTo(playerFeet) < 5f) continue;
            var occupied = false;
            foreach (var other in reserved)
                if (new Vector2(other.X-target.Value.X,other.Z-target.Value.Z).Length() < 4f)
                    occupied = true;
            if (occupied) continue;
            // Try the near side first, then all other source-neutral sides.
            var stations = new List<Vector3>
            {
                target.Value + new Vector3(0,0,4.5f), target.Value + new Vector3(0,0,-4.5f),
                target.Value + new Vector3(5,0,0), target.Value + new Vector3(-5,0,0)
            };
            stations.Sort((a,b) => a.DistanceSquaredTo(playerFeet).CompareTo(b.DistanceSquaredTo(playerFeet)));
            var exclude = new global::Godot.Collections.Array<Rid> { playerRid };
            foreach (var point in stations)
            {
                var floor = Pass90EscortArrivalSafety.TryGroundDestination(world,point,exclude);
                if (!floor.HasValue || !SourceTraversalRoutes.TryPlan(world,playerFeet,floor.Value,
                    playerRid,graph,out var route) || RouteCrossesTarget(route,target.Value)) continue;
                // Future tanker is not in PhysicsServer yet. A world wall may
                // not block the actual line of fire to its intended centre.
                using var ray = PhysicsRayQueryParameters3D.Create(
                    floor.Value+Vector3.Up*1.58f,target.Value+Vector3.Up*1.1f);
                ray.CollisionMask = 1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
                ray.Exclude = exclude;
                if (world.DirectSpaceState.IntersectRay(ray).Count != 0) continue;
                return new(target.Value, floor.Value, route);
            }
        }
        return null;
    }

    public static Vector3? TryGroundFootprint(World3D world, Vector3 marker, Rid playerRid)
    {
        if (!marker.IsFinite()) return null;
        var exclude = new global::Godot.Collections.Array<Rid> { playerRid };
        var mask = 1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        float highest = float.NegativeInfinity, lowest = float.PositiveInfinity;
        // Centre plus full-width support rows: a player-sized ledge is not a
        // valid platform for the existing tanker collider.
        foreach (var x in new[] {-2.3f,0f,2.3f})
        foreach (var z in new[] {-.85f,0f,.85f})
        {
            var at = marker + new Vector3(x,0,z);
            using var ray = PhysicsRayQueryParameters3D.Create(at+Vector3.Up*.75f,at+Vector3.Down*1.3f);
            ray.CollisionMask=mask; ray.Exclude=exclude;
            var hit=world.DirectSpaceState.IntersectRay(ray);
            if (hit.Count==0 || hit["normal"].AsVector3().Y<.90f) return null;
            var y=hit["position"].AsVector3().Y;
            if (!float.IsFinite(y) || Math.Abs(y-marker.Y)>.85f) return null;
            highest=Math.Max(highest,y); lowest=Math.Min(lowest,y);
        }
        if (highest-lowest>MaximumFloorDifference) return null;
        var origin=new Vector3(marker.X,highest+Clearance,marker.Z);
        using var box = new BoxShape3D { Size=TargetSize+new Vector3(.12f,.04f,.12f) };
        using var query = new PhysicsShapeQueryParameters3D
        {
            Shape=box, Transform=new Transform3D(Basis.Identity,origin+Vector3.Up*1.1f),
            CollisionMask=mask, CollideWithBodies=true, Exclude=exclude
        };
        return world.DirectSpaceState.IntersectShape(query,1).Count==0 ? origin : null;
    }

    /// <summary>Sweep player foot-route segments against the FUTURE collider.
    /// World raycasts alone cannot see a tanker that has not been instantiated.</summary>
    public static bool RouteCrossesTarget(IReadOnlyList<Vector3> route, Vector3 target)
    {
        var low=target+new Vector3(-2.75f,-1.8f,-1.30f);
        var high=target+new Vector3(2.75f,2.2f,1.30f);
        for (var i=1;i<route.Count;i++)
        {
            float enter=0,exit=1;
            var a=route[i-1]; var d=route[i]-a;
            var intersects=true;
            for (var axis=0;axis<3;axis++)
            {
                if (Math.Abs(d[axis])<.00001f)
                {
                    if (a[axis]<low[axis] || a[axis]>high[axis]) { intersects=false; break; }
                }
                else
                {
                    var one=(low[axis]-a[axis])/d[axis]; var two=(high[axis]-a[axis])/d[axis];
                    enter=Math.Max(enter,Math.Min(one,two)); exit=Math.Min(exit,Math.Max(one,two));
                    if (enter>exit) { intersects=false; break; }
                }
            }
            if (intersects) return true;
        }
        return false;
    }
}

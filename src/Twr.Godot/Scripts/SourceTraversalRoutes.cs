using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Reconstructed walking routes, never original Roblox navigation claims.
/// Graph connectivity is only a proposal: validate every leg, including
/// approach and arrival, against current world collision in a physics tick.
/// A partial/truncated graph path must never authorize an unreachable goal.
/// </summary>
public static class SourceTraversalRoutes
{
    public const int MaximumWaypoints = 160;
    public const float MaximumLength = 75f;
    public const float MaximumAnchorGap = 2.25f;

    public static bool TryPlan(World3D world, Vector3 from, Vector3 to,
        Rid playerRid, Func<Vector3, Vector3, Vector3[]>? graphRoute,
        out Vector3[] route)
    {
        route = [];
        if (!from.IsFinite() || !to.IsFinite()) return false;
        var exclude = new global::Godot.Collections.Array<Rid> { playerRid };
        var start = Pass90EscortArrivalSafety.TryGroundDestination(world, from, exclude);
        var end = Pass90EscortArrivalSafety.TryGroundDestination(world, to, exclude);
        if (!start.HasValue || !end.HasValue) return false;
        if (CanWalk(world, start.Value, end.Value, exclude))
        {
            route = [start.Value, end.Value];
            return true;
        }
        if (graphRoute is null) return false;
        var proposed = graphRoute(start.Value, end.Value);
        if (proposed is null || proposed.Length < 2 || proposed.Length > MaximumWaypoints ||
            proposed.Any(p => !p.IsFinite()) ||
            proposed[0].DistanceTo(start.Value) > MaximumAnchorGap ||
            proposed[^1].DistanceTo(end.Value) > MaximumAnchorGap)
            return false;

        var grounded = new List<Vector3>(proposed.Length + 2) { start.Value };
        foreach (var point in proposed)
        {
            var floor = Pass90EscortArrivalSafety.TryGroundDestination(world, point, exclude);
            if (!floor.HasValue || Math.Abs(floor.Value.Y - point.Y) > .50f)
                return false;
            if (floor.Value.DistanceSquaredTo(grounded[^1]) > .0064f)
                grounded.Add(floor.Value);
        }
        if (end.Value.DistanceSquaredTo(grounded[^1]) > .0025f)
            grounded.Add(end.Value);
        float length = 0;
        for (var i = 1; i < grounded.Count; i++)
        {
            length += grounded[i - 1].DistanceTo(grounded[i]);
            if (length > MaximumLength ||
                !CanWalk(world, grounded[i - 1], grounded[i], exclude))
                return false;
        }
        // Simplify collinear graph cells only AFTER all original legs pass.
        // Do not smooth corners: that would cut through real source walls.
        var reduced = new List<Vector3> { grounded[0] };
        for (var i = 1; i < grounded.Count - 1; i++)
        {
            var a = (grounded[i] - reduced[^1]).Normalized();
            var b = (grounded[i + 1] - grounded[i]).Normalized();
            if (a.Dot(b) < .9999f) reduced.Add(grounded[i]);
        }
        reduced.Add(grounded[^1]);
        route = reduced.ToArray();
        return true;
    }

    private static bool CanWalk(World3D world, Vector3 a, Vector3 b,
        global::Godot.Collections.Array<Rid> exclude) =>
        a.DistanceTo(b) <= MaximumLength &&
        Pass89EscortSurfaceSafety.CanTravel(world, a, b, exclude) &&
        Pass89EscortSurfaceSafety.CanTravel(world, b, a, exclude);
}

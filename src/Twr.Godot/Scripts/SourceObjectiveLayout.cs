using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// A provisional objective with a verified player route and, for Load/Repair,
/// a complete round-trip material layout. Nothing here is recovered retail
/// objective placement. Original player/enemy/item markers remain untouched.
/// </summary>
public sealed record SourceObjectiveLayout(Vector3 Position, Vector3[] PlayerRoute,
    Vector3[] MaterialFeet, Vector3[][] MaterialRoutes)
{
    public static SourceObjectiveLayout? TryPlan(World3D world, Vector3 playerFeet,
        Rid playerRid, string family, IReadOnlyList<Vector3> preferred,
        IReadOnlyList<Vector3> reserved, Pass25SourceNavigationRuntime? navigator,
        bool largeRepair = true)
    {
        Func<Vector3, Vector3, Vector3[]>? graph = navigator is null ? null : navigator.GetRoute;
        var candidates = new List<Vector3>(preferred);
        if (navigator is not null)
        {
            candidates.AddRange(navigator.GetProvisionalCandidates(playerFeet,
                preferred.Count == 0 ? playerFeet : preferred[0], 24, 4f, 20f));
            // A whole ring around the player prevents a blocked preferred
            // doorway from monopolizing the finite candidate budget.
            candidates.AddRange(navigator.GetProvisionalCandidates(playerFeet,
                playerFeet, 24, 4f, 12f));
        }
        var tried = new List<Vector3>();
        var attemptedDeposits = 0;
        foreach (var marker in candidates)
        {
            if (tried.Exists(p => p.DistanceSquaredTo(marker) < .16f)) continue;
            tried.Add(marker);
            var grounded = Pass122ProvisionalObjectiveSafety.TryFind(world,
                new[] { marker }, 0, playerFeet, playerRid);
            if (!grounded.HasValue) continue;
            var position = grounded.Value;
            if (Overlaps(reserved, position, 3.0f) ||
                !SourceTraversalRoutes.TryPlan(world, playerFeet, position,
                    playerRid, graph, out var playerRoute)) continue;
            attemptedDeposits++;
            var count = family == "Load" ? 10 : family == "Repair" ? (largeRepair ? 12 : 10) : 0;
            if (count == 0) return new(position, playerRoute, [], []);
            var material = new List<Vector3>();
            var routes = new List<Vector3[]>();
            var materialCandidates = new List<Vector3>();
            // Source-neutral layout around a physically supported deposit.
            // The graph adds detours around walls; a radius alone is not proof.
            foreach (var radius in new[] { 2f, 3.5f, 5f })
                for (var i = 0; i < 24; i++)
                {
                    var angle = Mathf.Tau * i / 24;
                    materialCandidates.Add(position + new Vector3(
                        Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
                }
            if (navigator is not null)
                materialCandidates.AddRange(navigator.GetProvisionalCandidates(position,
                    position, 48, 1.5f, 8f));
            foreach (var at in materialCandidates)
            {
                var exclude = new global::Godot.Collections.Array<Rid> { playerRid };
                var floor = Pass90EscortArrivalSafety.TryGroundDestination(world, at, exclude);
                if (!floor.HasValue || Overlaps(material, floor.Value, .95f) ||
                    floor.Value.DistanceTo(position) < 1.25f) continue;
                if (!SourceTraversalRoutes.TryPlan(world, position, floor.Value,
                    playerRid, graph, out var route)) continue;
                material.Add(floor.Value);
                routes.Add(route);
                if (material.Count == count)
                    return new(position, playerRoute, material.ToArray(), routes.ToArray());
            }
            // Incomplete material circuits must not start with unreachable
            // props or fake completion. Try a different usable depot instead.
            if (attemptedDeposits >= 6) break;
        }
        return null;
    }

    private static bool Overlaps(IReadOnlyList<Vector3> points, Vector3 p, float gap)
    {
        foreach (var old in points)
            if (new Vector2(old.X-p.X, old.Z-p.Z).LengthSquared() < gap*gap)
                return true;
        return false;
    }
}

using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// The owner-held Laboratory map currently has collision support near only
/// eight of fifteen original infected spawn markers. This helper is used ONLY
/// when players explicitly enable F9 accessibility. Never mutates the source
/// spawn records and never creates invented geometry.
/// </summary>
public static class Pass32SpawnSafety
{
    public const float MinimumPlayerDistance = 24f;
    public const float MaximumFloorOffset = .70f;
    private const uint WorldMask = 1;
    private const float MinWalkableNormalY = .69f;
    private const int MaximumCandidates = 32;

    public static Vector3? FindSupportedPlacement(World3D world,
        IEnumerable<Vector3> candidateCenters, Vector3 playerPosition)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(candidateCenters);
        var space = world.DirectSpaceState;
        using var capsule = new CapsuleShape3D { Radius = .30f, Height = 1.60f };
        var examined = 0;
        foreach (var candidate in candidateCenters)
        {
            if (++examined > MaximumCandidates) break;
            if (!IsFinite(candidate) ||
                candidate.DistanceSquaredTo(playerPosition) <
                    MinimumPlayerDistance * MinimumPlayerDistance)
                continue;

            // Nav waypoints are feet points; candidates are initial capsule
            // centres 0.8 m above them. Cast across a short source-height
            // window only. A roof six metres below must NOT pass as the floor.
            var feet = candidate.Y - .8f;
            var from = new Vector3(candidate.X, feet + 1.30f, candidate.Z);
            var to = new Vector3(candidate.X, feet - 1.00f, candidate.Z);
            var ray = PhysicsRayQueryParameters3D.Create(from, to);
            ray.CollisionMask = WorldMask;
            var floorHit = space.IntersectRay(ray);
            if (floorHit.Count == 0) continue;
            var floorNormal = (Vector3)floorHit["normal"];
            var floorPosition = (Vector3)floorHit["position"];
            if (floorNormal.Y < MinWalkableNormalY ||
                Math.Abs(floorPosition.Y - feet) > MaximumFloorOffset)
                continue;

            // Capsule base clears the measured floor by 0.10 m. Reject world
            // geometry overlapping its standing volume (walls, fences, roof).
            var standing = new Vector3(
                floorPosition.X, floorPosition.Y + .90f, floorPosition.Z);
            var shapeQuery = new PhysicsShapeQueryParameters3D
            {
                Shape = capsule,
                Transform = new Transform3D(Basis.Identity, standing),
                CollisionMask = WorldMask,
                CollideWithBodies = true
            };
            if (space.IntersectShape(shapeQuery, 1).Count != 0) continue;
            return standing;
        }
        return null;
    }

    private static bool IsFinite(Vector3 position) =>
        float.IsFinite(position.X) &&
        float.IsFinite(position.Y) &&
        float.IsFinite(position.Z);
}

/// <summary>
/// Bounded rescue from already-stranded enemies. No periodic teleports in
/// source-exact default mode; the player must first enable F9. The counters
/// are per enemy, preventing loops or infinite behind-the-player respawns.
/// </summary>
public static class Pass32RescuePolicy
{
    public const int MaximumRescuesPerEnemy = 2;
    public const double StallThresholdSeconds = 7.5;
    public const double MinimumSecondsBetweenRescues = 18.0;

    public static bool CanRescue(float horizontalDistance,
        float enemyHeightRelativeToPlayer, double stationarySeconds,
        int previousRescues, double secondsSinceLastRescue)
    {
        if (!float.IsFinite(horizontalDistance) ||
            !float.IsFinite(enemyHeightRelativeToPlayer) ||
            !double.IsFinite(stationarySeconds) ||
            !double.IsFinite(secondsSinceLastRescue) ||
            previousRescues < 0 || previousRescues >= MaximumRescuesPerEnemy)
            return false;
        return horizontalDistance >= 7f &&
            secondsSinceLastRescue >= MinimumSecondsBetweenRescues &&
            (stationarySeconds >= StallThresholdSeconds ||
             enemyHeightRelativeToPlayer < -9f);
    }
}

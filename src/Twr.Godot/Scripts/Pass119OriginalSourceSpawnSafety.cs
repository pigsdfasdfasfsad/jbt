using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Validate ORIGINAL infected feet markers against Godot collision.
/// No invented X/Z, terrain or original mesh bytes; unsupported locations
/// are skipped rather than allowing a character to fall through the map.
/// </summary>
public static class Pass119OriginalSourceSpawnSafety
{
    public const int MaximumSourceMarkers = 64;
    public const float MaximumSourceFootHeightError = .70f;
    public const float MinimumFloorNormal = .74f;
    public const float MinimumPlanarPlayerDistance = 2.5f;
    private const uint WorldLayer = 1u;

    public static Vector3? TryFind(
        World3D world, IReadOnlyList<Vector3> originalFeet,
        Vector3 selectedCentre, Vector3 playerFeet, Rid playerRid,
        out bool alternateOriginalMarker)
    {
        alternateOriginalMarker = false;
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(originalFeet);
        if (originalFeet.Count == 0 || originalFeet.Count > MaximumSourceMarkers ||
            !Finite(selectedCentre) || !Finite(playerFeet))
            return null;

        // Do not accept an arbitrary caller-provided location masquerading
        // as a source marker. RandomSpawnPoint adds exactly 0.8m to its feet.
        var sourceFeet = selectedCentre - Vector3.Up * .8f;
        var first = -1;
        for (var i = 0; i < originalFeet.Count; i++)
            if (Finite(originalFeet[i]) &&
                originalFeet[i].DistanceSquaredTo(sourceFeet) < .0001f)
            {
                first = i;
                break;
            }
        if (first < 0) return null;

        using var capsule = new CapsuleShape3D
        {
            Radius = .30f,
            Height = 1.60f
        };
        var exclude = new global::Godot.Collections.Array<Rid> { playerRid };
        var space = world.DirectSpaceState;

        for (var attempt = 0; attempt < originalFeet.Count; attempt++)
        {
            var markerIndex = (first + attempt) % originalFeet.Count;
            var marker = originalFeet[markerIndex];
            if (!Finite(marker)) continue;

            var planar = new Vector2(
                marker.X - playerFeet.X, marker.Z - playerFeet.Z);
            if (planar.LengthSquared() <
                MinimumPlanarPlayerDistance * MinimumPlanarPlayerDistance)
                continue;

            // Sample only around the original feet elevation. A roof or
            // basement multiple meters below is not authentic floor support.
            using var ray = PhysicsRayQueryParameters3D.Create(
                marker + Vector3.Up * 1.0f,
                marker + Vector3.Down * .80f);
            ray.CollisionMask = WorldLayer;
            ray.Exclude = exclude;
            var hit = space.IntersectRay(ray);
            if (hit.Count == 0 ||
                ((Vector3)hit["normal"]).Y < MinimumFloorNormal)
                continue;
            var floor = (Vector3)hit["position"];
            if (Math.Abs(floor.Y - marker.Y) > MaximumSourceFootHeightError)
                continue;

            // Body origin is 0.9m above floor: 1.6m capsule has 0.1m
            // clearance. X and Z are the selected original marker's exact.
            var centre = new Vector3(marker.X, floor.Y + .90f, marker.Z);
            using var shape = new PhysicsShapeQueryParameters3D
            {
                Shape = capsule,
                Transform = new Transform3D(Basis.Identity, centre),
                CollisionMask = WorldLayer,
                CollideWithBodies = true,
                Exclude = exclude
            };
            if (space.IntersectShape(shape, 1).Count != 0)
                continue;
            alternateOriginalMarker = markerIndex != first;
            return centre;
        }
        return null;
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);
}

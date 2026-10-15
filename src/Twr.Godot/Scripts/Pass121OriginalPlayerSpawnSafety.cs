using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass121: SOURCE-ONLY player spawn clearance. A source pack may carry eight
/// authentic Roblox PlayerSpawns yet have incomplete Godot MeshPart/terrain
/// collision. Select strictly among the imported original source markers;
/// never teleport to invented map coordinates or unrelated lower floors.
/// The runtime calls this after PhysicsServer has registered world collision
/// and excludes its own player character RID from the clearance query.
/// </summary>
public static class Pass121OriginalPlayerSpawnSafety
{
    public const int MaximumOriginalPlayerMarkers = 64;
    public const float OriginalMarkerPlayerLift = .5f;
    public const float MaximumSourceFloorOffset = .95f;
    public const float MinimumWalkableNormalY = .74f;
    public const float FootClearance = .08f;
    private const uint SourceSolidCollisionLayer = 1u;
    private static readonly Vector2[] SupportOffsets =
    [
        Vector2.Zero, new(.21f, 0f), new(-.21f, 0f),
        new(0f, .21f), new(0f, -.21f)
    ];

    public static Vector3? TryFind(
        World3D world, IReadOnlyList<Vector3> originalFootMarkers,
        Vector3 selectedPlayerOrigin, Rid playerRid,
        out bool alternateOriginalMarker, out int rejectedCandidates)
    {
        alternateOriginalMarker = false;
        rejectedCandidates = 0;
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(originalFootMarkers);
        if (originalFootMarkers.Count == 0 ||
            originalFootMarkers.Count > MaximumOriginalPlayerMarkers ||
            !Finite(selectedPlayerOrigin))
            return null;

        // Fail closed if selectedPlayerOrigin is NOT derived from the
        // source's named SpawnPart, before even querying the world.
        var selectedMarker = selectedPlayerOrigin -
            Vector3.Up * OriginalMarkerPlayerLift;
        var first = -1;
        for (var i = 0; i < originalFootMarkers.Count; i++)
        {
            if (Finite(originalFootMarkers[i]) &&
                originalFootMarkers[i].DistanceSquaredTo(selectedMarker) < .0001f)
            {
                first = i;
                break;
            }
        }
        if (first < 0) return null;

        var worldSpace = world.DirectSpaceState;
        var excluded = new global::Godot.Collections.Array<Rid> { playerRid };
        using var capsule = new CapsuleShape3D
        {
            Radius = .29f, Height = 1.80f
        };

        for (var attempt = 0; attempt < originalFootMarkers.Count; attempt++)
        {
            var index = (first + attempt) % originalFootMarkers.Count;
            var originalFeet = originalFootMarkers[index];
            if (!Finite(originalFeet))
            {
                rejectedCandidates++;
                continue;
            }
            // Only sample near the actual source CFrame's Y layer. Missing
            // terrain cannot be replaced by a far-away basement slab.
            using var centerRay = PhysicsRayQueryParameters3D.Create(
                originalFeet + Vector3.Up * 1.10f,
                originalFeet + Vector3.Down * 1.60f);
            centerRay.CollisionMask = SourceSolidCollisionLayer;
            centerRay.Exclude = excluded;
            var hit = worldSpace.IntersectRay(centerRay);
            if (hit.Count == 0 ||
                ((Vector3)hit["normal"]).Y < MinimumWalkableNormalY)
            {
                rejectedCandidates++;
                continue;
            }
            var floor = (Vector3)hit["position"];
            if (!Finite(floor) ||
                Math.Abs(floor.Y - originalFeet.Y) > MaximumSourceFloorOffset)
            {
                rejectedCandidates++;
                continue;
            }

            // At least a player-sized underfoot footprint must be
            // continuously supported, not a small mesh corner or ledge.
            var supported = true;
            foreach (var offset in SupportOffsets)
            {
                using var ray = PhysicsRayQueryParameters3D.Create(
                    new Vector3(originalFeet.X + offset.X,
                        floor.Y + .55f, originalFeet.Z + offset.Y),
                    new Vector3(originalFeet.X + offset.X,
                        floor.Y - .70f, originalFeet.Z + offset.Y));
                ray.CollisionMask = SourceSolidCollisionLayer;
                ray.Exclude = excluded;
                var support = worldSpace.IntersectRay(ray);
                if (support.Count == 0 ||
                    ((Vector3)support["normal"]).Y < MinimumWalkableNormalY ||
                    Math.Abs(((Vector3)support["position"]).Y - floor.Y) > .32f)
                {
                    supported = false;
                    break;
                }
            }
            if (!supported)
            {
                rejectedCandidates++;
                continue;
            }

            // FirstPersonPlayer has a 1.8m-high capsule centered +.9m
            // above its Node3D origin. Check exact production dimensions.
            var origin = new Vector3(
                originalFeet.X, floor.Y + FootClearance, originalFeet.Z);
            using var standing = new PhysicsShapeQueryParameters3D
            {
                Shape = capsule,
                Transform = new Transform3D(Basis.Identity,
                    origin + Vector3.Up * .9f),
                CollisionMask = SourceSolidCollisionLayer,
                CollideWithBodies = true,
                Exclude = excluded
            };
            if (worldSpace.IntersectShape(standing, 1).Count > 0)
            {
                rejectedCandidates++;
                continue;
            }
            alternateOriginalMarker = index != first;
            return origin;
        }
        return null;
    }

    private static bool Finite(Vector3 p) =>
        float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
}

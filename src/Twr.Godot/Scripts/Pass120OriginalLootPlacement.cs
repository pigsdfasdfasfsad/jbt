using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass120: validate original source item and fortification spawn markers
/// against the currently recovered offline WORLD collision. Never move X/Z
/// or pretend unrecovered terrain and cloud MeshParts have real collision.
/// The resulting centre is render-only pickup placement, not a new source
/// marker. The marker itself must remain unchanged for respawn scheduling.
/// </summary>
public static class Pass120OriginalLootPlacement
{
    public const float MaximumOriginalFloorGap = 1.35f;
    public const float MinimumWalkableNormalY = .70f;
    public const float PickupCentreAboveFloor = .62f;
    private const uint SolidWorldLayer = 1u;

    public static Vector3? TryGround(World3D world, Vector3 originalMarker)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!Finite(originalMarker)) return null;
        var space = world.DirectSpaceState;

        // The original ItemPickupBox / FortificationPickupBox CFrame is a
        // position marker, NOT proof that its collision is reconstructed.
        // Restrict the floor search to a small original-elevation interval;
        // a distant lower deck must not fake a supported pickup.
        using var floorRay = PhysicsRayQueryParameters3D.Create(
            originalMarker + Vector3.Up * 1.0f,
            originalMarker + Vector3.Down * 1.55f);
        floorRay.CollisionMask = SolidWorldLayer;
        var hit = space.IntersectRay(floorRay);
        if (hit.Count == 0 ||
            ((Vector3)hit["normal"]).Y < MinimumWalkableNormalY)
            return null;

        var floor = (Vector3)hit["position"];
        if (!Finite(floor) ||
            Math.Abs(floor.Y - originalMarker.Y) > MaximumOriginalFloorGap)
            return null;

        // Original X/Z is invariant. The physical pickup is a 0.55m-wide
        // visual object and must not intersect static walls or furniture.
        var centre = new Vector3(
            originalMarker.X, floor.Y + PickupCentreAboveFloor,
            originalMarker.Z);
        using var pickupVolume = new BoxShape3D
        {
            Size = new Vector3(.58f, .55f, .58f)
        };
        using var freeSpace = new PhysicsShapeQueryParameters3D
        {
            Shape = pickupVolume,
            Transform = new Transform3D(Basis.Identity, centre),
            CollisionMask = SolidWorldLayer,
            CollideWithBodies = true
        };
        if (space.IntersectShape(freeSpace, 1).Count > 0)
            return null;
        return centre;
    }

    private static bool Finite(Vector3 point) =>
        float.IsFinite(point.X) &&
        float.IsFinite(point.Y) &&
        float.IsFinite(point.Z);
}

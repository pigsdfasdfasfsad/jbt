using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Reject fortification placement in the void or inside solid geometry before
/// the authoritative inventory ConsumeItem command. Uses the live Godot
/// colliders of the selected map; source geometry is never modified.
/// </summary>
public static class Pass76PlacementSafety
{
    private const uint WorldMask =
        1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
    private static readonly BoxShape3D Footprint = new()
    {
        Size = new Vector3(.80f, .70f, .80f)
    };

    public static bool TryFindGround(FirstPersonPlayer player,
        Vector3 candidate, out Vector3 result)
    {
        result = candidate;
        ArgumentNullException.ThrowIfNull(player);
        if (!GodotObject.IsInstanceValid(player) ||
            !float.IsFinite(candidate.X) ||
            !float.IsFinite(candidate.Y) ||
            !float.IsFinite(candidate.Z))
            return false;

        var world = player.GetWorld3D().DirectSpaceState;
        var exclude = new global::Godot.Collections.Array<Rid>
        {
            player.GetRid()
        };
        var floorQuery = PhysicsRayQueryParameters3D.Create(
            candidate + Vector3.Up * 1.6f,
            candidate + Vector3.Down * 2.5f);
        floorQuery.CollisionMask = WorldMask;
        floorQuery.Exclude = exclude;
        var hit = world.IntersectRay(floorQuery);
        if (hit.Count == 0 || ((Vector3)hit["normal"]).Y < .70f)
            return false;
        var ground = (Vector3)hit["position"];
        if (Math.Abs(ground.Y - (player.GlobalPosition.Y - .85f)) > 2.0f)
            return false;

        var space = new PhysicsShapeQueryParameters3D
        {
            Shape = Footprint,
            Transform = new Transform3D(Basis.Identity,
                ground + Vector3.Up * .48f),
            CollisionMask = WorldMask,
            CollideWithBodies = true,
            Exclude = exclude
        };
        if (world.IntersectShape(space, 1).Count != 0)
            return false;
        result = ground + Vector3.Up * .04f;
        return true;
    }
}

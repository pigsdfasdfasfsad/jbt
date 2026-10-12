using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass75: solid-world visibility for instantaneous launcher and Frag splash.
/// Blast radius alone cannot establish a hit through a wall. Source server
/// colliders, if loaded and activated, are included in the occlusion mask.
/// No original mesh, Roblox code, or player inventory is altered.
/// </summary>
public static class Pass75BlastOcclusion
{
    private const uint SolidWorldMask =
        1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;

    public static bool CanReachTarget(World3D world, Vector3 blast,
        CollisionObject3D target, float radiusMetres,
        CollisionObject3D? sourcePlayer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        if (!GodotObject.IsInstanceValid(target) ||
            !Finite(blast) || !Finite(target.GlobalPosition) ||
            !float.IsFinite(radiusMetres) || radiusMetres <= 0f)
            return false;

        var delta = target.GlobalPosition - blast;
        if (delta.Length() > radiusMetres) return false;

        // The objective's node origin is below its collision-box centre.
        // Sample actual central mass instead of raycasting below a wall.
        var targetCentre = target is DamageObjectiveTarget
            ? target.GlobalPosition + Vector3.Up * 1.10f
            : target.GlobalPosition + Vector3.Up * .10f;
        // Slightly lift the blast ray off floor contact; surface detonations
        // from rockets are separately offset toward the incoming face.
        var from = blast + Vector3.Up * .15f;
        var query = PhysicsRayQueryParameters3D.Create(from, targetCentre);
        query.CollisionMask = SolidWorldMask;
        var excluded = new global::Godot.Collections.Array<Rid>
        {
            target.GetRid()
        };
        if (sourcePlayer is not null &&
            GodotObject.IsInstanceValid(sourcePlayer))
            excluded.Add(sourcePlayer.GetRid());
        query.Exclude = excluded;

        return world.DirectSpaceState.IntersectRay(query).Count == 0;
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);
}

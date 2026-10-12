using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass74 physical safety gates for reconstructed special-infected attacks.
/// The original Roblox numeric path/trajectory parameters are not recoverable.
/// These checks do not spawn, teleport or move any actor: existing Godot
/// collision remains authoritative. They also do not change source map data.
/// </summary>
public static class Pass74SpecialInfectedSafety
{
    private const uint SolidWorldMask =
        1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
    private const float WalkableNormalY = .70f;
    private const float MaximumFloorOffset = .65f;

    /// <summary>
    /// Requires physical walkable ground at the *target's* feet. A Bolter
    /// cannot launch a jump toward an unsupported airborne player, roof
    /// projection or missing floor. No artificial landing is created.
    /// </summary>
    public static bool HasSupportedLanding(World3D world, Vector3 targetFeet,
        Rid attacker, Rid target)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!Finite(targetFeet)) return false;
        var start = targetFeet + Vector3.Up * 1.25f;
        var finish = targetFeet + Vector3.Down * 1.2f;
        var ray = PhysicsRayQueryParameters3D.Create(start, finish);
        ray.CollisionMask = SolidWorldMask;
        ray.Exclude = new global::Godot.Collections.Array<Rid>
        {
            attacker, target
        };
        var hit = world.DirectSpaceState.IntersectRay(ray);
        if (hit.Count == 0) return false;
        var normal = (Vector3)hit["normal"];
        var floor = (Vector3)hit["position"];
        return normal.Y >= WalkableNormalY &&
            Math.Abs(floor.Y - targetFeet.Y) <= MaximumFloorOffset;
    }

    /// <summary>
    /// Burster's instant detonation must obey solid walls just as the later
    /// spore cloud already does. A wall cannot be damaged through merely
    /// because the victim is inside the explosion's nominal radius.
    /// </summary>
    public static bool CanHitPlayerWithBlast(World3D world, Vector3 origin,
        FirstPersonPlayer player, float radiusMetres)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(player);
        if (!GodotObject.IsInstanceValid(player) || !Finite(origin) ||
            !Finite(player.GlobalPosition) || !float.IsFinite(radiusMetres) ||
            radiusMetres <= 0f)
            return false;
        if (player.GlobalPosition.DistanceTo(origin) > radiusMetres)
            return false;

        var start = origin + Vector3.Up * .15f;
        var target = player.GlobalPosition + Vector3.Up * .85f;
        var ray = PhysicsRayQueryParameters3D.Create(start, target);
        ray.CollisionMask = SolidWorldMask;
        ray.Exclude = new global::Godot.Collections.Array<Rid>
        {
            player.GetRid()
        };
        return world.DirectSpaceState.IntersectRay(ray).Count == 0;
    }

    private static bool Finite(Vector3 p) =>
        float.IsFinite(p.X) && float.IsFinite(p.Y) &&
        float.IsFinite(p.Z);
}

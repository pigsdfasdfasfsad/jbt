using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass76 world-aware E-key pickup / objective interaction. A player cannot
/// collect a pickup or deposit materials on the far side of a solid world wall.
/// When multiple pickups overlap the E range, only the closest visible
/// candidate handles that press; other physical pickups remain for later.
/// Source collision walls, if enabled by the owner, are included.
/// </summary>
public static class Pass76InteractionSafety
{
    private const uint WorldMask =
        1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;

    // An earlier pickup may QueueFree during this same physics/render frame.
    // Candidate-order checks alone would let the NEXT pickup claim the
    // SAME held E key. Keep one authoritative local press budget per player.
    private static readonly System.Collections.Generic.HashSet<ulong>
        ClaimedPickupPresses = [];

    public static void ObservePickupKey(FirstPersonPlayer player, bool down)
    {
        if (!down) ClaimedPickupPresses.Remove(player.GetInstanceId());
    }

    public static void ClaimPickupPress(FirstPersonPlayer player)
    {
        ClaimedPickupPresses.Add(player.GetInstanceId());
    }

    public static void ForgetPlayer(FirstPersonPlayer player)
    {
        ClaimedPickupPresses.Remove(player.GetInstanceId());
    }

    public static bool CanReach(FirstPersonPlayer player, Vector3 target,
        float radius)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!GodotObject.IsInstanceValid(player) ||
            !float.IsFinite(radius) || radius <= 0 ||
            !Finite(target) || !Finite(player.GlobalPosition))
            return false;
        if (player.GlobalPosition.DistanceTo(target) > radius)
            return false;

        var ray = PhysicsRayQueryParameters3D.Create(
            player.AimOrigin, target + Vector3.Up * .25f);
        ray.CollisionMask = WorldMask;
        ray.Exclude = new global::Godot.Collections.Array<Rid>
        {
            player.GetRid()
        };
        return player.GetWorld3D().DirectSpaceState.IntersectRay(ray).Count == 0;
    }

    public static bool IsNearestPickup(PickupActor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var player = candidate.Player;
        if (player is null ||
            candidate.Runtime?.Player?.IsAlive != true ||
            ClaimedPickupPresses.Contains(player.GetInstanceId()) ||
            !CanReach(player, candidate.GlobalPosition, 2.5f))
            return false;

        var candidateDistance = player.GlobalPosition.DistanceSquaredTo(
            candidate.GlobalPosition);
        foreach (var node in candidate.GetTree().GetNodesInGroup("twr-pickup"))
        {
            if (node is not PickupActor other ||
                ReferenceEquals(other, candidate) ||
                !GodotObject.IsInstanceValid(other) ||
                other.IsQueuedForDeletion() ||
                !ReferenceEquals(other.Player, player) ||
                !ReferenceEquals(other.Runtime, candidate.Runtime) ||
                !CanReach(player, other.GlobalPosition, 2.5f))
                continue;

            var distance = player.GlobalPosition.DistanceSquaredTo(
                other.GlobalPosition);
            // Stable tie-break: exactly one eligible pickup per E press,
            // regardless of child process order in a live Godot scene.
            if (distance + .0001f < candidateDistance ||
                (Math.Abs(distance - candidateDistance) <= .0001f &&
                    other.GetInstanceId() < candidate.GetInstanceId()))
                return false;
        }
        return true;
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);
}

using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass123: physically reachable, non-overlapping placement for the
/// source loader's PROVISIONAL objectives only. These points are NOT
/// original Roblox objective markers or verified retail navigation.
/// The existing Pass122 grounding check stays authoritative; after
/// grounding, require a continuous static-world, full-capsule corridor
/// in BOTH directions from the verified human spawn. No path is invented
/// through unknown source geometry.
/// Call ONLY inside a Godot physics-frame callback.
/// </summary>
public static class Pass123ProvisionalObjectiveRouting
{
    public const int MaximumProvisionalAnchors = 16;
    public const float MinimumObjectiveSeparation = 3.0f;

    public static Vector3? TryFind(
        World3D world,
        IReadOnlyList<Vector3> provisionalAnchors,
        int preferredIndex,
        Vector3 verifiedPlayerFeet,
        Rid playerRid,
        IReadOnlyList<Vector3> alreadyPlaced,
        out bool choseAlternate,
        out int rejectedCandidates,
        out int duplicateCandidates)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(provisionalAnchors);
        ArgumentNullException.ThrowIfNull(alreadyPlaced);
        choseAlternate = false;
        rejectedCandidates = 0;
        duplicateCandidates = 0;

        if (provisionalAnchors.Count is < 1 or > MaximumProvisionalAnchors ||
            preferredIndex < 0 ||
            !Finite(verifiedPlayerFeet) ||
            alreadyPlaced.Count >= MaximumProvisionalAnchors)
            return null;

        var exclude = new global::Godot.Collections.Array<Rid>
        {
            playerRid
        };
        for (var attempt = 0; attempt < provisionalAnchors.Count; attempt++)
        {
            var index = (preferredIndex + attempt) % provisionalAnchors.Count;
            var marker = provisionalAnchors[index];
            if (!Finite(marker))
            {
                rejectedCandidates++;
                continue;
            }

            // Single marker list: do not allow Pass122 to silently return
            // an unexamined neighboring objective during reachability audit.
            var grounded = Pass122ProvisionalObjectiveSafety.TryFind(
                world, new[] { marker }, 0, verifiedPlayerFeet, playerRid);
            if (!grounded.HasValue)
            {
                rejectedCandidates++;
                continue;
            }

            var tooCloseToAnother = false;
            foreach (var previous in alreadyPlaced)
            {
                var delta = new Vector2(
                    grounded.Value.X - previous.X,
                    grounded.Value.Z - previous.Z);
                if (delta.LengthSquared() <
                    MinimumObjectiveSeparation * MinimumObjectiveSeparation ||
                    Math.Abs(grounded.Value.Y - previous.Y) > 1.10f)
                {
                    tooCloseToAnother = true;
                    break;
                }
            }
            if (tooCloseToAnother)
            {
                duplicateCandidates++;
                continue;
            }

            // This checks an uninterrupted direct walking corridor, not
            // an inferred A* route around unknown walls. It is deliberately
            // conservative when original source meshes are missing.
            if (!Pass89EscortSurfaceSafety.CanTravel(
                    world, verifiedPlayerFeet, grounded.Value, exclude) ||
                !Pass89EscortSurfaceSafety.CanTravel(
                    world, grounded.Value, verifiedPlayerFeet, exclude))
            {
                rejectedCandidates++;
                continue;
            }

            choseAlternate = index != preferredIndex % provisionalAnchors.Count;
            return grounded.Value;
        }

        return null;
    }

    private static bool Finite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) &&
        float.IsFinite(v.Z);
}

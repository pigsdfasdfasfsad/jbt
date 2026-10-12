using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass85 rejects player-adjacent, wall-embedded or airborne spawn candidates
/// for public approximate maps, WITHOUT changing owner-authored source
/// markers. The collision world is authoritative for all accepted locations.
/// </summary>
public static class Pass85FallbackInfectedSpawn
{
    public const float MinimumPlanarPlayerDistance = 11f;
    public const float MaximumFloorAdjustment = .85f;
    public const int MaximumCandidates = 24;

    public static Vector3? TryFind(
        World3D world, IEnumerable<Vector3> proposedCenters,
        Vector3 playerFeet, Rid playerRid)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(proposedCenters);
        if (!Finite(playerFeet)) return null;
        using var capsule = new CapsuleShape3D
        {
            Radius = .30f, Height = 1.60f
        };
        var space=world.DirectSpaceState;
        var checkedCount=0;
        foreach(var candidate in proposedCenters)
        {
            if(++checkedCount>MaximumCandidates) break;
            if(!Finite(candidate)) continue;
            var planar=new Vector2(candidate.X-playerFeet.X,
                candidate.Z-playerFeet.Z);
            if(planar.LengthSquared() <
                MinimumPlanarPlayerDistance*MinimumPlanarPlayerDistance)
                continue;

            // The candidate node origin is the enemy's capsule centre,
            // exactly 0.8 m above its feet. Query only the nearby floor:
            // a distant basement or roof may not fake a valid foothold.
            var feetY=candidate.Y-.8f;
            var ray=PhysicsRayQueryParameters3D.Create(
                new Vector3(candidate.X,feetY+.95f,candidate.Z),
                new Vector3(candidate.X,feetY-.95f,candidate.Z));
            ray.CollisionMask=1u;
            ray.Exclude=new global::Godot.Collections.Array<Rid>
            {
                playerRid
            };
            var hit=space.IntersectRay(ray);
            if(hit.Count==0 ||
               ((Vector3)hit["normal"]).Y<.74f) continue;
            var floor=(Vector3)hit["position"];
            if(Math.Abs(floor.Y-feetY)>MaximumFloorAdjustment)
                continue;

            // Offset the body's centre 10 cm above ground to prevent
            // the capsule's base penetrating the floor collider.
            var actualCentre=floor+Vector3.Up*.90f;
            var query=new PhysicsShapeQueryParameters3D
            {
                Shape=capsule,
                Transform=new Transform3D(Basis.Identity,actualCentre),
                CollideWithBodies=true,
                CollisionMask=1u,
                Exclude=new global::Godot.Collections.Array<Rid>
                {
                    playerRid
                }
            };
            if(space.IntersectShape(query,1).Count!=0) continue;
            return actualCentre;
        }
        return null;
    }

    private static bool Finite(Vector3 p) =>
        float.IsFinite(p.X) &&
        float.IsFinite(p.Y) &&
        float.IsFinite(p.Z);
}

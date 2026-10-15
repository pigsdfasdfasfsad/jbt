using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Physics-gated positions only for the source importer's PROVISIONAL
/// (not recovered Roblox) objective markers. Never modifies original
/// authenticated objective coordinates or approximate-map placements.
/// </summary>
public static class Pass122ProvisionalObjectiveSafety
{
    public static Vector3? TryFind(World3D world,
        IReadOnlyList<Vector3> anchors, int preferred,
        Vector3 player, Rid playerRid)
    {
        if (anchors.Count == 0 || preferred < 0 ||
            !float.IsFinite(player.X) || !float.IsFinite(player.Y) ||
            !float.IsFinite(player.Z)) return null;
        var space = world.DirectSpaceState;
        var excluded = new global::Godot.Collections.Array<Rid> { playerRid };
        using var volume = new CapsuleShape3D { Radius = .45f, Height = 2.0f };
        for (var n = 0; n < anchors.Count; n++)
        {
            var anchor = anchors[(preferred+n)%anchors.Count];
            if (!float.IsFinite(anchor.X) || !float.IsFinite(anchor.Y) ||
                !float.IsFinite(anchor.Z)) continue;
            if (new Vector2(anchor.X-player.X,anchor.Z-player.Z)
                .LengthSquared() < 3f*3f) continue;
            using var ray = PhysicsRayQueryParameters3D.Create(
                anchor+Vector3.Up*1.25f, anchor+Vector3.Down*1.45f);
            ray.CollisionMask=1u;
            ray.Exclude=excluded;
            var hit=space.IntersectRay(ray);
            if (hit.Count==0 || ((Vector3)hit["normal"]).Y<.72f) continue;
            var floor=(Vector3)hit["position"];
            if (Math.Abs(floor.Y-anchor.Y)>1.0f) continue;
            var centre=new Vector3(anchor.X,floor.Y+.12f,anchor.Z);
            using var query = new PhysicsShapeQueryParameters3D
            {
                Shape=volume,
                Transform=new Transform3D(Basis.Identity,centre+Vector3.Up*1f),
                CollisionMask=1u,
                CollideWithBodies=true,
                Exclude=excluded
            };
            if (space.IntersectShape(query,1).Count != 0) continue;
            return centre;
        }
        return null;
    }
}

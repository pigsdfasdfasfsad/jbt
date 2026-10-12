using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Real Godot physics support test for naturally spawning loot on fallback
/// blockouts. Original source-owned item/fortification positions are never
/// relocated or culled by this approximation-only safety filter.
/// </summary>
public static class Pass86NaturalLootPlacement
{
    public const float MaximumMarkerFloorGap = 1.20f;
    public const float MinimumUpNormal = .70f;

    public static Vector3? TryGround(World3D world,Vector3 marker)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!Finite(marker)) return null;
        var space=world.DirectSpaceState;
        var ray=PhysicsRayQueryParameters3D.Create(
            marker+Vector3.Up*.75f,
            marker+Vector3.Down*1.70f);
        ray.CollisionMask=1u;
        var hit=space.IntersectRay(ray);
        if(hit.Count==0 ||
           ((Vector3)hit["normal"]).Y < MinimumUpNormal)
            return null;
        var floor=(Vector3)hit["position"];
        if(Math.Abs(marker.Y-floor.Y)>MaximumMarkerFloorGap)
            return null;
        // The mesh actor is 35cm tall. Raising its centre 62cm
        // above the measured floor keeps the complete object visible.
        var center=floor+Vector3.Up*.62f;
        using var volume=new BoxShape3D
        {
            Size=new Vector3(.58f,.55f,.58f)
        };
        var clearance=new PhysicsShapeQueryParameters3D
        {
            Shape=volume,
            Transform=new Transform3D(Basis.Identity,center),
            CollideWithBodies=true,
            CollisionMask=1u
        };
        // Wall, roof and solid prop intersection makes that loot
        // permanently uncollectable, so reject rather than bury it.
        if(space.IntersectShape(clearance,1).Count != 0)
            return null;
        return center;
    }

    private static bool Finite(Vector3 p) =>
        float.IsFinite(p.X) &&
        float.IsFinite(p.Y) &&
        float.IsFinite(p.Z);
}

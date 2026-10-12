using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass90: physically ground approximate Escort destination markers and
/// validate final survival/line-of-sight before awarding one objective.
/// This deliberately does not modify owner-source maps or the other
/// objective families.
/// </summary>
public static class Pass90EscortArrivalSafety
{
    public const float MaximumMarkerFloorGap=1.40f;
    public const float MinimumUpwardNormal=.74f;
    public const float MaximumArrivalHeightGap=.48f;
    public const float MaximumArrivalPlanarGap=1.50f;
    private static readonly Vector3[] FootprintSamples =
    [
        Vector3.Zero, Vector3.Right*.22f, Vector3.Left*.22f,
        Vector3.Forward*.22f, Vector3.Back*.22f
    ];

    public static Vector3? TryGroundDestination(
        World3D world,Vector3 marker,
        global::Godot.Collections.Array<Rid> exclude)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(exclude);
        if (!Finite(marker)) return null;
        var space=world.DirectSpaceState;
        Vector3? firstFloor=null;
        foreach(var offset in FootprintSamples)
        {
            var at=marker+offset;
            var ray=PhysicsRayQueryParameters3D.Create(
                at+Vector3.Up*.68f,
                at+Vector3.Down*2.30f);
            ray.CollisionMask=1u;
            ray.Exclude=exclude;
            var hit=space.IntersectRay(ray);
            if(hit.Count==0 ||
               ((Vector3)hit["normal"]).Y<MinimumUpwardNormal)
                return null;
            var floor=(Vector3)hit["position"];
            if (Math.Abs(floor.Y-marker.Y)>MaximumMarkerFloorGap)
                return null;
            if(firstFloor.HasValue &&
               Math.Abs(floor.Y-firstFloor.Value.Y)>.28f)
                return null; // No half-overhanging goal on a broken ledge.
            firstFloor ??= floor;
        }
        if (!firstFloor.HasValue) return null;

        // A plausible floor marker is not enough: the survivor's actual
        // 0.42m-radius, 1.8m-tall capsule must fit where it will stand.
        using var survivorShape=new CapsuleShape3D
        {
            Radius=.42f,Height=1.8f
        };
        var position=new Vector3(
            marker.X,firstFloor.Value.Y,marker.Z);
        var query=new PhysicsShapeQueryParameters3D
        {
            Shape=survivorShape,
            Transform=new Transform3D(
                Basis.Identity,position+Vector3.Up*.99f),
            CollideWithBodies=true,
            CollisionMask=1u,
            Exclude=exclude
        };
        if(space.IntersectShape(query,1).Count!=0) return null;
        return position;
    }

    public static bool CanComplete(
        World3D world,Vector3 survivorFeet,Vector3 goalFeet,
        bool isOnFloor,bool playerNearby,
        global::Godot.Collections.Array<Rid> exclude)
    {
        if (!isOnFloor || !playerNearby ||
            !Finite(survivorFeet) || !Finite(goalFeet))
            return false;
        var separation=goalFeet-survivorFeet;
        var flat=new Vector2(separation.X,separation.Z);
        if (flat.Length()>MaximumArrivalPlanarGap ||
            Math.Abs(separation.Y)>MaximumArrivalHeightGap)
            return false;
        return Pass89EscortSurfaceSafety.CanTravel(
            world,survivorFeet,goalFeet,exclude);
    }

    private static bool Finite(Vector3 value)=>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);
}

using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass79 uses the real Godot fallback collision world to detect missing
/// upper-floor support. It does NOT invent recovered Roblox navmesh data.
/// </summary>
public static class Pass79UpperFloorSafety
{
    public const float MaximumSegmentMetres = 45f;
    public const float SampleSpacingMetres = .60f;
    public const float MinimumFloorNormal = .74f;
    private static readonly float[] LateralOffsets = [0f, -.20f, .20f];

    public static bool IsSupported(
        Node3D owner, Vector3 fromFeet, Vector3 toFeet,
        global::Godot.Collections.Array<Rid> exclude,
        Func<Vector3,Vector3,bool> corridorClear)
    {
        if (!Finite(fromFeet) || !Finite(toFeet) ||
            fromFeet.DistanceTo(toFeet) > MaximumSegmentMetres ||
            Math.Abs(fromFeet.Y - toFeet.Y) > .62f ||
            !corridorClear(fromFeet,toFeet))
            return false;

        var distance = fromFeet.DistanceTo(toFeet);
        var sections = Math.Max(1,
            (int)Math.Ceiling(distance/SampleSpacingMetres));
        if (sections > 76) return false;
        var planar = toFeet-fromFeet;
        planar.Y = 0;
        var side = planar.LengthSquared() > .01f
            ? new Vector3(-planar.Z,0,planar.X).Normalized()
            : Vector3.Right;
        var space = owner.GetWorld3D().DirectSpaceState;
        for (var i=0;i<=sections;i++)
        {
            var expected=fromFeet.Lerp(toFeet,i/(float)sections);
            foreach (var offset in LateralOffsets)
            {
                var test=expected+side*offset;
                var ray=PhysicsRayQueryParameters3D.Create(
                    test+Vector3.Up*.58f,test+Vector3.Down*.44f);
                ray.CollisionMask=1u;
                ray.Exclude=exclude;
                var hit=space.IntersectRay(ray);
                if(hit.Count==0 ||
                   ((Vector3)hit["normal"]).Y<MinimumFloorNormal ||
                   Math.Abs(((Vector3)hit["position"]).Y-expected.Y)>.26f)
                    return false;
            }
        }
        return true;
    }

    private static bool Finite(Vector3 p) =>
        float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
}

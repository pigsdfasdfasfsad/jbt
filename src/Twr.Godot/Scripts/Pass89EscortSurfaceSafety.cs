using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Ground/obstacle audit for Pass89 rebuilt Escort objective routing.
/// Uses static Godot physics, not appearance, unverified retail data or
/// an invented navmesh. All callers must exclude their own actor RID.
/// </summary>
public static class Pass89EscortSurfaceSafety
{
    public const float MaximumSegmentMetres=55f;
    public const float SampleSpacingMetres=.60f;
    public const float MinimumFloorNormal=.72f;
    public const float MaximumHeightDeviation=.47f;
    private static readonly float[] SideOffsets=[0f,-.22f,.22f];

    public static bool CanTravel(World3D world,Vector3 fromFeet,
        Vector3 toFeet,global::Godot.Collections.Array<Rid> exclude)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(exclude);
        if(!Finite(fromFeet) || !Finite(toFeet))return false;
        var distance=fromFeet.DistanceTo(toFeet);
        if(distance>MaximumSegmentMetres ||
           Math.Abs(fromFeet.Y-toFeet.Y)>1.10f)
            return false;
        var count=Math.Max(1,(int)Math.Ceiling(distance/SampleSpacingMetres));
        if(count>93)return false;
        var delta=toFeet-fromFeet;
        delta.Y=0f;
        var side=delta.LengthSquared()>.0001f
            ? new Vector3(-delta.Z,0,delta.X).Normalized()
            : Vector3.Right;
        var space=world.DirectSpaceState;

        // Horizontal line-of-sight alone does not prove there's a floor.
        // Check the NPC torso and a lower body segment against real walls.
        foreach(var height in new[]{.48f,.96f})
        {
            var wall=PhysicsRayQueryParameters3D.Create(
                fromFeet+Vector3.Up*height,
                toFeet+Vector3.Up*height);
            wall.CollisionMask=1u;
            wall.Exclude=exclude;
            if(space.IntersectRay(wall).Count>0)return false;
        }

        for(var i=0;i<=count;i++)
        {
            var expected=fromFeet.Lerp(toFeet,i/(float)count);
            foreach(var lateral in SideOffsets)
            {
                var at=expected+side*lateral;
                var floorRay=PhysicsRayQueryParameters3D.Create(
                    at+Vector3.Up*.68f,
                    at+Vector3.Down*.84f);
                floorRay.CollisionMask=1u;
                floorRay.Exclude=exclude;
                var floorHit=space.IntersectRay(floorRay);
                if(floorHit.Count==0 ||
                   ((Vector3)floorHit["normal"]).Y < MinimumFloorNormal ||
                   Math.Abs(((Vector3)floorHit["position"]).Y-
                            expected.Y) > MaximumHeightDeviation)
                    return false;
            }
        }
        return true;
    }

    private static bool Finite(Vector3 v)=>
        float.IsFinite(v.X) &&
        float.IsFinite(v.Y) &&
        float.IsFinite(v.Z);
}

using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Physical supply delivery on the current offline map. Locations are an
/// explicit reconstruction, not recovered helicopter waypoints. A complete
/// plan includes the pallet footprint, clear descent and eight reachable loot
/// positions, so a successful-looking landing never strands the payload.
/// </summary>
public static class SupplyLandingPlanner
{
    public sealed record Plan(Vector3 PalletCentre, Vector3[] PickupCentres);
    private static readonly Vector2[] Footprint =
    [ new(-1.05f,-1.05f), new(1.05f,-1.05f), new(-1.05f,1.05f), new(1.05f,1.05f) ];
    private static readonly float[] Distances = [7f, 4.5f, 10f];

    public const int MaximumCandidates = 24;

    public static Plan? TryCandidate(World3D world, Vector3 playerFeet, Rid playerRid, int index)
    {
        if (!playerFeet.IsFinite() || index < 0 || index >= MaximumCandidates) return null;
        var excluded = new global::Godot.Collections.Array<Rid> { playerRid };
        var playerFloor=Pass90EscortArrivalSafety.TryGroundDestination(world,playerFeet,excluded);
        if (!playerFloor.HasValue) return null;
        // Production checks two candidates per physics frame, so a crowded
        // map cannot monopolize a single frame with a complete route scan.
        var angle=Mathf.Tau*(index%8)/8f;
        var distance=Distances[index/8];
        var guess=playerFloor.Value+new Vector3(MathF.Cos(angle)*distance,0,MathF.Sin(angle)*distance);
        var floor=Pass90EscortArrivalSafety.TryGroundDestination(world,guess,excluded);
        if (!floor.HasValue || Math.Abs(floor.Value.Y-playerFloor.Value.Y)>.8f) return null;
        return AtFloor(world,floor.Value,playerFeet,playerRid,true);
    }

    public static Plan? AtFloor(World3D world, Vector3 floor, Vector3 playerFeet,
        Rid playerRid, bool requireClearDescent)
    {
        if (!floor.IsFinite() || !playerFeet.IsFinite()) return null;
        var excluded=new global::Godot.Collections.Array<Rid> { playerRid };
        // The caller can be mid-jump when a radio transmission completes.
        // Test walking access from the physical floor below that position,
        // not an airborne body origin mistaken for a floating floor.
        var playerFloor=Pass90EscortArrivalSafety.TryGroundDestination(world,playerFeet,excluded);
        if (!playerFloor.HasValue) return null;
        var routeOrigin=playerFloor.Value;
        var space=world.DirectSpaceState;
        foreach(var offset in Footprint)
        {
            var at=floor+new Vector3(offset.X,0,offset.Y);
            using var ray=PhysicsRayQueryParameters3D.Create(at+Vector3.Up*.5f,at-Vector3.Up*.6f);
            ray.CollisionMask=1u;
            ray.Exclude=excluded;
            var support=space.IntersectRay(ray);
            if(support.Count==0 || ((Vector3)support["normal"]).Y<.7f ||
                Math.Abs(((Vector3)support["position"]).Y-floor.Y)>.15f) return null;
        }
        using var volume=new BoxShape3D { Size=new Vector3(2.4f,.45f,2.4f) };
        using var query=new PhysicsShapeQueryParameters3D
        {
            Shape=volume, CollisionMask=1u, CollideWithBodies=true,
            Transform=new Transform3D(Basis.Identity,floor+Vector3.Up*.26f), Exclude=excluded
        };
        if(space.IntersectShape(query,1).Count>0) return null;
        if(requireClearDescent)
        {
            query.Transform=new Transform3D(Basis.Identity,floor+Vector3.Up*15.23f);
            query.Motion=Vector3.Down*14.96f;
            if(space.IntersectShape(query,1).Count>0) return null;
            var motion=space.CastMotion(query);
            if(motion.Length<1 || motion[0]<.999f) return null;
        }
        // Existing payload contract: four regular and four fortification
        // slots. Every position needs real floor and a clear return path.
        var pickupCentres=new List<Vector3>(8);
        for(var i=0;i<8;i++)
        {
            var angle=Mathf.Tau*i/8f;
            var candidate=floor+new Vector3(MathF.Cos(angle)*2.3f,0,MathF.Sin(angle)*2.3f);
            var grounded=Pass90EscortArrivalSafety.TryGroundDestination(world,candidate,excluded);
            if(!grounded.HasValue || Math.Abs(grounded.Value.Y-floor.Y)>.3f ||
                !Pass89EscortSurfaceSafety.CanTravel(world,routeOrigin,grounded.Value,excluded) ||
                !Pass89EscortSurfaceSafety.CanTravel(world,grounded.Value,routeOrigin,excluded)) return null;
            pickupCentres.Add(grounded.Value+Vector3.Up*.48f);
        }
        return new Plan(new Vector3(floor.X,floor.Y+.23f,floor.Z),pickupCentres.ToArray());
    }
}

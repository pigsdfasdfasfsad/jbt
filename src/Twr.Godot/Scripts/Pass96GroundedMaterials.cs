using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass96, public fallback maps ONLY: physically grounded and line-of-sight
/// accessible Load / Repair objective objects. A real full-size survivor
/// capsule stands in for player clearance. The source-owner-authored scene
/// placements never enter this planner.
/// </summary>
public static class Pass96GroundedMaterials
{
    public readonly record struct Plan(
        Vector3? DepositFeet,Vector3[] ItemFeet,
        int RejectedCandidates,int OverlapFallbacks)
    {
        public bool IsComplete(int count) =>
            DepositFeet.HasValue && ItemFeet.Length==count;
    }

    private static readonly Vector2[] DepositOffsets =
    [
        Vector2.Zero,
        new(-2f,0f),new(2f,0f),
        new(0f,-2f),new(0f,2f),
        new(-4f,0f),new(4f,0f),
        new(0f,-4f),new(0f,4f),
        new(-3f,-3f),new(3f,-3f),
        new(-3f,3f),new(3f,3f),
        new(-6f,0f),new(6f,0f),
        new(0f,-6f),new(0f,6f)
    ];

    private static readonly float[] MaterialRadii =
        [4.0f,2.6f,5.5f,1.65f,7.0f];
    private const int DirectionsPerRing=24;
    private const float MinimumItemSeparation=0.95f;
    private const float MaximumSameFloorDifference=.35f;

    public static Plan TryPlan(
        World3D world,Vector3 approximateDepositFeet,
        Rid playerRid,int itemCount)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(itemCount<1 || itemCount>24 ||
           !Finite(approximateDepositFeet))
            return new Plan(null,[],0,0);

        var exclude=new global::Godot.Collections.Array<Rid>
        {
            playerRid
        };
        var rejected=0;
        Vector3? deposit=null;
        foreach(var offset in DepositOffsets)
        {
            var marker=approximateDepositFeet+
                new Vector3(offset.X,0,offset.Y);
            var candidate=Pass90EscortArrivalSafety.TryGroundDestination(
                world,marker,exclude);
            if(!candidate.HasValue)
            {
                rejected++;
                continue;
            }
            // Never change floor when adjusting a source-absent objective.
            // Lower floor and upper Manor need separate valid anchors.
            if(Math.Abs(candidate.Value.Y-approximateDepositFeet.Y)>1.45f)
            {
                rejected++;
                continue;
            }
            deposit=candidate;
            break;
        }
        if(!deposit.HasValue)
            return new Plan(null,[],rejected,0);

        var origin=deposit.Value;
        var points=new List<Vector3>(itemCount);
        // Cycle first-choice radial directions across all required items.
        // Rejected points are never used: they might be inside an existing
        // building, hanging off an upstairs ledge, or behind a solid wall.
        for(var item=0;item<itemCount;item++)
        {
            var located=false;
            for(var attempt=0;
                attempt<DirectionsPerRing*MaterialRadii.Length;
                attempt++)
            {
                var ring=attempt/DirectionsPerRing;
                var spoke=(attempt%DirectionsPerRing+item*7)%
                    DirectionsPerRing;
                var angle=(float)(Math.Tau*spoke/DirectionsPerRing);
                var radius=MaterialRadii[ring];
                var marker=origin+new Vector3(
                    MathF.Cos(angle)*radius,0,
                    MathF.Sin(angle)*radius);
                var grounded=Pass90EscortArrivalSafety.TryGroundDestination(
                    world,marker,exclude);
                if(!grounded.HasValue ||
                   Math.Abs(grounded.Value.Y-origin.Y)>
                       MaximumSameFloorDifference ||
                   FlatDistanceSquared(origin,grounded.Value)<1.1f ||
                   !Pass89EscortSurfaceSafety.CanTravel(
                       world,origin,grounded.Value,exclude))
                {
                    rejected++;
                    continue;
                }
                var separated=true;
                foreach(var prior in points)
                    if(FlatDistanceSquared(prior,grounded.Value)<
                        MinimumItemSeparation*MinimumItemSeparation)
                    {
                        separated=false;
                        break;
                    }
                if(!separated)
                {
                    rejected++;
                    continue;
                }
                points.Add(grounded.Value);
                located=true;
                break;
            }
            if(!located)
            {
                // If a genuinely tiny supported room cannot hold twelve
                // unique markers, overlap existing verified reachable
                // positions rather than invent an inaccessible pickup.
                // Each mesh is non-colliding; the original E-key interaction
                // still takes precisely one item per independent press.
                var fallback=points.Count>0 ? points[item%points.Count] :
                    origin;
                points.Add(fallback);
            }
        }

        var overlaps=0;
        for(var i=0;i<points.Count;i++)
            for(var j=0;j<i;j++)
                if(FlatDistanceSquared(points[i],points[j])<
                    MinimumItemSeparation*MinimumItemSeparation)
                {
                    overlaps++;
                    break;
                }
        return new Plan(origin,points.ToArray(),rejected,overlaps);
    }

    private static float FlatDistanceSquared(Vector3 a,Vector3 b)
    {
        var dx=a.X-b.X;
        var dz=a.Z-b.Z;
        return dx*dx+dz*dz;
    }
    private static bool Finite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) &&
        float.IsFinite(v.Z);
}

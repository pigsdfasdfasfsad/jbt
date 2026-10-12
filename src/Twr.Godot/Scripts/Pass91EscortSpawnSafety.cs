using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass91: validate that approximate Escort survivor origin can actually
/// hold a full character capsule on a collision-supported floor. Missing
/// original Roblox markers remain classified as approximated; original
/// source-owned map locations are not adjusted by this filter.
/// </summary>
public static class Pass91EscortSpawnSafety
{
    public const int MaximumSpawnCandidates=17;
    public const float MaximumLateralRelocation=10f;
    private static readonly Vector2[] Offsets =
    [
        Vector2.Zero,
        new(-3,0),new(3,0),new(0,-3),new(0,3),
        new(-6,0),new(6,0),new(0,-6),new(0,6),
        new(-4,-4),new(4,-4),new(-4,4),new(4,4),
        new(-8,0),new(8,0),new(0,-8),new(0,8)
    ];

    public static Vector3? TryFind(
        World3D world,Vector3 authoredApproximateMarker,
        Vector3 playerFeet,Rid playerRid)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(!Finite(authoredApproximateMarker)||!Finite(playerFeet))
            return null;
        var exclude=new global::Godot.Collections.Array<Rid>{playerRid};
        for(var i=0;i<Math.Min(Offsets.Length,MaximumSpawnCandidates);i++)
        {
            var offset=Offsets[i];
            if(offset.Length()>MaximumLateralRelocation)continue;
            var candidate=authoredApproximateMarker+
                new Vector3(offset.X,0,offset.Y);
            var ground=Pass90EscortArrivalSafety.TryGroundDestination(
                world,candidate,exclude);
            if(!ground.HasValue || ground.Value.Y>2.2f)
                continue; // Pass73 is a ground-floor-only graph.
            var planar=new Vector2(
                ground.Value.X-playerFeet.X,
                ground.Value.Z-playerFeet.Z);
            if(planar.LengthSquared()<2f*2f)
                continue; // never instantiate a capsule inside the player.
            return ground.Value;
        }
        return null;
    }

    private static bool Finite(Vector3 v)=>
        float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
}

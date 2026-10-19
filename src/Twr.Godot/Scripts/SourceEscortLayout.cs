using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>Provisional source-map Escort: a reachable survivor start AND a
/// complete collision-checked destination route. Not original objective data.</summary>
public sealed record SourceEscortLayout(Vector3 Position, Vector3 Destination,
    Vector3[] PlayerRoute, Vector3[] SurvivorRoute)
{
    public static SourceEscortLayout? TryCreate(World3D world, Vector3 playerFeet,
        Vector3 start, Vector3 goal, Rid playerRid,
        Func<Vector3,Vector3,Vector3[]>? graph)
    {
        if(!start.IsFinite() || !goal.IsFinite() || start.DistanceTo(goal)<6f ||
            start.DistanceTo(goal)>30f) return null;
        if(!SourceTraversalRoutes.TryPlan(world,playerFeet,start,playerRid,graph,out var approach) ||
            !SourceTraversalRoutes.TryPlan(world,start,goal,playerRid,graph,out var escort)) return null;
        return new(approach[^1],escort[^1],approach,escort);
    }

    public static SourceEscortLayout? TryPlan(World3D world, Vector3 playerFeet,
        Rid playerRid, IReadOnlyList<Vector3> preferred, IReadOnlyList<Vector3> reserved,
        Pass25SourceNavigationRuntime? navigator)
    {
        Func<Vector3,Vector3,Vector3[]>? graph=navigator is null?null:navigator.GetRoute;
        var starts=new List<Vector3>(preferred);
        if(navigator is not null)
            starts.AddRange(navigator.GetProvisionalCandidates(playerFeet,playerFeet,32,4f,12f));
        var attempts=0;
        foreach(var start in starts.Distinct())
        {
            if(reserved.Any(p=>new Vector2(p.X-start.X,p.Z-start.Z).Length()<3f)) continue;
            var grounded=Pass122ProvisionalObjectiveSafety.TryFind(world,new[]{start},0,playerFeet,playerRid);
            if(!grounded.HasValue)continue;
            if(++attempts>8)break;
            var goals=new List<Vector3>();
            if(navigator is not null)
                goals.AddRange(navigator.GetProvisionalCandidates(grounded.Value,
                    grounded.Value,48,7f,24f));
            foreach(var radius in new[]{8f,12f,18f})
                for(var i=0;i<8;i++)
                    goals.Add(grounded.Value+new Vector3(Mathf.Cos(i*Mathf.Tau/8)*radius,0,
                        Mathf.Sin(i*Mathf.Tau/8)*radius));
            foreach(var goal in goals)
            {
                if(reserved.Any(p=>new Vector2(p.X-goal.X,p.Z-goal.Z).Length()<3f))continue;
                var layout=TryCreate(world,playerFeet,grounded.Value,goal,playerRid,graph);
                if(layout is not null)return layout;
            }
        }
        return null;
    }
}

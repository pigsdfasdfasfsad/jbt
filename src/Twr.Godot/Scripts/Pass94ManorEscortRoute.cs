using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass94: physical, full-width survivor routes on the three PUBLIC FALLBACK
/// Manor stairs. All route legs use actual static Godot floor/capsule queries.
/// This is not a recovered retail Manor navmesh or original NPC animation.
/// </summary>
public static class Pass94ManorEscortRoute
{
    private const int RampSections = 12;
    private const float MaximumUpperStairDistance = 11f;
    private const float MinimumUpperFloorY = 2.5f;

    public static Vector3? TryGroundUpperSpawn(
        World3D world,Vector3 marker,Vector3 playerFeet,Rid playerRid)
    {
        if (!Finite(marker) || !Finite(playerFeet)) return null;
        var exclude=new global::Godot.Collections.Array<Rid>{playerRid};
        var grounded=Pass90EscortArrivalSafety.TryGroundDestination(
            world,marker,exclude);
        if (!grounded.HasValue ||
            grounded.Value.Y<MinimumUpperFloorY ||
            grounded.Value.Y>3.6f ||
            (grounded.Value-playerFeet).LengthSquared()<4f)
            return null;
        return grounded.Value;
    }

    // Original approximate destination is 24m north of its marker.
    // On reconstructed Manor upper rooms this often places its X/Z above
    // the LOWER courtyard. Ground the actual destination at that X/Z
    // rather than leave an unreachable floating marker at upper-floor Y.
    public static Vector3? TryGroundGoal(
        World3D world,Vector3 approximateGoal,
        global::Godot.Collections.Array<Rid> exclude)
    {
        if (!Finite(approximateGoal)) return null;
        var onUpper=Pass90EscortArrivalSafety.TryGroundDestination(
            world,approximateGoal,exclude);
        if (onUpper.HasValue && onUpper.Value.Y>=MinimumUpperFloorY)
            return onUpper.Value;
        var lowerMarker=new Vector3(
            approximateGoal.X,1.0f,approximateGoal.Z);
        var lower=Pass90EscortArrivalSafety.TryGroundDestination(
            world,lowerMarker,exclude);
        return lower.HasValue && lower.Value.Y<=2.2f ? lower : null;
    }

    public static Vector3[] GetVerifiedRoute(
        Pass73GroundNavigationRuntime nav,Vector3 fromFeet,
        Vector3 goalFeet,Rid survivorRid,Rid playerRid)
    {
        ArgumentNullException.ThrowIfNull(nav);
        if (nav.MapName!="Manor" || !nav.IsGraphReady ||
            nav.Layout.ApproximateStairs77.Count!=3 ||
            nav.Layout.ApproximateCrossings80.Count!=3 ||
            !Finite(fromFeet) || !Finite(goalFeet))
            return [];

        // Once real physical descent is complete, hand off to the unchanged
        // Escort-only ground graph. Do not reuse the thinner infected graph.
        if (fromFeet.Y<=2.20f)
            return nav.GetVerifiedEscortRoute89(fromFeet,goalFeet,survivorRid);

        var exclude=new global::Godot.Collections.Array<Rid>
        {
            survivorRid,playerRid
        };
        var world=nav.GetWorld3D();
        if (goalFeet.Y>=MinimumUpperFloorY)
        {
            if (Pass89EscortSurfaceSafety.CanTravel(
                    world,fromFeet,goalFeet,exclude))
                return [goalFeet];
            return TryUpperCrossing(nav,fromFeet,goalFeet,exclude);
        }

        // Every candidate stair, upper walkway, and lower-floor detour
        // is physically validated. A graph route or teleported shortcut is
        // never substituted for a missing/blocked stair connection.
        Vector3[] best=[];
        var bestDistance=float.MaxValue;
        for(var index=0;index<nav.Layout.ApproximateStairs77.Count;index++)
        {
            var stair=nav.Layout.ApproximateStairs77[index];
            var crossing=nav.Layout.ApproximateCrossings80[index];
            var ramp=stair.UpperFeet-stair.LowerFeet;
            var progress=Mathf.Clamp(
                (fromFeet-stair.LowerFeet).Dot(ramp)/ramp.LengthSquared(),
                0f,1f);
            var expected=stair.LowerFeet.Lerp(stair.UpperFeet,progress);
            var onRamp=progress>.015f && progress<.995f &&
                Math.Abs(fromFeet.X-expected.X)<1.15f &&
                Math.Abs(fromFeet.Z-expected.Z)<.76f &&
                Math.Abs(fromFeet.Y-expected.Y)<.53f;
            if (!onRamp &&
                (Math.Abs(fromFeet.Y-stair.UpperFeet.Y)>.78f ||
                 FlatDistance(fromFeet,stair.UpperFeet)>
                     MaximumUpperStairDistance))
                continue;

            var route=new List<Vector3>();
            var previous=fromFeet;
            if(!onRamp)
            {
                if (Math.Abs(fromFeet.X-stair.UpperFeet.X)>1.8f)
                {
                    // The upper landing is divided into side decks: the
                    // real Pass80 crosswalk is the only approved connection.
                    var entrance=fromFeet.X<stair.UpperFeet.X
                        ? crossing.LeftFeet : crossing.RightFeet;
                    if (!Append(world,route,ref previous,entrance,exclude) ||
                        !Append(world,route,ref previous,crossing.CentreFeet,
                            exclude))
                        continue;
                }
                if (!Append(world,route,ref previous,
                        stair.UpperFeet,exclude))
                    continue;
            }

            // Descend 12 measured sloped-collider sections, never step
            // vertically down through a deck, ceiling, or wall.
            var firstSection=onRamp
                ? Math.Clamp((int)Math.Floor(progress*RampSections)-1,
                    0,RampSections-1)
                : RampSections-1;
            var accepted=true;
            for(var section=firstSection;section>=0;section--)
                if(!Append(world,route,ref previous,
                    stair.LowerFeet.Lerp(
                        stair.UpperFeet,section/(float)RampSections),
                    exclude))
                {
                    accepted=false;
                    break;
                }
            if(!accepted) continue;

            // The ground-floor graph independently checks the complete
            // lower route around actual Manor shell walls/thresholds.
            var ground=nav.GetVerifiedEscortRoute89(
                stair.LowerFeet,goalFeet,survivorRid);
            if(ground.Length==0) continue;
            foreach(var waypoint in ground)
                route.Add(waypoint);

            var travelled=0f;
            var last=fromFeet;
            foreach(var point in route)
            {
                travelled+=last.DistanceTo(point);
                last=point;
            }
            if(travelled>=bestDistance)continue;
            bestDistance=travelled;
            best=route.ToArray();
        }
        return best;
    }

    // Same-height upper goals can cross a real Pass80 catwalk, but may not
    // cut diagonally across the unsupported void between the two side decks.
    private static Vector3[] TryUpperCrossing(
        Pass73GroundNavigationRuntime nav,Vector3 from,Vector3 goal,
        global::Godot.Collections.Array<Rid> exclude)
    {
        var world=nav.GetWorld3D();
        foreach(var crossing in nav.Layout.ApproximateCrossings80)
        {
            var mid=crossing.CentreFeet;
            if(Math.Abs(from.Z-mid.Z)>10f ||
               Math.Abs(goal.Z-mid.Z)>10f ||
               Math.Abs(from.Y-mid.Y)>.45f ||
               Math.Abs(goal.Y-mid.Y)>.45f)
                continue;
            var fromSide=Math.Sign(from.X-mid.X);
            var goalSide=Math.Sign(goal.X-mid.X);
            if(fromSide==0 || fromSide==goalSide)continue;
            var entrance=fromSide<0
                ? crossing.LeftFeet : crossing.RightFeet;
            var exit=goalSide<0
                ? crossing.LeftFeet : crossing.RightFeet;
            var path=new List<Vector3>();
            var previous=from;
            if(Append(world,path,ref previous,entrance,exclude) &&
               Append(world,path,ref previous,mid,exclude) &&
               Append(world,path,ref previous,exit,exclude) &&
               Append(world,path,ref previous,goal,exclude))
                return path.ToArray();
        }
        return [];
    }

    private static bool Append(
        World3D world,List<Vector3> path,ref Vector3 previous,
        Vector3 next,global::Godot.Collections.Array<Rid> exclude)
    {
        if (!Pass89EscortSurfaceSafety.CanTravel(
                world,previous,next,exclude))
            return false;
        if(previous.DistanceTo(next)>.04f)
            path.Add(next);
        previous=next;
        return true;
    }

    private static float FlatDistance(Vector3 a,Vector3 b)=>
        new Vector2(a.X-b.X,a.Z-b.Z).Length();

    private static bool Finite(Vector3 point)=>
        float.IsFinite(point.X) && float.IsFinite(point.Y) &&
        float.IsFinite(point.Z);
}

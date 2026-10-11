using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass66 first-person proxy upper/forearm two-bone kinematics. Authored
/// weapon LeftWeld/RightWeld hand targets are inputs; no original Roblox
/// avatar Motor6D C0/C1 or mesh triangles are claimed or executed.
/// Shoulder anchors are camera-local reconstruction assumptions.
/// Survivor R6 arms measured 1 x 2 x 1 studs in source TestPlace; each
/// two-segment chain has a 2-stud *total* nominal length.
/// </summary>
public static class Pass66ArmKinematics
{
    public const float NominalSegmentStuds=1f;
    public const float MaxReachScale=1.65f;

    public readonly record struct Solution(Vector3 Shoulder,
        Vector3 Elbow,Vector3 Wrist,float UpperLength,
        float ForeLength,float ReachScale,bool ReachClamped)
    {
        public bool IsFinite =>
            Finite(Shoulder) && Finite(Elbow) && Finite(Wrist) &&
            float.IsFinite(UpperLength) &&
            float.IsFinite(ForeLength) &&
            float.IsFinite(ReachScale);
        public float ElbowError =>
            Math.Abs(Shoulder.DistanceTo(Elbow)-UpperLength);
        public float WristError =>
            Math.Abs(Elbow.DistanceTo(Wrist)-ForeLength);
    }

    public static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);

    public static Vector3 Shoulder(bool left) =>
        new(left?-.235f:.235f,-.27f,.245f);

    public static Solution Solve(bool left,Vector3 wrist)
    {
        var shoulder=Shoulder(left);
        if(!Finite(wrist) || wrist.LengthSquared()>9f)
            wrist=shoulder+new Vector3(0f,.02f,-.42f);
        var delta=wrist-shoulder;
        var distance=delta.Length();
        var toward=distance>.0001f
            ? delta/distance : Vector3.Forward;
        var nominal=NominalSegmentStuds*RobloxUnits.MetersPerStud;
        // Extend only on large authored source hand motions; cap at
        // 1.65x nominal and report any farther target as clamped.
        var scale=Math.Clamp(
            distance/(2f*nominal-.015f),1f,MaxReachScale);
        var upper=nominal*scale;
        var fore=nominal*scale;
        var minDist=Math.Abs(upper-fore)+.001f;
        var maxDist=upper+fore-.001f;
        var reach=Math.Clamp(distance,minDist,maxDist);
        var projected=shoulder+toward*reach;
        var horizontal=(left?Vector3.Left:Vector3.Right) +
            Vector3.Down*.55f + Vector3.Back*.05f;
        var bend=horizontal-toward*horizontal.Dot(toward);
        if(bend.LengthSquared()<.0001f)
        {
            var alternate=Math.Abs(toward.Dot(Vector3.Up))<.96f
                ? Vector3.Up : Vector3.Right;
            bend=alternate-toward*alternate.Dot(toward);
        }
        bend=bend.Normalized();
        var along=(upper*upper+reach*reach-fore*fore)/
            (2f*reach);
        var elevation=MathF.Sqrt(Math.Max(0f,upper*upper-along*along));
        var elbow=shoulder+toward*along+bend*elevation;
        return new Solution(shoulder,elbow,projected,
            upper,fore,scale,Math.Abs(reach-distance)>.002f);
    }

    /// <summary>
    /// Align a length-one Y-axis limb primitive between two finite joints.
    /// The basis stays right-handed even for a nearly camera-aligned arm.
    /// </summary>
    public static bool TrySegment(Vector3 start,Vector3 end,
        out Transform3D transform,out float length)
    {
        transform=Transform3D.Identity;
        length=0f;
        if(!Finite(start) || !Finite(end))return false;
        var offset=end-start;
        length=offset.Length();
        if(!float.IsFinite(length) || length<.005f || length>3f)
            return false;
        var up=offset/length;
        var x=up.Cross(Vector3.Back);
        if(x.LengthSquared()<.0001f)x=up.Cross(Vector3.Up);
        if(x.LengthSquared()<.0001f)return false;
        x=x.Normalized();
        var z=x.Cross(up).Normalized();
        var basis=new Basis(x,up,z);
        if(!float.IsFinite(basis.Determinant()) ||
            Math.Abs(basis.Determinant()-1f)>.005f)
            return false;
        transform=new Transform3D(basis,(start+end)*.5f);
        return true;
    }
}

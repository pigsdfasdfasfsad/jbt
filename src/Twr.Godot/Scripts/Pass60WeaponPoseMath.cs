using System;
using System.Globalization;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass60: restricted, data-only reader for authored Roblox CFrame literals.
/// The pose is expressed in CAMERA-LOCAL weapon coordinates (same forward -Z
/// convention as Godot's Camera3D). No source Luau is evaluated.
/// </summary>
public static class Pass60WeaponPoseMath
{
    private const float Stud=RobloxUnits.MetersPerStud;
    public static bool TryParse(string? expression,Transform3D? sourceGrip,
        out Transform3D pose)
    {
        pose=Transform3D.Identity;
        if(string.IsNullOrWhiteSpace(expression) || expression.Length>640)
            return false;
        var terms=expression.Split(" * ",StringSplitOptions.None);
        if(terms.Length is <1 or >8)return false;
        var value=Transform3D.Identity;
        foreach(var term in terms)
        {
            Transform3D next;
            if(term=="module.Stats.GripCF" && sourceGrip.HasValue)
                next=sourceGrip.Value;
            else if(term.StartsWith("CFrame.new(",StringComparison.Ordinal) &&
                    term.EndsWith(')'))
            {
                if(!Numbers(term[11..^1],out var n) ||
                   n.Length is not (0 or 3 or 12))return false;
                var position=n.Length>=3
                    ? new Vector3(n[0],n[1],n[2])*Stud : Vector3.Zero;
                var basis=Basis.Identity;
                if(n.Length==12)
                {
                    for(var i=3;i<12;i++)
                        if(Math.Abs(n[i])>1.15f)return false;
                    // Roblox's CFrame matrix is row-major. Godot's Basis
                    // constructor takes COLUMN vectors.
                    basis=new Basis(
                        new Vector3(n[3],n[6],n[9]),
                        new Vector3(n[4],n[7],n[10]),
                        new Vector3(n[5],n[8],n[11]));
                    var det=basis.Determinant();
                    if(!float.IsFinite(det) || det<.3f || det>1.6f)
                        return false;
                    basis=basis.Orthonormalized();
                }
                if(position.LengthSquared()>16f)return false;
                next=new Transform3D(basis,position);
            }
            else if(term.StartsWith("CFrame.Angles(",StringComparison.Ordinal)
                     && term.EndsWith(')'))
            {
                if(!Numbers(term[14..^1],out var a) ||
                   a.Length!=3 || Math.Abs(a[0])>1.6f ||
                   Math.Abs(a[1])>1.6f || Math.Abs(a[2])>1.6f)
                    return false;
                var basis=new Basis(Vector3.Right,a[0]) *
                    new Basis(Vector3.Up,a[1]) *
                    new Basis(Vector3.Back,a[2]);
                next=new Transform3D(basis,Vector3.Zero);
            }
            else return false;
            value*=next;
            if(value.Origin.LengthSquared()>16f)return false;
        }
        pose=value;
        return true;
    }

    public static Transform3D Interpolate(Transform3D from,
        Transform3D to,float fraction)
    {
        var t=Math.Clamp(fraction,0f,1f);
        var a=from.Basis.Orthonormalized().GetRotationQuaternion();
        var b=to.Basis.Orthonormalized().GetRotationQuaternion();
        return new Transform3D(new Basis(a.Slerp(b,t)),
            from.Origin.Lerp(to.Origin,t));
    }

    private static bool Numbers(string csv,out float[] values)
    {
        values=[];
        if(csv.Length==0)return true;
        var pieces=csv.Split(',',StringSplitOptions.TrimEntries);
        if(pieces.Length is <1 or >12)return false;
        var parsed=new float[pieces.Length];
        for(var i=0;i<pieces.Length;i++)
            if(pieces[i].Length is <1 or >32 ||
                !float.TryParse(pieces[i],NumberStyles.Float,
                    CultureInfo.InvariantCulture,out parsed[i]) ||
                !float.IsFinite(parsed[i]) || Math.Abs(parsed[i])>8f)
                return false;
        values=parsed;
        return true;
    }
}

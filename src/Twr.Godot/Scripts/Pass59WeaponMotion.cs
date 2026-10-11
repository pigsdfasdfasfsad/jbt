using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

// Pass59. Reads only inert source-normalized motion records, never executes Luau.
public static class Pass59WeaponMotion
{
    public readonly record struct Shot(float Pullback, float Pitch, float Out, float Back);
    public readonly record struct Profile(Vector3 Sprint, Vector3 SprintEuler,
        float SprintIn, float SprintOut, Shot Hip, Shot Aim, bool HasHip, bool HasAim);
    private static Dictionary<string,Profile>? _catalog;
    public static bool TryGet(string name,out Profile profile)
    {
        Load();
        return _catalog!.TryGetValue(name,out profile);
    }
    public static bool ParseVector(string? value,out Vector3 result)
    {
        result=Vector3.Zero;
        if(value is null || !value.StartsWith("CFrame.new(",StringComparison.Ordinal))return false;
        var end=value.IndexOf(')');
        if(end<11)return false;
        var csv=value[11..end];
        var fields=csv.Split(',',StringSplitOptions.TrimEntries);
        if(fields.Length!=3)return false;
        var v=new float[3];
        for(var i=0;i<3;i++)
            if(!float.TryParse(fields[i],NumberStyles.Float,CultureInfo.InvariantCulture,out v[i])
                || !float.IsFinite(v[i]) || Math.Abs(v[i])>8)return false;
        result=new Vector3(v[0],v[1],v[2]);
        return true;
    }
    private static bool ReadShot(JsonElement animations,string animation,out Shot shot)
    {
        shot=default;
        if(!animations.TryGetProperty(animation,out var entry) ||
           !entry.TryGetProperty("Sequence",out var sequence) ||
           sequence.GetArrayLength()<2)return false;
        var a=sequence[0];var b=sequence[1];
        if(!a.TryGetProperty("Time",out var t1) || !b.TryGetProperty("Time",out var t2) ||
           !a.TryGetProperty("AV",out var av) || !av.TryGetProperty("Fire",out var fire) ||
           !fire.TryGetProperty("To",out var target) ||
           !target.TryGetProperty("expression",out var expr) ||
           !ParseVector(expr.GetString(),out var vec))return false;
        var outward=t1.GetSingle();var returning=t2.GetSingle();
        if(!float.IsFinite(outward)||!float.IsFinite(returning)||
           outward<=0||returning<=0||outward>2||returning>2)return false;
        shot=new Shot(Math.Clamp(vec.Z*RobloxUnits.MetersPerStud,0,.3f),0,
            outward,returning);
        return shot.Pullback>0;
    }
    private static void Load()
    {
        if(_catalog is not null)return;
        var result=new Dictionary<string,Profile>(StringComparer.Ordinal);
        try
        {
            using var catalog=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString("res://Content/weapons/catalog.json"));
            foreach(var item in catalog.RootElement.GetProperty("weapons").EnumerateArray())
            {
                var path=item.GetProperty("path").GetString()??"";
                if(!path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.Contains('\\') || path.StartsWith('/'))continue;
                try
                {
                    using var document=JsonDocument.Parse(global::Godot.FileAccess
                        .GetFileAsString("res://Content/weapons/"+path));
                    var root=document.RootElement;
                    var name=root.GetProperty("name").GetString()??"";
                    var stats=root.GetProperty("stats");
                    var animations=root.GetProperty("animations");
                    var sprint=Vector3.Zero;
                    var hasSprint=stats.TryGetProperty("SprintCF",out var cf) &&
                        cf.TryGetProperty("expression",out var expr) &&
                        ParseVector(expr.GetString(),out sprint);
                    var hip=ReadShot(animations,"Fire",out var hipShot);
                    var aim=ReadShot(animations,"AimFire",out var aimShot);
                    if(!hasSprint && !hip && !aim)continue;
                    result[name]=new Profile(sprint,Vector3.Zero,.4f,.3f,
                        hipShot,aimShot,hip,aim);
                }
                catch(Exception){ /* An unsupported source expression uses baseline motion. */ }
            }
        }
        catch(Exception e){GD.PushWarning("TWR_PASS59_MOTION_FALLBACK "+e.Message);}
        _catalog=result;
        GD.Print($"TWR_PASS59_SOURCE_MOTION_READY count={result.Count}");
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass60: source-authored weapon pose keyframes. Reads the existing inert
/// Content/weapons JSON. Animation affects visuals only; player reload speed,
/// weapon cooldowns and all gameplay authority remain unchanged.
/// </summary>
public static class Pass60WeaponAnimationLibrary
{
    public readonly record struct Shot(Transform3D Target,float Out,float Back);
    public readonly record struct ReloadFrame(
        float Start,float End,Transform3D From,Transform3D To);
    public sealed record ReloadClip(ReloadFrame[] Frames,float Duration);
    public sealed record Motion(Transform3D Sprint,bool HasSprint,
        float SprintIn,float SprintOut,Shot? Fire,Shot? AimFire,
        ReloadClip? Reload);

    private static Dictionary<string,Motion>? _motions;
    public static int SourceMotionCount
    {
        get{Load();return _motions!.Count;}
    }
    public static bool TryGet(string name,out Motion result)
    {
        Load();
        if(_motions!.TryGetValue(name,out var value))
        {
            result=value;
            return true;
        }
        result=null!;
        return false;
    }

    public static Transform3D SampleReload(ReloadClip clip,float progress)
    {
        if(clip.Frames.Length==0 || !float.IsFinite(progress) ||
           progress<=0f || progress>=1f)
            return Transform3D.Identity;
        var elapsed=progress*clip.Duration;
        foreach(var frame in clip.Frames)
        {
            if(elapsed>frame.End)continue;
            var portion=Math.Max(.0001f,frame.End-frame.Start);
            return Pass60WeaponPoseMath.Interpolate(frame.From,frame.To,
                (elapsed-frame.Start)/portion);
        }
        return Transform3D.Identity;
    }

    private static bool Field(JsonElement container,string key,
        out string? expression)
    {
        expression=null;
        if(!container.TryGetProperty(key,out var field) ||
           field.ValueKind!=JsonValueKind.Object ||
           !field.TryGetProperty("expression",out var value) ||
           value.ValueKind!=JsonValueKind.String)return false;
        expression=value.GetString();
        return true;
    }

    private static bool FrameTarget(JsonElement part,out string? expression)
    {
        expression=null;
        if(part.ValueKind!=JsonValueKind.Object ||
           !part.TryGetProperty("To",out var target) ||
           target.ValueKind!=JsonValueKind.Object ||
           !target.TryGetProperty("expression",out var value) ||
           value.ValueKind!=JsonValueKind.String)return false;
        expression=value.GetString();
        return expression is not null;
    }

    private static bool Duration(JsonElement part,out float duration)
    {
        duration=0f;
        if(!part.TryGetProperty("Time",out var value) ||
           value.ValueKind!=JsonValueKind.Number ||
           !value.TryGetSingle(out duration) ||
           !float.IsFinite(duration) || duration<0f || duration>3f)
            return false;
        return true;
    }

    private static bool Sequence(JsonElement animations,string key,
        out JsonElement steps)
    {
        steps=default;
        return animations.TryGetProperty(key,out var entry) &&
            entry.TryGetProperty("Sequence",out steps) &&
            steps.ValueKind==JsonValueKind.Array &&
            steps.GetArrayLength() is >0 and <=128;
    }

    private static Shot? ReadFire(JsonElement animations,string key)
    {
        if(!Sequence(animations,key,out var steps) ||
           steps.GetArrayLength()<2 ||
           !Duration(steps[0],out var timeOut) ||
           !Duration(steps[1],out var timeBack) ||
           timeOut<=.001f || timeBack<=.001f ||
           !steps[0].TryGetProperty("AV",out var av) ||
           !av.TryGetProperty("Fire",out var track) ||
           !FrameTarget(track,out var literal) ||
           !Pass60WeaponPoseMath.TryParse(literal,null,out var pose) ||
           pose.Origin.LengthSquared()>2.25f)
            return null;
        // Only a source-authored neutral return is modeled. Complex or
        // dynamic return scripts keep the conservative viewmodel fallback.
        if(!steps[1].TryGetProperty("AV",out var tail) ||
           !tail.TryGetProperty("Fire",out var reset) ||
           !FrameTarget(reset,out var resetLiteral) ||
           !Pass60WeaponPoseMath.TryParse(resetLiteral,null,out var rest) ||
           rest.Origin.LengthSquared()>.000001f ||
           Math.Abs(rest.Basis.GetRotationQuaternion().W-1f)>0.0001f)
            return null;
        return new Shot(pose,timeOut,timeBack);
    }

    private static float ClipTime(JsonElement animations,string name,
        float fallback)
    {
        if(Sequence(animations,name,out var steps) &&
           Duration(steps[0],out var time) && time>.001f)
            return time;
        return fallback;
    }

    private static ReloadClip? ReadReload(JsonElement stats,
        JsonElement animations)
    {
        if(!Field(stats,"GripCF",out var gripText) ||
           !Pass60WeaponPoseMath.TryParse(gripText,null,out var grip) ||
           !Sequence(animations,"Reload",out var steps))
            return null;
        var frames=new List<ReloadFrame>();
        var current=Transform3D.Identity;
        var now=0f;
        var targets=0;
        foreach(var step in steps.EnumerateArray())
        {
            if(!Duration(step,out var dt) || now+dt>18f)
                return null;
            var target=current;
            if(step.TryGetProperty("AV",out var av) &&
               av.TryGetProperty("Main",out var main) &&
               FrameTarget(main,out var literal) &&
               Pass60WeaponPoseMath.TryParse(literal,grip,out var absolute))
            {
                var relative=grip.Inverse()*absolute;
                if(relative.Origin.LengthSquared()<=2.25f)
                {
                    target=relative;
                    targets++;
                }
            }
            frames.Add(new ReloadFrame(now,now+dt,current,target));
            now+=dt;
            current=target;
        }
        if(targets<2 || now<.05f)return null;
        return new ReloadClip(frames.ToArray(),now);
    }

    private static Motion? Parse(JsonElement root)
    {
        if(!root.TryGetProperty("stats",out var stats) ||
           !root.TryGetProperty("animations",out var animations))
            return null;
        var pose=Transform3D.Identity;
        var hasSprint=Field(stats,"SprintCF",out var sprintText) &&
            Pass60WeaponPoseMath.TryParse(sprintText,null,out pose);
        var sprint=hasSprint ? pose : Transform3D.Identity;
        var hip=ReadFire(animations,"Fire");
        var aimed=ReadFire(animations,"AimFire");
        var reload=ReadReload(stats,animations);
        if(!hasSprint && hip is null && aimed is null && reload is null)
            return null;
        return new Motion(sprint,hasSprint,
            ClipTime(animations,"Sprint",.4f),
            ClipTime(animations,"UnSprint",.3f),
            hip,aimed,reload);
    }

    private static void Load()
    {
        if(_motions is not null)return;
        var found=new Dictionary<string,Motion>(StringComparer.Ordinal);
        try
        {
            using var catalog=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/catalog.json"));
            foreach(var record in catalog.RootElement
                .GetProperty("weapons").EnumerateArray())
            {
                var path=record.GetProperty("path").GetString()??"";
                if(path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.StartsWith('/') ||
                   path.Contains('\\'))continue;
                try
                {
                    using var data=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var root=data.RootElement;
                    var name=root.GetProperty("name").GetString()??"";
                    if(name.Length is <1 or >128)continue;
                    var motion=Parse(root);
                    if(motion is not null)found[name]=motion;
                }
                catch(Exception e)
                {
                    GD.PushWarning("TWR_PASS60_MOTION_ENTRY_SKIPPED "+
                        path+" "+e.Message);
                }
            }
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS60_MOTION_FALLBACK "+e.Message);
        }
        _motions=found;
        GD.Print($"TWR_PASS60_SOURCE_ANIMATION_READY weapons={found.Count}");
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass63: original per-weapon Reload ModelMotor.Mag keyframes.
/// Resolves authored Rels.MagRel through Pass62's SHA-verified source
/// Handle hierarchy; absolute numeric CFrames remain inert data.
/// No Roblox scripts, network calls or gameplay ammo edits are performed.
/// </summary>
public static class Pass63SourceMagazineReload
{
    public readonly record struct Target(Transform3D Pose,bool RelativeToHandle);
    public enum EaseKind { Linear, SlowDown, SpeedUp }
    public readonly record struct Cue(
        float At,float Seconds,bool Animate,EaseKind Ease,
        Target Destination,Target? Start);
    public sealed record Clip(Cue[] Cues,float Duration);

    private static Dictionary<string,Clip>? _clips;
    public static int ProfileCount
    {
        get { Load();return _clips!.Count; }
    }

    public static bool TryGet(string name,out Clip? clip)
    {
        Load();
        if(_clips!.TryGetValue(name,out var found))
        {
            clip=found;return true;
        }
        clip=null;return false;
    }

    /// <summary>
    /// Strict symbolic grammar: only the exact authored MagRel prefix and
    /// optional "* CFrame..." literals, or fully literal absolute CFrames.
    /// The dynamic definition of MagRel is never evaluated; it is resolved
    /// from the separately verified original Mag.Handle transform.
    /// </summary>
    public static bool TryParseTarget(string? source,out Target target)
    {
        target=default;
        if(string.IsNullOrWhiteSpace(source) || source.Length>640)
            return false;
        const string symbol="module.Stats.Rels.MagRel";
        bool relative=false;
        string literal=source;
        if(source==symbol)
        {
            relative=true;
            literal="CFrame.new()";
        }
        else if(source.StartsWith(symbol+" * ",StringComparison.Ordinal))
        {
            relative=true;
            literal=source[(symbol.Length+3)..];
        }
        if(!Pass60WeaponPoseMath.TryParse(literal,null,out var raw) ||
           raw.Origin.LengthSquared()>2.25f)
            return false;
        // The normalized source transform is Roblox +Z back. Reflect its
        // position and orientation to the Godot -Z-forward convention.
        var reflect=new Basis(Vector3.Right,Vector3.Up,Vector3.Forward);
        var converted=new Transform3D(
            reflect*raw.Basis*reflect,reflect*raw.Origin);
        if(!float.IsFinite(converted.Basis.Determinant()) ||
           Math.Abs(converted.Basis.Determinant()-1f)>.03f)
            return false;
        target=new Target(converted,relative);
        return true;
    }

    private static bool Time(JsonElement step,out float value)
    {
        value=0f;
        return step.TryGetProperty("Time",out var input) &&
            input.ValueKind==JsonValueKind.Number &&
            input.TryGetSingle(out value) &&
            float.IsFinite(value) && value>=0f && value<=3f;
    }

    private static string? Expression(JsonElement motor,string name)
    {
        if(motor.TryGetProperty(name,out var obj) &&
           obj.ValueKind==JsonValueKind.Object &&
           obj.TryGetProperty("expression",out var e) &&
           e.ValueKind==JsonValueKind.String)
            return e.GetString();
        return null;
    }

    private static bool ExactMagMotor(JsonElement motor)
    {
        if(!motor.TryGetProperty("MotorHierarchy",out var path) ||
           path.ValueKind!=JsonValueKind.Array ||
           path.GetArrayLength()!=3)return false;
        return path[0].GetString()=="Mag" &&
               path[1].GetString()=="Handle" &&
               path[2].GetString()=="MagMotor";
    }

    private static EaseKind Ease(JsonElement motor)
    {
        if(!motor.TryGetProperty("Ease",out var value) ||
           value.ValueKind!=JsonValueKind.String)return EaseKind.Linear;
        return value.GetString() switch
        {
            "SlowDown" => EaseKind.SlowDown,
            "SpeedUp" => EaseKind.SpeedUp,
            _ => EaseKind.Linear
        };
    }

    private static Clip? Read(JsonElement root)
    {
        if(!root.TryGetProperty("animations",out var animations) ||
           !animations.TryGetProperty("Reload",out var reload) ||
           !reload.TryGetProperty("Sequence",out var steps) ||
           steps.ValueKind!=JsonValueKind.Array ||
           steps.GetArrayLength() is <2 or >128)
            return null;
        var cues=new List<Cue>();
        float at=0f;
        bool moved=false,returned=false;
        foreach(var step in steps.EnumerateArray())
        {
            if(!Time(step,out var dt) || at+dt>18f)return null;
            if(step.TryGetProperty("ModelMotor",out var motors) &&
               motors.ValueKind==JsonValueKind.Object &&
               motors.TryGetProperty("Mag",out var motor) &&
               motor.ValueKind==JsonValueKind.Object &&
               ExactMagMotor(motor) &&
               TryParseTarget(Expression(motor,"To"),out var destination))
            {
                bool lerp=!motor.TryGetProperty("Lerp",out var flag) ||
                          flag.ValueKind!=JsonValueKind.False;
                var duration=dt;
                if(motor.TryGetProperty("Time",out var overrideTime) &&
                   overrideTime.ValueKind==JsonValueKind.Number &&
                   overrideTime.TryGetSingle(out var n) &&
                   float.IsFinite(n) && n>=0f && n<=3f)
                    duration=n;
                Target? start=null;
                if(TryParseTarget(Expression(motor,"Start"),out var explicitFrom))
                    start=explicitFrom;
                cues.Add(new Cue(at,lerp?duration:0f,lerp,Ease(motor),
                    destination,start));
                moved |= !destination.RelativeToHandle ||
                    destination.Pose.Origin.LengthSquared()>.0001f ||
                    Math.Abs(destination.Pose.Basis.GetRotationQuaternion().W-1f)>.0001f;
                returned=destination.RelativeToHandle &&
                    destination.Pose.Origin.LengthSquared()<.000001f &&
                    Math.Abs(destination.Pose.Basis.GetRotationQuaternion().W-1f)<.00001f;
            }
            at+=dt;
        }
        // No guessing if source has no documented rest keyframe.
        return at>.05f && cues.Count is >=2 and <=40 &&
               moved && returned
            ? new Clip(cues.ToArray(),at) : null;
    }

    private static float EaseFraction(float t,EaseKind ease)
    {
        t=Math.Clamp(t,0f,1f);
        return ease switch
        {
            EaseKind.SlowDown => 1f-(1f-t)*(1f-t),
            EaseKind.SpeedUp => t*t,
            _ => t
        };
    }

    public static Transform3D Sample(Clip clip,float progress,
        Pass62SourceWeaponHierarchy.Binding binding)
    {
        if(!binding.TryGetGroupRestPose("Mag",out var home))
            return Transform3D.Identity;
        if(!float.IsFinite(progress) || progress<=0f ||
           progress>=1f || clip.Cues.Length==0)return home;
        var now=progress*clip.Duration;
        var from=home;
        var to=home;
        var startTime=0f;
        var seconds=0f;
        var easing=EaseKind.Linear;
        foreach(var cue in clip.Cues)
        {
            if(cue.At>now)break;
            var elapsed=seconds<=.0001f ? 1f :
                EaseFraction((cue.At-startTime)/seconds,easing);
            var previous=Pass60WeaponPoseMath.Interpolate(from,to,elapsed);
            from=cue.Start is { } explicitStart
                ? binding.ResolveSourcePose("Mag",explicitStart.Pose,
                    explicitStart.RelativeToHandle)
                : previous;
            to=binding.ResolveSourcePose("Mag",cue.Destination.Pose,
                cue.Destination.RelativeToHandle);
            startTime=cue.At;
            seconds=cue.Animate ? cue.Seconds : 0f;
            easing=cue.Ease;
        }
        var t=seconds<=.0001f ? 1f :
            EaseFraction((now-startTime)/seconds,easing);
        return Pass60WeaponPoseMath.Interpolate(from,to,t);
    }

    private static void Load()
    {
        if(_clips is not null)return;
        var found=new Dictionary<string,Clip>(StringComparer.Ordinal);
        try
        {
            using var catalog=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/catalog.json"));
            foreach(var row in catalog.RootElement.GetProperty(
                "weapons").EnumerateArray())
            {
                var path=row.GetProperty("path").GetString()??"";
                if(path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.Contains('\\') ||
                   path.StartsWith('/'))continue;
                try
                {
                    using var entry=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var root=entry.RootElement;
                    var name=root.GetProperty("name").GetString()??"";
                    if(name.Length is <1 or >128)continue;
                    var clip=Read(root);
                    if(clip is not null)found[name]=clip;
                }
                catch(Exception e)
                {
                    GD.PushWarning("TWR_PASS63_MAG_ENTRY_SKIPPED "+
                        path+" "+e.Message);
                }
            }
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS63_MAG_CATALOG_FALLBACK "+e.Message);
        }
        _clips=found;
        GD.Print($"TWR_PASS63_MAGAZINE_SOURCE_READY weapons={found.Count}");
    }
}

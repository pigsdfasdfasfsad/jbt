using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass65: original per-weapon LeftCF/RightCF and inert AV.LeftWeld /
/// AV.RightWeld equip, reload and cycling timelines. The rendered hands are
/// deliberately labeled local proxy geometry, not recovered Roblox avatars.
/// The only permitted source grammar is bounded literal CFrame.new/Angles,
/// the exact original pi/2 literal and exact LeftCF/RightCF symbols.
/// </summary>
public static class Pass65SourceHands
{
    public enum Hand { Left, Right }
    public enum Ease { Linear, SlowDown, SpeedUp }
    public readonly record struct Target(Transform3D Offset,string Group);
    public readonly record struct Cue(float At,float Duration,bool Animate,
        Ease Easing,Target Destination,Target? Start);
    public sealed record Track(Cue[] Cues);
    public sealed record Clip(Track Left,Track Right,float Duration);
    public sealed record Profile(Transform3D LeftRest,Transform3D RightRest,
        Clip? Equip,Clip? Reload,Clip? Cycling);

    private static Dictionary<string,Profile>? _profiles;
    public static int SourceProfileCount
    {
        get { Load();return _profiles!.Count; }
    }
    public static bool TryGet(string name,out Profile? profile)
    {
        Load();
        if(_profiles!.TryGetValue(name,out var entry))
        {
            profile=entry;return true;
        }
        profile=null;return false;
    }

    private static bool Angle(string token,out float result)
    {
        result=0f;
        if(token=="(math.pi/2)")result=Mathf.Pi/2f;
        else if(token=="(-math.pi/2)")result=-Mathf.Pi/2f;
        else if(!float.TryParse(token,NumberStyles.Float,
            CultureInfo.InvariantCulture,out result))
            return false;
        return float.IsFinite(result) &&
            Math.Abs(result)<=Mathf.Pi+.001f;
    }

    /// <summary>
    /// No expression parser / Luau execution. The only original symbolic
    /// numeric constant accepted is the exact string "(math.pi/2)".
    /// Returns Godot -Z-forward camera-local CFrame values.
    /// </summary>
    public static bool TryLiteralPose(string? expression,out Transform3D pose)
    {
        pose=Transform3D.Identity;
        if(string.IsNullOrWhiteSpace(expression) ||
           expression.Length>640)return false;
        var terms=expression.Split(" * ",StringSplitOptions.None);
        if(terms.Length is <1 or >8)return false;
        var value=Transform3D.Identity;
        foreach(var term in terms)
        {
            Transform3D factor;
            if(term.StartsWith("CFrame.new(",StringComparison.Ordinal))
            {
                if(!Pass60WeaponPoseMath.TryParse(term,null,out factor))
                    return false;
            }
            else if(term.StartsWith("CFrame.Angles(",StringComparison.Ordinal) &&
                    term.EndsWith(')'))
            {
                var arguments=term[14..^1].Split(',',
                    StringSplitOptions.TrimEntries);
                if(arguments.Length!=3 ||
                   !Angle(arguments[0],out var x) ||
                   !Angle(arguments[1],out var y) ||
                   !Angle(arguments[2],out var z))
                    return false;
                var rotation=new Basis(Vector3.Right,x)*
                    new Basis(Vector3.Up,y)*
                    new Basis(Vector3.Back,z);
                factor=new Transform3D(rotation,Vector3.Zero);
            }
            else return false;
            value*=factor;
            if(value.Origin.LengthSquared()>9f)return false;
        }
        var reflect=new Basis(Vector3.Right,Vector3.Up,Vector3.Forward);
        var converted=new Transform3D(
            reflect*value.Basis*reflect,reflect*value.Origin);
        if(!float.IsFinite(converted.Basis.Determinant()) ||
           Math.Abs(converted.Basis.Determinant()-1f)>.03f)return false;
        pose=converted;
        return true;
    }

    private static bool ParseDestination(string? source,Hand hand,
        Transform3D leftHome,Transform3D rightHome,
        out Transform3D pose)
    {
        pose=Transform3D.Identity;
        if(source is null)return false;
        if(source=="module.Stats.LeftCF" && hand==Hand.Left)
        {
            pose=leftHome;return true;
        }
        if(source=="module.Stats.RightCF" && hand==Hand.Right)
        {
            pose=rightHome;return true;
        }
        return TryLiteralPose(source,out pose);
    }

    private static bool GroupName(JsonElement node,out string group)
    {
        group="";
        if(!node.TryGetProperty("TargetHierarchy",out var path) ||
           path.ValueKind!=JsonValueKind.Array ||
           path.GetArrayLength() is <1 or >4)return false;
        var segments=new List<string>();
        foreach(var value in path.EnumerateArray())
        {
            if(value.ValueKind!=JsonValueKind.String)return false;
            var segment=value.GetString()??"";
            if(segment.Length is <1 or >52 ||
               segment.Contains('/') || segment.Contains(".."))
                return false;
            segments.Add(segment);
        }
        if(segments.Count==1 && segments[0]=="Handle")return true;
        if(segments[^1]!="Handle")return false;
        segments.RemoveAt(segments.Count-1);
        group=string.Join("/",segments);
        return group.Length<=80;
    }

    private static string? Expression(JsonElement track,string key)
    {
        return track.TryGetProperty(key,out var container) &&
            container.ValueKind==JsonValueKind.Object &&
            container.TryGetProperty("expression",out var entry) &&
            entry.ValueKind==JsonValueKind.String
            ? entry.GetString() : null;
    }

    private static bool Time(JsonElement item,out float dt)
    {
        dt=0f;
        return item.TryGetProperty("Time",out var t) &&
            t.ValueKind==JsonValueKind.Number &&
            t.TryGetSingle(out dt) &&
            float.IsFinite(dt) && dt>=0f && dt<=3f;
    }

    private static Ease ReadEase(JsonElement record)
    {
        if(!record.TryGetProperty("Ease",out var node) ||
           node.ValueKind!=JsonValueKind.String)return Ease.Linear;
        return node.GetString() switch
        {
            "SlowDown"=>Ease.SlowDown,
            "SpeedUp"=>Ease.SpeedUp,
            _=>Ease.Linear
        };
    }

    private static Track ReadTrack(List<Cue> cues) =>
        new(cues.ToArray());

    private static Clip? ReadClip(JsonElement animation,string name,
        Transform3D leftHome,Transform3D rightHome)
    {
        if(!animation.TryGetProperty(name,out var section) ||
           !section.TryGetProperty("Sequence",out var sequence) ||
           sequence.ValueKind!=JsonValueKind.Array ||
           sequence.GetArrayLength() is <1 or >128)return null;
        var left=new List<Cue>();
        var right=new List<Cue>();
        var now=0f;
        foreach(var step in sequence.EnumerateArray())
        {
            if(!Time(step,out var dt) || now+dt>18f)return null;
            if(step.TryGetProperty("AV",out var av) &&
               av.ValueKind==JsonValueKind.Object)
            {
                foreach(var side in new[]{Hand.Left,Hand.Right})
                {
                    var key=side==Hand.Left ? "LeftWeld":"RightWeld";
                    if(!av.TryGetProperty(key,out var record) ||
                       record.ValueKind!=JsonValueKind.Object ||
                       (record.TryGetProperty("TargetChar",out var charFlag) &&
                        charFlag.ValueKind==JsonValueKind.True) ||
                       !GroupName(record,out var group) ||
                       !ParseDestination(Expression(record,"OffsetEnd"),
                           side,leftHome,rightHome,out var destination) ||
                       destination.Origin.LengthSquared()>9f)
                        continue;
                    var duration=dt;
                    if(record.TryGetProperty("Time",out var time) &&
                       time.ValueKind==JsonValueKind.Number &&
                       time.TryGetSingle(out var n) &&
                       float.IsFinite(n) && n>=0f && n<=3f)
                        duration=n;
                    var animate=!record.TryGetProperty("Lerp",out var lerp) ||
                                lerp.ValueKind!=JsonValueKind.False;
                    Target? start=null;
                    if(ParseDestination(Expression(record,"OffsetStart"),
                        side,leftHome,rightHome,out var explicitFrom))
                        start=new Target(explicitFrom,group);
                    var cue=new Cue(now,animate?duration:0f,
                        animate,ReadEase(record),
                        new Target(destination,group),start);
                    if(side==Hand.Left)left.Add(cue);
                    else right.Add(cue);
                }
            }
            now+=dt;
        }
        if(left.Count==0 && right.Count==0 || now<=.001f)
            return null;
        return new Clip(ReadTrack(left),ReadTrack(right),now);
    }

    private static Profile? Read(JsonElement root)
    {
        if(!root.TryGetProperty("stats",out var stats) ||
           !root.TryGetProperty("animations",out var animation) ||
           !stats.TryGetProperty("LeftCF",out var left) ||
           !stats.TryGetProperty("RightCF",out var right) ||
           left.ValueKind!=JsonValueKind.Object ||
           right.ValueKind!=JsonValueKind.Object ||
           !left.TryGetProperty("expression",out var leftExp) ||
           !right.TryGetProperty("expression",out var rightExp) ||
           !TryLiteralPose(leftExp.GetString(),out var lh) ||
           !TryLiteralPose(rightExp.GetString(),out var rh) ||
           lh.Origin.LengthSquared()>2.25f ||
           rh.Origin.LengthSquared()>2.25f)
            return null;
        return new Profile(lh,rh,
            ReadClip(animation,"Equip",lh,rh),
            ReadClip(animation,"Reload",lh,rh),
            ReadClip(animation,"Sliding",lh,rh));
    }

    private static float EaseFraction(float t,Ease easing)
    {
        t=Math.Clamp(t,0f,1f);
        return easing switch
        {
            Ease.SlowDown=>1f-(1f-t)*(1f-t),
            Ease.SpeedUp=>t*t,
            _=>t
        };
    }

    private static bool Resolve(Target target,
        Pass62SourceWeaponHierarchy.Binding? binding,
        out Transform3D result)
    {
        result=target.Offset;
        if(target.Group.Length==0)return true;
        if(binding is null ||
           !binding.TryGetCurrentGroupPose(target.Group,out var group))
            return false;
        result=group*target.Offset;
        return result.Origin.LengthSquared()<=9f;
    }

    public static Transform3D Sample(Profile profile,Clip? clip,
        Hand side,float elapsed,Pass62SourceWeaponHierarchy.Binding? binding)
    {
        var home=side==Hand.Left?profile.LeftRest:profile.RightRest;
        if(clip is null || !float.IsFinite(elapsed) ||
           elapsed<=0f || elapsed>=clip.Duration)return home;
        var track=side==Hand.Left?clip.Left:clip.Right;
        if(track.Cues.Length==0)return home;
        var from=home;
        var to=home;
        var start=0f;
        var duration=0f;
        var easing=Ease.Linear;
        foreach(var cue in track.Cues)
        {
            if(cue.At>elapsed)break;
            if(!Resolve(cue.Destination,binding,out var destination))
                continue;
            var previous=Pass60WeaponPoseMath.Interpolate(from,to,
                duration<=.0001f ? 1f :
                EaseFraction((cue.At-start)/duration,easing));
            from=cue.Start is { } explicitStart &&
                 Resolve(explicitStart,binding,out var sourceStart)
                    ? sourceStart : previous;
            to=destination;
            start=cue.At;
            duration=cue.Animate?cue.Duration:0f;
            easing=cue.Easing;
        }
        return Pass60WeaponPoseMath.Interpolate(from,to,
            duration<=.0001f ? 1f :
            EaseFraction((elapsed-start)/duration,easing));
    }

    private static void Load()
    {
        if(_profiles is not null)return;
        var found=new Dictionary<string,Profile>(StringComparer.Ordinal);
        try
        {
            using var catalog=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/catalog.json"));
            foreach(var entry in catalog.RootElement.GetProperty(
                "weapons").EnumerateArray())
            {
                var path=entry.GetProperty("path").GetString()??"";
                if(path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.Contains('\\') ||
                   path.StartsWith('/'))continue;
                try
                {
                    using var file=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var root=file.RootElement;
                    var weapon=root.GetProperty("name").GetString()??"";
                    if(weapon.Length is <1 or >128)continue;
                    var profile=Read(root);
                    if(profile is not null)found[weapon]=profile;
                }
                catch(Exception ex)
                {
                    GD.PushWarning("TWR_PASS65_HAND_SOURCE_ENTRY "+
                        path+" "+ex.Message);
                }
            }
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS65_HAND_SOURCE_FALLBACK "+ex.Message);
        }
        _profiles=found;
        GD.Print($"TWR_PASS65_SOURCE_HANDS_READY weapons={found.Count}");
    }
}

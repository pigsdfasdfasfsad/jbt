using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass61: data-only original weapon Equip AV.Main and Slide/Pump
/// ModelMotor animation tracks. Script expressions are never evaluated.
/// Motor offsets require exact source-authored Rels.*Rel references and
/// numeric literal suffixes; absolute, dynamic or unknown motors are skipped.
/// </summary>
public static class Pass61SourceWeaponHandling
{
    public readonly record struct EquipFrame(float Start,float End,
        Transform3D From,Transform3D To);
    public sealed record EquipClip(EquipFrame[] Frames,float Duration);
    public readonly record struct MotorFrame(float Start,float End,
        Vector3 From,Vector3 To);
    public sealed record MotorClip(MotorFrame[] Frames,float Duration);
    public sealed record Profile(EquipClip? Equip,
        MotorClip? CyclingSlide,MotorClip? CyclingPump,
        MotorClip? ReloadSlide,MotorClip? ReloadPump);

    private static Dictionary<string,Profile>? _cache;
    public static int ProfileCount
    {
        get { Load();return _cache!.Count; }
    }
    public static bool TryGet(string name,out Profile? profile)
    {
        Load();
        if(_cache!.TryGetValue(name,out var found))
        {
            profile=found;return true;
        }
        profile=null;return false;
    }

    public static Transform3D SampleEquip(EquipClip clip,float elapsed)
    {
        if(clip.Frames.Length==0 || !float.IsFinite(elapsed) ||
           elapsed<=0f || elapsed>=clip.Duration)
            return Transform3D.Identity;
        foreach(var frame in clip.Frames)
        {
            if(elapsed>frame.End)continue;
            var dt=Math.Max(.0001f,frame.End-frame.Start);
            return Pass60WeaponPoseMath.Interpolate(frame.From,frame.To,
                (elapsed-frame.Start)/dt);
        }
        return Transform3D.Identity;
    }

    public static Vector3 SampleMotor(MotorClip clip,float elapsed)
    {
        if(clip.Frames.Length==0 || !float.IsFinite(elapsed) ||
           elapsed<=0f || elapsed>=clip.Duration)
            return Vector3.Zero;
        foreach(var frame in clip.Frames)
        {
            if(elapsed>frame.End)continue;
            var dt=Math.Max(.0001f,frame.End-frame.Start);
            return frame.From.Lerp(frame.To,
                Math.Clamp((elapsed-frame.Start)/dt,0f,1f));
        }
        return Vector3.Zero;
    }

    // The authored coordinate uses Roblox +Z backwards. Godot's view
    // convention is -Z forwards. The group pivot is an approximation:
    // old Pass47 weapon meshes preserve parts but flatten motor hierarchy.
    public static bool TryRelativeMotorOffset(string? expression,
        string relation,out Vector3 delta)
    {
        delta=Vector3.Zero;
        if(relation is not ("SlideRel" or "PumpRel" or "MagRel"))
            return false;
        var prefix="module.Stats.Rels."+relation;
        if(expression==prefix)return true;
        if(expression is null || expression.Length>240 ||
           !expression.StartsWith(prefix+" * ",StringComparison.Ordinal))
            return false;
        var literal=expression[(prefix.Length+3)..];
        if(!Pass60WeaponPoseMath.TryParse(literal,null,out var pose) ||
           pose.Origin.LengthSquared()>.16f ||
           Math.Abs(pose.Basis.GetRotationQuaternion().W)<.99999f)
            return false;
        delta=new Vector3(pose.Origin.X,pose.Origin.Y,-pose.Origin.Z);
        return true;
    }

    private static bool StepDuration(JsonElement step,out float seconds)
    {
        seconds=0f;
        return step.TryGetProperty("Time",out var field) &&
            field.ValueKind==JsonValueKind.Number &&
            field.TryGetSingle(out seconds) &&
            float.IsFinite(seconds) && seconds>=0f && seconds<=3f;
    }

    private static bool Sequence(JsonElement animations,string name,
        out JsonElement steps)
    {
        steps=default;
        return animations.TryGetProperty(name,out var data) &&
            data.TryGetProperty("Sequence",out steps) &&
            steps.ValueKind==JsonValueKind.Array &&
            steps.GetArrayLength() is >0 and <=128;
    }

    private static string? Destination(JsonElement track)
    {
        if(track.ValueKind==JsonValueKind.Object &&
           track.TryGetProperty("To",out var to) &&
           to.ValueKind==JsonValueKind.Object &&
           to.TryGetProperty("expression",out var expression) &&
           expression.ValueKind==JsonValueKind.String)
            return expression.GetString();
        return null;
    }

    private static EquipClip? ReadEquip(JsonElement stats,
        JsonElement animations)
    {
        if(!stats.TryGetProperty("GripCF",out var gripInfo) ||
           !gripInfo.TryGetProperty("expression",out var gripExpression) ||
           !Pass60WeaponPoseMath.TryParse(
               gripExpression.GetString(),null,out var grip) ||
           !Sequence(animations,"Equip",out var steps))
            return null;
        var frames=new List<EquipFrame>();
        var current=Transform3D.Identity;
        var total=0f;var targets=0;
        foreach(var step in steps.EnumerateArray())
        {
            if(!StepDuration(step,out var dt) || total+dt>18f)
                return null;
            var next=current;
            if(step.TryGetProperty("AV",out var av) &&
               av.TryGetProperty("Main",out var main))
            {
                var expression=Destination(main);
                if(Pass60WeaponPoseMath.TryParse(expression,grip,
                    out var absolute))
                {
                    var relative=grip.AffineInverse()*absolute;
                    if(relative.Origin.LengthSquared()<=2.25f)
                    {
                        next=relative;targets++;
                    }
                }
            }
            frames.Add(new EquipFrame(total,total+dt,current,next));
            current=next;total+=dt;
        }
        return targets>=2 && total>.05f
            ? new EquipClip(frames.ToArray(),total) : null;
    }

    private static MotorClip? ReadMotor(JsonElement animations,string clipName,
        string trackName,string group,string relation)
    {
        if(!Sequence(animations,clipName,out var steps))return null;
        var frames=new List<MotorFrame>();
        var current=Vector3.Zero;var time=0f;
        var hits=0;var moved=false;
        foreach(var step in steps.EnumerateArray())
        {
            if(!StepDuration(step,out var dt) || time+dt>18f)return null;
            var next=current;
            if(step.TryGetProperty("ModelMotor",out var motors) &&
               motors.TryGetProperty(trackName,out var motor) &&
               motor.TryGetProperty("MotorHierarchy",out var hierarchy) &&
               hierarchy.ValueKind==JsonValueKind.Array &&
               hierarchy.GetArrayLength()==3 &&
               hierarchy[0].GetString()==group &&
               hierarchy[1].GetString()=="Handle" &&
               hierarchy[2].GetString()==group+"Motor")
            {
                if(TryRelativeMotorOffset(Destination(motor),relation,
                    out var offset))
                {
                    next=offset;hits++;
                    moved|=offset.LengthSquared()>.000001f;
                }
            }
            frames.Add(new MotorFrame(time,time+dt,current,next));
            current=next;time+=dt;
        }
        return hits>=2 && moved && time>.05f
            ? new MotorClip(frames.ToArray(),time) : null;
    }

    private static Profile? Read(JsonElement source)
    {
        if(!source.TryGetProperty("stats",out var stats) ||
           !source.TryGetProperty("animations",out var animations))
            return null;
        var equip=ReadEquip(stats,animations);
        var slide=ReadMotor(animations,"Sliding","SlideSliding",
            "Slide","SlideRel");
        var pump=ReadMotor(animations,"Sliding","Pump",
            "Pump","PumpRel");
        var reloadSlide=ReadMotor(animations,"Reload","SlideSliding",
            "Slide","SlideRel");
        var reloadPump=ReadMotor(animations,"Reload","Pump",
            "Pump","PumpRel");
        return equip is null && slide is null && pump is null &&
            reloadSlide is null && reloadPump is null
            ? null : new Profile(equip,slide,pump,reloadSlide,reloadPump);
    }

    private static void Load()
    {
        if(_cache is not null)return;
        var found=new Dictionary<string,Profile>(StringComparer.Ordinal);
        try
        {
            using var catalog=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/catalog.json"));
            foreach(var row in catalog.RootElement
                .GetProperty("weapons").EnumerateArray())
            {
                var path=row.GetProperty("path").GetString()??"";
                if(path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.Contains('\\') ||
                   path.StartsWith('/'))continue;
                try
                {
                    using var record=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var root=record.RootElement;
                    var name=root.GetProperty("name").GetString()??"";
                    if(name.Length is <1 or >128)continue;
                    var result=Read(root);
                    if(result is not null)found[name]=result;
                }
                catch(Exception ex)
                {
                    GD.PushWarning("TWR_PASS61_SOURCE_ENTRY_SKIPPED "+
                        path+" "+ex.Message);
                }
            }
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS61_SOURCE_HANDLING_FALLBACK "+ex.Message);
        }
        _cache=found;
        GD.Print($"TWR_PASS61_SOURCE_HANDLING_READY weapons={found.Count}");
    }
}

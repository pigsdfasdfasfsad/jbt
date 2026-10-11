using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass64. Data-only source cycling/reload action keyframes: original
/// bolt-carrier translation, nested Bolt Handle twist, and shell ejection.
/// No Luau evaluation. Shell geometry / ejection origin are approximations
/// when original mesh/port data are unavailable. Combat remains authoritative
/// in the original player/domain runtime.
/// </summary>
public static class Pass64SourceAction
{
    public enum EaseKind { Linear, SlowDown, SpeedUp }
    public readonly record struct MotionCue(float At,float Seconds,
        float From,float To,EaseKind Ease);
    public readonly record struct ShellCue(float At,string Name,
        Vector3 Direction,float SpeedMetersPerSecond,Vector3 Offset);
    public sealed record Clip(MotionCue[] Bolt,MotionCue[] BoltHandle,
        ShellCue[] Ejections,float Duration);
    public sealed record Profile(Clip Cycling,Clip Reload,bool IncrementalReload);

    private static Dictionary<string,Profile>? _profiles;
    public static int ProfileCount {get { Load();return _profiles!.Count; }}

    public static bool TryGet(string name,out Profile? profile)
    {
        Load();
        if(_profiles!.TryGetValue(name,out var found))
        {
            profile=found;return true;
        }
        profile=null;return false;
    }

    public static float Sample(MotionCue[] track,float elapsed)
    {
        if(!float.IsFinite(elapsed) || track.Length==0 ||
           elapsed<=0f)return 0f;
        float result=0f;
        foreach(var cue in track)
        {
            if(elapsed<cue.At)break;
            var t=cue.Seconds<=.0001f?1f:
                Math.Clamp((elapsed-cue.At)/cue.Seconds,0f,1f);
            t=cue.Ease switch
            {
                EaseKind.SlowDown=>1f-(1f-t)*(1f-t),
                EaseKind.SpeedUp=>t*t,
                _=>t
            };
            result=Mathf.Lerp(cue.From,cue.To,t);
        }
        return result;
    }

    /// <summary>
    /// Accept only the literal authored 0..2 rad bolt-handle Z-angle,
    /// including exactly "(math.pi/2)" present in the Mosin source.
    /// Generic math.pi formulas remain rejected.
    /// </summary>
    public static bool TryBoltHandleAngle(string? expression,out float radians)
    {
        radians=0f;
        if(expression=="CFrame.new()")return true;
        const string prefix="CFrame.Angles(0, 0, ";
        if(expression is null || expression.Length>80 ||
           !expression.StartsWith(prefix,StringComparison.Ordinal) ||
           !expression.EndsWith(')'))
            return false;
        var term=expression[prefix.Length..^1];
        float sourceAngle;
        if(term=="(math.pi/2)")sourceAngle=Mathf.Pi/2f;
        else if(term=="(-math.pi/2)")sourceAngle=-Mathf.Pi/2f;
        else if(!float.TryParse(term,NumberStyles.Float,
                   CultureInfo.InvariantCulture,out sourceAngle))
            return false;
        if(!float.IsFinite(sourceAngle) || Math.Abs(sourceAngle)>2.0f)
            return false;
        // Reflect Roblox +Z axis to camera-local Godot -Z.
        radians=-sourceAngle;
        return true;
    }

    public static bool TryShellDirection(string? expression,
        out Vector3 direction)
    {
        direction=Vector3.Zero;
        const string prefix="Vector3.new(";
        if(expression is null || expression.Length>130 ||
           !expression.StartsWith(prefix,StringComparison.Ordinal) ||
           !expression.EndsWith(')'))
            return false;
        var nums=expression[prefix.Length..^1].Split(',',
            StringSplitOptions.TrimEntries);
        if(nums.Length!=3)return false;
        var values=new float[3];
        for(var i=0;i<3;i++)
            if(nums[i].Length is <1 or >26 ||
               !float.TryParse(nums[i],NumberStyles.Float,
                   CultureInfo.InvariantCulture,out values[i]) ||
               !float.IsFinite(values[i]) || Math.Abs(values[i])>10f)
                return false;
        var vector=new Vector3(values[0],values[1],-values[2]);
        if(vector.LengthSquared()<.0001f)return false;
        direction=vector.Normalized();
        return true;
    }

    private static bool Time(JsonElement entry,out float length)
    {
        length=0f;
        return entry.TryGetProperty("Time",out var element) &&
            element.ValueKind==JsonValueKind.Number &&
            element.TryGetSingle(out length) &&
            float.IsFinite(length) && length>=0f && length<=3f;
    }

    private static string? Expression(JsonElement value)
    {
        return value.ValueKind==JsonValueKind.Object &&
            value.TryGetProperty("To",out var target) &&
            target.ValueKind==JsonValueKind.Object &&
            target.TryGetProperty("expression",out var expression) &&
            expression.ValueKind==JsonValueKind.String
                ? expression.GetString() : null;
    }

    private static bool MotorPath(JsonElement motor,bool handle)
    {
        if(!motor.TryGetProperty("MotorHierarchy",out var nodes) ||
           nodes.ValueKind!=JsonValueKind.Array ||
           nodes.GetArrayLength()!=(handle?4:3))return false;
        if(!handle)
            return nodes[0].GetString()=="Bolt" &&
                nodes[1].GetString()=="Handle" &&
                nodes[2].GetString()=="BoltMotor";
        return nodes[0].GetString()=="Bolt" &&
            nodes[1].GetString()=="Bolt Handle" &&
            nodes[2].GetString()=="Handle" &&
            nodes[3].GetString()=="Bolt HandleMotor";
    }

    private static EaseKind Ease(JsonElement motor)
    {
        if(!motor.TryGetProperty("Ease",out var e) ||
           e.ValueKind!=JsonValueKind.String)return EaseKind.Linear;
        return e.GetString() switch
        {
            "SlowDown"=>EaseKind.SlowDown,
            "SpeedUp"=>EaseKind.SpeedUp,
            _=>EaseKind.Linear
        };
    }

    private static float MotorDuration(JsonElement motor,float dt)
    {
        if(motor.TryGetProperty("Lerp",out var lerp) &&
           lerp.ValueKind==JsonValueKind.False)return 0f;
        if(motor.TryGetProperty("Time",out var explicitTime) &&
           explicitTime.ValueKind==JsonValueKind.Number &&
           explicitTime.TryGetSingle(out var length) &&
           float.IsFinite(length) && length>=0f && length<=3f)
            return length;
        return dt;
    }

    private static bool Shell(JsonElement raw,float at,
        List<ShellCue> events)
    {
        if(raw.ValueKind!=JsonValueKind.Object ||
           !raw.TryGetProperty("Shells",out var shells) ||
           shells.ValueKind!=JsonValueKind.Array ||
           shells.GetArrayLength() is <1 or >8)return false;
        foreach(var spec in shells.EnumerateArray())
        {
            if(!spec.TryGetProperty("Name",out var nameValue) ||
               nameValue.ValueKind!=JsonValueKind.String)
                continue;
            var name=nameValue.GetString()??"";
            if(name.Length is <2 or >48 ||
               !name.EndsWith("Shell",StringComparison.Ordinal))
                continue;
            if(!spec.TryGetProperty("Vel",out var velocity) ||
               velocity.ValueKind!=JsonValueKind.Number ||
               !velocity.TryGetSingle(out var speed) ||
               !float.IsFinite(speed) || speed is <1f or >80f ||
               !spec.TryGetProperty("Dir",out var dir) ||
               dir.ValueKind!=JsonValueKind.Object ||
               !dir.TryGetProperty("expression",out var expression) ||
               expression.ValueKind!=JsonValueKind.String ||
               !TryShellDirection(expression.GetString(),out var heading))
                continue;
            var offset=Vector3.Zero;
            if(spec.TryGetProperty("Offset",out var obj) &&
               obj.ValueKind==JsonValueKind.Object &&
               obj.TryGetProperty("expression",out var str) &&
               str.ValueKind==JsonValueKind.String &&
               Pass60WeaponPoseMath.TryParse(str.GetString(),null,
                   out var pose))
            {
                offset=new Vector3(pose.Origin.X,pose.Origin.Y,-pose.Origin.Z);
                if(offset.LengthSquared()>.5f)offset=Vector3.Zero;
            }
            events.Add(new ShellCue(at,name,heading,
                speed*RobloxUnits.MetersPerStud,offset));
        }
        return events.Count>0;
    }

    private static Clip ReadClip(JsonElement animations,string name)
    {
        if(!animations.TryGetProperty(name,out var node) ||
           !node.TryGetProperty("Sequence",out var steps) ||
           steps.ValueKind!=JsonValueKind.Array ||
           steps.GetArrayLength() is <1 or >128)
            return new Clip([],[],[],0f);
        var bolt=new List<MotionCue>();
        var handle=new List<MotionCue>();
        var ejections=new List<ShellCue>();
        float at=0f,boltAt=0f,handleAt=0f;
        foreach(var step in steps.EnumerateArray())
        {
            if(!Time(step,out var dt) || at+dt>18f)
                return new Clip([],[],[],0f);
            if(step.TryGetProperty("ModelMotor",out var motors) &&
               motors.ValueKind==JsonValueKind.Object &&
               motors.TryGetProperty("SlideSliding",out var motor) &&
               motor.ValueKind==JsonValueKind.Object)
            {
                if(MotorPath(motor,false) &&
                   Pass61SourceWeaponHandling.TryRelativeMotorOffset(
                       Expression(motor),"BoltRel",out var delta))
                {
                    var from=Sample(bolt.ToArray(),at);
                    bolt.Add(new MotionCue(at,MotorDuration(motor,dt),from,
                        delta.Z,Ease(motor)));
                    boltAt=at+dt;
                }
                else if(MotorPath(motor,true) &&
                    TryBoltHandleAngle(Expression(motor),out var rotation))
                {
                    var from=Sample(handle.ToArray(),at);
                    handle.Add(new MotionCue(at,MotorDuration(motor,dt),
                        from,rotation,Ease(motor)));
                    handleAt=at+dt;
                }
            }
            if(step.TryGetProperty("Shell",out var sourceShell))
                Shell(sourceShell,at,ejections);
            at+=dt;
        }
        // Leave position reset to Godot home when source path cannot prove
        // a safe reset. This does not imply recovered Motor6D C0/C1 joints.
        if(bolt.Count>0 && Math.Abs(bolt[^1].To)>.00001f)bolt.Clear();
        if(handle.Count>0 && Math.Abs(handle[^1].To)>.00001f)handle.Clear();
        return new Clip(bolt.ToArray(),handle.ToArray(),
            ejections.ToArray(),at);
    }

    private static Profile? Read(JsonElement root)
    {
        if(!root.TryGetProperty("animations",out var animations) ||
           !root.TryGetProperty("stats",out var stats))return null;
        var cycling=ReadClip(animations,"Sliding");
        var reload=ReadClip(animations,"Reload");
        if(cycling.Bolt.Length==0 && cycling.BoltHandle.Length==0 &&
           cycling.Ejections.Length==0 && reload.Bolt.Length==0 &&
           reload.BoltHandle.Length==0)return null;
        var increment=stats.TryGetProperty("ReloadType",out var reloadType) &&
            reloadType.ValueKind==JsonValueKind.String &&
            reloadType.GetString()=="Increment";
        return new Profile(cycling,reload,increment);
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
                    var data=Read(root);
                    if(data is not null)found[weapon]=data;
                }
                catch(Exception e)
                {
                    GD.PushWarning("TWR_PASS64_ACTION_SOURCE_ENTRY "+
                        path+" "+e.Message);
                }
            }
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS64_ACTION_SOURCE_FALLBACK "+e.Message);
        }
        _profiles=found;
        GD.Print($"TWR_PASS64_SOURCE_ACTION_READY weapons={found.Count}");
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass67: inert original Aim/UnAim AV.Aim timing and easing, plus an
/// authored AimPart marker read ONLY from an independently verified
/// private Pass47 weapon assembly. Dynamic Luau AimCF is not executed.
/// Sight-marker centering is a geometric reconstruction, not proof of
/// exact source Roblox AimCF or the original scope rendering.
/// </summary>
public static class Pass67SourceAimAlignment
{
    public enum EaseKind { Linear, SlowDown, SlowDown2 }
    public readonly record struct Clip(float Duration,EaseKind Ease);
    public sealed record Profile(Clip Aim,Clip UnAim,
        Vector3 SourceCameraAimOffset);

    public const float SourceSightCameraDepth=.43f;
    private static Dictionary<string,Profile>? _profiles;
    public static int ProfileCount {get{Load();return _profiles!.Count;}}

    public static bool TryGet(string weapon,out Profile? profile)
    {
        Load();
        if(_profiles!.TryGetValue(weapon,out var found))
        {
            profile=found;return true;
        }
        profile=null;return false;
    }

    public static float Eased(float progress,EaseKind kind)
    {
        if(!float.IsFinite(progress))return 0f;
        var t=Math.Clamp(progress,0f,1f);
        return kind switch
        {
            EaseKind.SlowDown=>1f-(1f-t)*(1f-t),
            EaseKind.SlowDown2=>1f-(1f-t)*(1f-t)*(1f-t),
            _=>t
        };
    }

    public static bool TryAimRoot(Transform3D originalSight,
        out Transform3D cameraLocalRoot)
    {
        cameraLocalRoot=Transform3D.Identity;
        if(!Pass66ArmKinematics.Finite(originalSight.Origin) ||
           originalSight.Origin.LengthSquared()>4f)return false;
        var basis=originalSight.Basis;
        var determinant=basis.Determinant();
        if(!float.IsFinite(determinant) ||
           Math.Abs(determinant-1f)>.035f)return false;
        // AimPart records are relative to the original, invisible Tool
        // Handle. The body root is positioned so the sight lies ON the
        // camera -Z axis; the weapon keeps the source-aligned forward
        // direction rather than reversing to match a rotated helper.
        var targetPoint=new Vector3(0f,0f,-SourceSightCameraDepth);
        var offset=targetPoint-originalSight.Origin;
        if(!Pass66ArmKinematics.Finite(offset) ||
           offset.LengthSquared()>4f ||
           Math.Abs(offset.Z)>1.8f)return false;
        cameraLocalRoot=new Transform3D(Basis.Identity,offset);
        return true;
    }

    public static float ReticleError(
        Transform3D weaponRoot,Transform3D sight)
    {
        var point=(weaponRoot*sight).Origin;
        return new Vector2(point.X,point.Y).Length();
    }

    private static bool ReadClip(JsonElement animations,string key,
        string target,out Clip clip,out Vector3 cameraSourceOffset)
    {
        clip=default;
        cameraSourceOffset=Vector3.Zero;
        if(!animations.TryGetProperty(key,out var animation) ||
           !animation.TryGetProperty("Sequence",out var steps) ||
           steps.ValueKind!=JsonValueKind.Array ||
           steps.GetArrayLength()!=1)return false;
        var step=steps[0];
        if(!step.TryGetProperty("Time",out var duration) ||
           duration.ValueKind!=JsonValueKind.Number ||
           !duration.TryGetSingle(out var seconds) ||
           !float.IsFinite(seconds) || seconds is <.03f or >3f ||
           !step.TryGetProperty("AV",out var av) ||
           !av.TryGetProperty("Aim",out var track) ||
           !track.TryGetProperty("To",out var destination) ||
           !destination.TryGetProperty("expression",out var expression) ||
           expression.ValueKind!=JsonValueKind.String ||
           expression.GetString()!=target)
            return false;
        if(track.TryGetProperty("Lerp",out var lerp) &&
           lerp.ValueKind==JsonValueKind.False)
            return false;
        var ease=EaseKind.Linear;
        if(track.TryGetProperty("Ease",out var easeValue) &&
           easeValue.ValueKind==JsonValueKind.String)
        {
            ease=easeValue.GetString() switch
            {
                "SlowDown"=>EaseKind.SlowDown,
                "SlowDown2"=>EaseKind.SlowDown2,
                "Linear"=>EaseKind.Linear,
                _ => (EaseKind)(-1)
            };
            if((int)ease<0)return false;
        }
        if(av.TryGetProperty("CamAim",out var cameraCue) &&
           cameraCue.ValueKind==JsonValueKind.Object &&
           cameraCue.TryGetProperty("To",out var cameraTarget) &&
           cameraTarget.ValueKind==JsonValueKind.Object &&
           cameraTarget.TryGetProperty("expression",out var text) &&
           text.ValueKind==JsonValueKind.String &&
           Pass60WeaponPoseMath.TryParse(text.GetString(),null,out var pose) &&
           pose.Origin.LengthSquared()<=1f)
        {
            // Original CamAim is preserved as source evidence but not
            // applied to player camera collision, recoil, or shot rays.
            cameraSourceOffset=pose.Origin;
        }
        clip=new Clip(seconds,ease);
        return true;
    }

    private static Profile? Parse(JsonElement entry)
    {
        if(!entry.TryGetProperty("animations",out var animations) ||
           !ReadClip(animations,"Aim","module.Stats.AimCF",
               out var aim,out var camera) ||
           !ReadClip(animations,"UnAim","CFrame.new()",
               out var unAim,out _))return null;
        return new Profile(aim,unAim,camera);
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
            foreach(var row in catalog.RootElement.GetProperty("weapons")
                .EnumerateArray())
            {
                var path=row.GetProperty("path").GetString()??"";
                if(path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.StartsWith('/') ||
                   path.Contains('\\'))continue;
                try
                {
                    using var file=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var root=file.RootElement;
                    var weapon=root.GetProperty("name").GetString()??"";
                    if(weapon.Length is <1 or >128)continue;
                    var profile=Parse(root);
                    if(profile is not null)found[weapon]=profile;
                }
                catch(Exception e)
                {
                    GD.PushWarning("TWR_PASS67_AIM_ENTRY_REJECTED "+
                        path+" "+e.Message);
                }
            }
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS67_AIM_CATALOG_UNAVAILABLE "+
                e.Message);
        }
        _profiles=found;
        GD.Print($"TWR_PASS67_SOURCE_AIM_TIMELINES_READY count={found.Count}");
    }
}

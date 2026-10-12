using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass68: exact deterministic matrix multiplication for the *surviving*
/// AimCF assignment families, extracted as inert classifications from
/// owner-supplied weapon modules. NEVER evaluate Roblox Luau. The
/// computed original CFrame is separate from a camera-placement estimate
/// because source character/camera and Tool motor joints remain missing.
/// </summary>
public static class Pass68OriginalAimFormulas
{
    public enum Family
    {
        InverseSight,
        SightHandleBaseInside,
        SightHandleThenBase,
        SpasSpecial,
        NumericLiteral
    }
    public sealed record Formula(Family Kind,float SightPreZStuds,
        float PostZStuds,float SpecialZStuds,Transform3D Grip);
    private static Dictionary<string,Formula>? _formulas;
    public static int FormulaCount {get{Load();return _formulas!.Count;}}

    public static bool TryGet(string weapon,out Formula? formula)
    {
        Load();
        if(_formulas!.TryGetValue(weapon,out var item))
        {
            formula=item;return true;
        }
        formula=null;return false;
    }

    private static Transform3D Translation(float x,float y,float z) =>
        new(Basis.Identity,new Vector3(x,y,z)*
            RobloxUnits.MetersPerStud);
    public static Transform3D Reflection(Transform3D source)
    {
        var reflection=new Basis(Vector3.Right,Vector3.Up,Vector3.Forward);
        return new Transform3D(
            reflection*source.Basis*reflection,
            reflection*source.Origin);
    }

    private static readonly Transform3D AimBase=
        // CFrame.new(0,-0.5,0) * CFrame.new(0.25,0.4330127018922193,0)
        Translation(.25000000000000006f,-.0669872981077807f,0f);

    private static bool Valid(Transform3D value)
    {
        var det=value.Basis.Determinant();
        return Pass66ArmKinematics.Finite(value.Origin) &&
            value.Origin.LengthSquared()<=16f &&
            float.IsFinite(det) && Math.Abs(det-1f)<.045f;
    }

    /// <summary>
    /// Original CFrames are evaluated in reflected, unit-scaled Godot
    /// source Tool local coordinates. Reflection is a group homomorphism
    /// of transform products, so all original multiplication orders
    /// are preserved. Handle and AimPart must originate from the
    /// independently SHA-gated original owner weapon part pack.
    /// </summary>
    public static bool TryCompute(Formula? source,
        Transform3D handle,Transform3D aimPart,
        out Transform3D originalAimCf)
    {
        originalAimCf=Transform3D.Identity;
        if(source is null || !Valid(handle) || !Valid(aimPart) ||
           !Valid(source.Grip))return false;
        var invGrip=source.Grip.AffineInverse();
        var invSight=aimPart.AffineInverse();
        Transform3D result;
        switch(source.Kind)
        {
            case Family.InverseSight:
                // GripCF^-1 * (Handle^-1 * (AimPart * optional marker
                // shift))^-1 * source camera vertical/lateral offset
                var sight=aimPart*Translation(0f,0f,
                    source.SightPreZStuds);
                result=invGrip*
                    (handle.AffineInverse()*sight).AffineInverse()*
                    AimBase;
                break;
            case Family.SightHandleBaseInside:
                // AimPart:toObjectSpace(Handle * GripCF^-1 * base)
                result=invSight*handle*invGrip*AimBase;
                break;
            case Family.SightHandleThenBase:
                // AimPart:toObjectSpace(Handle * GripCF^-1) * base
                result=(invSight*handle*invGrip)*AimBase;
                break;
            case Family.SpasSpecial:
                // The original SPAS Lua is written literally:
                // AimPart * CFrame.new(0,0,-.6):toObjectSpace(
                //   Handle * GripCF^-1) * base
                // Lua ':' binds to CFrame.new(...) BEFORE multiplication.
                result=aimPart*
                    Translation(0f,0f,source.SpecialZStuds)
                        .AffineInverse()*handle*invGrip*AimBase;
                break;
            case Family.NumericLiteral:
                // Flamethrower exact source: CFrame.new(-.25,.25,0)
                result=Translation(-.25f,.25f,0f);
                break;
            default:return false;
        }
        if(source.PostZStuds!=0f)
            result*=Translation(0f,0f,source.PostZStuds);
        if(!Valid(result))return false;
        originalAimCf=result;
        return true;
    }

    /// <summary>
    /// Construct a *visual* ADS correction relative to the Pass67
    /// camera-centered original AimPart marker, using the difference
    /// between each exact source AimCF and the normal reference
    /// AimPart^-1 * Handle * GripCF^-1 * base formula. The exact
    /// original AimCF is retained separately for inspection/tests.
    /// We apply a bounded rotation/depth difference only: lateral
    /// aim is recentered to preserve a stable screen reticle, as the
    /// missing Roblox camera/arms prevent using raw AimCF as a Godot
    /// weapon-root transform verbatim.
    /// </summary>
    public static bool TryVisualRoot(Formula? formula,
        Transform3D sourceHandle,Transform3D sourceAimPart,
        Transform3D renderedAimPart,out Transform3D root,
        out Transform3D originalAimCf)
    {
        root=Transform3D.Identity;
        originalAimCf=Transform3D.Identity;
        if(formula is null ||
           !Pass67SourceAimAlignment.TryAimRoot(renderedAimPart,
               out var centered) ||
           !TryCompute(formula,sourceHandle,sourceAimPart,
               out originalAimCf))
            return false;
        var canonical=sourceAimPart.AffineInverse()*
            sourceHandle*formula.Grip.AffineInverse()*AimBase;
        if(!Valid(canonical))return false;
        var correction=originalAimCf*canonical.AffineInverse();
        if(!Valid(correction) ||
           correction.Origin.LengthSquared()>1.25f)return false;
        // Only differences from original per-weapon source family
        // affect the imported weapon; use no new user-visible offsets.
        var candidate=centered*correction;
        var projected=(candidate*renderedAimPart).Origin;
        if(!Pass66ArmKinematics.Finite(projected))return false;
        var offset=new Vector3(-projected.X,-projected.Y,0f);
        candidate=new Transform3D(candidate.Basis,
            candidate.Origin+offset);
        var settled=(candidate*renderedAimPart).Origin;
        if(settled.Z is >-.12f or <-1.2f ||
           Pass67SourceAimAlignment.ReticleError(candidate,
                renderedAimPart)>.001f)
            return false;
        root=candidate;
        return true;
    }

    private static float Optional(JsonElement entry,string name)
    {
        return entry.TryGetProperty(name,out var n) &&
            n.ValueKind==JsonValueKind.Number &&
            n.TryGetSingle(out var f) && float.IsFinite(f) &&
            Math.Abs(f)<=.8f ? f : 0f;
    }

    private static bool ParseKind(JsonElement entry,out Family kind)
    {
        kind=default;
        if(!entry.TryGetProperty("family",out var field) ||
           field.ValueKind!=JsonValueKind.String)return false;
        return field.GetString() switch
        {
            "inverse_sight"=>Set(Family.InverseSight,out kind),
            "sight_handle_with_base"=>
                Set(Family.SightHandleBaseInside,out kind),
            "sight_handle_then_base"=>
                Set(Family.SightHandleThenBase,out kind),
            "spas_special"=>Set(Family.SpasSpecial,out kind),
            "literal"=>Set(Family.NumericLiteral,out kind),
            _=>false
        };
    }
    private static bool Set(Family value,out Family kind)
    {
        kind=value;return true;
    }

    private static void Load()
    {
        if(_formulas is not null)return;
        var found=new Dictionary<string,Formula>(StringComparer.Ordinal);
        try
        {
            using var familyDoc=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/aim_cf_families68.json"));
            var source=familyDoc.RootElement;
            if(source.GetProperty("format").GetString()!=
                   "twr-inert-source-aim-formula-families-v1" ||
               source.GetProperty("weapon_count").GetInt32()!=73)
                throw new InvalidOperationException(
                    "Pass68 original aim family metadata format/count invalid");
            var entries=source.GetProperty("profiles");
            if(entries.EnumerateObject().Count()!=73)
                throw new InvalidOperationException(
                    "Pass68 original aim family inventory incomplete");
            using var catalog=JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/catalog.json"));
            foreach(var row in catalog.RootElement.GetProperty("weapons")
                .EnumerateArray())
            {
                var name=row.GetProperty("name").GetString()??"";
                var path=row.GetProperty("path").GetString()??"";
                if(!entries.TryGetProperty(name,out var metadata) ||
                   !ParseKind(metadata,out var kind) ||
                   path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.Contains('\\') ||
                   path.StartsWith('/'))continue;
                try
                {
                    using var file=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var stats=file.RootElement.GetProperty("stats");
                    Transform3D grip=Transform3D.Identity;
                    if(kind!=Family.NumericLiteral)
                    {
                        var expr=stats.GetProperty("GripCF")
                            .GetProperty("expression").GetString();
                        if(!Pass60WeaponPoseMath.TryParse(expr,null,
                                out var rawGrip))continue;
                        grip=Reflection(rawGrip);
                        if(!Valid(grip))continue;
                    }
                    var pre=Optional(metadata,"sight_pre_z_studs");
                    var post=Optional(metadata,"post_z_studs");
                    var special=Optional(metadata,"special_z_studs");
                    if((kind==Family.SpasSpecial && name!="SPAS-12") ||
                       (kind==Family.NumericLiteral &&
                        name!="Flamethrower") ||
                       (pre!=0f && name!="MP7") ||
                       (special!=0f && name!="SPAS-12"))
                        continue;
                    found[name]=new Formula(kind,pre,post,special,grip);
                }
                catch(Exception ex)
                {
                    GD.PushWarning("TWR_PASS68_ORIGINAL_AIM_SKIP "+
                        name+" "+ex.Message);
                }
            }
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS68_AIM_FAMILY_FALLBACK "+ex.Message);
        }
        _formulas=found;
        GD.Print($"TWR_PASS68_INERT_SOURCE_AIM_FAMILIES_READY count={found.Count}");
    }
}

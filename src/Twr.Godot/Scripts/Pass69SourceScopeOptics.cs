using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass69: approximate procedural first-person optic silhouettes, sized
/// from eight original normalized ModuleScript ScopeDiameter / ScopeRadius
/// values and source FOV. Reticle pattern families were studied against
/// user-supplied ADS screenshot references, NOT recovered textures or
/// original Roblox UI. No screenshots or owner source assets are shipped.
/// </summary>
public partial class Pass69SourceScopeOptics : Control
{
    public enum Style
    {
        CompactCross,
        ReflexDot,
        CircleCross,
        Duplex,
        MilDot,
        EasternPost
    }

    public sealed record Profile(string Weapon,float DiameterStuds,
        float SourceFov,Style Reticle,bool Magnified,
        string PrivateReferenceStill);

    private static Dictionary<string,Profile>? _profiles;
    private Profile? _selected;
    private float _blend;
    private float _fov=60f;
    private float _eyeRelief=Pass67SourceAimAlignment.SourceSightCameraDepth;
    public static int ProfileCount {get {Load();return _profiles!.Count;}}
    public string SourceWeaponName => _selected?.Weapon ?? "";
    public string SourceReferenceStill =>
        _selected?.PrivateReferenceStill ?? "";
    public bool HasSourceProfile => _selected is not null;
    public bool OpticVisible => _selected is not null && _blend>.08f;
    public float CurrentBlend => _blend;
    public Style? ReticleStyle => _selected?.Reticle;
    public float SourceApertureDiameterStuds =>
        _selected?.DiameterStuds ?? 0f;
    public float ProjectedRadiusPixels =>
        _selected is null ? 0f :
        ProjectRadius(_selected,Size,_fov,_eyeRelief);

    public static bool TryGet(string weapon,out Profile? profile)
    {
        Load();
        if(_profiles!.TryGetValue(weapon,out var result))
        {
            profile=result;return true;
        }
        profile=null;return false;
    }

    /// <summary>
    /// Source ScopeDiameter is a full diameter in Roblox studs, while
    /// Source ScopeRadius (only CBJ-MS) is half a diameter. The known
    /// model-to-screen calibration is *approximate* because Roblox's
    /// original eye/camera weld is not available. The lens remains
    /// finite and readable on unusual aspect ratios/FOVs.
    /// </summary>
    public static float ProjectRadius(Profile? profile,Vector2 viewport,
        float cameraFov,float eyeRelief)
    {
        if(profile is null ||
           !float.IsFinite(viewport.X) ||
           !float.IsFinite(viewport.Y) ||
           viewport.X<64f || viewport.Y<64f ||
           !float.IsFinite(cameraFov) ||
           cameraFov is <5f or >100f ||
           !float.IsFinite(eyeRelief) ||
           eyeRelief is <.10f or >2f)
            return 0f;
        var distance=profile.DiameterStuds*
            RobloxUnits.MetersPerStud*.5f;
        var focal=viewport.Y/
            (2f*MathF.Tan(Mathf.DegToRad(cameraFov)*.5f));
        var px=focal*distance/eyeRelief;
        if(!float.IsFinite(px))return 0f;
        return Math.Clamp(px,12f,
            Math.Min(viewport.X,viewport.Y)*.425f);
    }

    public void SetAimState(string weapon,float blend,float cameraFov,
        bool allow,bool sourceSightAvailable=false,
        float eyeRelief=Pass67SourceAimAlignment.SourceSightCameraDepth)
    {
        TryGet(weapon,out var profile);
        _selected=profile;
        _blend=allow && float.IsFinite(blend)
            ?Math.Clamp(blend,0f,1f):0f;
        _fov=float.IsFinite(cameraFov) &&
             cameraFov is >=5f and <=100f ?cameraFov:60f;
        // A valid source sight can provide a more accurate rendered
        // depth. Unsupported/nonexistent owner packs use Pass67's
        // bounded 0.43m reference, clearly not original exact eye relief.
        _eyeRelief=sourceSightAvailable &&
            float.IsFinite(eyeRelief) &&
            eyeRelief is >=.12f and <=1.2f
            ?eyeRelief:Pass67SourceAimAlignment.SourceSightCameraDepth;
        Visible=OpticVisible;
        QueueRedraw();
    }

    public override void _Ready()
    {
        MouseFilter=MouseFilterEnum.Ignore;
        Visible=false;
    }

    private static Style? StyleFor(string name) => name switch
    {
        "AUG"=>Style.CompactCross,
        "CBJ-MS"=>Style.ReflexDot,
        "Barrett M82A1"=>Style.CircleCross,
        "M110 SASS"=>Style.Duplex,
        "M14"=>Style.MilDot,
        "SVD"=>Style.EasternPost,
        "VSS"=>Style.EasternPost,
        "Winchester Model 70"=>Style.Duplex,
        _=>null
    };

    private static string? ReferenceFor(string name) => name switch
    {
        "AUG"=>"AUG-ADS.png",
        "CBJ-MS"=>"CBJMS-ADS.png",
        "Barrett M82A1"=>"M82A1-ADS.png",
        "M110 SASS"=>"M110-ADS.png",
        "M14"=>"M14-ADS.png",
        "SVD"=>"SVD-ADS.png",
        "VSS"=>"VSS-ADS.png",
        "Winchester Model 70"=>"M70-ADS.png",
        _=>null
    };

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
                var name=entry.GetProperty("name").GetString()??"";
                var style=StyleFor(name);
                var still=ReferenceFor(name);
                if(style is null || still is null)continue;
                var path=entry.GetProperty("path").GetString()??"";
                if(path.Length is <5 or >140 ||
                   !path.EndsWith(".json",StringComparison.Ordinal) ||
                   path.Contains("..") || path.StartsWith('/') ||
                   path.Contains('\\'))continue;
                try
                {
                    using var file=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var stats=file.RootElement.GetProperty("stats");
                    var diameter=0f;
                    if(stats.TryGetProperty("ScopeDiameter",out var d) &&
                       d.ValueKind==JsonValueKind.Number &&
                       d.TryGetSingle(out var full))
                        diameter=full;
                    else if(stats.TryGetProperty("ScopeRadius",out var r) &&
                            r.ValueKind==JsonValueKind.Number &&
                            r.TryGetSingle(out var half))
                        diameter=half*2f;
                    if(!float.IsFinite(diameter) ||
                       diameter is <.04f or >.30f)continue;
                    var fov=60f;
                    if(stats.TryGetProperty("FOV",out var zoom) &&
                       zoom.ValueKind==JsonValueKind.Number &&
                       zoom.TryGetSingle(out var original) &&
                       float.IsFinite(original) &&
                       original is >=5f and <=60f)
                        fov=original;
                    var magnified=name is not ("AUG" or "CBJ-MS");
                    found[name]=new Profile(name,diameter,fov,
                        style.Value,magnified,still);
                }
                catch(Exception ex)
                {
                    GD.PushWarning("TWR_PASS69_OPTIC_SOURCE_ENTRY "+
                        name+" "+ex.Message);
                }
            }
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS69_OPTIC_SOURCE_FALLBACK "+
                ex.Message);
        }
        _profiles=found;
        GD.Print($"TWR_PASS69_SOURCE_OPTIC_PROFILES_READY count={found.Count}");
    }

    private void DrawScopeRim(Vector2 center,float radius,
        Color frameColor,Color edgeColor)
    {
        var inner=new Vector2[8];
        // Screenshot-reference scope housings are angular rather than
        // perfect circles. The octagon is reconstructed geometry, not
        // a recovered original texture or model projection.
        for(var i=0;i<8;i++)
        {
            var theta=(i+.5f)*(Mathf.Pi/4f);
            inner[i]=center+new Vector2(MathF.Cos(theta),
                MathF.Sin(theta))*radius;
        }
        for(var i=0;i<8;i++)
        {
            var a=inner[i];
            var b=inner[(i+1)%8];
            DrawLine(a,b,frameColor,9f,true);
            DrawLine(a,b,edgeColor,2f,true);
        }
    }

    private void DrawDarkOutside(Vector2 center,float radius,float alpha)
    {
        var outer=new Color(.012f,.014f,.020f,.88f*alpha);
        var left=center.X-radius;
        var right=center.X+radius;
        var top=center.Y-radius;
        var bottom=center.Y+radius;
        DrawRect(new Rect2(0f,0f,Size.X,
            Math.Max(0f,top)),outer);
        DrawRect(new Rect2(0f,Math.Max(0f,bottom),Size.X,
            Math.Max(0f,Size.Y-bottom)),outer);
        DrawRect(new Rect2(0f,top,Math.Max(0f,left),
            radius*2f),outer);
        DrawRect(new Rect2(right,top,Math.Max(0f,Size.X-right),
            radius*2f),outer);
        // Reconstructed dark facets fill the four octagonal corners.
        // No screenshot pixels are shipped in the public code.
        var cut=radius*.41421356f;
        DrawColoredPolygon(new [] {
            new Vector2(center.X-cut,top),
            new Vector2(left,center.Y-cut),new Vector2(left,top)
        },outer);
        DrawColoredPolygon(new [] {
            new Vector2(center.X+cut,top),
            new Vector2(right,center.Y-cut),new Vector2(right,top)
        },outer);
        DrawColoredPolygon(new [] {
            new Vector2(right,center.Y+cut),
            new Vector2(center.X+cut,bottom),new Vector2(right,bottom)
        },outer);
        DrawColoredPolygon(new [] {
            new Vector2(center.X-cut,bottom),
            new Vector2(left,center.Y+cut),new Vector2(left,bottom)
        },outer);
    }

    private void DrawReticle(Vector2 center,float radius,Style style,
        float opacity)
    {
        var ink=new Color(.17f,.19f,.18f,.90f*opacity);
        var tint=new Color(.48f,.52f,.48f,.65f*opacity);
        var inner=style==Style.ReflexDot ?
            Math.Max(5f,radius*.16f):Math.Max(3f,radius*.045f);
        var extent=radius*.77f;
        if(style==Style.CompactCross)
        {
            DrawLine(center-new Vector2(extent,0f),
                center-new Vector2(inner,0f),ink,2f,true);
            DrawLine(center+new Vector2(inner,0f),
                center+new Vector2(extent,0f),ink,2f,true);
            DrawLine(center-new Vector2(0f,extent),
                center-new Vector2(0f,inner),ink,2f,true);
            DrawLine(center+new Vector2(0f,inner),
                center+new Vector2(0f,extent),ink,2f,true);
            DrawCircle(center,1.5f,tint);
            return;
        }
        if(style==Style.ReflexDot)
        {
            DrawArc(center,radius*.59f,0f,Mathf.Tau,48,
                tint,2f,true);
            DrawCircle(center,2.8f,
                new Color(.90f,.19f,.11f,.90f*opacity));
            return;
        }
        for(var i=0;i<4;i++)
        {
            var angle=i*Mathf.Pi*.5f;
            var direction=new Vector2(MathF.Cos(angle),
                MathF.Sin(angle));
            DrawLine(center+direction*inner,
                center+direction*extent,ink,
                style==Style.Duplex?2.4f:1.6f,true);
        }
        if(style==Style.CircleCross)
            DrawArc(center,radius*.14f,0f,Mathf.Tau,64,
                ink,2f,true);
        if(style==Style.MilDot || style==Style.EasternPost)
        {
            var dot=style==Style.MilDot ? 2f : 1.4f;
            for(var n=-3;n<=3;n++)
            {
                if(n==0)continue;
                DrawCircle(center+new Vector2(n*radius*.145f,0f),
                    dot,ink);
                DrawCircle(center+new Vector2(0f,n*radius*.145f),
                    dot,ink);
            }
        }
        DrawCircle(center,style==Style.EasternPost?1.5f:1f,ink);
    }

    public override void _Draw()
    {
        if(!OpticVisible || _selected is not { } optic)return;
        var radius=ProjectedRadiusPixels;
        if(radius<12f)return;
        var center=Size*.5f;
        var alpha=_blend*_blend*(3f-2f*_blend);
        if(optic.Magnified)
            DrawDarkOutside(center,radius,alpha);
        var rim=new Color(.035f,.042f,.050f,
            (optic.Magnified ? .91f : .44f)*alpha);
        var edge=new Color(.13f,.14f,.16f,
            (optic.Magnified ? .67f : .40f)*alpha);
        if(optic.Magnified)
            DrawScopeRim(center,radius,rim,edge);
        else DrawArc(center,radius,0f,Mathf.Tau,64,
            edge,Math.Max(1.3f,radius*.027f),true);
        DrawReticle(center,radius,optic.Reticle,alpha);
    }
}

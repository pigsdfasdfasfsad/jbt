using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass69: original weapon ModuleScript Stats.ScopeDiameter (seven scoped
/// weapons) and Stats.ReduceSensitivity (six). All values are inert data.
/// Visual mask is a reconstructed procedural optic; the original Retail
/// 2D scope texture/mask-hole shader and remote glass meshes were not found.
/// </summary>
public static class Pass69SourceScope
{
    public sealed record Profile(float DiameterStuds,bool ReduceSensitivity);
    private static Dictionary<string,Profile>? _profiles;

    public static int SourceScopedWeaponCount
    {
        get { Load(); return _profiles!.Count; }
    }

    public static bool TryGet(string weapon,out Profile? profile)
    {
        Load();
        if(_profiles!.TryGetValue(weapon,out var found))
        {
            profile=found;return true;
        }
        profile=null;return false;
    }

    /// <summary>
    /// Original intent: baseSensitivity*(liveFov/hipFov) for the six
    /// ReduceSensitivity guns, using the LIVE interpolated FOV (not
    /// the target FOV). AUG is scope-masked but does not reduce sensitivity.
    /// This game's existing hip camera FOV is 60 degrees, not the
    /// historical reference client default 70 degrees.
    /// </summary>
    public static float SensitivityScale(Profile? profile,
        float liveFov,float hipFov=60f)
    {
        if(profile is null || !profile.ReduceSensitivity)return 1f;
        if(!float.IsFinite(liveFov) || !float.IsFinite(hipFov) ||
           hipFov is <20f or >120f)return 1f;
        return Math.Clamp(liveFov/hipFov,.08f,1f);
    }

    /// <summary>
    /// Exact ideal-perspective screen diameter for a source-authored
    /// lens diameter in studs projected at a known camera-local depth.
    /// This is not a claim of the exact unknown retail mask shader.
    /// </summary>
    public static float ProjectedDiameterPx(float diameterStuds,
        float lensDepthMeters,float verticalFovDegrees,float viewportHeightPx)
    {
        if(!float.IsFinite(diameterStuds) ||
           !float.IsFinite(lensDepthMeters) ||
           !float.IsFinite(verticalFovDegrees) ||
           !float.IsFinite(viewportHeightPx) ||
           diameterStuds is <.01f or >.5f ||
           lensDepthMeters is <.06f or >4f ||
           verticalFovDegrees is <4f or >110f ||
           viewportHeightPx is <64f or >10000f)return 0f;
        var focal=viewportHeightPx/
            (2f*MathF.Tan(Mathf.DegToRad(verticalFovDegrees)*.5f));
        return diameterStuds*RobloxUnits.MetersPerStud*
            focal/lensDepthMeters;
    }

    /// <summary>
    /// Faithful perspective PROJECTED geometry through Godot's active
    /// Camera3D. The lens point comes from owner-SHA-verified invisible
    /// or mesh source parts when available. Diameter uses the ORIGINAL
    /// hand-tuned Stats.ScopeDiameter, not MeshPart.Size.X.
    /// </summary>
    public static bool TryProjectLens(Camera3D camera,Vector3 worldLens,
        float diameterStuds,out Vector2 center,out float diameterPx)
    {
        center=Vector2.Zero;
        diameterPx=0f;
        if(!Pass66ArmKinematics.Finite(worldLens) ||
           camera.IsPositionBehind(worldLens) ||
           !float.IsFinite(camera.Fov))return false;
        var local=camera.ToLocal(worldLens);
        if(!Pass66ArmKinematics.Finite(local) ||
           local.Z is >=-.06f or <-4f)return false;
        var viewport=camera.GetViewport().GetVisibleRect().Size;
        if(viewport.X<64 || viewport.Y<64)return false;
        var radiusMeters=diameterStuds*
            RobloxUnits.MetersPerStud*.5f;
        if(radiusMeters is <.001f or >.14f)return false;
        var sideways=camera.GlobalTransform.Basis.X.Normalized()*
            radiusMeters;
        var left=camera.UnprojectPosition(worldLens-sideways);
        var right=camera.UnprojectPosition(worldLens+sideways);
        center=camera.UnprojectPosition(worldLens);
        diameterPx=left.DistanceTo(right);
        var screen=new Vector2(viewport.X,viewport.Y);
        if(!float.IsFinite(diameterPx) ||
           diameterPx is <3f or >8000f ||
           !float.IsFinite(center.X) ||
           !float.IsFinite(center.Y) ||
           center.X< -screen.X*.2f ||
           center.X>screen.X*1.2f ||
           center.Y< -screen.Y*.2f ||
           center.Y>screen.Y*1.2f)return false;
        return true;
    }

    private static void Load()
    {
        if(_profiles is not null)return;
        var found=new Dictionary<string,Profile>(
            StringComparer.Ordinal);
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
                   path.StartsWith('/') || path.Contains("..") ||
                   path.Contains('\\'))continue;
                try
                {
                    using var file=JsonDocument.Parse(
                        global::Godot.FileAccess.GetFileAsString(
                            "res://Content/weapons/"+path));
                    var root=file.RootElement;
                    var stats=root.GetProperty("stats");
                    if(!stats.TryGetProperty("ScopeDiameter",out var scope) ||
                       scope.ValueKind!=JsonValueKind.Number ||
                       !scope.TryGetSingle(out var diameter) ||
                       !float.IsFinite(diameter) ||
                       diameter is <.05f or >.35f)
                        continue;
                    var name=root.GetProperty("name").GetString()??"";
                    if(name.Length is <1 or >128)continue;
                    var reduce=stats.TryGetProperty("ReduceSensitivity",
                        out var sourceFlag) &&
                        sourceFlag.ValueKind==JsonValueKind.True;
                    found[name]=new Profile(diameter,reduce);
                }
                catch(Exception ex)
                {
                    GD.PushWarning("TWR_PASS69_SCOPE_ENTRY_SKIPPED "+
                        path+" "+ex.Message);
                }
            }
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS69_SCOPE_CATALOG_FALLBACK "+ex.Message);
        }
        _profiles=found;
        GD.Print($"TWR_PASS69_SOURCE_SCOPE_READY count={found.Count}");
    }
}

/// <summary>
/// Source-sized procedural scope mask. The source ScopeDiameter controls
/// the hole; the crosshair/reticle marks and perimeter shading are
/// explicitly approximated, NOT the unavailable original 2D texture.
/// 100 quad bands approximate a circular opening without shaders, readback,
/// depth-buffer tricks, internet access or texture downloads.
/// </summary>
public partial class Pass69ScopeMask : Control
{
    public Vector2 ApertureCenter {get;private set;}
    public float ApertureDiameter {get;private set;}
    public float MaskOpacity {get;private set;}
    public bool SourceLensMarkerPresent {get;private set;}

    public void SetScope(Vector2 center,float diameter,float opacity,
        bool originalSourceLens)
    {
        if(!float.IsFinite(center.X) || !float.IsFinite(center.Y) ||
           !float.IsFinite(diameter) || !float.IsFinite(opacity) ||
           diameter<=1f || opacity<=.005f ||
           Size.X<=0f || Size.Y<=0f)
        {
            MaskOpacity=0f;
            Visible=false;
            return;
        }
        ApertureCenter=center;
        ApertureDiameter=Math.Clamp(diameter,24f,
            Math.Max(24f,Math.Max(Size.X,Size.Y)*1.8f));
        MaskOpacity=Math.Clamp(opacity,0f,1f);
        SourceLensMarkerPresent=originalSourceLens;
        Visible=true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if(!Visible || MaskOpacity<=.005f ||
           Size.X<=1f || Size.Y<=1f)return;
        var width=Size.X;
        var height=Size.Y;
        var radius=ApertureDiameter*.5f;
        var center=ApertureCenter;
        var shade=new Color(.002f,.003f,.005f,.96f*MaskOpacity);
        var top=center.Y-radius;
        var bottom=center.Y+radius;
        if(top>0f)DrawRect(new Rect2(0,0,width,top),shade);
        if(bottom<height)DrawRect(new Rect2(
            0,Math.Max(0f,bottom),width,
            Math.Max(0f,height-Math.Max(0f,bottom))),shade);
        var minY=Math.Max(0f,top);
        var maxY=Math.Min(height,bottom);
        const int bands=100;
        if(maxY>minY)
        {
            for(var i=0;i<bands;i++)
            {
                var y0=Mathf.Lerp(minY,maxY,(float)i/bands);
                var y1=Mathf.Lerp(minY,maxY,(float)(i+1)/bands);
                var delta0=y0-center.Y;
                var delta1=y1-center.Y;
                var edge0=MathF.Sqrt(Math.Max(0f,
                    radius*radius-delta0*delta0));
                var edge1=MathF.Sqrt(Math.Max(0f,
                    radius*radius-delta1*delta1));
                var left0=center.X-edge0;
                var left1=center.X-edge1;
                var right0=center.X+edge0;
                var right1=center.X+edge1;
                DrawColoredPolygon([
                    new Vector2(0f,y0),new Vector2(left0,y0),
                    new Vector2(left1,y1),new Vector2(0f,y1)],shade);
                DrawColoredPolygon([
                    new Vector2(right0,y0),new Vector2(width,y0),
                    new Vector2(width,y1),new Vector2(right1,y1)],shade);
            }
        }
        // Deliberately reconstructed crosshair: cannot represent
        // unavailable source optic textures or original reticle image.
        var rim=new Color(.10f,.13f,.12f,.95f*MaskOpacity);
        DrawArc(center,radius,0f,Mathf.Tau,128,rim,3f,true);
        var line=new Color(.13f,.16f,.13f,.76f*MaskOpacity);
        var reach=Math.Min(radius*.70f,220f);
        var gap=Math.Min(radius*.10f,15f);
        var up=new Vector2(0f,-1f);
        var right=new Vector2(1f,0f);
        DrawLine(center-up*reach,center-up*gap,line,1.1f,true);
        DrawLine(center+up*gap,center+up*reach,line,1.1f,true);
        DrawLine(center-right*reach,center-right*gap,line,1.1f,true);
        DrawLine(center+right*gap,center+right*reach,line,1.1f,true);
        DrawCircle(center,1.8f,new Color(.14f,.19f,.15f,.86f*MaskOpacity));
    }
}

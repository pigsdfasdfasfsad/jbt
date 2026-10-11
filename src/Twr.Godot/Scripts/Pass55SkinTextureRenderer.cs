using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass55 offline rendering of inert source Texture/Texture1 and MeshPart.TextureID
/// references. Source RGB, transparency, tiling and offsets are retained.
/// UV projection to Godot meshes, selection between overlapping source layers,
/// and moving neon/ForceField shimmer are RECONSTRUCTIONS, not original timing.
/// </summary>
public static class Pass55SkinTextureRenderer
{
    public sealed record Applied(
        bool HasReference, bool InstalledTexture, bool GeneratedFallback,
        int UnrenderedLayers, Vector3 OriginalUvOffset, bool Animated);

    private static readonly Dictionary<string, Texture2D> Generated =
        new(StringComparer.Ordinal);

    public static Applied Apply(StandardMaterial3D material,
        Pass55SourceSkinLayers.Slot source,string skinId,int sourceSlot,
        bool animate)
    {
        var layers=source.Layers;
        // Original Texture objects are drawn on six world-facing surfaces.
        // A single Godot AlbedoTexture per MeshInstance3D cannot reproduce
        // all six and additional ZIndex compositing. Prefer highest ZIndex,
        // then later source declaration; report other layers as unresolved.
        Pass55SourceSkinLayers.Layer? chosen=null;
        foreach(var candidate in layers)
            if(chosen is null || candidate.ZIndex>=chosen.ZIndex)
                chosen=candidate;

        var id=chosen?.TextureId ?? source.MeshTextureId;
        if(id.Length==0)
            return new Applied(false,false,false,0,Vector3.Zero,false);
        var texture=OfflineAssetResolver.Texture(id);
        var realTexture=texture is not null;
        if(texture is null)
            texture=GenerateFallback(id);
        material.AlbedoTexture=texture;
        var uv=Vector3.Zero;
        if(chosen is not null)
        {
            // Roblox dimensions are expressed in studs per tile. The source
            // numeric parameters are preserved; without the original per-
            // surface projected UVs the mapping here is an approximation.
            material.Uv1Scale = new Vector3(1f/chosen.TileU,1f/chosen.TileV,1f);
            uv=new Vector3(chosen.OffsetU,chosen.OffsetV,0);
            material.Uv1Offset=uv;
            var baseColor=material.AlbedoColor;
            material.AlbedoColor=new Color(
                baseColor.R*chosen.Tint.R,
                baseColor.G*chosen.Tint.G,
                baseColor.B*chosen.Tint.B,
                baseColor.A*(1f-chosen.Transparency));
            if(material.AlbedoColor.A<.995f)
                material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        }
        return new Applied(true,realTexture,!realTexture,
            Math.Max(0,layers.Count-1),uv,animate);
    }

    private static Texture2D GenerateFallback(string id)
    {
        if(Generated.TryGetValue(id,out var cached))return cached;
        // Deterministic procedural geometry is labeled explicitly, rather
        // than pretending that a missing Roblox texture was recovered.
        const int size=64;
        var image=Image.CreateEmpty(size,size,false,Image.Format.Rgb8);
        uint seed=2166136261;
        foreach(var c in id)seed=unchecked((seed ^ c)*16777619);
        var phase=(seed%1024)/1024f*6.2831853f;
        for(var y=0;y<size;y++)
        for(var x=0;x<size;x++)
        {
            var diagonal=(x*1.2f+y*.7f)/size;
            var stripe=MathF.Sin(diagonal*24f+phase);
            var cellular=MathF.Sin((x-y)*.21f+phase*.37f);
            var grain=Math.Clamp(.69f+.19f*stripe+.10f*cellular,.23f,1f);
            image.SetPixel(x,y,new Color(grain,grain,grain));
        }
        var result=ImageTexture.CreateFromImage(image);
        Generated[id]=result;
        return result;
    }

    /// <summary>
    /// Bounded deterministic synthetic effect. The original scripts provide
    /// no UV velocity / pulse period; these are clearly reconstructed values.
    /// </summary>
    public static float ShimmerMultiplier(float elapsed)
    {
        if(!float.IsFinite(elapsed))return 1f;
        return Math.Clamp(.83f+.17f*MathF.Sin(elapsed*2.6f),.5f,1f);
    }

    public static Vector3 ScrollOffset(Vector3 original,float elapsed)
    {
        if(!float.IsFinite(elapsed))return original;
        return original+new Vector3(elapsed*.018f,-elapsed*.012f,0f);
    }
}

using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Deterministic local micro-surface replacements for source Roblox materials.
/// Color and transforms remain author-derived; these are NOT original Roblox
/// diffuse/normal maps and are used only when the source asset is unavailable.
/// </summary>
public static class RobloxMaterialSurface
{
    private static readonly Dictionary<string, Texture2D> Fallbacks = new(StringComparer.Ordinal);

    public static void Apply(
        StandardMaterial3D material, string materialCode, Texture2D? originalTexture)
    {
        var metal = materialCode is "1040" or "1056" or "1072" or "1088";
        var glass = materialCode == "1568";
        var neon = materialCode == "288";
        material.Metallic = metal ? 0.68f : 0.015f;
        material.Roughness = metal ? 0.43f : glass ? 0.12f :
            materialCode is "272" or "256" ? 0.72f : 0.91f;
        material.EmissionEnabled = neon;
        if (neon) material.Emission = material.AlbedoColor;
        material.AlbedoTexture = originalTexture ?? FallbackTexture(materialCode);
        if (originalTexture is null && material.AlbedoTexture is not null)
        {
            material.Uv1Scale = new Vector3(2.5f, 2.5f, 1f);
            material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps;
        }
    }

    private static Texture2D? FallbackTexture(string code)
    {
        // Known Roblox enum values. Flat plastic, neon, glass and
        // transparent source parts deliberately preserve their base colors.
        if (code is not ("512" or "528" or "800" or "816" or
                        "848" or "880" or "1040" or "1056" or "1088" or
                        "1280" or "1296" or "1312" or "1328" or "1344" or
                        "1360" or "1376")) return null;
        if (Fallbacks.TryGetValue(code, out var cached)) return cached;
        const int size = 96;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        uint seed = 2166136261;
        foreach (var digit in code) seed = (seed ^ digit) * 16777619;
        var wood = code is "512" or "528";
        var grass = code == "1280";
        var metal = code is "1040" or "1056" or "1088";
        var brick = code == "848";
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            var noise = (seed & 0xffff) / 65535f;
            var stripe = wood ? Mathf.Sin(y * .34f + Mathf.Sin(x * .1f) * 1.4f) * .09f :
                brick ? ((y / 15 + (x / 29)) % 2 == 0 ? .06f : -.06f) :
                grass ? Mathf.Sin(x * .83f + y * 1.21f) * .045f :
                metal ? Mathf.Sin(x * .33f) * .02f : 0f;
            var grain = Math.Clamp(.82f + .30f * noise + stripe, .55f, 1.0f);
            image.SetPixel(x, y, new Color(grain, grain, grain));
        }
        var texture = ImageTexture.CreateFromImage(image);
        Fallbacks[code] = texture;
        return texture;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Opt-in F3 preview of owner-recovered CMaps Lighting Effects. The source
/// numbers are authentic; Godot fog, bloom and grading are approximations.
/// Never implies that original skybox textures or lighting Lua were recovered.
/// </summary>
public partial class Pass37SourceLightingRuntime : Node
{
    private const string OwnerSourceSha =
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    private const int MaxFileBytes = 16384;
    private static readonly Dictionary<string, (string Sha, int Count)> Source = new(StringComparer.Ordinal)
    {
        ["Ranch"]=("ee285a5043d544a3a9e410aba9d9c580347289f9f6adeb27a23c862f902637d2",6),
        ["Mill"]=("dcb81d20f68480ce0cb8820d07d4880625b83d5a7b1a6581ec67186ad6280335",6),
        ["Bypass"]=("311a2b11162e098b642bcd0167cdb1b67c15c8c8bd8e85556cbba030d440c93e",5),
        ["Cabin"]=("848b3614c6e32c28de84894792f38e0da5b61228c06835bc8ae05ea17a492b56",5),
        ["Cargo"]=("bfb13b04f7e167bbed060b639e3af282b5dde86fb247c771aa9e04edd0f14ad4",4),
        ["District"]=("164f3a1c1d866722f791d9540ad8b43699fef60d5dd7ae5a40e00b591c229c22",3),
        ["Expressway"]=("b4474fd4b85b3576139acfa784b4e28e88c17c7954db003e9200d6904d903a80",4),
        ["Prison"]=("112f5a470d6b3236c6de19d7f63c13f8e0b0d45b4658a1682a57096665d812d4",5),
        ["Laboratory"]=("8a8aa31d56adbf09bfd7b48228a681bd82e8d5f44589087c6591aa0e60efe01c",3),
        ["Manor"]=("d488d3cd9d6113d5006bcceef046002846b1c1c452597678a1bd96682353bfec",4)
    };

    private WorldEnvironment _world = null!;
    private global::Godot.Environment _baseline = null!;
    private global::Godot.Environment _preview = null!;
    public int SourceEffectCount { get; private set; }
    public bool Enabled { get; private set; }
    public bool SkyboxImageAvailable => false;

    public static Pass37SourceLightingRuntime? TryBuild(Node3D owner, string mapName)
    {
        if (!Source.TryGetValue(mapName, out var expected)) return null;
        var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var filename = mapName + ".lighting37.json";
        var path = new[]
        {
            Path.Combine(executableDirectory, "Content", "Lighting", filename),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Lighting", filename),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot", "Content", "Lighting", filename)
        }.FirstOrDefault(File.Exists);
        if (path is null) return null;

        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 128 || bytes.Length > MaxFileBytes)
                throw new InvalidDataException("Source lighting sidecar size invalid");
            var synthetic = OS.GetCmdlineUserArgs().Contains("--smoke-pass37", StringComparer.Ordinal)
                && mapName == "Manor";
            var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!synthetic && digest != expected.Sha)
                throw new InvalidDataException("Original source lighting SHA-256 mismatch");
            using var doc = JsonDocument.Parse(bytes);
            var data = doc.RootElement;
            if (String(data, "format") != "twr-source-lighting37-v1" ||
                String(data, "map") != mapName ||
                String(data, "owner_rbxlx_sha256") !=
                    (synthetic ? new string('0',64) : OwnerSourceSha) ||
                String(data, "source_path") !=
                    $"ReplicatedStorage/CMaps/{mapName}/Lighting Effects" ||
                data.GetProperty("skybox_images_present").GetBoolean())
                throw new InvalidDataException("Original source lighting provenance rejected");

            var world = FindWorld(owner)
                ?? throw new InvalidDataException("Game map has no WorldEnvironment");
            if (world.Environment is null)
                throw new InvalidDataException("Existing map environment unavailable");
            var baseline = (global::Godot.Environment)world.Environment.Duplicate();
            var preview = (global::Godot.Environment)baseline.Duplicate();
            var effects = data.GetProperty("effects");
            if (effects.ValueKind != JsonValueKind.Array ||
                effects.GetArrayLength() == 0 || effects.GetArrayLength() > 12 ||
                (!synthetic && effects.GetArrayLength() != expected.Count))
                throw new InvalidDataException("Source lighting effect count invalid");

            var skyCount=0;
            var corrections=0;
            var bloomCount=0;
            var fogCount=0;
            var raysCount=0;
            var tint = new Color(1f,1f,1f);
            var brightness = 1f;
            var contrast = 1f;
            var saturation = 1f;
            foreach (var effect in effects.EnumerateArray())
            {
                var kind = String(effect, "kind");
                switch (kind)
                {
                    case "sky":
                        skyCount++;
                        Number(effect,"sunangularsize",0,180);
                        Number(effect,"starcount",0,100000);
                        // IDs cannot reproduce absent source skybox image bytes.
                        break;
                    case "atmosphere":
                        fogCount++;
                        var density = Number(effect,"density",0,1);
                        Number(effect,"offset",-2,2);
                        Number(effect,"glare",0,10);
                        Number(effect,"haze",0,10);
                        var fogColor = RGB(effect,"color");
                        RGB(effect,"decay");
                        // Roblox's Atmosphere scattering is not Godot's linear fog.
                        preview.FogEnabled = true;
                        preview.FogDensity = Math.Clamp(density * .012f, .0003f, .015f);
                        preview.FogLightColor = fogColor;
                        break;
                    case "bloom":
                        bloomCount++;
                        Number(effect,"size",0,1000);
                        var intensity = Number(effect,"intensity",0,10);
                        var threshold = Number(effect,"threshold",0,10);
                        preview.GlowEnabled = effect.GetProperty("enabled").GetBoolean();
                        preview.GlowIntensity = Math.Clamp(intensity,.05f,2f);
                        preview.GlowHdrThreshold = Math.Clamp(threshold,.1f,5f);
                        break;
                    case "color_correction":
                        corrections++;
                        var ccBrightness = Number(effect,"brightness",-1,1);
                        var ccContrast = Number(effect,"contrast",-1,1);
                        var ccSaturation = Number(effect,"saturation",-1,1);
                        var ccTint = RGB(effect,"tintcolor");
                        if (effect.GetProperty("enabled").GetBoolean())
                        {
                            brightness *= 1f + ccBrightness;
                            contrast *= 1f + ccContrast;
                            saturation *= 1f + ccSaturation;
                            tint = new Color(tint.R*ccTint.R,tint.G*ccTint.G,tint.B*ccTint.B);
                        }
                        break;
                    case "sun_rays":
                        raysCount++;
                        var strength = Number(effect,"intensity",0,1);
                        Number(effect,"spread",0,1);
                        if (preview.FogEnabled && effect.GetProperty("enabled").GetBoolean())
                            preview.FogSunScatter = Math.Clamp(strength*5f,0f,.35f);
                        break;
                    default:
                        throw new InvalidDataException("Unknown original lighting effect type");
                }
            }
            if (skyCount != 1 || corrections < 1 || bloomCount > 1 ||
                fogCount > 1 || raysCount > 1)
                throw new InvalidDataException("Original source lighting effect identity invalid");
            preview.AdjustmentEnabled = true;
            preview.AdjustmentBrightness = Math.Clamp(brightness,.5f,1.5f);
            preview.AdjustmentContrast = Math.Clamp(contrast,.4f,1.6f);
            preview.AdjustmentSaturation = Math.Clamp(saturation,.4f,1.6f);
            // Godot lacks the original screen-space TintColor here. Blend only
            // against ambient lighting; no fake original LUT or skybox.
            var tintBlend = new Color(.6f+.4f*tint.R,
                                      .6f+.4f*tint.G,.6f+.4f*tint.B);
            preview.AmbientLightColor *= tintBlend;

            var node = new Pass37SourceLightingRuntime
            {
                Name = "Pass37SourceLighting",
                _world = world,
                _baseline = baseline,
                _preview = preview,
                SourceEffectCount = effects.GetArrayLength()
            };
            owner.AddChild(node);
            GD.Print($"TWR_PASS37_SOURCE_LIGHTING_AVAILABLE map={mapName} " +
                $"effects={node.SourceEffectCount} preview_default=OFF skybox_images=MISSING");
            return node;
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS37_SOURCE_LIGHTING_REJECTED map=" + mapName + ": " + error.Message);
            return null;
        }
    }

    public void SetEnabled(bool enabled)
    {
        Enabled=enabled;
        _world.Environment=enabled ? _preview : _baseline;
    }

    private static WorldEnvironment? FindWorld(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is WorldEnvironment environment) return environment;
            var nested = FindWorld(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static string String(JsonElement e, string key) =>
        e.TryGetProperty(key,out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString() ?? "" : "";

    private static float Number(JsonElement e, string key, float min, float max)
    {
        var item = e.GetProperty(key);
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetSingle(out var value) ||
            !float.IsFinite(value) || value < min || value > max)
            throw new InvalidDataException("Invalid original source lighting numeric: "+key);
        return value;
    }

    private static Color RGB(JsonElement e, string key)
    {
        var values=e.GetProperty(key);
        if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength()!=3)
            throw new InvalidDataException("Invalid original lighting RGB vector: "+key);
        var colors=values.EnumerateArray().ToArray();
        return new Color(Number(colors[0],0f,1f),Number(colors[1],0f,1f),Number(colors[2],0f,1f));
    }

    private static float Number(JsonElement element,float min,float max)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetSingle(out var value) ||
            !float.IsFinite(value) || value<min || value>max)
            throw new InvalidDataException("Invalid source lighting RGB scalar");
        return value;
    }
}

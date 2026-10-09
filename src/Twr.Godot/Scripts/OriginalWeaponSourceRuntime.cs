using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Offline source-derived weapon model assemblies from the original game's
/// ReplicatedStorage/Models/Tools hierarchy. Actual MeshPart mesh binaries
/// remain optional owner-provided local resources; unavailable meshes use
/// original-sized/colorized box proxies without invented model placement.
/// </summary>
public static class OriginalWeaponSourceRuntime
{
    public const string PackName = "SourceWeaponModels.json.gz";
    private static JsonDocument? _document;
    private static bool _initialized;
    private static readonly Dictionary<string, Mesh?> CachedMeshes = new();

    public static bool TryBuild(Node3D parent, string weaponName)
    {
        if (!LoadDocument()) return false;
        if (!_document!.RootElement.GetProperty("models")
            .TryGetProperty(weaponName, out var model))
            return false;

        var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        var sourceParts = model.GetProperty("parts").EnumerateArray().ToArray();
        // Roblox local model axes may point towards +Z after the conversion
        // into Godot's -Z-forward camera convention. Detect barrel direction
        // from the actual original source hierarchy rather than guessing.
        var flipForward = SourceBarrelFacesBackwards(sourceParts);
        var alignment = flipForward
            ? new Basis(Vector3.Up, Mathf.Pi) : Basis.Identity;
        var count = 0;
        foreach (var part in sourceParts)
        {
            var name = part.GetProperty("name").GetString() ?? "OriginalPart";
            var meshId = PartId(part);
            Mesh? mesh = null;
            if (!string.IsNullOrEmpty(meshId))
            {
                if (!CachedMeshes.TryGetValue(meshId, out mesh))
                {
                    mesh = OfflineAssetResolver.Mesh(meshId);
                    CachedMeshes[meshId] = mesh;
                }
            }
            mesh ??= new BoxMesh { Size = Vector3.One };
            var size = SourceSize(part);
            var rgb = part.GetProperty("rgb").EnumerateArray()
                .Select(c => c.GetSingle()).ToArray();
            var alpha = part.GetProperty("opacity").GetSingle();
            var color = new Color(rgb[0]/255f,rgb[1]/255f,rgb[2]/255f,alpha);
            var textureId = part.TryGetProperty("textureId", out var sourceTexture)
                ? sourceTexture.GetString() ?? "" : "";
            var materialKey = color.ToHtml(true) + "|" + textureId;
            if (!materials.TryGetValue(materialKey, out var material))
            {
                var preparedTexture = OfflineAssetResolver.Texture(textureId);
                var surface = new StandardMaterial3D
                {
                    AlbedoColor = color,
                    Roughness = .51f,
                    Metallic = .28f,
                    Transparency = alpha < .995f
                        ? BaseMaterial3D.TransparencyEnum.Alpha
                        : BaseMaterial3D.TransparencyEnum.Disabled
                };
                RobloxMaterialSurface.Apply(surface,
                    part.TryGetProperty("mat", out var m) ? m.GetString() ?? "" : "",
                    preparedTexture);
                material = surface;
                materials[materialKey] = material;
            }
            parent.AddChild(new MeshInstance3D
            {
                Name = name,
                Mesh = mesh,
                MaterialOverride = material,
                Transform = new Transform3D(alignment * SourceRotation(part),
                    alignment * SourcePosition(part)).ScaledLocal(size)
            });
            count++;
        }
        if (count < 1) return false;
        GD.Print($"TWR_ORIGINAL_TOOL_ASSEMBLED name={weaponName} parts={count}");
        return true;
    }

    public static Vector3? EstimatedMuzzle(string weaponName)
    {
        if (!LoadDocument() ||
            !_document!.RootElement.GetProperty("models")
                .TryGetProperty(weaponName, out var model))
            return null;
        var parts = model.GetProperty("parts").EnumerateArray().ToArray();
        var flip = SourceBarrelFacesBackwards(parts);
        var barrel = parts
            .Where(p => IsFrontPart(p.GetProperty("name").GetString() ?? ""))
            .OrderByDescending(p =>
                Math.Abs(SourcePosition(p).Z - AverageZ(parts)))
            .FirstOrDefault();
        if (barrel.ValueKind == JsonValueKind.Undefined) return null;
        var transform = flip ? new Basis(Vector3.Up, Mathf.Pi) : Basis.Identity;
        var center = transform * SourcePosition(barrel);
        // The front of the barrel extends beyond its part CFrame center.
        // Using original dimensions is more stable than a fixed gun category
        // muzzle Z offset across pistols, rifles and launchers.
        return center + new Vector3(0,0,-SourceSize(barrel).Z * .5f);
    }

    private static float AverageZ(JsonElement[] parts) =>
        parts.Length == 0 ? 0f : parts.Average(p => SourcePosition(p).Z);

    private static bool IsFrontPart(string name) =>
        name.Contains("Barrel",StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Muzzle",StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Nozzle",StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Tube",StringComparison.OrdinalIgnoreCase);

    private static bool SourceBarrelFacesBackwards(JsonElement[] parts)
    {
        if (parts.Length == 0) return false;
        var middle = AverageZ(parts);
        var barrel = parts
            .Where(p => IsFrontPart(p.GetProperty("name").GetString() ?? ""))
            .OrderByDescending(p => Math.Abs(SourcePosition(p).Z - middle))
            .FirstOrDefault();
        return barrel.ValueKind != JsonValueKind.Undefined &&
            SourcePosition(barrel).Z > middle;
    }

    private static bool LoadDocument()
    {
        if (_initialized) return _document is not null;
        _initialized = true;
        var exeFolder = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var paths = new[]
        {
            Path.Combine(exeFolder,"Content","Weapons",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Weapons",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                "Content","Weapons",PackName)
        };
        var filename = paths.FirstOrDefault(File.Exists);
        if (filename is null) return false;
        try
        {
            using var input = File.OpenRead(filename);
            using var decompressed = new GZipStream(input,CompressionMode.Decompress);
            _document = JsonDocument.Parse(decompressed);
            if (_document.RootElement.GetProperty("format").GetString() !=
                "twr-original-weapon-assemblies-v1")
                throw new InvalidDataException("Wrong original weapon source data");
            GD.Print("TWR_ORIGINAL_TOOLS_LOADED total=" +
                _document.RootElement.GetProperty("models").EnumerateObject().Count());
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning("TWR_ORIGINAL_TOOLS_FAILED: " + ex.Message);
            _document?.Dispose();
            _document = null;
            return false;
        }
    }

    private static string PartId(JsonElement part)
    {
        foreach (var property in new[] {"meshId","specialMeshId"})
            if (part.TryGetProperty(property, out var id) &&
                id.ValueKind == JsonValueKind.String)
                return id.GetString() ?? "";
        return "";
    }

    private static Vector3 SourcePosition(JsonElement part)
    {
        var value = part.GetProperty("t").EnumerateArray()
            .Select(v => v.GetSingle()).ToArray();
        return new Vector3(value[0],value[1],-value[2]) *
            RobloxUnits.MetersPerStud;
    }

    private static Vector3 SourceSize(JsonElement part)
    {
        var value = part.GetProperty("s").EnumerateArray()
            .Select(v => v.GetSingle()).ToArray();
        return new Vector3(Math.Max(.001f,value[0]),
            Math.Max(.001f,value[1]),Math.Max(.001f,value[2])) *
            RobloxUnits.MetersPerStud;
    }

    private static Basis SourceRotation(JsonElement part)
    {
        var r = part.GetProperty("r").EnumerateArray()
            .Select(v => v.GetSingle()).ToArray();
        return new Basis(
            new Vector3(r[0],r[3],-r[6]),
            new Vector3(r[1],r[4],-r[7]),
            new Vector3(-r[2],-r[5],r[8]));
    }
}

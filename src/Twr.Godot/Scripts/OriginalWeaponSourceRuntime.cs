using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
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
    public const string OriginalSourceSha =
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    public const string OriginalPackSha =
        "fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7";
    private const int MaxPackedBytes = 1024 * 1024;
    public static bool OwnerSourcePackVerified { get; private set; }
    public static int SourceModelCount =>
        LoadDocument() ? _document!.RootElement.GetProperty("models").EnumerateObject().Count() : 0;
    public static int LastVisibleSourceParts { get; private set; }
    public static int LastUnavailableMeshProxies { get; private set; }
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
        var missingMeshProxies = 0;
        LastVisibleSourceParts = 0;
        LastUnavailableMeshProxies = 0;
        foreach (var part in sourceParts)
        {
            var name = part.GetProperty("name").GetString() ?? "OriginalPart";
            // Source Handle/Pos/AimPart helper geometry is intentionally
            // invisible in the original place; do not turn its missing
            // MeshPart binary into a visible Godot bounding cube.
            var alpha = part.GetProperty("opacity").GetSingle();
            if (!float.IsFinite(alpha) || alpha <= .001f) continue;
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
            if (mesh is null && !string.IsNullOrEmpty(meshId))
                missingMeshProxies++;
            // Native Roblox ball Part is precisely approximable as a sphere
            // from authored CFrame/Size. Custom MeshPart/CSG files are NOT
            // embedded in TestPlace and remain explicit bounding proxies.
            var nativeBall = part.GetProperty("class").GetString() == "Part" &&
                part.TryGetProperty("shape", out var shape) &&
                shape.GetString() == "0";
            mesh ??= nativeBall
                ? new SphereMesh { Radius=.5f, Height=1f }
                : new BoxMesh { Size = Vector3.One };
            var size = SourceSize(part);
            var rgb = part.GetProperty("rgb").EnumerateArray()
                .Select(c => c.GetSingle()).ToArray();
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
        LastVisibleSourceParts = count;
        LastUnavailableMeshProxies = missingMeshProxies;
        if (count < 1) return false;
        GD.Print($"TWR_ORIGINAL_TOOL_ASSEMBLED name={weaponName} " +
            $"visible_parts={count} source_mesh_proxy_parts={missingMeshProxies} " +
            $"sha_verified={OwnerSourcePackVerified}");
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
            var fileInfo = new FileInfo(filename);
            if (fileInfo.Length <= 0 || fileInfo.Length > MaxPackedBytes)
                throw new InvalidDataException("Source weapon pack exceeds size budget");
            var compressed = File.ReadAllBytes(filename);
            var packedSha = Convert.ToHexString(SHA256.HashData(compressed))
                .ToLowerInvariant();
            using var input = new MemoryStream(compressed);
            using var decompressed = new GZipStream(input,CompressionMode.Decompress);
            _document = JsonDocument.Parse(decompressed);
            var root = _document.RootElement;
            if (root.GetProperty("format").GetString() !=
                "twr-original-weapon-assemblies-v1")
                throw new InvalidDataException("Wrong original weapon source data");
            var synthetic = root.TryGetProperty("synthetic",out var fixture) &&
                fixture.ValueKind == JsonValueKind.True;
            var testMode = OS.GetCmdlineUserArgs().Any(arg =>
                arg is "--smoke-source-weapons" or "--smoke-pass47" or "--smoke-pass51" or "--smoke-pass52" or "--smoke-pass53");
            // A synthetic fixture may never silently impersonate an original
            // tool pack during ordinary play.
            if (synthetic && !testMode)
                throw new InvalidDataException("Synthetic weapon pack used outside smoke");
            if (!synthetic &&
                (packedSha != OriginalPackSha ||
                 root.GetProperty("original_rbxlx_sha256").GetString()!=OriginalSourceSha ||
                 root.GetProperty("source_model_count").GetInt32()!=98))
                throw new InvalidDataException("Original source weapon SHA mismatch");
            var models = root.GetProperty("models");
            var total = models.EnumerateObject().Count();
            if (total<1 || total>120 || (!synthetic && total!=100))
                throw new InvalidDataException("Unexpected source tool model count");
            foreach(var item in models.EnumerateObject())
            {
                if (item.Name.Length>128 || item.Value.GetProperty("parts")
                    .GetArrayLength() is <1 or >200)
                    throw new InvalidDataException("Source tool model part count invalid");
            }
            OwnerSourcePackVerified = !synthetic;
            GD.Print($"TWR_ORIGINAL_TOOLS_LOADED total={total} " +
                $"owner_source_verified={OwnerSourcePackVerified}");
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

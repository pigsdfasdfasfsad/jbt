using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Opt-in, private 2D reference artwork beside the exported Windows binary.
/// Only ten known map names and canonically named weapon silhouettes resolve.
/// No HTTP / Roblox asset-ID fetch is performed. Missing art retains the
/// existing non-textured HUD/menu without crashing the game.
/// </summary>
public static class Pass27SourceArtCatalog
{
    private const string Format = "twr-pass27-offline-visual-reference-v1";
    private const long MaxManifestBytes = 128 * 1024;
    private const long MaxPngBytes = 2 * 1024 * 1024;
    private static readonly string[] Maps =
    [
        "Ranch", "Mill", "Bypass", "Cabin", "Cargo", "District",
        "Expressway", "Prison", "Laboratory", "Manor"
    ];
    private sealed record Asset(string Path, string Sha256);
    private static bool _initialized;
    private static string? _artFolder;
    private static readonly Dictionary<string, Asset> MapFiles = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Asset> WeaponFiles = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Texture2D?> Loaded = new(StringComparer.Ordinal);

    public static Texture2D? MapCard(string map)
    {
        EnsureManifest();
        if (!Maps.Contains(map, StringComparer.Ordinal)) return null;
        return MapFiles.TryGetValue(map, out var art) ? TextureFor(art) : null;
    }

    public static Texture2D? WeaponIcon(string name)
    {
        EnsureManifest();
        var normalized = Normalize(name);
        return WeaponFiles.TryGetValue(normalized, out var art) ? TextureFor(art) : null;
    }

    private static string Normalize(string name) =>
        new string(name.Where(c => c is >= '0' and <= '9' or >= 'a' and <= 'z' or
                                    >= 'A' and <= 'Z').Select(char.ToLowerInvariant).ToArray());

    private static void EnsureManifest()
    {
        if (_initialized) return;
        _initialized = true;
        var executableDir = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        foreach (var baseDir in new[]
        {
            executableDir,
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot")
        })
        {
            var folder = Path.Combine(baseDir, "Content", "Art");
            var manifest = Path.Combine(folder, "visuals.json");
            var info = new FileInfo(manifest);
            if (!info.Exists || info.Length <= 0 || info.Length > MaxManifestBytes) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllBytes(manifest));
                var root = doc.RootElement;
                if (root.GetProperty("format").GetString() != Format) continue;
                var mapFiles = ParseCategory(root.GetProperty("map_cards"), "MapCards/");
                var weapons = ParseCategory(root.GetProperty("weapon_icons"), "WeaponIcons/");
                if (mapFiles.Count != Maps.Length || weapons.Count < 1 ||
                    Maps.Any(map => !mapFiles.ContainsKey(map))) continue;
                _artFolder = folder;
                foreach (var pair in mapFiles) MapFiles.Add(pair.Key, pair.Value);
                foreach (var pair in weapons) WeaponFiles.Add(pair.Key, pair.Value);
                GD.Print($"TWR_PASS27_VISUALS_READY maps={MapFiles.Count} weapon_icons={WeaponFiles.Count}");
                return;
            }
            catch (Exception error)
            {
                GD.PushWarning("TWR_PASS27_MANIFEST_SKIPPED: " + error.Message);
            }
        }
    }

    private static Dictionary<string, Asset> ParseCategory(JsonElement entries, string allowedPrefix)
    {
        var result = new Dictionary<string, Asset>(StringComparer.Ordinal);
        foreach (var property in entries.EnumerateObject())
        {
            var file = property.Value.GetProperty("file").GetString() ?? "";
            var sha = property.Value.GetProperty("sha256").GetString() ?? "";
            // Even a modified local JSON manifest cannot escape Content/Art.
            if (!file.StartsWith(allowedPrefix, StringComparison.Ordinal) ||
                file.Contains("..", StringComparison.Ordinal) || file.Contains('\\') ||
                !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                !file.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '-' or '_' or '.') ||
                sha.Length != 64 || !sha.All(Uri.IsHexDigit))
                throw new InvalidDataException("Unsafe or malformed offline art reference");
            result.Add(property.Name, new Asset(file, sha.ToLowerInvariant()));
        }
        return result;
    }

    private static Texture2D? TextureFor(Asset asset)
    {
        if (_artFolder is null) return null;
        if (Loaded.TryGetValue(asset.Path, out var existing)) return existing;
        Texture2D? result = null;
        try
        {
            var path = Path.Combine(_artFolder, asset.Path.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= 0 || info.Length > MaxPngBytes)
                throw new InvalidDataException("Missing/oversized offline art");
            var png = File.ReadAllBytes(path);
            if (!SHA256.HashData(png).AsSpan().SequenceEqual(Convert.FromHexString(asset.Sha256)))
                throw new InvalidDataException("Offline art SHA-256 differs from manifest");
            var image = Image.LoadPngFromBuffer(png);
            if (image is null || image.IsEmpty() || image.GetWidth() > 2048 || image.GetHeight() > 2048)
                throw new InvalidDataException("Offline artwork is not a valid PNG");
            result = ImageTexture.CreateFromImage(image);
        }
        catch (Exception ex)
        {
            GD.PushWarning("TWR_PASS27_ART_FALLBACK " + asset.Path + ": " + ex.Message);
        }
        Loaded[asset.Path] = result;
        return result;
    }
}

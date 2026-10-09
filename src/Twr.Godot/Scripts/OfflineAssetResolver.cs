using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Resolves authorized, locally installed art without Roblox, HTTP or editor
/// imports. PNG images and unit-normalized Wavefront OBJ meshes may live in a
/// private Content folder BESIDE the exported EXE. Packed .res resources
/// remain supported when included in the Godot export itself.
/// </summary>
public static class OfflineAssetResolver
{
    private const long MaxMeshBytes = 32L * 1024 * 1024;
    private const long MaxTextureBytes = 64L * 1024 * 1024;
    private const int MaxTriangles = 300_000;
    private static readonly Dictionary<string, Texture2D?> Textures = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Mesh?> Meshes = new(StringComparer.Ordinal);

    public static Texture2D? Texture(string id)
    {
        if (!NumericId(id)) return null;
        if (Textures.TryGetValue(id, out var cached)) return cached;

        var bundled = $"res://Content/Assets/Textures/{id}.png";
        Texture2D? result = ResourceLoader.Exists(bundled)
            ? ResourceLoader.Load<Texture2D>(bundled) : null;
        if (result is null)
        {
            var path = FindPrivateFile("Textures", id + ".png", MaxTextureBytes);
            if (path is not null)
            {
                var image = Image.LoadFromFile(path);
                if (image is not null && !image.IsEmpty())
                    result = ImageTexture.CreateFromImage(image);
            }
        }
        Textures[id] = result;
        return result;
    }

    public static Mesh? Mesh(string id)
    {
        if (!NumericId(id)) return null;
        if (Meshes.TryGetValue(id, out var cached)) return cached;

        var packed = $"res://Content/Assets/Meshes/{id}.res";
        Mesh? result = ResourceLoader.Exists(packed)
            ? ResourceLoader.Load<Mesh>(packed) : null;
        if (result is null)
        {
            var path = FindPrivateFile("Meshes", id + ".obj", MaxMeshBytes);
            if (path is not null)
            {
                try { result = ReadObj(path); }
                catch (Exception error)
                {
                    GD.PushWarning($"TWR_OFFLINE_OBJ_FAILED asset={id}: {error.Message}");
                }
            }
        }
        Meshes[id] = result;
        return result;
    }

    private static bool NumericId(string id)
    {
        if (id.Length is < 1 or > 20) return false;
        foreach (var c in id)
            if (c is < '0' or > '9') return false;
        return true;
    }

    private static string? FindPrivateFile(string folder, string name, long maximumBytes)
    {
        var executable = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(executable, "Content", "Assets", folder, name),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Assets", folder, name),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Twr.Godot",
                "Content", "Assets", folder, name)
        };
        foreach (var candidate in candidates)
        {
            var info = new FileInfo(candidate);
            if (info.Exists && info.Length > 0 && info.Length <= maximumBytes)
                return candidate;
        }
        return null;
    }

    private static Mesh ReadObj(string path)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tool = new SurfaceTool();
        tool.Begin(global::Godot.Mesh.PrimitiveType.Triangles);
        var triangles = 0;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2) continue;
            switch (fields[0])
            {
                case "v" when fields.Length >= 4:
                    vertices.Add(new Vector3(
                        Parse(fields[1]), Parse(fields[2]), Parse(fields[3])));
                    break;
                case "vt" when fields.Length >= 3:
                    uvs.Add(new Vector2(Parse(fields[1]), 1f - Parse(fields[2])));
                    break;
                case "f" when fields.Length >= 4:
                    // OBJ polygon fan: support tris, quads and modest n-gons
                    // without depending on editor-only Godot mesh importers.
                    if (fields.Length > 17)
                        throw new InvalidDataException("OBJ polygon too large");
                    for (var face = 2; face < fields.Length - 1; face++)
                    {
                        if (++triangles > MaxTriangles)
                            throw new InvalidDataException("OBJ triangle limit exceeded");
                        AddCorner(fields[1], vertices, uvs, tool);
                        AddCorner(fields[face], vertices, uvs, tool);
                        AddCorner(fields[face + 1], vertices, uvs, tool);
                    }
                    break;
            }
            if (vertices.Count > MaxTriangles * 3)
                throw new InvalidDataException("OBJ vertex limit exceeded");
        }
        if (triangles == 0) throw new InvalidDataException("OBJ has no faces");
        tool.GenerateNormals();
        return tool.Commit() ?? throw new InvalidDataException("OBJ triangulation failed");
    }

    private static void AddCorner(
        string token, List<Vector3> vertices, List<Vector2> uvs, SurfaceTool tool)
    {
        var parts = token.Split('/');
        var vi = ObjIndex(parts[0], vertices.Count);
        var uv = Vector2.Zero;
        if (parts.Length >= 2 && parts[1].Length > 0)
            uv = uvs[ObjIndex(parts[1], uvs.Count)];
        tool.SetUV(uv);
        tool.AddVertex(vertices[vi]);
    }

    private static int ObjIndex(string input, int count)
    {
        if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var value) || value == 0)
            throw new InvalidDataException("Invalid OBJ vertex index");
        var index = value > 0 ? value - 1 : count + value;
        if (index < 0 || index >= count)
            throw new InvalidDataException("OBJ index out of bounds");
        return index;
    }

    private static float Parse(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var result) ||
            !float.IsFinite(result) || Math.Abs(result) > 100000f)
            throw new InvalidDataException("Nonfinite or excessive OBJ coordinate");
        return result;
    }
}

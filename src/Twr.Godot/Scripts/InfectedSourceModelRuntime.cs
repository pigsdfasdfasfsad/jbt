using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Reassembles visible, source-positioned R6 infected body parts from the
/// actual Workspace/Entities/Infected snapshots inside the owner's private
/// original-place archive. Does not imply original cloud meshes, clothing
/// textures or animation tracks have been recovered.
/// </summary>
public static class InfectedSourceModelRuntime
{
    public const string PackName = "InfectedSourceVariants.json.gz";
    private static JsonDocument? _data;
    private static bool _attempted;

    public static bool TryBuild(Node3D parent, string type,
        out Node3D? leftArm, out Node3D? rightArm,
        out Node3D? leftLeg, out Node3D? rightLeg)
    {
        leftArm = rightArm = leftLeg = rightLeg = null;
        if (!EnsureLoaded()) return false;
        if (!_data!.RootElement.GetProperty("types").TryGetProperty(type, out var variants) ||
            variants.GetArrayLength() == 0) return false;
        var variant = variants[Random.Shared.Next(variants.GetArrayLength())];
        var parts = variant.GetProperty("parts");
        var rig = new Node3D { Name = "SourceR6Body" };
        var anchors = new Dictionary<string, Node3D>(StringComparer.Ordinal);
        parent.AddChild(rig);

        // Anchor pivots are derived from the source's limb/body layout and
        // then animated through the existing InfectedAgent animation driver.
        foreach (var part in parts.EnumerateArray())
        {
            var region = part.GetProperty("region").GetString() ?? "Torso";
            if (region is not ("LeftArm" or "RightArm" or "LeftLeg" or "RightLeg"))
                continue;
            if (anchors.ContainsKey(region)) continue;
            var pos = Position(part);
            var dimensions = Size(part);
            var y = region.EndsWith("Arm",StringComparison.Ordinal)
                ? dimensions.Y * 0.42f : dimensions.Y * 0.45f;
            var node = new Node3D
            {
                Name = region + "Pivot",
                Position = pos + Vector3.Up * y
            };
            rig.AddChild(node);
            anchors[region] = node;
        }

        var materialCache = new Dictionary<string, StandardMaterial3D>();
        var meshCache = new Dictionary<string, Mesh?>();
        var hasSourceHead = false;
        foreach (var part in parts.EnumerateArray())
        {
            var partName = part.GetProperty("name").GetString() ?? "OriginalPart";
            if (partName is "Head" or "BloatHead") hasSourceHead = true;
            // A recovered plain accessory Handle has no mesh of its own.
            // Showing it as a 2x1x1 solid block hides the face/eyes.
            if (partName == "Handle" && string.IsNullOrEmpty(SourceId(part)))
                continue;
            var region = part.GetProperty("region").GetString() ?? "Torso";
            var localPosition = Position(part);
            var parentNode = anchors.TryGetValue(region, out var pivot) ? pivot : rig;
            var offset = parentNode == rig
                ? localPosition : localPosition - pivot!.Position;
            var color = ColorFor(part);
            var textureId = part.TryGetProperty("textureId", out var originalTexture)
                ? originalTexture.GetString() ?? "" : "";
            var texturePath = $"res://Content/Assets/Textures/{textureId}.png";
            var key = color.ToHtml(true) + "|" + textureId;
            if (!materialCache.TryGetValue(key, out var material))
            {
                material = new StandardMaterial3D
                {
                    AlbedoColor = color,
                    AlbedoTexture = OfflineAssetResolver.Texture(textureId),
                    Roughness = 0.87f,
                    Transparency = color.A < 0.999f
                        ? BaseMaterial3D.TransparencyEnum.Alpha
                        : BaseMaterial3D.TransparencyEnum.Disabled
                };
                materialCache[key] = material;
            }
            var id = SourceId(part);
            Mesh? mesh = null;
            if (!string.IsNullOrEmpty(id))
            {
                if (!meshCache.TryGetValue(id, out mesh))
                {
                    mesh = OfflineAssetResolver.Mesh(id);
                    meshCache[id] = mesh;
                }
            }
            var dimensions = Size(part);
            mesh ??= part.GetProperty("name").GetString() is "Head"
                ? new SphereMesh { Radius = 0.5f, Height = 1.0f }
                : new BoxMesh { Size = Vector3.One };
            var basis = Rotation(part);
            var position = new Transform3D(basis,offset);
            parentNode.AddChild(new MeshInstance3D
            {
                Name = partName,
                Transform = position.ScaledLocal(dimensions),
                Mesh = mesh,
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On
            });
        }

        if (!hasSourceHead)
        {
            // The original sampled Roblox infected snapshots omit the R6
            // Head part (all 44 recovered active variants). Restore an R6
            // head in its authored position, without fabricating a MeshId.
            var headMaterial = new StandardMaterial3D
            {
                AlbedoColor = new Color(.54f,.49f,.42f),
                Roughness = .91f
            };
            rig.AddChild(new MeshInstance3D
            {
                Name = "ReconstructedR6Head",
                Position = Vector3.Up * RobloxUnits.Distance(1.5f),
                Scale = Vector3.One * RobloxUnits.Distance(1f),
                Mesh = new SphereMesh { Radius = .5f, Height = 1f },
                MaterialOverride = headMaterial
            });
        }

        anchors.TryGetValue("LeftArm", out leftArm);
        anchors.TryGetValue("RightArm", out rightArm);
        anchors.TryGetValue("LeftLeg", out leftLeg);
        anchors.TryGetValue("RightLeg", out rightLeg);
        GD.Print($"TWR_SOURCE_INFECTED_ASSEMBLED type={type} parts={parts.GetArrayLength()}");
        return true;
    }

    private static string SourceId(JsonElement part)
    {
        foreach (var key in new[] { "meshId", "specialMeshId" })
            if (part.TryGetProperty(key, out var id) &&
                id.ValueKind == JsonValueKind.String)
                return id.GetString() ?? "";
        return "";
    }

    private static Vector3 Size(JsonElement p)
    {
        var v = Read(p.GetProperty("s"));
        return new Vector3(Math.Max(.005f, v[0]) * RobloxUnits.MetersPerStud,
                           Math.Max(.005f, v[1]) * RobloxUnits.MetersPerStud,
                           Math.Max(.005f, v[2]) * RobloxUnits.MetersPerStud);
    }

    private static Vector3 Position(JsonElement p)
    {
        var v = Read(p.GetProperty("t"));
        return new Vector3(v[0],v[1],-v[2]) * RobloxUnits.MetersPerStud;
    }

    private static Basis Rotation(JsonElement p)
    {
        var v = Read(p.GetProperty("r"));
        return new Basis(
            new Vector3(v[0],v[3],-v[6]),
            new Vector3(v[1],v[4],-v[7]),
            new Vector3(-v[2],-v[5],v[8]));
    }

    private static float[] Read(JsonElement arr) =>
        arr.EnumerateArray().Select(x => x.GetSingle()).ToArray();

    private static Color ColorFor(JsonElement part)
    {
        var rgb = Read(part.GetProperty("rgb"));
        var alpha = part.GetProperty("opacity").GetSingle();
        return new Color(
            rgb[0]/255f,rgb[1]/255f,rgb[2]/255f,Math.Clamp(alpha,0,1));
    }

    private static bool EnsureLoaded()
    {
        if (_attempted) return _data is not null;
        _attempted = true;
        var folder = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var paths = new[]
        {
            Path.Combine(folder,"Content","Enemies",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Enemies",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                "Content","Enemies",PackName)
        };
        var path = paths.FirstOrDefault(File.Exists);
        if (path is null) return false;
        try
        {
            using var input = File.OpenRead(path);
            using var gzip = new GZipStream(input,CompressionMode.Decompress);
            _data = JsonDocument.Parse(gzip);
            if (_data.RootElement.GetProperty("format").GetString() !=
                "twr-source-infected-variants-v1")
                throw new InvalidDataException("Wrong private infected blueprint format");
            GD.Print("TWR_SOURCE_INFECTED_VARIANTS_LOADED");
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning("TWR_SOURCE_INFECTED_FAILED: " + ex.Message);
            _data?.Dispose();
            _data = null;
            return false;
        }
    }
}

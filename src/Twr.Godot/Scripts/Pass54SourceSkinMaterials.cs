using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass54 private owner-only material metadata extracted from the original
/// ModuleScript.Skins and source Tools part-tag hierarchy. Does not evaluate
/// source Luau, fetch Roblox IDs, or package owner assets in public CI.
/// Two independently SHA-pinned sidecars provide five source RGB/material
/// values per skin and the exact recovered Value1..Value5 part assignments.
/// </summary>
public static class Pass54SourceSkinMaterials
{
    public const string PaletteName = "SourceSkinMaterials54.json.gz";
    public const string SlotMapName = "SourceWeaponSkinSlots54.json.gz";
    public const string OriginalSkinModuleSha256 =
        "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015";
    public const string OriginalPlaceSha256 =
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    public const string OwnerPalettePackSha256 =
        "e9881057cd8bbe9c16c9c3e567de9ccba1fe94250f4db0d55ef5c61b6871b023";
    public const string OwnerSlotPackSha256 =
        "f38d7e6275db2689f8244af7abf0e06ac8690fd6da5a1d13edc887b05cb24afe";

    public sealed record SourceMaterial(
        Color Color, string RobloxMaterial, float Transparency,
        float Reflectance, bool MissingTexture, string MaterialVariant,
        string SurfaceAppearance);

    private static Dictionary<string, SourceMaterial[]>? _skinValues;
    private static Dictionary<string,Dictionary<int,int>>? _weaponSlots;
    public static bool OwnerPaletteVerified { get; private set; }
    public static bool OwnerSlotsVerified { get; private set; }
    public static int LoadedSkinCount { get { LoadPalette(); return _skinValues!.Count; } }
    public static int LoadedTaggedParts
    {
        get { LoadSlots(); return _weaponSlots!.Values.Sum(v => v.Count); }
    }

    public static bool TryGet(string? skinId, out SourceMaterial[] materials)
    {
        LoadPalette();
        if (skinId is not null && _skinValues!.TryGetValue(skinId,out var array))
        {
            materials = array;
            return true;
        }
        materials = [];
        return false;
    }

    public static bool TryGetPartSlot(string weapon, int sourceIndex, out int slot)
    {
        LoadSlots();
        slot=0;
        return sourceIndex>=0 &&
            _weaponSlots!.TryGetValue(weapon,out var parts) &&
            parts.TryGetValue(sourceIndex,out slot);
    }

    public static string Provenance(bool exactPartMapping)
    {
        LoadPalette();
        LoadSlots();
        if (_skinValues!.Count==0)
            return "APPROXIMATE COLOR - ORIGINAL PALETTE NOT INSTALLED";
        if (!OwnerPaletteVerified)
            return "SYNTHETIC PALETTE FIXTURE (TEST ONLY)";
        return exactPartMapping && OwnerSlotsVerified
            ? "SOURCE RGB / MATERIAL / EXACT SOURCE PART TAGS"
            : "SOURCE RGB / MATERIAL / APPROXIMATE PART MAPPING";
    }

    private static void LoadPalette()
    {
        if (_skinValues is not null) return;
        var values=new Dictionary<string,SourceMaterial[]>(StringComparer.Ordinal);
        try
        {
            using var json=ReadPacked(PaletteName,OwnerPalettePackSha256,
                "twr-source-skin-material-v1",out var verified);
            if (json is null) { _skinValues=values; return; }
            var root=json.RootElement;
            var all=root.GetProperty("skins");
            if (all.ValueKind!=JsonValueKind.Object || all.EnumerateObject().Count() >
                118 || all.EnumerateObject().Count()<1)
                throw new InvalidDataException("Skin catalog entry count invalid");
            if (verified && (root.GetProperty("source_sha256").GetString() !=
                OriginalSkinModuleSha256 ||
                root.GetProperty("source_entries").GetInt32()!=118 ||
                root.GetProperty("values_per_skin").GetInt32()!=5 ||
                all.EnumerateObject().Count()!=118))
                throw new InvalidDataException("Verified owner material schema mismatch");
            foreach(var entry in all.EnumerateObject())
            {
                if (SkinCaseCatalog.FindSkin(entry.Name) is null ||
                    entry.Value.ValueKind!=JsonValueKind.Array ||
                    entry.Value.GetArrayLength()!=5)
                    throw new InvalidDataException("Unknown or malformed source skin");
                var slots=new List<SourceMaterial>(5);
                foreach(var item in entry.Value.EnumerateArray())
                {
                    var rgb=item.GetProperty("rgb").EnumerateArray().ToArray();
                    if(rgb.Length!=3) throw new InvalidDataException("Wrong RGB length");
                    var valuesRgb=rgb.Select(x=>x.GetInt32()).ToArray();
                    if(valuesRgb.Any(v=>v<0 || v>255))
                        throw new InvalidDataException("RGB component out of bounds");
                    var kind=item.GetProperty("material").GetString() ?? "";
                    if(kind.Length>32 || kind.Any(c=>!char.IsLetterOrDigit(c)))
                        throw new InvalidDataException("Unrecognized material token");
                    var opacity=item.TryGetProperty("transparency",out var a)
                        ? a.GetSingle() : 0f;
                    var reflect=item.TryGetProperty("reflectance",out var b)
                        ? b.GetSingle() : 0f;
                    if(!float.IsFinite(opacity)||opacity<0||opacity>1||
                       !float.IsFinite(reflect)||reflect<0||reflect>1)
                        throw new InvalidDataException("Unbounded surface scalar");
                    static string Optional(JsonElement source,string key) =>
                        source.TryGetProperty(key,out var e) &&
                        e.ValueKind==JsonValueKind.String ? e.GetString() ?? "" : "";
                    var variant=Optional(item,"variant");
                    var appearance=Optional(item,"surface");
                    if(variant.Length>40 || appearance.Length>40)
                        throw new InvalidDataException("Unbounded surface identifier");
                    var textureMissing=item.TryGetProperty("textureUnavailable",out var flag)
                        && flag.ValueKind==JsonValueKind.True;
                    slots.Add(new SourceMaterial(
                        new Color(valuesRgb[0]/255f,valuesRgb[1]/255f,
                            valuesRgb[2]/255f,1f-opacity),
                        kind,opacity,reflect,textureMissing,variant,appearance));
                }
                values.Add(entry.Name,slots.ToArray());
            }
            OwnerPaletteVerified=verified;
            GD.Print($"TWR_PASS54_PALETTE_LOADED skins={values.Count} " +
                $"values={values.Count*5} owner_verified={verified}");
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS54_PALETTE_REJECTED "+ex.Message);
            values.Clear();
            OwnerPaletteVerified=false;
        }
        _skinValues=values;
    }

    private static void LoadSlots()
    {
        if (_weaponSlots is not null) return;
        var models=new Dictionary<string,Dictionary<int,int>>(StringComparer.Ordinal);
        try
        {
            using var json=ReadPacked(SlotMapName,OwnerSlotPackSha256,
                "twr-source-skin-slot-map-v1",out var verified);
            if(json is null) { _weaponSlots=models; return; }
            var root=json.RootElement;
            var container=root.GetProperty("models");
            if(container.ValueKind!=JsonValueKind.Object ||
                container.EnumerateObject().Count()>120)
                throw new InvalidDataException("Unexpected source weapon count");
            if(verified && (root.GetProperty("source_rbxlx_sha256").GetString() !=
                OriginalPlaceSha256 ||
                root.GetProperty("original_models").GetInt32()!=98 ||
                root.GetProperty("original_parts").GetInt32()!=1484 ||
                root.GetProperty("tagged_parts").GetInt32()!=728 ||
                container.EnumerateObject().Count()!=98))
                throw new InvalidDataException("Owner source slot schema mismatch");
            foreach(var weapon in container.EnumerateObject())
            {
                if(weapon.Name.Length>128 || weapon.Value.ValueKind!=JsonValueKind.Object)
                    throw new InvalidDataException("Unknown source model shape");
                var parts=new Dictionary<int,int>();
                foreach(var tag in weapon.Value.EnumerateObject())
                {
                    if(!int.TryParse(tag.Name,out var index)||index<0||index>=200 ||
                        tag.Value.ValueKind!=JsonValueKind.Number)
                        throw new InvalidDataException("Invalid source part index");
                    var slot=tag.Value.GetInt32();
                    if(slot<1 || slot>5)
                        throw new InvalidDataException("Invalid source Value1..Value5 label");
                    parts.Add(index,slot);
                }
                models.Add(weapon.Name,parts);
            }
            if(verified && models.Values.Sum(m=>m.Count)!=728)
                throw new InvalidDataException("Original source tag count mismatch");
            OwnerSlotsVerified=verified;
            GD.Print($"TWR_PASS54_PART_TAGS_LOADED total={models.Values.Sum(m=>m.Count)} " +
                $"owner_verified={verified}");
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS54_PART_TAGS_REJECTED "+ex.Message);
            models.Clear();
            OwnerSlotsVerified=false;
        }
        _weaponSlots=models;
    }

    private static JsonDocument? ReadPacked(
        string filename,string expectedSha,string format,out bool verified)
    {
        verified=false;
        var exeFolder=Path.GetDirectoryName(OS.GetExecutablePath()) ??
            Directory.GetCurrentDirectory();
        var paths=new[] {
            Path.Combine(exeFolder,"Content","Skins",filename),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Skins",filename),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                "Content","Skins",filename)
        };
        var path=paths.FirstOrDefault(File.Exists);
        if(path is null)return null;
        var compressed=File.ReadAllBytes(path);
        if(compressed.Length<20 || compressed.Length>128*1024)
            throw new InvalidDataException("Private source skin pack exceeds budget");
        var sha=Convert.ToHexString(SHA256.HashData(compressed)).ToLowerInvariant();
        var test=OS.GetCmdlineUserArgs().Contains("--smoke-pass54",
            StringComparer.Ordinal);
        using var input=new MemoryStream(compressed);
        using var gzip=new GZipStream(input,CompressionMode.Decompress);
        using var buffer=new MemoryStream();
        var block=new byte[16*1024];
        int read;
        while((read=gzip.Read(block,0,block.Length))>0)
        {
            if(buffer.Length+read>512*1024)
                throw new InvalidDataException("Decompressed skin metadata exceeds budget");
            buffer.Write(block,0,read);
        }
        buffer.Position=0;
        var json=JsonDocument.Parse(buffer);
        try
        {
            var root=json.RootElement;
            if(root.GetProperty("format").GetString()!=format)
                throw new InvalidDataException("Wrong skin metadata format");
            var synthetic=root.TryGetProperty("synthetic",out var flag) &&
                flag.ValueKind==JsonValueKind.True;
            if(synthetic && !test)
                throw new InvalidDataException("Synthetic skin metadata outside CI smoke");
            if(!synthetic && sha!=expectedSha)
                throw new InvalidDataException("Original skin pack SHA256 mismatch");
            verified=!synthetic;
            return json;
        }
        catch
        {
            json.Dispose();
            throw;
        }
    }
}

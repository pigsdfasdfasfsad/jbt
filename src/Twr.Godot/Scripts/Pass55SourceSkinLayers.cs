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
/// Pass55 inert private source texture-reference metadata. The owner Luau
/// provides 118 skin image references, 94 face texture layers and 33 mesh
/// TextureID references, but not image bytes or an animation controller.
/// No Roblox network access or source Luau evaluation is permitted.
/// </summary>
public static class Pass55SourceSkinLayers
{
    public const string FileName = "SourceSkinLayers55.json.gz";
    public const string SourceSha256 =
        "aeff32781cc9eafdbbd5ae7bc12ff157cc32934e7dae6773c87d7bfba355a015";
    public const string VerifiedPackSha256 =
        "c6af38ce98042dc47f928b73720d5f7035b3f1d278936695855fe025d406bcdd";

    public sealed record Layer(string Kind,string TextureId,Color Tint,
        float Transparency,float TileU,float TileV,float OffsetU,float OffsetV,int ZIndex);
    public sealed record Slot(string MeshTextureId,IReadOnlyList<Layer> Layers);
    public sealed record SkinTextureEntry(string IconId,IReadOnlyList<Slot> Slots);

    private static Dictionary<string,SkinTextureEntry>? _entries;
    public static bool OwnerPackVerified {get;private set;}
    public static int LoadedSkinCount {get {EnsureLoaded();return _entries!.Count;} }
    public static int LayerReferenceCount
    {
        get {EnsureLoaded();return _entries!.Values.Sum(s=>s.Slots.Sum(v=>v.Layers.Count));}
    }
    public static int MeshTextureReferenceCount
    {
        get {EnsureLoaded();return _entries!.Values.Sum(s=>s.Slots.Count(v=>v.MeshTextureId.Length>0));}
    }
    public static bool TryGet(string? skinId,out SkinTextureEntry entry)
    {
        EnsureLoaded();
        if(skinId is not null && _entries!.TryGetValue(skinId,out var found))
        {
            entry=found;return true;
        }
        entry=new SkinTextureEntry("",Array.Empty<Slot>());
        return false;
    }
    public static bool TryGetSlot(string? skinId,int slot,out Slot value)
    {
        if(slot is >=1 and <=5 && TryGet(skinId,out var entry)
            && entry.Slots.Count==5)
        {
            value=entry.Slots[slot-1];return true;
        }
        value=new Slot("",Array.Empty<Layer>());
        return false;
    }

    private static string ReadAssetId(JsonElement element,string property)
    {
        if(!element.TryGetProperty(property,out var id) ||
           id.ValueKind!=JsonValueKind.String)
            return "";
        var value=id.GetString()??"";
        if(value.Length>20 || value.Any(c=>c<'0'||c>'9'))
            throw new InvalidDataException("Non-numeric skin texture asset ID");
        return value;
    }

    private static float Number(JsonElement element,string key,
        float defaultValue,float minimum,float maximum)
    {
        if(!element.TryGetProperty(key,out var field))return defaultValue;
        var result=field.GetSingle();
        if(!float.IsFinite(result)||result<minimum||result>maximum)
            throw new InvalidDataException("Out-of-range skin texture scalar");
        return result;
    }

    private static void EnsureLoaded()
    {
        if(_entries is not null)return;
        var results=new Dictionary<string,SkinTextureEntry>(StringComparer.Ordinal);
        try
        {
            var exeFolder=Path.GetDirectoryName(OS.GetExecutablePath()) ??
                Directory.GetCurrentDirectory();
            var paths=new[] {
                Path.Combine(exeFolder,"Content","Skins",FileName),
                Path.Combine(Directory.GetCurrentDirectory(),"Content","Skins",FileName),
                Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                    "Content","Skins",FileName)
            };
            var path=paths.FirstOrDefault(File.Exists);
            if(path is null){_entries=results;return;}
            var packed=File.ReadAllBytes(path);
            if(packed.Length is <20 or >98304)
                throw new InvalidDataException("Skin texture pack compressed size exceeded");
            var sha=Convert.ToHexString(SHA256.HashData(packed)).ToLowerInvariant();
            using var input=new MemoryStream(packed);
            using var gz=new GZipStream(input,CompressionMode.Decompress);
            using var buffer=new MemoryStream();
            var block=new byte[16*1024];
            int len;
            while((len=gz.Read(block))>0)
            {
                if(buffer.Length+len>262144)
                    throw new InvalidDataException("Skin texture pack decompressed size exceeded");
                buffer.Write(block,0,len);
            }
            buffer.Position=0;
            using var json=JsonDocument.Parse(buffer);
            var root=json.RootElement;
            if(root.GetProperty("format").GetString()!="twr-source-skin-texture-layers-v1")
                throw new InvalidDataException("Wrong skin texture pack format");
            var synthetic=root.TryGetProperty("synthetic",out var synth) &&
                synth.ValueKind==JsonValueKind.True;
            var args=OS.GetCmdlineUserArgs();
            var test=args.Contains("--smoke-pass55",StringComparer.Ordinal) ||
                args.Contains("--smoke-pass56",StringComparer.Ordinal);
            if(synthetic && !test)
                throw new InvalidDataException("Synthetic source textures refused outside smoke");
            if(!synthetic && (sha!=VerifiedPackSha256 ||
                root.GetProperty("source_sha256").GetString()!=SourceSha256 ||
                root.GetProperty("skin_count").GetInt32()!=118 ||
                root.GetProperty("layer_count").GetInt32()!=94 ||
                root.GetProperty("mesh_texture_count").GetInt32()!=33))
                throw new InvalidDataException("Source skin texture pack SHA/schema mismatch");

            var skins=root.GetProperty("skins");
            if(skins.ValueKind!=JsonValueKind.Object ||
               skins.EnumerateObject().Count() is <1 or >118 ||
               (!synthetic && skins.EnumerateObject().Count()!=118))
                throw new InvalidDataException("Unexpected source skin texture count");
            foreach(var skin in skins.EnumerateObject())
            {
                if(SkinCaseCatalog.FindSkin(skin.Name) is null)
                    throw new InvalidDataException("Unknown source skin texture key");
                var icon=ReadAssetId(skin.Value,"imageId");
                var slots=skin.Value.GetProperty("slots");
                if(slots.ValueKind!=JsonValueKind.Array || slots.GetArrayLength()!=5)
                    throw new InvalidDataException("Expected five source material slots");
                var records=new List<Slot>(5);
                foreach(var slot in slots.EnumerateArray())
                {
                    var mesh=ReadAssetId(slot,"meshTextureId");
                    var layers=slot.GetProperty("layers");
                    if(layers.ValueKind!=JsonValueKind.Array || layers.GetArrayLength()>2)
                        throw new InvalidDataException("Too many source texture layers");
                    var parsed=new List<Layer>(2);
                    foreach(var layer in layers.EnumerateArray())
                    {
                        var kind=layer.GetProperty("kind").GetString()??"";
                        if(kind is not ("Texture" or "Texture1"))
                            throw new InvalidDataException("Unknown original layer class");
                        var id=ReadAssetId(layer,"textureId");
                        if(id.Length==0)throw new InvalidDataException("Empty source texture");
                        var color=layer.GetProperty("rgb").EnumerateArray().ToArray();
                        if(color.Length!=3)
                            throw new InvalidDataException("Texture RGB length invalid");
                        var channel=color.Select(c=>c.GetInt32()).ToArray();
                        if(channel.Any(c=>c<0||c>255))
                            throw new InvalidDataException("Source texture color invalid");
                        var transparency=Number(layer,"transparency",0f,0f,1f);
                        var tileU=Number(layer,"tileU",1f,.05f,64f);
                        var tileV=Number(layer,"tileV",1f,.05f,64f);
                        var offsetU=Number(layer,"offsetU",0f,-32f,32f);
                        var offsetV=Number(layer,"offsetV",0f,-32f,32f);
                        var z=(int)Number(layer,"zIndex",0f,-100f,100f);
                        parsed.Add(new Layer(kind,id,new Color(
                            channel[0]/255f,channel[1]/255f,channel[2]/255f),
                            transparency,tileU,tileV,offsetU,offsetV,z));
                    }
                    records.Add(new Slot(mesh,parsed));
                }
                results.Add(skin.Name,new SkinTextureEntry(icon,records));
            }
            if(!synthetic && (results.Values.Sum(x=>x.Slots.Sum(v=>v.Layers.Count))!=94 ||
                 results.Values.Sum(x=>x.Slots.Count(v=>v.MeshTextureId.Length>0))!=33))
                throw new InvalidDataException("Original reference totals mismatched");
            OwnerPackVerified=!synthetic;
            GD.Print($"TWR_PASS55_LAYER_REFERENCES_READY skins={results.Count} " +
                $"layers={results.Values.Sum(x=>x.Slots.Sum(v=>v.Layers.Count))} " +
                $"verified={OwnerPackVerified}");
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS55_LAYER_PACK_REJECTED "+ex.Message);
            results.Clear();
            OwnerPackVerified=false;
        }
        _entries=results;
    }
}

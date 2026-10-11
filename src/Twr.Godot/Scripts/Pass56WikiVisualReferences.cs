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
/// Pass56: offline, private, SHA-checked gallery of owner-supplied wiki
/// reference SCREENSHOTS. These are not original unwrapped UV textures.
/// No HTTP/Roblox fetching and no source images shipped in public Git.
/// </summary>
public static class Pass56WikiVisualReferences
{
    public const string PackName="SourceWikiReferenceImages56.zip";
    public const string OwnerPackSha256=
        "c06abbaefcaa33430a5a37ddb10fe7540fb27ac39c863fd05c420cad0d2cd2c1";
    public const string OwnerImagesArchiveSha256=
        "b07b058367d115b9710feb25accf0ac5cc8a1de74b4b1769a890ff0c457115f0";
    public const string OwnerDocsArchiveSha256=
        "e2ebf80af7b6591af0de885a9cee9e863da398407fdc741ac3f215077936984f";
    private const string SourceFormat="twr-wiki-weapon-reference-v1";
    private const int MaxArchiveBytes=32*1024*1024;
    private const int MaxManifestBytes=1_500_000;
    private const int MaxSingleImageBytes=1_500_000;
    private const int MaxImageSide=4096;

    private sealed record FileRecord(string Hash,int Bytes,int Width,int Height);
    private sealed record WeaponReferences(
        string Default,Dictionary<string,string> Skins,
        Dictionary<string,string> Poses);
    private static readonly Dictionary<string,FileRecord> Files=
        new(StringComparer.Ordinal);
    private static readonly Dictionary<string,WeaponReferences> Weapons=
        new(StringComparer.Ordinal);
    private static readonly Dictionary<string,string> Icons=
        new(StringComparer.Ordinal);
    private static readonly Dictionary<string,Texture2D> Cache=
        new(StringComparer.Ordinal);
    private static readonly Queue<string> CacheOrder=new();
    private static bool _loaded;
    private static string? _packPath;
    public static bool OwnerPackVerified {get;private set;}
    public static bool SyntheticFixture {get;private set;}
    public static int WeaponCount {get {EnsureLoaded();return Weapons.Count;}}
    public static int ImageCount {get {EnsureLoaded();return Files.Count;}}
    public static int SkinIconCount {get {EnsureLoaded();return Icons.Count;}}
    public static string Provenance => SyntheticFixture
        ? "SYNTHETIC WIKI IMAGE FIXTURE (TEST ONLY)" :
          "OWNER WIKI REFERENCE IMAGE - NOT ORIGINAL 3D/UV TEXTURE";

    public static bool TryWeaponImage(string weapon,string? skinId,
        out Texture2D? texture,out string caption)
    {
        EnsureLoaded();
        texture=null;
        caption="No offline wiki visual reference for this weapon.";
        if(!Weapons.TryGetValue(weapon,out var choices))return false;
        string? exact=null;
        var match=skinId is not null &&
            choices.Skins.TryGetValue(skinId,out exact);
        var imageName=match ? exact! : choices.Default;
        if(string.IsNullOrWhiteSpace(imageName))
        {
            caption="No reference image for this exact weapon and skin pair.";
            return false;
        }
        texture=LoadImage(imageName);
        if(texture is null)
        {
            caption="Reference image is missing or did not pass integrity checks.";
            return false;
        }
        caption=match
            ? weapon+" with "+skinId+" (historical wiki reference)"
            : (skinId is not null && skinId.Length>0)
                ? weapon+" DEFAULT image only; NO exact "+skinId+
                    " screenshot available"
                : weapon+" default historical wiki reference";
        return true;
    }

    public static bool TrySkinIcon(string skinId,out Texture2D? texture)
    {
        EnsureLoaded();
        texture=null;
        return Icons.TryGetValue(skinId,out var name) &&
            (texture=LoadImage(name)) is not null;
    }

    private static bool SafeName(string path)
    {
        if(path.Length is <20 or >170 ||
           !path.StartsWith("reference-images/",StringComparison.Ordinal) ||
           !path.EndsWith(".png",StringComparison.OrdinalIgnoreCase) &&
           !path.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase) &&
           !path.EndsWith(".jpeg",StringComparison.OrdinalIgnoreCase))
            return false;
        var rest=path["reference-images/".Length..];
        return rest.Length>0 && rest.IndexOfAny(['/', '\\', ':'])<0 &&
            rest.All(c=>char.IsAsciiLetterOrDigit(c) ||
                c is '-' or '_' or '.' or ' ' or '+' or '(' or ')');
    }

    private static string CheckedReference(JsonElement element)
    {
        if(element.ValueKind!=JsonValueKind.String)
            throw new InvalidDataException("Reference name not a string");
        var path=element.GetString()??"";
        if(!SafeName(path)||!Files.ContainsKey(path))
            throw new InvalidDataException("Unlisted reference path");
        return path;
    }

    private static void EnsureLoaded()
    {
        if(_loaded)return;
        _loaded=true;
        try
        {
            var exe=Path.GetDirectoryName(OS.GetExecutablePath()) ??
                Directory.GetCurrentDirectory();
            _packPath=new[] {
                Path.Combine(exe,"Content","References",PackName),
                Path.Combine(Directory.GetCurrentDirectory(),"Content","References",PackName),
                Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                    "Content","References",PackName)
            }.FirstOrDefault(File.Exists);
            if(_packPath is null)return;
            var info=new FileInfo(_packPath);
            if(info.Length is <200 or >MaxArchiveBytes)
                throw new InvalidDataException("Wiki reference archive outside size budget");
            var bytes=File.ReadAllBytes(_packPath);
            var hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            using var stream=new MemoryStream(bytes);
            using var zip=new ZipArchive(stream,ZipArchiveMode.Read,false);
            var manifest=zip.GetEntry("manifest.json") ??
                throw new InvalidDataException("Wiki reference manifest missing");
            if(manifest.Length is <100 or >MaxManifestBytes)
                throw new InvalidDataException("Wiki reference manifest exceeds budget");
            using var manifestStream=manifest.Open();
            using var json=JsonDocument.Parse(manifestStream);
            var root=json.RootElement;
            if(root.GetProperty("format").GetString()!=SourceFormat)
                throw new InvalidDataException("Wiki reference manifest version mismatch");
            var synthetic=root.TryGetProperty("synthetic",out var fake)
                && fake.ValueKind==JsonValueKind.True;
            var smoke=OS.GetCmdlineUserArgs().Contains("--smoke-pass56",
                StringComparer.Ordinal);
            if(synthetic && !smoke)
                throw new InvalidDataException("Synthetic images outside native test");
            if(!synthetic && (hash!=OwnerPackSha256 ||
                root.GetProperty("sourceImageZipSha256").GetString()!=
                    OwnerImagesArchiveSha256 ||
                root.GetProperty("sourceImageDocsZipSha256").GetString()!=
                    OwnerDocsArchiveSha256))
                throw new InvalidDataException("Owner wiki archive checksum mismatch");

            var fileObject=root.GetProperty("files");
            var imageCount=fileObject.EnumerateObject().Count();
            if(imageCount is <1 or >3000)
                throw new InvalidDataException("Reference count outside budget");
            foreach(var entry in fileObject.EnumerateObject())
            {
                if(!SafeName(entry.Name) ||
                   zip.GetEntry(entry.Name) is not { } zipEntry ||
                   zipEntry.Length is <1 or >MaxSingleImageBytes)
                    throw new InvalidDataException("Unlisted/oversized reference image");
                var metadata=entry.Value;
                var sha=metadata.GetProperty("sha256").GetString()??"";
                var width=metadata.GetProperty("width").GetInt32();
                var height=metadata.GetProperty("height").GetInt32();
                var length=metadata.GetProperty("length").GetInt32();
                if(sha.Length!=64 || sha.Any(c=>!Uri.IsHexDigit(c)) ||
                   width is <1 or >MaxImageSide ||
                   height is <1 or >MaxImageSide ||
                   (long)width*height>8_000_000 ||
                   zipEntry.Length!=length)
                    throw new InvalidDataException("Invalid wiki image metadata");
                Files.Add(entry.Name,new FileRecord(sha,length,width,height));
            }

            var sourceWeapons=root.GetProperty("weapons");
            if(sourceWeapons.ValueKind!=JsonValueKind.Object ||
               sourceWeapons.EnumerateObject().Count()>91)
                throw new InvalidDataException("Wrong weapon reference index");
            foreach(var record in sourceWeapons.EnumerateObject())
            {
                if(record.Name.Length is <1 or >120)
                    throw new InvalidDataException("Bad original weapon name");
                var weapon=record.Value;
                var defaultImage=weapon.GetProperty("default");
                var primary=defaultImage.ValueKind==JsonValueKind.Null
                    ? "" : CheckedReference(defaultImage);
                var skins=new Dictionary<string,string>(StringComparer.Ordinal);
                var poses=new Dictionary<string,string>(StringComparer.Ordinal);
                foreach(var row in weapon.GetProperty("skins").EnumerateObject())
                {
                    if(row.Name.Length>100)
                        throw new InvalidDataException("Invalid skin reference key");
                    skins.Add(row.Name,CheckedReference(row.Value));
                }
                foreach(var row in weapon.GetProperty("poses").EnumerateObject())
                {
                    if(row.Name.Length>30)
                        throw new InvalidDataException("Invalid weapon pose label");
                    poses.Add(row.Name,CheckedReference(row.Value));
                }
                Weapons.Add(record.Name,new WeaponReferences(primary,skins,poses));
            }
            foreach(var row in root.GetProperty("skinIcons").EnumerateObject())
            {
                if(row.Name.Length>100)
                    throw new InvalidDataException("Invalid wiki skin icon");
                Icons.Add(row.Name,CheckedReference(row.Value));
            }
            if(!synthetic &&
               (Weapons.Count!=91 || Files.Count!=2033 || Icons.Count!=110 ||
                root.GetProperty("stats").GetProperty("skinVariantReferences").GetInt32()!=1563))
                throw new InvalidDataException("Owner wiki reference catalog counts changed");
            OwnerPackVerified=!synthetic;
            SyntheticFixture=synthetic;
            GD.Print($"TWR_PASS56_WIKI_REFERENCES_READY weapons={Weapons.Count} "+
                $"files={Files.Count} verified={OwnerPackVerified}");
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS56_WIKI_REFERENCES_REJECTED "+ex.Message);
            Files.Clear();
            Weapons.Clear();
            Icons.Clear();
            _packPath=null;
            OwnerPackVerified=false;
            SyntheticFixture=false;
        }
    }

    private static Texture2D? LoadImage(string name)
    {
        if(Cache.TryGetValue(name,out var cached))return cached;
        if(_packPath is null || !Files.TryGetValue(name,out var meta))return null;
        try
        {
            using var zip=ZipFile.OpenRead(_packPath);
            var entry=zip.GetEntry(name);
            if(entry is null || entry.Length!=meta.Bytes ||
               entry.Length>MaxSingleImageBytes)return null;
            using var reader=entry.Open();
            using var memory=new MemoryStream();
            var block=new byte[16*1024];
            int length;
            while((length=reader.Read(block))>0)
            {
                if(memory.Length+length>MaxSingleImageBytes)return null;
                memory.Write(block,0,length);
            }
            var bytes=memory.ToArray();
            var hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if(!hash.Equals(meta.Hash,StringComparison.Ordinal))return null;
            var image=new Image();
            var result=name.EndsWith(".png",StringComparison.OrdinalIgnoreCase)
                ? image.LoadPngFromBuffer(bytes) : image.LoadJpgFromBuffer(bytes);
            if(result!=Error.Ok || image.IsEmpty() ||
               image.GetWidth()!=meta.Width || image.GetHeight()!=meta.Height)
                return null;
            var texture=ImageTexture.CreateFromImage(image);
            if(Cache.Count>=8 && CacheOrder.TryDequeue(out var old))
                Cache.Remove(old);
            Cache.Add(name,texture);
            CacheOrder.Enqueue(name);
            return texture;
        }
        catch(Exception ex)
        {
            GD.PushWarning("TWR_PASS56_WIKI_IMAGE_REJECTED "+ex.Message);
            return null;
        }
    }
}

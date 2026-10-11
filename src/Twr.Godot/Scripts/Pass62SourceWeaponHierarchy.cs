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
/// Pass62. Optional owner-only hierarchy sidecar, SHA-bound to the exact
/// original TestPlace.rbxlx and legacy Pass47 source assemblies. The old
/// source pack remains immutable. Recovers nested Model groups and original
/// nested Handle CFrames, NOT missing Motor6D C0/C1 joints or mesh bytes.
/// </summary>
public static class Pass62SourceWeaponHierarchy
{
    public const string PackName="SourceWeaponMotorHierarchy62.json.gz";
    public const string OwnerPackSha=
        "505678dcbad987d28233753a359d189f520e927f10cfffc6e16f01381406b130";
    private const int MaxPackedBytes=128*1024;
    private const int MaxExpandedBytes=2*1024*1024;
    private static JsonDocument? _document;
    private static bool _initialized;
    public static bool OwnerHierarchyVerified {get;private set;}

    private sealed record Group(Transform3D Pivot,
        (MeshInstance3D Part, Transform3D Home)[] Parts);

    public sealed class Binding
    {
        private readonly Dictionary<string,Group> _groups;
        internal Binding(Dictionary<string,Group> groups)
        {
            _groups=groups;
        }
        public int GroupCount => _groups.Count;
        public bool HasGroup(string name) => _groups.ContainsKey(name);
        public int PartCount(string name) => _groups.TryGetValue(name,
            out var group) ? group.Parts.Length : 0;
        public Vector3 GroupPivot(string name) =>
            _groups.TryGetValue(name,out var group)
                ? group.Pivot.Origin : Vector3.Zero;

        /// <summary>
        /// Translate ALL visible source parts in an authored top-level Model
        /// around that Model's original Handle CFrame. The offset is Godot
        /// LOCAL coordinates from the strict Pass61 Rels.*Rel parser; no
        /// world-space guessed part-name attachment or pose accumulation.
        /// </summary>
        public int ApplyTranslation(string groupName,Vector3 localDelta)
        {
            if(!_groups.TryGetValue(groupName,out var group) ||
               !Finite(localDelta) || localDelta.LengthSquared()>.25f)
                return 0;
            var move=group.Pivot *
                new Transform3D(Basis.Identity,localDelta) *
                group.Pivot.AffineInverse();
            var changed=0;
            foreach(var (part,home) in group.Parts)
            {
                if(!GodotObject.IsInstanceValid(part))continue;
                part.Transform=move*home;
                changed++;
            }
            return changed;
        }
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);

    private static bool Vector(JsonElement arr,int length,out float[] result)
    {
        result=[];
        if(arr.ValueKind!=JsonValueKind.Array || arr.GetArrayLength()!=length)
            return false;
        var vals=new float[length];
        var index=0;
        foreach(var number in arr.EnumerateArray())
        {
            if(number.ValueKind!=JsonValueKind.Number ||
               !number.TryGetSingle(out vals[index]) ||
               !float.IsFinite(vals[index]) || Math.Abs(vals[index])>100f)
                return false;
            index++;
        }
        result=vals;
        return true;
    }

    private static bool Pivot(JsonElement element,Basis alignment,
        out Transform3D pivot)
    {
        pivot=Transform3D.Identity;
        if(!element.TryGetProperty("pivot_t",out var origin) ||
           !element.TryGetProperty("pivot_r",out var matrix) ||
           !Vector(origin,3,out var p) || !Vector(matrix,9,out var r))
            return false;
        var translated=new Vector3(p[0],p[1],-p[2])*
            RobloxUnits.MetersPerStud;
        var basis=new Basis(
            new Vector3(r[0],r[3],-r[6]),
            new Vector3(r[1],r[4],-r[7]),
            new Vector3(-r[2],-r[5],r[8]));
        var det=basis.Determinant();
        if(!Finite(translated) || translated.LengthSquared()>256f ||
           !float.IsFinite(det) || Math.Abs(det-1f)>.08f)
            return false;
        pivot=new Transform3D(alignment*basis.Orthonormalized(),
            alignment*translated);
        return true;
    }

    /// <summary>
    /// Bind only when the same verified source-geometry pack is currently
    /// installed. A smoke-only synthetic pair cannot be used in normal play.
    /// Source part indices exactly match Pass47 postorder, including invisible
    /// Handle helpers: skipped visuals never shift any index.
    /// </summary>
    public static Binding? Bind(string weapon,Node3D parent,
        int sourcePartCount,IReadOnlyDictionary<int,MeshInstance3D> parts,
        Basis alignment,bool assemblyIsOwnerVerified)
    {
        if(!Load() || _document is null)return null;
        var root=_document.RootElement;
        var synthetic=root.GetProperty("synthetic").GetBoolean();
        if(assemblyIsOwnerVerified==synthetic)return null;
        if(!root.GetProperty("models").TryGetProperty(weapon,out var entry) ||
           !entry.TryGetProperty("part_count",out var count) ||
           count.GetInt32()!=sourcePartCount || sourcePartCount>200)
            return null;
        var groups=new Dictionary<string,Group>(StringComparer.Ordinal);
        try
        {
            foreach(var g in entry.GetProperty("groups").EnumerateArray())
            {
                var path=g.GetProperty("path");
                // Pass62 animates top-level subassemblies; deeper nested
                // groups are recovered in the owner pack for later passes.
                if(path.GetArrayLength()!=1)continue;
                var name=path[0].GetString()??"";
                if(name.Length is <1 or >80 || groups.ContainsKey(name))
                    continue;
                if(!Pivot(g,alignment,out var pivot))continue;
                var groupParts=new List<(MeshInstance3D,Transform3D)>();
                var used=new HashSet<int>();
                var indices=g.GetProperty("part_indices");
                if(indices.GetArrayLength()>200)continue;
                foreach(var item in indices.EnumerateArray())
                {
                    if(!item.TryGetInt32(out var id) ||
                       id<0 || id>=sourcePartCount || !used.Add(id))
                        throw new InvalidDataException(
                            "Invalid duplicate or out-of-bounds source motor index");
                    if(parts.TryGetValue(id,out var mesh) &&
                       GodotObject.IsInstanceValid(mesh))
                        groupParts.Add((mesh,mesh.Transform));
                }
                if(groupParts.Count>0)
                    groups[name]=new Group(pivot,groupParts.ToArray());
            }
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS62_BIND_REJECTED "+e.Message);
            return null;
        }
        if(groups.Count==0)return null;
        GD.Print($"TWR_PASS62_HIERARCHY_BOUND weapon={weapon} "+
            $"groups={groups.Count} owner_verified={OwnerHierarchyVerified}");
        return new Binding(groups);
    }

    private static bool Load()
    {
        if(_initialized)return _document is not null;
        _initialized=true;
        var folder=Path.GetDirectoryName(OS.GetExecutablePath()) ??
            Directory.GetCurrentDirectory();
        var candidates=new[]{
            Path.Combine(folder,"Content","Weapons",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"Content",
                "Weapons",PackName),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                "Content","Weapons",PackName)};
        var source=candidates.FirstOrDefault(File.Exists);
        if(source is null)return false;
        try
        {
            var bytes=File.ReadAllBytes(source);
            if(bytes.Length is <20 or >MaxPackedBytes)
                throw new InvalidDataException("Oversized motor hierarchy sidecar");
            var checksum=Convert.ToHexString(
                SHA256.HashData(bytes)).ToLowerInvariant();
            using var input=new MemoryStream(bytes);
            using var gzip=new GZipStream(input,CompressionMode.Decompress);
            using var expanded=new MemoryStream();
            var buffer=new byte[8192];
            int n;
            while((n=gzip.Read(buffer,0,buffer.Length))>0)
            {
                if(expanded.Length+n>MaxExpandedBytes)
                    throw new InvalidDataException(
                        "Motor hierarchy data exceeded expanded safety budget");
                expanded.Write(buffer,0,n);
            }
            _document=JsonDocument.Parse(expanded.ToArray(),
                new JsonDocumentOptions { MaxDepth=32 });
            var root=_document.RootElement;
            if(root.GetProperty("format").GetString() !=
                "twr-source-weapon-hierarchy62-v1")
                throw new InvalidDataException("Unknown hierarchy format");
            var synthetic=root.GetProperty("synthetic").GetBoolean();
            var testMode=OS.GetCmdlineUserArgs().Contains(
                "--smoke-pass62",StringComparer.Ordinal);
            if(synthetic && !testMode)
                throw new InvalidDataException(
                    "Synthetic motor hierarchy outside native smoke");
            if(!synthetic &&
               (checksum!=OwnerPackSha ||
                root.GetProperty("original_rbxlx_sha256").GetString() !=
                    OriginalWeaponSourceRuntime.OriginalSourceSha ||
                root.GetProperty("source_assembly_sha256").GetString() !=
                    OriginalWeaponSourceRuntime.OriginalPackSha ||
                root.GetProperty("source_model_count").GetInt32()!=98 ||
                root.GetProperty("original_part_count").GetInt32()!=1484))
                throw new InvalidDataException(
                    "Original source hierarchy SHA mismatch");
            var models=root.GetProperty("models");
            var entries=models.EnumerateObject().Count();
            if(entries>120 || entries<1 ||
               (!synthetic && entries!=78))
                throw new InvalidDataException(
                    "Source hierarchy model count outside evidence bounds");
            OwnerHierarchyVerified=!synthetic;
            GD.Print($"TWR_PASS62_SOURCE_HIERARCHY_LOADED "+
                $"models={entries} original_pack_verified={OwnerHierarchyVerified}");
            return true;
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS62_HIERARCHY_FALLBACK "+e.Message);
            _document?.Dispose();
            _document=null;
            OwnerHierarchyVerified=false;
            return false;
        }
    }
}

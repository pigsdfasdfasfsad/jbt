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

    internal sealed record Group(Transform3D Pivot,
        (MeshInstance3D Part, Transform3D Home)[] Parts);

    public sealed class Binding
    {
        private readonly Dictionary<string,Group> _groups;
        private readonly Basis _alignment;
        internal Binding(Dictionary<string,Group> groups,Basis alignment)
        {
            _groups=groups;
            _alignment=alignment;
        }
        public int GroupCount => _groups.Count;
        public bool HasGroup(string name) => _groups.ContainsKey(name);
        public int PartCount(string name) => _groups.TryGetValue(name,
            out var group) ? group.Parts.Length : 0;
        public Vector3 GroupPivot(string name) =>
            _groups.TryGetValue(name,out var group)
                ? group.Pivot.Origin : Vector3.Zero;

        /// <summary>
        /// Expose original, immutable group Handle reference pose to the
        /// bounded Pass63 magazine timeline. This is source group data, not
        /// a recovered original Motor6D C0/C1 joint.
        /// </summary>
        public bool TryGetGroupRestPose(string name,out Transform3D rest)
        {
            if(_groups.TryGetValue(name,out var group))
            {
                rest=group.Pivot;return true;
            }
            rest=Transform3D.Identity;
            return false;
        }

        /// <summary>
        /// Resolve animated, currently visible group Handle pose from the
        /// source-index matched mesh's original transform. The companion
        /// first-person hand can follow moving Mag, Pump and Bolt groups
        /// instead of remaining glued to a static world position.
        /// </summary>
        public bool TryGetCurrentGroupPose(string name,out Transform3D pose)
        {
            pose=Transform3D.Identity;
            if(!_groups.TryGetValue(name,out var group))return false;
            foreach(var (part,home) in group.Parts)
            {
                if(!GodotObject.IsInstanceValid(part))continue;
                var current=part.Transform*home.AffineInverse()*group.Pivot;
                var determinant=current.Basis.Determinant();
                if(!Finite(current.Origin) ||
                   !float.IsFinite(determinant) ||
                   Math.Abs(determinant-1f)>.08f)
                    continue;
                pose=new Transform3D(current.Basis.Orthonormalized(),
                    current.Origin);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Convert a safe numeric source CFrame: a MagRel-relative target
        /// starts from the verified source Handle; an absolute target uses
        /// the original weapon Handle coordinate space. Both were already
        /// reflected to Godot coordinates by the Pass63 parser.
        /// </summary>
        public Transform3D ResolveSourcePose(string name,Transform3D pose,
            bool relativeToHandle)
        {
            if(!_groups.TryGetValue(name,out var group))
                return Transform3D.Identity;
            return relativeToHandle
                ? group.Pivot*pose
                : new Transform3D(_alignment*pose.Basis,
                    _alignment*pose.Origin);
        }

        public int ApplyPose(string name,Transform3D target)
        {
            if(!_groups.TryGetValue(name,out var group) ||
               !Finite(target.Origin) ||
               target.Origin.DistanceSquaredTo(group.Pivot.Origin)>4f)
                return 0;
            var determinant=target.Basis.Determinant();
            if(!float.IsFinite(determinant) ||
               Math.Abs(determinant-1f)>.08f)
                return 0;
            var motion=target*group.Pivot.AffineInverse();
            var count=0;
            foreach(var (part,home) in group.Parts)
            {
                if(!GodotObject.IsInstanceValid(part))continue;
                part.Transform=motion*home;
                count++;
            }
            return count;
        }

        public int ResetGroup(string name)
        {
            return TryGetGroupRestPose(name,out var home)
                ? ApplyPose(name,home) : 0;
        }

        /// <summary>
        /// Pass64: compose source Bolt translation with nested Bolt Handle
        /// rotation against IMMUTABLE original part transforms. The outer
        /// movement is applied AFTER the inner rotation, so the handle
        /// follows its carrier, instead of snapping back to its old pivot.
        /// This is anchored on recovered Model Handles, not a claim that
        /// original Motor6D C0/C1 joint offsets were found.
        /// </summary>
        public int ApplyBoltAction(Vector3 delta,float handleRadians)
        {
            if(!_groups.TryGetValue("Bolt",out var carrier) ||
               !Finite(delta) || delta.LengthSquared()>.25f ||
               !float.IsFinite(handleRadians) ||
               Math.Abs(handleRadians)>2.0f)
                return 0;
            var carrierDelta=carrier.Pivot*
                new Transform3D(Basis.Identity,delta)*
                carrier.Pivot.AffineInverse();
            var nested=_groups.TryGetValue("Bolt/Bolt Handle",out var grip)
                ? grip : null;
            var localRotation=new Transform3D(
                new Basis(Vector3.Back,handleRadians),Vector3.Zero);
            var twist=nested is null ? Transform3D.Identity :
                nested.Pivot*localRotation*nested.Pivot.AffineInverse();
            int changed=0;
            foreach(var (part,home) in carrier.Parts)
            {
                if(!GodotObject.IsInstanceValid(part))continue;
                var nestedPart=nested is not null && Array.Exists(
                    nested.Parts,item=>ReferenceEquals(item.Part,part));
                part.Transform=carrierDelta*
                    (nestedPart ? twist : Transform3D.Identity)*home;
                changed++;
            }
            return changed;
        }

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
                // Pass64 can bind the original nested Bolt Handle while
                // retaining exact source indices and the original
                // top-level Slide/Pump/Mag group behavior.
                if(path.GetArrayLength() is <1 or >2)continue;
                var name=string.Join("/",path.EnumerateArray()
                    .Select(item=>item.GetString()??""));
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
        return new Binding(groups,alignment);
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
            var testMode=OS.GetCmdlineUserArgs().Any(arg =>
                arg is "--smoke-pass62" or "--smoke-pass63" or "--smoke-pass64" or "--smoke-pass65" or "--smoke-pass66" or "--smoke-pass67");
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

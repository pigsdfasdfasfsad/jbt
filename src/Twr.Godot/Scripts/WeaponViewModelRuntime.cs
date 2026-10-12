using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// First-person weapon renderer. Owner-authorized prepared scenes are loaded
/// from packaged local resources; otherwise an explicitly approximate,
/// weapon-category-specific multi-part model is displayed.
/// No Roblox network access is permitted at runtime.
/// </summary>
public partial class WeaponViewModelRuntime : Node3D
{
    public bool UsingPreparedScene { get; private set; }
    public string ActiveCosmeticSkinId { get; private set; } = "";
    public int CosmeticProxyTintParts { get; private set; }
    public int SourceSkinMappedParts { get; private set; }
    public int SourceSkinColorParts { get; private set; }
    public int SourceSkinMissingTextureParts { get; private set; }
    public int SourceTextureReferencedParts { get; private set; }
    public int SourceInstalledTextureParts { get; private set; }
    public int SourceProceduralTextureParts { get; private set; }
    public int SourceUnrenderedTextureLayers { get; private set; }
    public int AnimatedSkinMaterialCount => _animatedSkinMaterials.Count;
    public float CosmeticEffectElapsed { get; private set; }
    public float LastShimmerMultiplier { get; private set; } = 1f;
    public bool StaticShowroomMode { get; set; }
    public string CosmeticSourceStatus { get; private set; } = "";
    public bool UsingOriginalToolAssembly { get; private set; }
    public int SourceVisibleParts { get; private set; }
    public int SourceMissingMeshProxies { get; private set; }
    public int SourceLocallyResolvedMeshes { get; private set; }
    public int SourceCircularProxyParts { get; private set; }
    public int SourceConservativeProxyParts { get; private set; }
    public int SourceNativePrimitiveParts { get; private set; }
    public int SourceDetailProfileProxies { get; private set; }
    public int SourceLensProxies { get; private set; }
    public int SourceReloadFrames => _poseMotion?.Reload?.Frames.Length ?? 0;
    public bool SourceSprintRotationAvailable => _poseMotion?.HasSprint == true;
    public bool HasSourceEquipClip => _handling?.Equip is not null;
    public int SourceCyclingSlideFrames =>
        _handling?.CyclingSlide?.Frames.Length ?? 0;
    public int SourceCyclingPumpFrames =>
        _handling?.CyclingPump?.Frames.Length ?? 0;
    public int SourceReloadSlideFrames =>
        _handling?.ReloadSlide?.Frames.Length ?? 0;
    public int SourceReloadPumpFrames =>
        _handling?.ReloadPump?.Frames.Length ?? 0;
    public Vector3 LastSlideMotorOffset { get; private set; }
    public Vector3 LastPumpMotorOffset { get; private set; }
    public int SourceMotorHierarchyGroups => _sourceHierarchy?.GroupCount ?? 0;
    public int SourceSlideGroupParts =>
        _sourceHierarchy?.PartCount("Slide") ?? 0;
    public int SourcePumpGroupParts =>
        _sourceHierarchy?.PartCount("Pump") ?? 0;
    public bool SourceHierarchyOriginalVerified =>
        SourceMotorHierarchyGroups>0 &&
        Pass62SourceWeaponHierarchy.OwnerHierarchyVerified;
    public int SourceMagazineCueCount => _magazineReload?.Cues.Length ?? 0;
    public int SourceMagazineGroupParts =>
        _sourceHierarchy?.PartCount("Mag") ?? 0;
    public bool SourceMagazinePoseActive {get;private set;}
    public float SourceMagazineHandleTravel {get;private set;}
    public int SourceBoltCarrierCueCount =>
        _action?.Cycling.Bolt.Length ?? 0;
    public int SourceBoltHandleCueCount =>
        _action?.Cycling.BoltHandle.Length ?? 0;
    public int SourceBoltCarrierGroupParts =>
        _sourceHierarchy?.PartCount("Bolt") ?? 0;
    public int SourceNestedBoltHandleParts =>
        _sourceHierarchy?.PartCount("Bolt/Bolt Handle") ?? 0;
    public float LastSourceBoltTravel {get;private set;}
    public float LastSourceBoltHandleAngle {get;private set;}
    public int SourceShellEjectionCueCount =>
        _action?.Cycling.Ejections.Length ?? 0;
    public int EjectedShellVisualCount {get;private set;}
    public int ActiveEjectedShellVisualCount => _activeShells.Count;
    public string LastEjectedShellSourceName {get;private set;} = "";
    public bool SourceIncrementalShellInsertPoseActive {get;private set;}
    public bool SourceHandProxiesAvailable => _leftHandProxy is not null &&
        _rightHandProxy is not null;
    public int SourceLeftReloadWeldCues =>
        _handSource?.Reload?.Left.Cues.Length ?? 0;
    public int SourceRightReloadWeldCues =>
        _handSource?.Reload?.Right.Cues.Length ?? 0;
    public int SourceRightCyclingWeldCues =>
        _handSource?.Cycling?.Right.Cues.Length ?? 0;
    public Vector3 SourceLeftHandPosition =>
        _leftHandProxy?.Position ?? Vector3.Zero;
    public Vector3 SourceRightHandPosition =>
        _rightHandProxy?.Position ?? Vector3.Zero;
    public int SourceHandProxyMeshCount {get;private set;}
    public bool SourceHandAnimationActive {get;private set;}
    public bool SourceAimPartAvailable => _sourceSight67.HasValue;
    public bool SourceAimGeometryOwnerVerified =>
        _sourceSight67.HasValue &&
        OriginalWeaponSourceRuntime.OwnerSourcePackVerified;
    public Vector3 SourceAimPartLocalPosition =>
        _sourceSight67?.Origin ?? Vector3.Zero;
    public float SourceAimReticleLateralError =>
        _sourceSight67 is { } sight
            ? Pass67SourceAimAlignment.ReticleError(Transform,sight)
            : -1f;
    private Transform3D? _sourceSight67;
    private Pass68OriginalAimFormulas.Formula? _originalAimFormula68;
    private Transform3D? _originalAimPart68;
    private Transform3D? _originalHandle68;
    public bool SourceExactAimCFAvailable68 =>
        _originalAimFormula68 is not null &&
        _originalAimPart68.HasValue &&
        _originalHandle68.HasValue;
    public string SourceOriginalAimFamily68 =>
        _originalAimFormula68?.Kind.ToString() ?? "";
    public bool TryGetOriginalAimCF68(out Transform3D originalAimCf)
    {
        originalAimCf=Transform3D.Identity;
        return SourceExactAimCFAvailable68 &&
            Pass68OriginalAimFormulas.TryCompute(
                _originalAimFormula68,
                _originalHandle68!.Value,
                _originalAimPart68!.Value,out originalAimCf);
    }

    public bool TryGetSourceAimRoot(out Transform3D target)
    {
        target=Transform3D.Identity;
        if(_sourceSight67 is not { } sight)return false;
        // Prefer actual original, finite AimCF family algebra while
        // preserving the Pass67 original-marker camera-centering
        // calibration in this different Godot avatar/camera rig.
        if(_originalAimFormula68 is not null &&
           _originalAimPart68 is { } sourceAim &&
           _originalHandle68 is { } sourceHandle &&
           Pass68OriginalAimFormulas.TryVisualRoot(
                _originalAimFormula68,sourceHandle,sourceAim,
                sight,out target,out _))
            return true;
        return Pass67SourceAimAlignment.TryAimRoot(sight,out target);
    }

    public bool SourceArmRigAvailable => _armRig66 is not null &&
        _leftUpperArm66 is not null && _rightUpperArm66 is not null;
    public int SourceArmProxyMeshCount {get;private set;}
    public Vector3 SourceLeftElbowPosition =>
        _leftElbow66?.Position ?? Vector3.Zero;
    public Vector3 SourceRightElbowPosition =>
        _rightElbow66?.Position ?? Vector3.Zero;
    public Vector3 SourceLeftShoulderPosition =>
        Pass66ArmKinematics.Shoulder(true);
    public Vector3 SourceRightShoulderPosition =>
        Pass66ArmKinematics.Shoulder(false);
    public bool LeftArmReachClamped {get;private set;}
    public bool RightArmReachClamped {get;private set;}
    public float LeftArmReachScale {get;private set;}=1f;
    public float RightArmReachScale {get;private set;}=1f;
    public int VisualPartCount => _rig is null ? 0 :
        Math.Max(0, _rig.GetChildCount() - (_flash is null ? 0 : 1));

    private Node3D? _rig;
    private MeshInstance3D? _flash;
    private bool _melee;
    private float _flashSeconds;
    private float _mechanicalKick;
    private Pass59WeaponMotion.Profile? _sourceMotion;
    private Pass59WeaponMotion.Shot _sourceShot;
    private float _shotRemaining;
    private bool _shotActive;
    private Pass60WeaponAnimationLibrary.Motion? _poseMotion;
    private Pass60WeaponAnimationLibrary.Shot? _poseFire;
    private Pass61SourceWeaponHandling.Profile? _handling;
    private Pass62SourceWeaponHierarchy.Binding? _sourceHierarchy;
    private Pass63SourceMagazineReload.Clip? _magazineReload;
    private Pass64SourceAction.Profile? _action;
    private Pass65SourceHands.Profile? _handSource;
    private Node3D? _leftHandProxy;
    private Node3D? _rightHandProxy;
    // Camera-local torso anchors are separate from the recoiling gun
    // WeaponBody. Original Roblox first-person upper arm meshes and
    // Motor6D shoulder/elbow joints are not present in owner sources.
    private Node3D? _armRig66;
    private MeshInstance3D? _leftUpperArm66;
    private MeshInstance3D? _rightUpperArm66;
    private MeshInstance3D? _leftForeArm66;
    private MeshInstance3D? _rightForeArm66;
    private MeshInstance3D? _leftElbow66;
    private MeshInstance3D? _rightElbow66;
    private int _nextShellEjection;
    private sealed class SpentShellVisual
    {
        public MeshInstance3D Part {get;init;} = null!;
        public Vector3 Velocity;
        public float Remaining;
    }
    private readonly List<SpentShellVisual> _activeShells = [];
    private float _equipDuration;
    private float _equipRemaining;
    private float _cycleDuration;
    private float _cycleElapsed;
    private readonly List<(MeshInstance3D Part, Vector3 Home)> _slides = [];
    private readonly List<(MeshInstance3D Part, Vector3 Home)> _magazines = [];
    private readonly List<(MeshInstance3D Part, Vector3 Home)> _pumps = [];
    private float _phase;
    private float _kick;
    private float _reloadRemaining;
    private float _reloadDuration;
    private readonly List<(StandardMaterial3D Material,float Emission,Vector3 Uv,bool Scroll)>
        _animatedSkinMaterials = [];

    /// <summary>
    /// Pass54: source material Values 1..5, applied ONLY to the authored
    /// Value-slot-tagged source parts when their private mapping is present.
    /// For approximate geometry the five slot placements remain explicitly
    /// approximate. Absent private palettes retain the Pass53 hash tint.
    /// All code here affects rendering only, never weapon gameplay authority.
    /// </summary>
    public void ApplyCosmeticOverlay(string? skinId)
    {
        ActiveCosmeticSkinId="";
        CosmeticProxyTintParts=0;
        SourceSkinMappedParts=0;
        SourceSkinColorParts=0;
        SourceSkinMissingTextureParts=0;
        SourceTextureReferencedParts=0;
        SourceInstalledTextureParts=0;
        SourceProceduralTextureParts=0;
        SourceUnrenderedTextureLayers=0;
        _animatedSkinMaterials.Clear();
        CosmeticEffectElapsed=0f;
        LastShimmerMultiplier=1f;
        CosmeticSourceStatus="";
        if(_rig is null)return;
        var meshes=_rig.FindChildren("*","MeshInstance3D",true,false)
            .OfType<MeshInstance3D>()
            .Where(mesh=>mesh.Name.ToString()!="MuzzleFlash").ToArray();
        foreach(var mesh in meshes) mesh.MaterialOverlay=null;
        var skin=SkinCaseCatalog.FindSkin(skinId);
        if(skin is null)return;
        var hasSource=Pass54SourceSkinMaterials.TryGet(skin.Id,out var palette);
        if(!hasSource)
        {
            var tint=BuildCosmeticProxyTint(skin);
            foreach(var part in meshes)
            {
                part.MaterialOverlay=tint;
                CosmeticProxyTintParts++;
            }
            CosmeticSourceStatus=Pass54SourceSkinMaterials.Provenance(false);
            ActiveCosmeticSkinId=skin.Id;
            return;
        }

        var surfaces=palette.Select(BuildSourceMaterial).ToArray();
        var overlays=new Pass55SkinTextureRenderer.Applied[5];
        for(var i=0;i<surfaces.Length;i++)
        {
            var surface=surfaces[i];
            var animated=surface.EmissionEnabled;
            if(Pass55SourceSkinLayers.TryGetSlot(skin.Id,i+1,out var slot))
                overlays[i]=Pass55SkinTextureRenderer.Apply(
                    surface,slot,skin.Id,i+1,animated);
            else
                overlays[i]=new Pass55SkinTextureRenderer.Applied(
                    false,false,false,0,Vector3.Zero,false);
            if(animated)
            {
                _animatedSkinMaterials.Add((surface,surface.EmissionEnergyMultiplier,
                    overlays[i].OriginalUvOffset,overlays[i].HasReference));
            }
        }
        var approximate=0;
        foreach(var part in meshes)
        {
            var sourceSlot=0;
            var tagged=part.HasMeta("TwrSkinSlot54") &&
                int.TryParse(part.GetMeta("TwrSkinSlot54").ToString(),out sourceSlot)
                && sourceSlot is >=1 and <=5;
            if(!tagged)
            {
                // Untagged source parts are NOT skinned in the original
                // author model: preserve their original colors/materials.
                if(UsingOriginalToolAssembly &&
                    Pass54SourceSkinMaterials.LoadedTaggedParts>0)
                    continue;
                // Prepared/approximate geometry has no original BoolValue
                // children. Stable slot order is a disclosed reconstruction.
                sourceSlot=(approximate++ % 5)+1;
            }
            part.MaterialOverlay=surfaces[sourceSlot-1];
            CosmeticProxyTintParts++;
            SourceSkinColorParts++;
            if(tagged) SourceSkinMappedParts++;
            var slot=palette[sourceSlot-1];
            var layer=overlays[sourceSlot-1];
            if(layer.HasReference) SourceTextureReferencedParts++;
            if(layer.InstalledTexture) SourceInstalledTextureParts++;
            if(layer.GeneratedFallback) SourceProceduralTextureParts++;
            SourceUnrenderedTextureLayers+=layer.UnrenderedLayers;
            if((slot.MissingTexture && !layer.InstalledTexture) ||
                slot.SurfaceAppearance.Length>0 ||
                slot.MaterialVariant.Length>0)
                SourceSkinMissingTextureParts++;
        }
        CosmeticSourceStatus=Pass54SourceSkinMaterials.Provenance(
            SourceSkinMappedParts>0);
        if(SourceTextureReferencedParts>0)
        {
            CosmeticSourceStatus+=" | "+
                (SourceInstalledTextureParts>0 ? "LOCAL SOURCE TEXTURE" :
                    "PROCEDURAL TEXTURE PROXY - ORIGINAL IMAGE UNAVAILABLE");
            CosmeticSourceStatus+=" | UV MAPPING APPROXIMATE";
        }
        if(_animatedSkinMaterials.Count>0)
            CosmeticSourceStatus+=" | RECONSTRUCTED SHIMMER (TIMING UNKNOWN)";
        ActiveCosmeticSkinId=skin.Id;
    }

    private static StandardMaterial3D BuildSourceMaterial(
        Pass54SourceSkinMaterials.SourceMaterial source)
    {
        var kind=source.RobloxMaterial;
        var metallic=kind is "Metal" or "CorrodedMetal" or "Foil";
        var glass=kind=="Glass";
        var forceField=kind=="ForceField";
        var neon=kind=="Neon";
        var alpha=source.Color.A;
        // Godot's glass/ForceField shading is an optical approximation;
        // the RGB and explicit Transparency/Reflectance come from the owner
        // Luau source, not our previous skin-name hash.
        if(glass && source.Transparency<=0f)alpha*=.68f;
        if(forceField && source.Transparency<=0f)alpha*=.48f;
        var color=new Color(source.Color.R,source.Color.G,source.Color.B,alpha);
        return new StandardMaterial3D {
            AlbedoColor=color,
            Transparency=alpha<.995f
                ? BaseMaterial3D.TransparencyEnum.Alpha
                : BaseMaterial3D.TransparencyEnum.Disabled,
            Metallic=metallic ? Math.Max(.68f,source.Reflectance) : .015f,
            Roughness=glass ? .13f : metallic ? .36f : neon ? .35f :
                kind=="Marble" ? .55f : .76f,
            EmissionEnabled=neon || forceField,
            Emission=color,
            EmissionEnergyMultiplier=neon ? 1.45f : forceField ? .38f : 0f
        };
    }

    private static StandardMaterial3D BuildCosmeticProxyTint(SkinCaseCatalog.Skin skin)
    {
        // Pass53 fallback remains intentionally approximate ONLY when the
        // owner has not installed SourceSkinMaterials54.json.gz.
        uint hash=2166136261;
        foreach(var ch in skin.Id)
            hash=unchecked((hash ^ ch)*16777619);
        var hue=(hash%360)/360f;
        var saturation=skin.CaseName=="Neon" ? .82f : .56f;
        var rgb=Color.FromHsv(hue,saturation,.86f,.34f);
        return new StandardMaterial3D {
            AlbedoColor=rgb,
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness=.58f
        };
    }

    /// <summary>
    /// Deterministic, frame-rate-independent cosmetic-only animation.
    /// The source defines material names but no original animation speed.
    /// A static showroom may animate only these materials, not gun recoil.
    /// </summary>
    public void AdvanceCosmeticEffects(float dt)
    {
        if(!float.IsFinite(dt)||dt<=0f || _animatedSkinMaterials.Count==0)return;
        CosmeticEffectElapsed=(CosmeticEffectElapsed+
            Math.Clamp(dt,0f,.25f)) % 3600f;
        LastShimmerMultiplier=Pass55SkinTextureRenderer.ShimmerMultiplier(
            CosmeticEffectElapsed);
        foreach(var item in _animatedSkinMaterials)
        {
            if(!GodotObject.IsInstanceValid(item.Material))continue;
            item.Material.EmissionEnergyMultiplier=
                item.Emission*LastShimmerMultiplier;
            if(item.Scroll)
                item.Material.Uv1Offset=Pass55SkinTextureRenderer.ScrollOffset(
                    item.Uv,CosmeticEffectElapsed);
        }
    }

    public void SetWeapon(RuntimeWeaponDefinition spec)
    {
        ResetRig();
        if (Pass59WeaponMotion.TryGet(spec.Name,out var profile))
            _sourceMotion=profile;
        if (Pass60WeaponAnimationLibrary.TryGet(spec.Name,out var motion))
            _poseMotion=motion;
        if (Pass61SourceWeaponHandling.TryGet(spec.Name,out var handling))
            _handling=handling;
        if (Pass63SourceMagazineReload.TryGet(spec.Name,out var magClip))
            _magazineReload=magClip;
        if (Pass64SourceAction.TryGet(spec.Name,out var action))
            _action=action;
        if(Pass65SourceHands.TryGet(spec.Name,out var hands))
            _handSource=hands;
        _melee = spec.IsMelee;
        _flash!.Position = new Vector3(0,.025f,
            spec.Slot == "Secondary" ? -.46f : -1.02f);
        var fileName = new string(spec.Name.Select(ch =>
            char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray());
        var source = $"res://Content/Assets/Weapons/{fileName}.tscn";
        if (ResourceLoader.Exists(source))
        {
            var scene = ResourceLoader.Load<PackedScene>(source);
            if (scene is not null)
            {
                var instance = scene.Instantiate();
                if (instance is Node3D model)
                {
                    model.Name = "PreparedOriginalWeapon";
                    _rig!.AddChild(model);
                    UsingPreparedScene = true;
                    BuildSourceHandProxies();
                    return;
                }
                instance.Free();
            }
        }
        // Preserve the original game's weapon model part transforms, sizes,
        // and colors when the private owner-derived tool pack is installed.
        if (OriginalWeaponSourceRuntime.TryBuild(_rig!, spec.Name))
        {
            UsingOriginalToolAssembly = true;
            SourceVisibleParts = OriginalWeaponSourceRuntime.LastVisibleSourceParts;
            SourceMissingMeshProxies = OriginalWeaponSourceRuntime.LastUnavailableMeshProxies;
            SourceLocallyResolvedMeshes = OriginalWeaponSourceRuntime.LastResolvedMeshParts;
            SourceCircularProxyParts = OriginalWeaponSourceRuntime.LastCircularProxyParts;
            SourceConservativeProxyParts =
                OriginalWeaponSourceRuntime.LastConservativeProxyParts;
            SourceNativePrimitiveParts = OriginalWeaponSourceRuntime.LastNativePrimitiveParts;
            SourceDetailProfileProxies = OriginalWeaponSourceRuntime.LastDetailProfileProxies;
            SourceLensProxies = OriginalWeaponSourceRuntime.LastLensProxies;
            _sourceHierarchy=OriginalWeaponSourceRuntime.LastSubassemblyBinding;
            if(OriginalWeaponSourceRuntime.TryGetSourceSightLocal(
                spec.Name,out var aimPart67))
                _sourceSight67=aimPart67;
            if(_sourceSight67.HasValue &&
               Pass68OriginalAimFormulas.TryGet(spec.Name,
                    out var family68) &&
               OriginalWeaponSourceRuntime.TryGetSourceFormulaFrames(
                    spec.Name,out var sourceAim68,out var sourceHandle68))
            {
                _originalAimFormula68=family68;
                _originalAimPart68=sourceAim68;
                _originalHandle68=sourceHandle68;
            }
            _flash.Position = OriginalWeaponSourceRuntime.EstimatedMuzzle(spec.Name)
                ?? _flash.Position;
            TrackMechanicalParts();
            BuildSourceHandProxies();
            return;
        }
        BuildApproximateWeapon(spec);
        TrackMechanicalParts();
        BuildSourceHandProxies();
    }

    public void SetThrowable(string type)
    {
        ResetRig();
        if (OriginalWeaponSourceRuntime.TryBuild(_rig!, type))
        {
            UsingOriginalToolAssembly = true;
            SourceVisibleParts = OriginalWeaponSourceRuntime.LastVisibleSourceParts;
            SourceMissingMeshProxies = OriginalWeaponSourceRuntime.LastUnavailableMeshProxies;
            SourceLocallyResolvedMeshes = OriginalWeaponSourceRuntime.LastResolvedMeshParts;
            SourceCircularProxyParts = OriginalWeaponSourceRuntime.LastCircularProxyParts;
            SourceConservativeProxyParts =
                OriginalWeaponSourceRuntime.LastConservativeProxyParts;
            SourceNativePrimitiveParts = OriginalWeaponSourceRuntime.LastNativePrimitiveParts;
            SourceDetailProfileProxies = OriginalWeaponSourceRuntime.LastDetailProfileProxies;
            SourceLensProxies = OriginalWeaponSourceRuntime.LastLensProxies;
            return;
        }
        var housing = Material(new Color(0.27f, 0.29f, 0.23f));
        var metal = Material(new Color(0.42f, 0.45f, 0.47f), true);
        if (type == "Molotov")
        {
            AddCylinder("Bottle", new Vector3(0, 0, 0),
                0.085f, 0.28f, Material(new Color(0.36f, 0.31f, 0.20f)));
            AddCylinder("Neck", new Vector3(0, 0.19f, 0),
                0.031f, 0.13f, metal);
            AddBox("Rag", new Vector3(0, 0.29f, 0),
                new Vector3(0.045f, 0.15f, 0.04f),
                Material(new Color(0.75f, 0.66f, 0.46f)));
        }
        else
        {
            AddCylinder("Canister", Vector3.Zero, 0.12f, 0.24f,
                type == "Nerve Gas" ? Material(new Color(0.20f,0.34f,0.20f)) : housing);
            AddBox("Lever", new Vector3(0, 0.15f, 0),
                new Vector3(0.055f, 0.08f, 0.18f), metal);
            AddCylinder("Pin", new Vector3(0.085f, 0.17f, 0),
                0.03f, 0.04f, metal);
        }
    }

    public void BeginEquip(double seconds)
    {
        _equipDuration=float.IsFinite((float)seconds)
            ? (float)Math.Clamp(seconds,.01,30.0) : .1f;
        _equipRemaining=_handling?.Equip is not null ||
            _handSource?.Equip is not null ? _equipDuration : 0f;
    }

    public void Fire(bool aiming = false)
    {
        _cycleElapsed=0f;
        _cycleDuration=Math.Max(
            Math.Max(_handling?.CyclingSlide?.Duration ?? 0f,
                _handling?.CyclingPump?.Duration ?? 0f),
            Math.Max(_action?.Cycling.Duration ?? 0f,
                _handSource?.Cycling?.Duration ?? 0f));
        _nextShellEjection=0;
        _shotActive=false;
        _shotRemaining=0f;
        _poseFire=_poseMotion is { } poseMotion
            ? (aiming ? poseMotion.AimFire ?? poseMotion.Fire : poseMotion.Fire)
            : null;
        if(_sourceMotion is { } motion &&
           (aiming ? motion.HasAim || motion.HasHip : motion.HasHip))
        {
            _sourceShot=aiming && motion.HasAim ? motion.Aim : motion.Hip;
            _shotRemaining=_sourceShot.Out+_sourceShot.Back;
            _shotActive=true;
            _kick=0f;
        }
        else if(_poseFire is { } shot)
        {
            _sourceShot=new Pass59WeaponMotion.Shot(
                Math.Max(0f,shot.Target.Origin.Z),0f,shot.Out,shot.Back);
            _shotRemaining=shot.Out+shot.Back;
            _shotActive=true;
            _kick=0f;
        }
        else _kick = 0.16f;
        if (!_melee && _flash is not null)
        {
            _flashSeconds = .055f;
            _mechanicalKick = .11f;
            _flash.Visible = true;
        }
    }

    public void Reload(double seconds)
    {
        _equipRemaining=0f;
        _cycleDuration=0f;
        _cycleElapsed=0f;
        _nextShellEjection=0;
        _reloadDuration = (float)Math.Max(0.1, seconds);
        _reloadRemaining = _reloadDuration;
    }

    public override void _Process(double delta)
    {
        if (_rig is null) return;
        var dt = (float)delta;
        AdvanceCosmeticEffects(dt);
        if(StaticShowroomMode)
        {
            // The armory showroom displays the gun, not invented glove
            // proxy geometry. Live first-person views enable both hands.
            if(_leftHandProxy is not null)_leftHandProxy.Visible=false;
            if(_rightHandProxy is not null)_rightHandProxy.Visible=false;
            if(_armRig66 is not null)_armRig66.Visible=false;
            return;
        }
        _flashSeconds = Mathf.Max(0,_flashSeconds - dt);
        _mechanicalKick = Mathf.Max(0,_mechanicalKick - dt);
        if (_flash is not null) _flash.Visible = _flashSeconds > 0;
        _equipRemaining=Mathf.Max(0f,_equipRemaining-dt);
        if(_cycleDuration>0f)
            _cycleElapsed=Math.Min(_cycleDuration,_cycleElapsed+dt);
        // Sample source magazine, bolt and pump clips at the END of this
        // frame, so a completed reload cannot leave an active insertion
        // pose for one additional render frame.
        _reloadRemaining=Mathf.Max(0f,_reloadRemaining-dt);
        var reloadFraction=_reloadDuration>0f && _reloadRemaining>0f
            ? 1f-_reloadRemaining/_reloadDuration : 0f;
        Vector3 SampleMotor(Pass61SourceWeaponHandling.MotorClip? clip,
            float seconds) => clip is null ? Vector3.Zero :
            Pass61SourceWeaponHandling.SampleMotor(clip,seconds);
        float ReloadTime(Pass61SourceWeaponHandling.MotorClip? clip) =>
            clip is null ? 0f : reloadFraction*clip.Duration;
        var reloadActive=_reloadRemaining>0f && _reloadDuration>0f;
        LastSlideMotorOffset=reloadActive
            ? SampleMotor(_handling?.ReloadSlide,
                ReloadTime(_handling?.ReloadSlide))
            : SampleMotor(_handling?.CyclingSlide,_cycleElapsed);
        LastPumpMotorOffset=reloadActive
            ? SampleMotor(_handling?.ReloadPump,
                ReloadTime(_handling?.ReloadPump))
            : SampleMotor(_handling?.CyclingPump,_cycleElapsed);
        var slideTrack=reloadActive
            ? _handling?.ReloadSlide : _handling?.CyclingSlide;
        var pumpTrack=reloadActive
            ? _handling?.ReloadPump : _handling?.CyclingPump;
        // The Pass47 assembly was visually flattened. When the separately
        // verified Pass62 source hierarchy is installed, transform ALL
        // original Slide/Pump child meshes around their source Handle CFrame.
        // A group without an eligible source track falls back to Pass61
        // part-name approximations without retaining any stale group pose.
        var groupSlide=_sourceHierarchy?.HasGroup("Slide")==true;
        var groupPump=_sourceHierarchy?.HasGroup("Pump")==true;
        if(groupSlide)
            _sourceHierarchy!.ApplyTranslation("Slide",
                slideTrack is null ? Vector3.Zero : LastSlideMotorOffset);
        if(groupPump)
            _sourceHierarchy!.ApplyTranslation("Pump",
                pumpTrack is null ? Vector3.Zero : LastPumpMotorOffset);
        if(!groupSlide || slideTrack is null)
            foreach (var (part,home) in _slides)
                if (GodotObject.IsInstanceValid(part))
                    part.Position=home+(slideTrack is not null
                        ? part.Basis.Orthonormalized()*LastSlideMotorOffset
                        : new Vector3(0,0,_mechanicalKick*.35f));
        if(!groupPump || pumpTrack is null)
            foreach (var (part,home) in _pumps)
                if (GodotObject.IsInstanceValid(part))
                    part.Position=home+(pumpTrack is not null
                        ? part.Basis.Orthonormalized()*LastPumpMotorOffset
                        : Vector3.Zero);
        // Source bolt carrier motion and its nested bolt-handle twist are
        // composed from the original Handle pivots, never double-applied
        // to flattened parts. Without SHA-verified owner source geometry,
        // original data remains inert and old animations are preserved.
        var boltClip=reloadActive ? _action?.Reload : _action?.Cycling;
        var boltTime=reloadActive && boltClip is not null
            ? reloadFraction*boltClip.Duration : _cycleElapsed;
        LastSourceBoltTravel=boltClip is null ? 0f :
            Pass64SourceAction.Sample(boltClip.Bolt,boltTime);
        LastSourceBoltHandleAngle=boltClip is null ? 0f :
            Pass64SourceAction.Sample(boltClip.BoltHandle,boltTime);
        if(_sourceHierarchy?.HasGroup("Bolt")==true)
            _sourceHierarchy.ApplyBoltAction(
                new Vector3(0f,0f,LastSourceBoltTravel),
                LastSourceBoltHandleAngle);
        // Shell names, source event timing, and ejection direction are
        // original evidence. The actual shell shapes/port are explicit
        // local proxies until original remote mesh bytes are recovered.
        var shellSequence=_action?.Cycling.Ejections;
        if(!reloadActive && shellSequence is not null)
            while(_nextShellEjection<shellSequence.Length &&
                  _cycleElapsed>=shellSequence[_nextShellEjection].At)
                SpawnSourceShell(shellSequence[_nextShellEjection++]);
        AdvanceSourceShells(dt);

        var magSourceReady=_magazineReload is not null &&
            _sourceHierarchy?.HasGroup("Mag")==true;
        SourceIncrementalShellInsertPoseActive=
            reloadActive && magSourceReady &&
            _action?.IncrementalReload==true;
        SourceMagazinePoseActive=magSourceReady && reloadActive;
        SourceMagazineHandleTravel=0f;
        if(magSourceReady)
        {
            var progress=reloadActive
                ? reloadFraction : 1f;
            var pose=Pass63SourceMagazineReload.Sample(
                _magazineReload!,progress,_sourceHierarchy!);
            _sourceHierarchy!.ApplyPose("Mag",pose);
            SourceMagazineHandleTravel=pose.Origin.DistanceTo(
                _sourceHierarchy.GroupPivot("Mag"));
        }
        else
        {
            // Old visual fallback remains for uninstalled owner pack,
            // unsupported/dynamic magazine clips and prepared meshes.
            _sourceHierarchy?.ResetGroup("Mag");
            var magazineDrop=reloadActive
                ? Mathf.Sin(Mathf.Pi*reloadFraction)*.14f : 0f;
            foreach (var (part,home) in _magazines)
                if(GodotObject.IsInstanceValid(part))
                    part.Position=home+Vector3.Down*magazineDrop;
        }
        UpdateSourceHands(reloadActive,reloadFraction);
        _phase += dt * 2.5f;
        _kick = Mathf.MoveToward(_kick, 0, dt * 1.2f);
        if(_shotActive)
        {
            _shotRemaining=Mathf.Max(0f,_shotRemaining-dt);
            var total=_sourceShot.Out+_sourceShot.Back;
            var elapsed=total-_shotRemaining;
            _kick=elapsed<=_sourceShot.Out
                ? elapsed/_sourceShot.Out
                : Mathf.Max(0f,1f-(elapsed-_sourceShot.Out)/_sourceShot.Back);
            if(_shotRemaining<=0f)_shotActive=false;
        }
        var reload = _reloadDuration > 0 && _reloadRemaining > 0
            ? Mathf.Sin(Mathf.Pi * (1f - _reloadRemaining / _reloadDuration))
            : 0f;
        var sourceReload=_poseMotion?.Reload;
        var reloadPose=sourceReload is not null &&
            _reloadDuration>0f && _reloadRemaining>0f
            ? Pass60WeaponAnimationLibrary.SampleReload(sourceReload,
                1f-_reloadRemaining/_reloadDuration)
            : Transform3D.Identity;
        var activeSourceFire=_poseFire.HasValue &&
            (_shotActive || _shotRemaining>0f);
        var sourceFirePose=activeSourceFire
            ? Pass60WeaponPoseMath.Interpolate(Transform3D.Identity,
                _poseFire!.Value.Target,_kick)
            : Transform3D.Identity;
        var sourcePullback=_sourceMotion is not null &&
            (_shotActive || _shotRemaining>0f)
            ? _sourceShot.Pullback*_kick : 0f;
        var fallbackFire=activeSourceFire ? Vector3.Zero :
            new Vector3(0f,0f,sourcePullback>0f
                ? sourcePullback : _kick*.22f);
        var fallbackReload=sourceReload is null ? reload : 0f;
        var sourceEquip=_handling?.Equip;
        var equipPose=sourceEquip is not null &&
            _equipDuration>0f && _equipRemaining>0f
            ? Pass61SourceWeaponHandling.SampleEquip(sourceEquip,
                (1f-_equipRemaining/_equipDuration)*sourceEquip.Duration)
            : Transform3D.Identity;
        var position=equipPose.Origin+reloadPose.Origin+
            sourceFirePose.Origin+fallbackFire+
            new Vector3(0f,-fallbackReload*.15f,0f);
        var sway=Basis.FromEuler(new Vector3(
            activeSourceFire ? 0f : -_kick*.75f,
            Mathf.Sin(_phase)*.003f,
            fallbackReload*.22f+Mathf.Sin(_phase*.8f)*.006f));
        _rig.Transform=new Transform3D(
            equipPose.Basis*reloadPose.Basis*sourceFirePose.Basis*sway,
            position);
        // Evaluate arms AFTER the final source-gun transform. The
        // torso-relative shoulder remains camera-stable during recoil
        // while the wrists follow animated Left/Right Weld targets.
        UpdateSourceArms();
    }

    private static void AddHandProxyBox(Node3D parent,
        string name,Vector3 center,Vector3 size,
        StandardMaterial3D material)
    {
        var mesh=new MeshInstance3D
        {
            Name=name,
            Mesh=new BoxMesh{Size=size},
            MaterialOverride=material,
            Position=center
        };
        parent.AddChild(mesh);
    }

    private Node3D CreateHandProxy(string name,bool left)
    {
        var root=new Node3D{Name=name};
        _rig!.AddChild(root);
        // These are clearly identified non-original procedural glove
        // proxies; they are NOT recovered TWR avatar or original arm meshes.
        var glove=new StandardMaterial3D
        {
            AlbedoColor=new Color(.24f,.26f,.28f),
            Metallic=.03f,
            Roughness=.87f
        };
        var cuff=new StandardMaterial3D
        {
            AlbedoColor=new Color(.12f,.14f,.16f),
            Roughness=.9f
        };
        AddHandProxyBox(root,"PalmProxy65",Vector3.Zero,
            new Vector3(.073f,.035f,.085f),glove);
        AddHandProxyBox(root,"WristProxy65",new Vector3(0f,0f,.066f),
            new Vector3(.065f,.045f,.06f),cuff);
        for(var finger=0;finger<4;finger++)
            AddHandProxyBox(root,$"Finger{finger}Proxy65",
                new Vector3((finger-1.5f)*.017f,-.003f,-.058f),
                new Vector3(.013f,.025f,.050f),glove);
        AddHandProxyBox(root,"ThumbProxy65",
            new Vector3(left ? .049f : -.049f,-.006f,-.004f),
            new Vector3(.026f,.028f,.040f),glove);
        SourceHandProxyMeshCount+=7;
        return root;
    }

    private void BuildSourceHandProxies()
    {
        if(_melee || _rig is null || _handSource is null)return;
        _leftHandProxy=CreateHandProxy("LeftHandProxy65",true);
        _rightHandProxy=CreateHandProxy("RightHandProxy65",false);
        _leftHandProxy.Transform=_handSource.LeftRest;
        _rightHandProxy.Transform=_handSource.RightRest;
        BuildSourceArms();
    }

    private static MeshInstance3D CreateArmSegment(
        Node3D parent,string name,bool upper,Material material)
    {
        // Height=1 means Scale.Y can directly represent the solved
        // R6-proportioned bone length in Godot metres.
        var segment=new MeshInstance3D
        {
            Name=name,
            Mesh=new CylinderMesh
            {
                TopRadius=upper?.052f:.044f,
                BottomRadius=upper?.062f:.052f,
                Height=1f,
                RadialSegments=9
            },
            MaterialOverride=material
        };
        parent.AddChild(segment);
        return segment;
    }

    private static MeshInstance3D CreateArmJoint(
        Node3D parent,string name,float radius,Material material)
    {
        var joint=new MeshInstance3D
        {
            Name=name,
            Mesh=new SphereMesh{Radius=radius,Height=radius*2f},
            MaterialOverride=material
        };
        parent.AddChild(joint);
        return joint;
    }

    private void BuildSourceArms()
    {
        if(_rig is null || _leftHandProxy is null ||
           _rightHandProxy is null || _armRig66 is not null)return;
        // The source R6 survivor limb dimensions (1 x 2 x 1 studs)
        // establish approximate arm scale only. This rig and materials
        // are procedurally reconstructed, not original character assets.
        var sleeves=new StandardMaterial3D
        {
            AlbedoColor=new Color(.20f,.23f,.255f),
            Metallic=.015f,
            Roughness=.92f
        };
        var gloveForearms=new StandardMaterial3D
        {
            AlbedoColor=new Color(.17f,.19f,.21f),
            Metallic=.02f,
            Roughness=.87f
        };
        _armRig66=new Node3D{Name="ArmRig66"};
        AddChild(_armRig66);
        _leftUpperArm66=CreateArmSegment(
            _armRig66,"LeftUpperArm66",true,sleeves);
        _leftForeArm66=CreateArmSegment(
            _armRig66,"LeftForeArm66",false,gloveForearms);
        _leftElbow66=CreateArmJoint(
            _armRig66,"LeftElbow66",.050f,gloveForearms);
        CreateArmJoint(_armRig66,"LeftShoulder66",.058f,sleeves)
            .Position=Pass66ArmKinematics.Shoulder(true);
        _rightUpperArm66=CreateArmSegment(
            _armRig66,"RightUpperArm66",true,sleeves);
        _rightForeArm66=CreateArmSegment(
            _armRig66,"RightForeArm66",false,gloveForearms);
        _rightElbow66=CreateArmJoint(
            _armRig66,"RightElbow66",.050f,gloveForearms);
        CreateArmJoint(_armRig66,"RightShoulder66",.058f,sleeves)
            .Position=Pass66ArmKinematics.Shoulder(false);
        SourceArmProxyMeshCount=8;
        UpdateSourceArms();
    }

    private static void PlaceArmSegment(MeshInstance3D segment,
        Vector3 start,Vector3 end)
    {
        if(!Pass66ArmKinematics.TrySegment(
            start,end,out var pose,out var length))
        {
            segment.Visible=false;
            return;
        }
        segment.Visible=true;
        segment.Transform=pose;
        segment.Scale=new Vector3(1f,length,1f);
    }

    private void UpdateSourceArms()
    {
        if(_rig is null || _armRig66 is null ||
           _leftHandProxy is null || _rightHandProxy is null ||
           _leftUpperArm66 is null || _rightUpperArm66 is null ||
           _leftForeArm66 is null || _rightForeArm66 is null ||
           _leftElbow66 is null || _rightElbow66 is null)return;
        // Wrist anchor is inside the existing Pass65 hand cuff; do NOT
        // translate original weapon meshes or alter fire authority.
        var cuff=new Vector3(0f,0f,.067f);
        // The first-person WeaponViewModel node itself moves for ADS.
        // Compensate its parent transform so shoulder anchors stay
        // camera-fixed rather than following the entire weapon to
        // the sights; the wrists still track the source hand welds.
        _armRig66.Transform=Transform.AffineInverse();
        var leftWrist=Transform*
            (_rig.Transform*(_leftHandProxy.Transform*cuff));
        var rightWrist=Transform*
            (_rig.Transform*(_rightHandProxy.Transform*cuff));
        var left=Pass66ArmKinematics.Solve(true,leftWrist);
        var right=Pass66ArmKinematics.Solve(false,rightWrist);
        PlaceArmSegment(_leftUpperArm66,left.Shoulder,left.Elbow);
        PlaceArmSegment(_leftForeArm66,left.Elbow,left.Wrist);
        PlaceArmSegment(_rightUpperArm66,right.Shoulder,right.Elbow);
        PlaceArmSegment(_rightForeArm66,right.Elbow,right.Wrist);
        _leftElbow66.Position=left.Elbow;
        _rightElbow66.Position=right.Elbow;
        LeftArmReachClamped=left.ReachClamped;
        RightArmReachClamped=right.ReachClamped;
        LeftArmReachScale=left.ReachScale;
        RightArmReachScale=right.ReachScale;
        _armRig66.Visible=!StaticShowroomMode;
    }

    private void UpdateSourceHands(bool reloadActive,float reloadFraction)
    {
        if(_handSource is null || _leftHandProxy is null ||
           _rightHandProxy is null)return;
        _leftHandProxy.Visible=!StaticShowroomMode;
        _rightHandProxy.Visible=!StaticShowroomMode;
        Pass65SourceHands.Clip? clip=null;
        var seconds=0f;
        if(reloadActive && _handSource.Reload is { } reloadClip)
        {
            clip=reloadClip;
            seconds=reloadFraction*reloadClip.Duration;
        }
        else if(_equipRemaining>0f && _equipDuration>0f &&
                _handSource.Equip is { } equipClip)
        {
            clip=equipClip;
            seconds=(1f-_equipRemaining/_equipDuration)*equipClip.Duration;
        }
        else if(_cycleDuration>0f &&
                _cycleElapsed<_cycleDuration &&
                _handSource.Cycling is { } cyclingClip)
        {
            clip=cyclingClip;
            seconds=Math.Min(_cycleElapsed,cyclingClip.Duration);
        }
        SourceHandAnimationActive=clip is not null;
        _leftHandProxy.Transform=Pass65SourceHands.Sample(_handSource,
            clip,Pass65SourceHands.Hand.Left,seconds,_sourceHierarchy);
        _rightHandProxy.Transform=Pass65SourceHands.Sample(_handSource,
            clip,Pass65SourceHands.Hand.Right,seconds,_sourceHierarchy);
    }

    private void SpawnSourceShell(Pass64SourceAction.ShellCue source)
    {
        if(_rig is null || _activeShells.Count>=8)return;
        var shotgun=source.Name=="12 Gauge Shell";
        var radius=shotgun ? .013f : .007f;
        var length=shotgun ? .062f : .048f;
        var mesh=new CylinderMesh
        {
            TopRadius=radius,BottomRadius=radius,Height=length
        };
        var material=new StandardMaterial3D
        {
            AlbedoColor=shotgun
                ? new Color(.72f,.13f,.08f)
                : new Color(.65f,.52f,.19f),
            Metallic=.35f,
            Roughness=.41f
        };
        var body=new MeshInstance3D
        {
            Name="SpentShellProxy64",
            Mesh=mesh,
            MaterialOverride=material,
            Position=new Vector3(.11f,-.01f,-.40f)+source.Offset
        };
        _rig.AddChild(body);
        _activeShells.Add(new SpentShellVisual
        {
            Part=body,
            Velocity=source.Direction*source.SpeedMetersPerSecond,
            Remaining=1.2f
        });
        EjectedShellVisualCount++;
        LastEjectedShellSourceName=source.Name;
    }

    private void AdvanceSourceShells(float dt)
    {
        for(var i=_activeShells.Count-1;i>=0;i--)
        {
            var shell=_activeShells[i];
            shell.Remaining-=Math.Clamp(dt,0f,1.2f);
            if(shell.Remaining<=0f || !GodotObject.IsInstanceValid(shell.Part))
            {
                if(GodotObject.IsInstanceValid(shell.Part))
                    shell.Part.QueueFree();
                _activeShells.RemoveAt(i);
                continue;
            }
            shell.Part.Position+=shell.Velocity*dt;
            shell.Part.Rotation+=new Vector3(.3f,.7f,1.1f)*dt*7f;
            shell.Velocity+=Vector3.Down*(6.0f*dt);
        }
    }

    private void ResetRig()
    {
        if(_armRig66 is not null)
        {
            RemoveChild(_armRig66);
            _armRig66.QueueFree();
            _armRig66=null;
        }
        _leftUpperArm66=_rightUpperArm66=null;
        _leftForeArm66=_rightForeArm66=null;
        _leftElbow66=_rightElbow66=null;
        SourceArmProxyMeshCount=0;
        LeftArmReachClamped=RightArmReachClamped=false;
        LeftArmReachScale=RightArmReachScale=1f;
        if (_rig is not null)
        {
            RemoveChild(_rig);
            _rig.QueueFree();
        }
        _rig = new Node3D { Name = "WeaponBody" };
        AddChild(_rig);
        _melee = false;
        _slides.Clear();
        _magazines.Clear();
        _pumps.Clear();
        _flashSeconds = 0f;
        _mechanicalKick = 0f;
        _shotActive=false;
        _shotRemaining=0f;
        _sourceMotion=null;
        _sourceSight67=null;
        _originalAimFormula68=null;
        _originalAimPart68=null;
        _originalHandle68=null;
        _poseMotion=null;
        _poseFire=null;
        _handling=null;
        _sourceHierarchy=null;
        _magazineReload=null;
        _action=null;
        _handSource=null;
        _leftHandProxy=null;
        _rightHandProxy=null;
        SourceHandProxyMeshCount=0;
        SourceHandAnimationActive=false;
        _nextShellEjection=0;
        LastSourceBoltTravel=0f;
        LastSourceBoltHandleAngle=0f;
        EjectedShellVisualCount=0;
        LastEjectedShellSourceName="";
        SourceIncrementalShellInsertPoseActive=false;
        foreach(var visual in _activeShells)
            if(GodotObject.IsInstanceValid(visual.Part))
                visual.Part.QueueFree();
        _activeShells.Clear();
        SourceMagazinePoseActive=false;
        SourceMagazineHandleTravel=0f;
        _equipDuration=_equipRemaining=0f;
        _cycleDuration=_cycleElapsed=0f;
        LastSlideMotorOffset=LastPumpMotorOffset=Vector3.Zero;
        _flash = new MeshInstance3D
        {
            Name = "MuzzleFlash",
            Visible = false,
            Position = new Vector3(0,.02f,-.95f),
            Mesh = new SphereMesh { Radius = .10f, Height = .20f },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = new Color(1f,.76f,.31f),
                EmissionEnabled = true,
                Emission = new Color(1f,.64f,.20f),
                EmissionEnergyMultiplier = 2.0f
            }
        };
        _rig.AddChild(_flash);
        UsingPreparedScene = false;
        UsingOriginalToolAssembly = false;
        ActiveCosmeticSkinId = "";
        CosmeticProxyTintParts = 0;
        SourceSkinMappedParts = 0;
        SourceSkinColorParts = 0;
        SourceSkinMissingTextureParts = 0;
        SourceTextureReferencedParts = 0;
        SourceInstalledTextureParts = 0;
        SourceProceduralTextureParts = 0;
        SourceUnrenderedTextureLayers = 0;
        _animatedSkinMaterials.Clear();
        CosmeticEffectElapsed = 0f;
        LastShimmerMultiplier = 1f;
        CosmeticSourceStatus = "";
        SourceVisibleParts = 0;
        SourceMissingMeshProxies = 0;
        SourceLocallyResolvedMeshes = 0;
        SourceCircularProxyParts = 0;
        SourceConservativeProxyParts = 0;
        SourceNativePrimitiveParts = 0;
        SourceDetailProfileProxies = 0;
        SourceLensProxies = 0;
        _reloadRemaining = _reloadDuration = _kick = 0;
    }

    private void TrackMechanicalParts()
    {
        if (_rig is null) return;
        foreach (var node in _rig.GetChildren().OfType<MeshInstance3D>())
        {
            var name = node.Name.ToString();
            if (name.Contains("Slide",StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Bolt",StringComparison.OrdinalIgnoreCase))
                _slides.Add((node,node.Position));
            if (name.Contains("Magazine",StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Mag",StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Clip",StringComparison.OrdinalIgnoreCase))
                _magazines.Add((node,node.Position));
            if(name.Equals("Pump",StringComparison.OrdinalIgnoreCase) ||
               name.Equals("ForeEnd",StringComparison.OrdinalIgnoreCase))
                _pumps.Add((node,node.Position));
        }
    }

    private void BuildApproximateWeapon(RuntimeWeaponDefinition spec)
    {
        var metal = Material(new Color(0.15f, 0.17f, 0.19f), true);
        var polymer = Material(new Color(0.10f, 0.11f, 0.13f));
        var wood = Material(new Color(0.35f, 0.21f, 0.13f));
        var detail = Material(new Color(0.35f, 0.36f, 0.38f), true);
        if (spec.IsMelee)
        {
            AddBox("MeleeHandle", new Vector3(0.02f, 0.0f, -0.15f),
                new Vector3(0.08f, 0.75f, 0.08f), wood);
            AddBox("MeleeHead", new Vector3(0.02f, 0.36f, -0.15f),
                new Vector3(0.15f, 0.14f, 0.12f), metal);
            return;
        }
        if (spec.IsLauncher)
        {
            AddBarrel("LaunchTube", new Vector3(0,0,-0.37f), 0.18f, 0.98f, metal);
            AddCylinder("MuzzleRing",new Vector3(0,0,-0.91f),0.20f,0.07f,detail, true);
            AddBox("LauncherGrip",new Vector3(0,-0.25f,-0.20f),
                new Vector3(0.12f,0.43f,0.15f),polymer);
            AddBox("LauncherSight",new Vector3(0,0.15f,-0.47f),
                new Vector3(0.08f,0.19f,0.22f),detail);
            return;
        }
        if (spec.IsFlamethrower)
        {
            AddBarrel("PressureBarrel",new Vector3(0,0,-0.47f),0.13f,0.95f,metal);
            AddCylinder("Tank",new Vector3(0.17f,-0.16f,-0.1f),0.16f,0.45f,detail);
            AddBox("Grip",new Vector3(0,-0.22f,-0.1f),
                new Vector3(0.12f,0.4f,0.14f),polymer);
            return;
        }
        if (spec.Slot == "Secondary" && !spec.IsShotgun)
        {
            AddBox("PistolSlide",new Vector3(0,0.08f,-0.23f),
                new Vector3(0.16f,0.10f,0.48f),metal);
            AddBox("PistolFrame",new Vector3(0,-0.02f,-0.16f),
                new Vector3(0.14f,0.12f,0.39f),polymer);
            AddBox("PistolGrip",new Vector3(0,-0.24f,0.01f),
                new Vector3(0.13f,0.37f,0.14f),polymer);
            AddBox("FrontSight",new Vector3(0,0.15f,-0.42f),
                new Vector3(0.04f,0.045f,0.04f),detail);
            AddBox("RearSight",new Vector3(0,0.15f,-0.04f),
                new Vector3(0.09f,0.045f,0.045f),detail);
            return;
        }

        // Long-gun fallback: barrel/receiver/stock/magazine/foregrip and sight.
        var shotgun = spec.IsShotgun;
        AddBox("Receiver",new Vector3(0,0,-0.26f),
            new Vector3(0.20f,0.19f,0.38f), metal);
        AddBarrel("Barrel",new Vector3(0,0.015f,-0.73f),
            shotgun ? 0.065f : 0.046f,shotgun ? 0.90f : 0.82f,detail);
        AddBox("Handguard",new Vector3(0,-0.07f,-0.58f),
            new Vector3(0.17f,0.12f,0.30f),shotgun ? wood : polymer);
        AddBox("Stock",new Vector3(0,-0.09f,0.19f),
            new Vector3(0.17f,0.21f,0.49f),shotgun ? wood : polymer);
        AddBox("PistolGrip",new Vector3(0,-0.27f,-0.13f),
            new Vector3(0.12f,0.37f,0.14f),polymer);
        if (!shotgun)
            AddBox("Magazine",new Vector3(0,-0.29f,-0.35f),
                new Vector3(0.12f,0.29f,0.19f),polymer);
        else
            AddBox("Pump",new Vector3(0,-0.09f,-0.72f),
                new Vector3(0.15f,0.12f,0.26f),wood);
        AddBox("FrontSight",new Vector3(0,0.13f,-1.01f),
            new Vector3(0.045f,0.12f,0.045f),detail);
        AddBox("RearSight",new Vector3(0,0.16f,-0.21f),
            new Vector3(0.10f,0.12f,0.08f),detail);
    }

    private void AddBox(string name, Vector3 point, Vector3 size, Material material)
    {
        _rig!.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = point,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = material
        });
    }

    private void AddCylinder(
        string name, Vector3 point, float radius, float length,
        Material material, bool alongZ = false)
    {
        _rig!.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = point,
            RotationDegrees = alongZ ? new Vector3(90f,0,0) : Vector3.Zero,
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = length
            },
            MaterialOverride = material
        });
    }

    private void AddBarrel(string name,Vector3 point,float radius,float length,Material material)
        => AddCylinder(name,point,radius,length,material,true);

    private static StandardMaterial3D Material(Color color,bool metallic = false) => new()
    {
        AlbedoColor = color,
        Metallic = metallic ? 0.70f : 0.02f,
        Roughness = metallic ? 0.36f : 0.81f
    };
}

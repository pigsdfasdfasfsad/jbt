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
    private readonly List<(MeshInstance3D Part, Vector3 Home)> _slides = [];
    private readonly List<(MeshInstance3D Part, Vector3 Home)> _magazines = [];
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
            _flash.Position = OriginalWeaponSourceRuntime.EstimatedMuzzle(spec.Name)
                ?? _flash.Position;
            TrackMechanicalParts();
            return;
        }
        BuildApproximateWeapon(spec);
        TrackMechanicalParts();
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

    public void Fire(bool aiming = false)
    {
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
        _reloadDuration = (float)Math.Max(0.1, seconds);
        _reloadRemaining = _reloadDuration;
    }

    public override void _Process(double delta)
    {
        if (_rig is null) return;
        var dt = (float)delta;
        AdvanceCosmeticEffects(dt);
        if(StaticShowroomMode)return;
        _flashSeconds = Mathf.Max(0,_flashSeconds - dt);
        _mechanicalKick = Mathf.Max(0,_mechanicalKick - dt);
        if (_flash is not null) _flash.Visible = _flashSeconds > 0;
        foreach (var (part,home) in _slides)
            if (GodotObject.IsInstanceValid(part))
                part.Position = home + new Vector3(0,0,_mechanicalKick * .35f);
        var magazineDrop = _reloadDuration > 0 && _reloadRemaining > 0
            ? Mathf.Sin(Mathf.Pi *
                (1f - _reloadRemaining / _reloadDuration)) * .14f : 0f;
        foreach (var (part,home) in _magazines)
            if (GodotObject.IsInstanceValid(part))
                part.Position = home + Vector3.Down * magazineDrop;
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
        _reloadRemaining = Mathf.Max(0, _reloadRemaining - dt);
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
        var position=reloadPose.Origin+sourceFirePose.Origin+fallbackFire+
            new Vector3(0f,-fallbackReload*.15f,0f);
        var sway=Basis.FromEuler(new Vector3(
            activeSourceFire ? 0f : -_kick*.75f,
            Mathf.Sin(_phase)*.003f,
            fallbackReload*.22f+Mathf.Sin(_phase*.8f)*.006f));
        _rig.Transform=new Transform3D(
            reloadPose.Basis*sourceFirePose.Basis*sway,position);
    }

    private void ResetRig()
    {
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
        _flashSeconds = 0f;
        _mechanicalKick = 0f;
        _shotActive=false;
        _shotRemaining=0f;
        _sourceMotion=null;
        _poseMotion=null;
        _poseFire=null;
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

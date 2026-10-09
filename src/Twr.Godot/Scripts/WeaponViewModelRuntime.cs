using System;
using System.Linq;
using Godot;

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
    public bool UsingOriginalToolAssembly { get; private set; }
    public int VisualPartCount => _rig is null ? 0 :
        Math.Max(0, _rig.GetChildCount() - (_flash is null ? 0 : 1));

    private Node3D? _rig;
    private MeshInstance3D? _flash;
    private bool _melee;
    private float _flashSeconds;
    private float _phase;
    private float _kick;
    private float _reloadRemaining;
    private float _reloadDuration;

    public void SetWeapon(RuntimeWeaponDefinition spec)
    {
        ResetRig();
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
            return;
        }
        BuildApproximateWeapon(spec);
    }

    public void SetThrowable(string type)
    {
        ResetRig();
        if (OriginalWeaponSourceRuntime.TryBuild(_rig!, type))
        {
            UsingOriginalToolAssembly = true;
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

    public void Fire()
    {
        _kick = 0.16f;
        if (!_melee && _flash is not null)
        {
            _flashSeconds = .055f;
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
        _flashSeconds = Mathf.Max(0,_flashSeconds - dt);
        if (_flash is not null) _flash.Visible = _flashSeconds > 0;
        _phase += dt * 2.5f;
        _kick = Mathf.MoveToward(_kick, 0, dt * 1.2f);
        _reloadRemaining = Mathf.Max(0, _reloadRemaining - dt);
        var reload = _reloadDuration > 0 && _reloadRemaining > 0
            ? Mathf.Sin(Mathf.Pi * (1f - _reloadRemaining / _reloadDuration))
            : 0f;
        _rig.Position = new Vector3(0, -reload * 0.15f, _kick * 0.22f);
        _rig.Rotation = new Vector3(-_kick * 0.75f,
            Mathf.Sin(_phase) * 0.003f,
            reload * 0.22f + Mathf.Sin(_phase * 0.8f) * 0.006f);
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
        _flashSeconds = 0f;
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
        _reloadRemaining = _reloadDuration = _kick = 0;
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

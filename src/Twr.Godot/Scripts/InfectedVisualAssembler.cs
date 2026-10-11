using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Offline infected presentation. An original, pre-converted packed scene takes
/// precedence if it is installed into the build. When unavailable, a clearly
/// non-original animated humanoid proxy replaces the former solid capsule.
/// No Roblox/network access occurs at runtime.
/// </summary>
public partial class InfectedVisualAssembler : Node3D
{
    public string InfectedType { get; set; } = "Civilian";
    public bool UsingOriginalScene { get; private set; }
    public bool UsingSourceBlueprint { get; private set; }
    public bool VerifiedOwnerSourceAccessoryKit { get; private set; }
    public int RecoveredSourceAccessoryParts { get; private set; }
    public int ReconstructedR6BodyParts { get; private set; }
    public int MissingSourceMeshProxies { get; private set; }

    private Node3D? _leftArm;
    private Node3D? _rightArm;
    private Node3D? _leftLeg;
    private Node3D? _rightLeg;
    private float _cycle;
    private float _attackDuration;
    private float _hitDuration;

    public void HitReaction() => _hitDuration = .20f;

    public override void _Ready()
    {
        var safeType = InfectedType switch
        {
            "Civilian" or "Sprinter" or "Military" or "Hazmat" or
            "Riot" or "Burster" or "Bloater" or "Bolter" => InfectedType,
            _ => "Civilian"
        };
        var scenePath = $"res://Content/Assets/Infected/{safeType}.tscn";
        if (ResourceLoader.Exists(scenePath))
        {
            var scene = ResourceLoader.Load<PackedScene>(scenePath);
            if (scene is not null)
            {
                var imported = scene.Instantiate<Node3D>();
                imported.Name = "PreparedOriginalInfected";
                AddChild(imported);
                UsingOriginalScene = true;
                return;
            }
        }
        // Actual R6 part positions, accessory references and original
        // colors from the user's place snapshots take priority over the
        // former procedural humanoid. Missing cloud meshes remain proxies.
        if (InfectedSourceModelRuntime.TryBuild(this, safeType,
            out _leftArm, out _rightArm, out _leftLeg, out _rightLeg))
        {
            UsingSourceBlueprint = true;
            VerifiedOwnerSourceAccessoryKit =
                InfectedSourceModelRuntime.OwnerSourceKitVerified;
            RecoveredSourceAccessoryParts =
                InfectedSourceModelRuntime.LastSourceAssetParts;
            ReconstructedR6BodyParts =
                InfectedSourceModelRuntime.LastReconstructedR6Parts;
            MissingSourceMeshProxies =
                InfectedSourceModelRuntime.LastUnresolvedMeshProxies;
            return;
        }
        BuildTemporaryHumanoid(safeType);
    }

    public void Attack()
    {
        _attackDuration = 0.38f;
        if (UsingOriginalScene)
        {
            // Original AnimationTree/AnimationPlayer assets, when present,
            // should eventually own animation state. Do not impose guessed
            // animation names on imported original scenes.
        }
    }

    public override void _Process(double delta)
    {
        _hitDuration = Math.Max(0,_hitDuration-(float)delta);
        if (!UsingOriginalScene)
        {
            // Short non-destructive flinch applies to the complete recovered
            // original-part group without altering its author colors.
            Rotation = new Vector3(
                _hitDuration > 0 ? -.065f * Mathf.Sin(_hitDuration * 25f) : 0,
                0,
                _hitDuration > 0 ? .045f * Mathf.Sin(_hitDuration * 33f) : 0);
        }
        if (UsingOriginalScene) return;
        if (GetParent() is not CharacterBody3D owner) return;
        var speed = new Vector2(owner.Velocity.X, owner.Velocity.Z).Length();
        var walking = speed > 0.4f;
        var gait = InfectedType switch
        {
            "Sprinter" or "Bolter" => 12.5f,
            "Bloater" or "Burster" => 6.6f,
            _ => 9.2f
        };
        _cycle += (float)delta * (walking ? gait : 2.4f);
        _attackDuration = Math.Max(0, _attackDuration - (float)delta);
        var swing = walking ? Mathf.Sin(_cycle) * 0.52f : 0f;
        if (_leftLeg is not null) _leftLeg.Rotation = new Vector3(swing, 0, 0);
        if (_rightLeg is not null) _rightLeg.Rotation = new Vector3(-swing, 0, 0);
        if (_leftArm is not null) _leftArm.Rotation = new Vector3(
            _attackDuration > 0 ? -1.15f : -swing * 0.75f, 0, -0.13f);
        if (_rightArm is not null) _rightArm.Rotation = new Vector3(
            _attackDuration > 0 ? -1.15f : swing * 0.75f, 0, 0.13f);
    }

    private void BuildTemporaryHumanoid(string type)
    {
        var palette = Palette(type);
        var skin = Material(palette.Skin);
        var cloth = Material(palette.Cloth);
        var trousers = Material(palette.Trousers);
        var gear = Material(palette.Gear);
        var dark = Material(new Color(0.05f, 0.06f, 0.07f));
        var glow = Material(new Color(0.67f, 0.17f, 0.12f));

        var large = type == "Bloater";
        var broad = large ? 0.82f : type == "Burster" ? 0.72f : 0.56f;
        var torso = new Node3D { Name = "Torso" };
        AddChild(torso);
        AddBox(torso, "TorsoSurface", new Vector3(0, 0.16f, 0),
            new Vector3(broad, 0.77f, large ? 0.58f : 0.35f), cloth);
        AddBox(torso, "Hips", new Vector3(0, -0.27f, 0),
            new Vector3(broad * 0.9f, 0.21f, 0.35f), trousers);
        AddSphere(torso, "Head", new Vector3(0, 0.70f, 0),
            new Vector3(0.39f, 0.45f, 0.37f), skin);
        AddBox(torso, "LeftEye", new Vector3(-0.10f, 0.73f, -0.173f),
            new Vector3(0.055f, 0.045f, 0.03f), glow);
        AddBox(torso, "RightEye", new Vector3(0.10f, 0.73f, -0.173f),
            new Vector3(0.055f, 0.045f, 0.03f), glow);

        _leftArm = Pivot(torso, "LeftShoulder",
            new Vector3(-broad / 2f - 0.10f, 0.42f, 0));
        _rightArm = Pivot(torso, "RightShoulder",
            new Vector3(broad / 2f + 0.10f, 0.42f, 0));
        AddBox(_leftArm, "LeftArm", new Vector3(0, -0.34f, 0),
            new Vector3(0.22f, 0.67f, 0.25f), cloth);
        AddBox(_rightArm, "RightArm", new Vector3(0, -0.34f, 0),
            new Vector3(0.22f, 0.67f, 0.25f), cloth);
        AddBox(_leftArm, "LeftHand", new Vector3(0, -0.69f, -0.01f),
            new Vector3(0.18f, 0.16f, 0.19f), skin);
        AddBox(_rightArm, "RightHand", new Vector3(0, -0.69f, -0.01f),
            new Vector3(0.18f, 0.16f, 0.19f), skin);

        _leftLeg = Pivot(torso, "LeftHip", new Vector3(-0.17f, -0.35f, 0));
        _rightLeg = Pivot(torso, "RightHip", new Vector3(0.17f, -0.35f, 0));
        AddBox(_leftLeg, "LeftLeg", new Vector3(0, -0.30f, 0),
            new Vector3(0.25f, 0.63f, 0.27f), trousers);
        AddBox(_rightLeg, "RightLeg", new Vector3(0, -0.30f, 0),
            new Vector3(0.25f, 0.63f, 0.27f), trousers);

        if (type is "Military" or "Riot" or "Hazmat")
        {
            AddBox(torso, "Helmet", new Vector3(0, 0.91f, 0),
                new Vector3(0.48f, 0.17f, 0.48f), gear);
            AddBox(torso, "ArmoredVest", new Vector3(0, 0.23f, -0.20f),
                new Vector3(broad * 0.94f, 0.58f, 0.13f), gear);
        }
        if (type == "Hazmat")
        {
            AddBox(torso, "Respirator", new Vector3(0, 0.63f, -0.19f),
                new Vector3(0.25f, 0.19f, 0.13f), dark);
        }
        if (type == "Burster")
        {
            AddSphere(torso, "SwollenTorso", new Vector3(0, 0.13f, -0.20f),
                new Vector3(0.65f, 0.61f, 0.51f), gear);
        }
    }

    private static Node3D Pivot(Node3D parent, string name, Vector3 position)
    {
        var node = new Node3D { Name = name, Position = position };
        parent.AddChild(node);
        return node;
    }

    private static void AddBox(
        Node3D parent, string name, Vector3 center,
        Vector3 size, StandardMaterial3D material)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = center,
            Mesh = new BoxMesh { Size = size, Material = material }
        });
    }

    private static void AddSphere(
        Node3D parent, string name, Vector3 center,
        Vector3 size, StandardMaterial3D material)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = center,
            Scale = size,
            Mesh = new SphereMesh
            {
                Radius = 0.5f,
                Height = 1.0f,
                Material = material
            }
        });
    }

    private static StandardMaterial3D Material(Color color) => new()
    {
        AlbedoColor = color,
        Roughness = 0.88f
    };

    private sealed record InfectedPalette(
        Color Skin, Color Cloth, Color Trousers, Color Gear);

    private static InfectedPalette Palette(string type) => type switch
    {
        "Sprinter" => new(new Color(0.49f,0.36f,0.31f),
            new Color(0.57f,0.24f,0.15f), new Color(0.30f,0.30f,0.27f),
            new Color(0.40f,0.12f,0.10f)),
        "Military" => new(new Color(0.44f,0.38f,0.29f),
            new Color(0.29f,0.37f,0.22f),new Color(0.21f,0.27f,0.18f),
            new Color(0.15f,0.24f,0.12f)),
        "Hazmat" => new(new Color(0.53f,0.52f,0.42f),
            new Color(0.75f,0.69f,0.17f),new Color(0.72f,0.65f,0.14f),
            new Color(0.48f,0.43f,0.11f)),
        "Riot" => new(new Color(0.33f,0.32f,0.28f),
            new Color(0.17f,0.21f,0.27f),new Color(0.11f,0.14f,0.19f),
            new Color(0.07f,0.11f,0.14f)),
        "Burster" => new(new Color(0.42f,0.56f,0.20f),
            new Color(0.26f,0.36f,0.17f),new Color(0.25f,0.23f,0.16f),
            new Color(0.43f,0.60f,0.22f)),
        "Bloater" => new(new Color(0.42f,0.33f,0.33f),
            new Color(0.33f,0.24f,0.33f),new Color(0.30f,0.23f,0.22f),
            new Color(0.45f,0.34f,0.34f)),
        "Bolter" => new(new Color(0.45f,0.23f,0.17f),
            new Color(0.51f,0.12f,0.12f),new Color(0.28f,0.19f,0.18f),
            new Color(0.41f,0.09f,0.09f)),
        _ => new(new Color(0.44f,0.40f,0.34f),
            new Color(0.34f,0.37f,0.28f),new Color(0.27f,0.28f,0.24f),
            new Color(0.24f,0.27f,0.20f))
    };
}

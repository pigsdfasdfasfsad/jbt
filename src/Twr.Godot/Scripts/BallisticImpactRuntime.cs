using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Short-lived local-only hit tracer and impact glint. The original game's
/// projectile particles and impact decals are not present in the XML sources;
/// this visible fallback preserves the gunfire feedback without network art.
/// </summary>
public partial class BallisticImpactRuntime : Node3D
{
    private static int _active;
    private const int MaximumActive = 80;
    private double _remaining = .13;
    private bool _counted;

    public static void Spawn(
        Node3D world, Vector3 origin, Vector3 hit, bool infectedTarget)
    {
        if (_active >= MaximumActive) return;
        var range = origin.DistanceTo(hit);
        if (range < .04f || range > 300f) return;
        var fx = new BallisticImpactRuntime
        {
            Name = "OfflineBulletImpact",
            _counted = true
        };
        _active++;
        world.AddChild(fx);
        fx.Build(origin, hit, infectedTarget);
    }

    private void Build(Vector3 origin, Vector3 impact, bool infectedTarget)
    {
        var direction = impact - origin;
        var distance = direction.Length();
        var tint = infectedTarget ? new Color(.73f,.12f,.08f)
                                  : new Color(.86f,.75f,.43f);
        var sparkle = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = tint,
            EmissionEnabled = true,
            Emission = tint,
            EmissionEnergyMultiplier = 1.4f
        };
        var line = new MeshInstance3D
        {
            Name = "ShotTrace",
            Position = (origin + impact) * .5f,
            Basis = Basis.LookingAt(direction.Normalized(),
                Math.Abs(direction.Normalized().Y) > .98f
                    ? Vector3.Forward : Vector3.Up),
            Mesh = new BoxMesh
            {
                Size = new Vector3(.009f,.009f,distance)
            },
            MaterialOverride = sparkle,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(line);
        AddChild(new MeshInstance3D
        {
            Name = infectedTarget ? "InfectedImpact" : "WallImpact",
            Position = impact,
            Mesh = new SphereMesh { Radius = .035f, Height = .07f },
            MaterialOverride = sparkle,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        });
    }

    public override void _Process(double delta)
    {
        _remaining -= delta;
        if (_remaining <= 0) QueueFree();
    }

    public override void _ExitTree()
    {
        if (_counted)
        {
            _active = Math.Max(0, _active - 1);
            _counted = false;
        }
    }
}

using System;
using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

/// <summary>
/// Offline spore cloud with deterministic 0.5-second pulses, gas mask immunity,
/// world occlusion, and an exact expiry boundary. Visual form is approximated.
/// </summary>
public partial class SporeCloudRuntime : Node3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public float Radius { get; set; } = 20f;
    public float TickDamage { get; set; } = 5f;
    public double DurationSeconds { get; set; } = 4.0;
    private readonly Pass24GasTickClock _clock = new();
    private float EffectiveRadius => RobloxUnits.Distance(Radius);

    public override void _Ready()
    {
        AddChild(new MeshInstance3D {
            Position = new Vector3(0, .15f, 0),
            Mesh = new CylinderMesh {
                TopRadius = EffectiveRadius,
                BottomRadius = EffectiveRadius,
                Height = .3f,
                Material = new StandardMaterial3D {
                    AlbedoColor = new Color(.30f,.50f,.18f,.22f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    Roughness = 1f
                }
            }
        });
    }

    private bool BlockedByWorld(Vector3 toward)
    {
        var query = PhysicsRayQueryParameters3D.Create(
            GlobalPosition + Vector3.Up * .4f, toward);
        query.CollisionMask = 1 | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        if (Target is not null && GodotObject.IsInstanceValid(Target))
            query.Exclude = new global::Godot.Collections.Array<Rid> { Target.GetRid() };
        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count != 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        var ticks = _clock.Advance(delta, Math.Max(.01, DurationSeconds));
        for (var tick = 0; tick < ticks; tick++)
        {
            if (Target is null || !GodotObject.IsInstanceValid(Target) ||
                Runtime?.Player?.IsAlive != true) break;
            var aim = Target.GlobalPosition + Vector3.Up * .8f;
            var inCloud = aim.DistanceTo(GlobalPosition) <= EffectiveRadius;
            var damage = Pass24SporeImpact.GasDamage(
                TickDamage, Runtime.Player.GasMaskActive,
                inCloud && BlockedByWorld(aim), inCloud);
            if (damage > 0) Runtime.DamagePlayer(damage, "Spore gas", true);
        }
        if (_clock.Expired) QueueFree();
    }
}

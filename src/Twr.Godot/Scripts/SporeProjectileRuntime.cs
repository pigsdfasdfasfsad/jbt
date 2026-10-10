using System;
using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

/// <summary>
/// Swept-collision offline Bloater projectile. Physics hits explode on the
/// impact side of a wall; splash cannot bleed through obstacles. Velocity,
/// radius and falloff remain approximations of undocumented original values.
/// </summary>
public partial class SporeProjectileRuntime : Node3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public float Damage { get; set; } = 26.4f;
    public float Radius { get; set; } = 15f;
    private const float Speed = 12f;
    private Vector3 _velocity;
    private double _life = 3.0;
    private bool _detonated;
    private bool _directHit;

    public override void _Ready()
    {
        var direction = Target is null
            ? Vector3.Forward
            : (Target.GlobalPosition + Vector3.Up - GlobalPosition).Normalized();
        _velocity = direction * Speed;
        AddChild(new MeshInstance3D {
            Mesh = new SphereMesh {
                Radius = .35f, Height = .7f,
                Material = new StandardMaterial3D {
                    AlbedoColor = new Color(.38f,.62f,.18f),
                    EmissionEnabled = true,
                    Emission = new Color(.16f,.28f,.08f)
                }
            }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_detonated) return;
        _life -= delta;
        if (_life <= 0) { QueueFree(); return; }
        var start = GlobalPosition;
        var next = start + _velocity * (float)Math.Min(delta, .1);
        var ray = PhysicsRayQueryParameters3D.Create(start, next);
        ray.CollisionMask = 1; // Source infected are on layer 2.
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if (hit.Count != 0)
        {
            var pos = (Vector3)hit["position"];
            var normal = (Vector3)hit["normal"];
            _directHit = Target is not null &&
                ReferenceEquals(hit["collider"].AsGodotObject(), Target);
            GlobalPosition = pos + normal * .03f;
            Detonate();
            return;
        }
        GlobalPosition = next;
    }

    private bool WorldOccludes(Vector3 toward)
    {
        var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition, toward);
        ray.CollisionMask = 1;
        if (Target is not null && GodotObject.IsInstanceValid(Target))
            ray.Exclude = new global::Godot.Collections.Array<Rid> { Target.GetRid() };
        return GetWorld3D().DirectSpaceState.IntersectRay(ray).Count != 0;
    }

    private void Detonate()
    {
        if (_detonated) return;
        _detonated = true;
        if (Target is not null && GodotObject.IsInstanceValid(Target) &&
            Runtime?.Player?.IsAlive == true)
        {
            var aim = Target.GlobalPosition + Vector3.Up * .8f;
            var effectiveRadius = RobloxUnits.Distance(Radius);
            var distance = aim.DistanceTo(GlobalPosition);
            var damage = Pass24SporeImpact.ClusterDamage(
                Damage, distance, effectiveRadius,
                !_directHit && WorldOccludes(aim));
            if (damage > 0) Runtime.DamagePlayer(damage, "Spore cluster", true);
        }
        QueueFree();
    }
}

using Godot;

namespace Twr.Godot;

public partial class SporeProjectileRuntime : Node3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public float Damage { get; set; } = 26.4f;
    public float Radius { get; set; } = 15f;

    // APPROXIMATED: throw speed/flight time are not documented.
    private const float Speed = 12f;
    private Vector3 _velocity;
    private double _life = 3.0;

    public override void _Ready()
    {
        var direction = Target is null
            ? Vector3.Forward
            : (Target.GlobalPosition + Vector3.Up - GlobalPosition).Normalized();
        _velocity = direction * Speed;

        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = 0.35f,
                Height = 0.7f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.38f, 0.62f, 0.18f),
                    EmissionEnabled = true,
                    Emission = new Color(0.16f, 0.28f, 0.08f)
                }
            }
        });
    }

    public override void _Process(double delta)
    {
        _life -= delta;
        GlobalPosition += _velocity * (float)delta;

        if (Target is not null && GlobalPosition.DistanceTo(Target.GlobalPosition + Vector3.Up) <= 1.2f)
        {
            Detonate();
            return;
        }

        if (_life <= 0)
            Detonate();
    }

    private void Detonate()
    {
        if (Target is not null && Runtime?.Player?.IsAlive == true)
        {
            var distance = Target.GlobalPosition.DistanceTo(GlobalPosition);
            if (distance <= Radius)
            {
                // APPROXIMATED radial falloff; source confirms damage decreases
                // farther from direct splash but does not provide the equation.
                var factor = Mathf.Lerp(1f, 0.5f, Math.Clamp(distance / Radius, 0f, 1f));
                Runtime.DamagePlayer(Damage * factor, "Spore cluster", true);
            }
        }
        QueueFree();
    }
}

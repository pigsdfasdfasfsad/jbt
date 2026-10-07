using Godot;

namespace Twr.Godot;

public partial class InfectedAgent : CharacterBody3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public string InfectedType { get; set; } = "Civilian";
    public float Health { get; set; } = 65;
    public float Damage { get; set; } = 8;
    public float MoveSpeed { get; set; } = 15;
    public Action<InfectedAgent>? Died { get; set; }

    private double _attackCooldown;

    public override void _Ready()
    {
        CollisionLayer = 2;
        CollisionMask = 1;

        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.42f, Height = 1.8f }
        });

        AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh
            {
                Radius = 0.42f,
                Height = 1.8f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = ColorForType(InfectedType),
                    Roughness = 0.9f
                }
            }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        _attackCooldown = Math.Max(0, _attackCooldown - delta);

        if (Target is null || Runtime?.Player is null || !Runtime.Player.IsAlive)
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
            MoveAndSlide();
            return;
        }

        var deltaToPlayer = Target.GlobalPosition - GlobalPosition;
        var flat = new Vector3(deltaToPlayer.X, 0, deltaToPlayer.Z);
        var distance = flat.Length();
        var velocity = Velocity;

        if (!IsOnFloor()) velocity.Y -= 22f * (float)delta;

        if (distance > 1.55f)
        {
            var direction = flat.LengthSquared() > 0.001f ? flat.Normalized() : Vector3.Zero;
            velocity.X = direction.X * MoveSpeed;
            velocity.Z = direction.Z * MoveSpeed;
            if (direction.LengthSquared() > 0.001f) LookAt(GlobalPosition + direction, Vector3.Up);
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
            if (_attackCooldown <= 0)
            {
                // APPROXIMATED: retail claw cadence is not recovered.
                _attackCooldown = 1.0;
                Runtime.DamagePlayer(Damage, InfectedType);
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0 || Health <= 0) return;
        Health = Math.Max(0, Health - amount);
        if (Health > 0) return;
        Died?.Invoke(this);
        QueueFree();
    }

    private static Color ColorForType(string type) => type switch
    {
        "Bolter" => new Color(0.55f, 0.17f, 0.14f),
        "Sprinter" => new Color(0.65f, 0.33f, 0.16f),
        "Military" => new Color(0.20f, 0.28f, 0.17f),
        "Hazmat" => new Color(0.72f, 0.68f, 0.15f),
        "Riot" => new Color(0.12f, 0.15f, 0.18f),
        "Burster" => new Color(0.38f, 0.48f, 0.19f),
        "Bloater" => new Color(0.36f, 0.23f, 0.28f),
        _ => new Color(0.32f, 0.36f, 0.30f)
    };
}

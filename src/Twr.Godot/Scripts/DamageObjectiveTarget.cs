using Godot;

namespace Twr.Godot;

public partial class DamageObjectiveTarget : StaticBody3D
{
    public Action<float>? ProgressChanged { get; set; }
    public Action<Vector3>? Destroyed { get; set; }

    // APPROXIMATED health scale: source documents only a percentage-to-zero
    // objective meter, not a numeric tanker HP value.
    public float MaxHealth { get; set; } = 1000f;
    public float Health { get; private set; } = 1000f;

    public override void _Ready()
    {
        AddToGroup("damage_objective");
        CollisionLayer = 1;
        CollisionMask = 0;
        Health = MaxHealth;

        AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, 1.1f, 0),
            Shape = new BoxShape3D { Size = new Vector3(4.8f, 2.2f, 1.9f) }
        });

        AddChild(new MeshInstance3D
        {
            Position = new Vector3(0, 1.1f, 0),
            Mesh = new BoxMesh
            {
                Size = new Vector3(4.8f, 2.2f, 1.9f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.38f, 0.09f, 0.06f),
                    Metallic = 0.35f,
                    Roughness = 0.58f
                }
            }
        });
    }

    public void ApplyDamage(float amount, string damageKind)
    {
        if (Health <= 0 || amount <= 0) return;
        if (damageKind is "Melee" or "Fire") return; // VERIFIED immunity cases.

        Health = Math.Max(0, Health - amount);
        ProgressChanged?.Invoke(Health / MaxHealth);
        if (Health > 0) return;

        Destroyed?.Invoke(GlobalPosition);
        QueueFree();
    }
}

using Godot;

namespace Twr.Godot;

public partial class ExplosiveProjectileRuntime : Node3D
{
    public FirstPersonPlayer? SourcePlayer { get; set; }
    public float Damage { get; set; }
    public Vector3 Direction { get; set; } = Vector3.Forward;
    public string WeaponName { get; set; } = "Launcher";

    // APPROXIMATED: source documents blast-radius behavior and falloff but
    // provides no numeric blast radius or projectile velocity.
    public float BlastRadius { get; set; } = 8f;
    public float Speed { get; set; } = 34f;

    private double _life = 6.0;

    public override void _Ready()
    {
        Direction = Direction.Normalized();
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = 0.13f,
                Height = 0.26f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.22f, 0.24f, 0.20f),
                    EmissionEnabled = true,
                    Emission = new Color(0.28f, 0.16f, 0.05f)
                }
            }
        });
    }

    public override void _Process(double delta)
    {
        _life -= delta;
        var start = GlobalPosition;
        var end = start + Direction * Speed * (float)delta;
        var query = PhysicsRayQueryParameters3D.Create(start, end);
        if (SourcePlayer is not null)
            query.Exclude = new global::Godot.Collections.Array<Rid> { SourcePlayer.GetRid() };

        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count > 0)
        {
            Detonate(hit["position"].AsVector3());
            return;
        }

        GlobalPosition = end;
        if (_life <= 0)
            Detonate(GlobalPosition);
    }

    private void Detonate(Vector3 position)
    {
        foreach (var node in GetTree().GetNodesInGroup("infected"))
        {
            if (node is not InfectedAgent infected || !GodotObject.IsInstanceValid(infected))
                continue;
            var distance = infected.GlobalPosition.DistanceTo(position);
            if (distance > BlastRadius) continue;

            // APPROXIMATED linear falloff curve; RPG documentation confirms
            // distance-dependent explosion damage but gives no equation.
            var factor = Mathf.Lerp(1f, 0.45f, Math.Clamp(distance / BlastRadius, 0f, 1f));
            infected.ApplyDamage(Damage * factor, false, "Explosion");
        }

        foreach (var node in GetTree().GetNodesInGroup("damage_objective"))
        {
            if (node is not DamageObjectiveTarget tanker) continue;
            var distance = tanker.GlobalPosition.DistanceTo(position);
            if (distance <= BlastRadius)
                tanker.ApplyDamage(Damage, "Explosive");
        }

        QueueFree();
    }
}

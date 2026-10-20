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

    public const float CollisionRadius = .13f;
    // Applied by a physics sweep from the eye, not an unchecked teleport.
    public float LaunchOffset { get; set; }
    public bool Resolved { get; private set; }
    public bool CancelledInvalidOrigin { get; private set; }
    public Vector3 ResolutionPosition { get; private set; }
    private ProjectileSweep? _sweep;
    private bool _launchPending = true;
    private double _life = 6.0;

    public override void _Ready()
    {
        Direction = Direction.Normalized();
        _sweep = new ProjectileSweep(CollisionRadius);
        AddToGroup("twr-projectile");
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

    public override void _PhysicsProcess(double delta)
    {
        _life -= delta;
        if (Resolved || _sweep is null) return;
        const uint mask = 1u | 2u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        if (_launchPending)
        {
            _launchPending = false;
            var launch = _sweep.Move(GetWorld3D(), GlobalPosition,
                Direction * LaunchOffset, mask, SourcePlayer);
            if (!launch.Valid || launch.InitialOverlap) { CancelInvalidOrigin(); return; }
            GlobalPosition = launch.Centre;
            if (launch.Hit) { Detonate(launch.Centre); return; }
        }
        var step = _sweep.Move(GetWorld3D(), GlobalPosition,
            Direction * Speed * (float)delta, mask, SourcePlayer);
        if (!step.Valid || step.InitialOverlap) { CancelInvalidOrigin(); return; }
        GlobalPosition = step.Centre;
        if (step.Hit || _life <= 0) Detonate(step.Centre);
    }

    private void CancelInvalidOrigin()
    {
        Resolved = true;
        CancelledInvalidOrigin = true;
        ResolutionPosition = GlobalPosition;
        SetPhysicsProcess(false);
        QueueFree(); // No guessed exit point or damage through a solid.
    }

    public override void _ExitTree()
    {
        _sweep?.Dispose();
        _sweep = null;
    }

    private void Detonate(Vector3 position)
    {
        if (Resolved) return;
        Resolved = true;
        ResolutionPosition = position;
        SetPhysicsProcess(false);
        foreach (var node in GetTree().GetNodesInGroup("infected"))
        {
            if (node is not InfectedAgent infected || !GodotObject.IsInstanceValid(infected))
                continue;
            var distance = infected.GlobalPosition.DistanceTo(position);
            if (!Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), position, infected, BlastRadius, SourcePlayer))
                continue;

            // APPROXIMATED linear falloff curve; RPG documentation confirms
            // distance-dependent explosion damage but gives no equation.
            var factor = Mathf.Lerp(1f, 0.45f, Math.Clamp(distance / BlastRadius, 0f, 1f));
            infected.ApplyDamage(Damage * factor, false, "Explosion");
        }

        foreach (var node in GetTree().GetNodesInGroup("damage_objective"))
        {
            if (node is not DamageObjectiveTarget tanker) continue;
            var distance = tanker.GlobalPosition.DistanceTo(position);
            if (Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), position, tanker, BlastRadius, SourcePlayer))
                tanker.ApplyDamage(Damage, "Explosive");
        }

        QueueFree();
    }
}

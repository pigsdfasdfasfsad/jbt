using Godot;

namespace Twr.Godot;

public partial class SporeCloudRuntime : Node3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public float Radius { get; set; } = 20f;
    public float TickDamage { get; set; } = 5f;
    public double DurationSeconds { get; set; } = 4.0; // APPROXIMATED duration; source says short duration only.

    private double _tickTimer;

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Position = new Vector3(0, 0.15f, 0),
            Mesh = new CylinderMesh
            {
                TopRadius = Radius,
                BottomRadius = Radius,
                Height = 0.3f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.30f, 0.50f, 0.18f, 0.22f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    Roughness = 1f
                }
            }
        });
    }

    public override void _Process(double delta)
    {
        DurationSeconds -= delta;
        _tickTimer -= delta;

        if (_tickTimer <= 0)
        {
            _tickTimer = 0.5; // VERIFIED spore gas tick interval.
            if (Target is not null && Runtime?.Player?.IsAlive == true &&
                !Runtime.Player.GasMaskActive &&
                Target.GlobalPosition.DistanceTo(GlobalPosition) <= Radius)
            {
                Runtime.DamagePlayer(TickDamage, "Spore gas", true);
            }
        }

        if (DurationSeconds <= 0)
            QueueFree();
    }
}

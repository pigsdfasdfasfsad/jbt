using Godot;

namespace Twr.Godot;

public partial class FortificationActor : Node3D
{
    public RuntimeFortificationDefinition Definition { get; set; } =
        new("Unknown", 1, 1, null, null, null);

    private double _timer;
    private int _remainingShots = 21;

    public override void _Ready()
    {
        var size = Definition.Radius.HasValue
            ? new Vector3(0.75f, 0.35f, 0.75f)
            : Definition.Damage.HasValue
                ? new Vector3(0.75f, 1.2f, 1.4f)
                : new Vector3(2.8f, 0.35f, 0.55f);

        AddChild(new MeshInstance3D
        {
            Position = new Vector3(0, size.Y * 0.5f, 0),
            Mesh = new BoxMesh
            {
                Size = size,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = Definition.Damage.HasValue
                        ? new Color(0.28f, 0.26f, 0.20f)
                        : new Color(0.34f, 0.34f, 0.34f),
                    Roughness = 0.9f
                }
            }
        });

        AddChild(new Label3D
        {
            Text = Definition.Name,
            Position = new Vector3(0, 1.7f, 0),
            FontSize = 30,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        });
    }

    public override void _Process(double delta)
    {
        _timer = Math.Max(0, _timer - delta);

        if (Definition.Damage.HasValue && Definition.Radius.HasValue)
            TickAreaDevice();
        else if (Definition.Damage.HasValue)
            TickMountedDevice();
        else
            TickSlowTrap();
    }

    private void TickAreaDevice()
    {
        if (_timer > 0) return;

        var trigger = FindNearest(2.0f);
        if (trigger is null) return;

        var radius = Definition.Radius!.Value;
        foreach (var infected in EnumerateInfected())
            if (GlobalPosition.DistanceTo(infected.GlobalPosition) <= radius)
                infected.ApplyDamage(Definition.Damage!.Value, false, "Explosive");

        foreach (var node in GetTree().GetNodesInGroup("damage_objective"))
            if (node is DamageObjectiveTarget tanker &&
                GlobalPosition.DistanceTo(tanker.GlobalPosition) <= radius)
                tanker.ApplyDamage(Definition.Damage!.Value, "Explosive");

        QueueFree();
    }

    private void TickMountedDevice()
    {
        if (_timer > 0 || _remainingShots <= 0) return;
        var target = FindNearest(55f);
        if (target is null) return;

        target.ApplyDamage(Definition.Damage!.Value);
        _remainingShots--;

        // Source animation timeline totals 0.225s; whether retail fire cadence
        // is exactly equal to that animation total is not recovered.
        _timer = 0.225;

        if (_remainingShots <= 0)
            QueueFree();
    }

    private void TickSlowTrap()
    {
        if (_timer > 0) return;

        var touched = false;
        foreach (var infected in EnumerateInfected())
        {
            if (GlobalPosition.DistanceTo(infected.GlobalPosition) > 2.5f)
                continue;

            // APPROXIMATED: source confirms slowing + damage, but exact amount
            // and slow factor/rate are not recovered.
            infected.ApplyDamage(5f, false, "SlowTrap");
            infected.ApplySlow(0.55f, 0.65);
            touched = true;
        }

        if (touched)
            _timer = 0.5;
    }

    private InfectedAgent? FindNearest(float maxDistance)
    {
        InfectedAgent? nearest = null;
        var nearestDistance = maxDistance;
        foreach (var infected in EnumerateInfected())
        {
            var distance = GlobalPosition.DistanceTo(infected.GlobalPosition);
            if (distance >= nearestDistance) continue;
            nearest = infected;
            nearestDistance = distance;
        }
        return nearest;
    }

    private IEnumerable<InfectedAgent> EnumerateInfected()
    {
        foreach (var node in GetTree().GetNodesInGroup("infected"))
            if (node is InfectedAgent infected && GodotObject.IsInstanceValid(infected))
                yield return infected;
    }
}

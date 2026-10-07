using Godot;

namespace Twr.Godot;

public partial class FortificationController : Node
{
    public FirstPersonPlayer? Player { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public Action<string>? StatusChanged { get; set; }

    private readonly List<FortificationActor> _deployed = [];
    private IReadOnlyList<RuntimeFortificationDefinition> _catalog = [];
    private bool _hammerMode;
    private int _selectedIndex;
    private int _swingsCompleted;
    private double _swingCooldown;

    // APPROXIMATED: exact base hammer swing cadence is not recovered.
    private const double SwingSeconds = 0.35;

    public override void _Ready()
    {
        _catalog = FortificationCatalogRuntime.Load();
        PublishStatus();
    }

    public override void _Process(double delta)
    {
        _swingCooldown = Math.Max(0, _swingCooldown - delta);

        if (!_hammerMode || !Input.IsMouseButtonPressed(MouseButton.Left))
            return;

        TrySwing();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F)
        {
            _hammerMode = !_hammerMode;
            _swingsCompleted = 0;
            Player?.SetHammerMode(_hammerMode);
            PublishStatus();
            return;
        }

        if (!_hammerMode || @event is not InputEventMouseButton mouse || !mouse.Pressed)
            return;

        if (mouse.ButtonIndex == MouseButton.WheelUp)
            Cycle(-1);
        else if (mouse.ButtonIndex == MouseButton.WheelDown)
            Cycle(1);
    }

    public void ClearDeployed()
    {
        foreach (var fort in _deployed.ToArray())
            if (GodotObject.IsInstanceValid(fort))
                fort.QueueFree();
        _deployed.Clear();
    }

    private void TrySwing()
    {
        if (_swingCooldown > 0 || Runtime?.Player is null || Player is null)
            return;

        var available = AvailableDefinitions();
        if (available.Count == 0)
        {
            StatusChanged?.Invoke("HAMMER | NO FORTIFICATIONS");
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, available.Count - 1);
        var definition = available[_selectedIndex];
        if (Runtime.Player.Inventory.GetValueOrDefault(definition.Name) <= 0)
            return;

        _swingCooldown = SwingSeconds;
        _swingsCompleted++;
        StatusChanged?.Invoke($"HAMMER | {definition.Name} | {_swingsCompleted}/{definition.Swings}");

        if (_swingsCompleted < Math.Max(1, definition.Swings))
            return;

        _swingsCompleted = 0;
        if (!Runtime.ConsumeItem(definition.Name))
            return;

        var forward = Player.AimDirection;
        forward.Y = 0;
        if (forward.LengthSquared() < 0.001f)
            forward = -Player.GlobalTransform.Basis.Z;
        forward = forward.Normalized();

        var actor = new FortificationActor
        {
            Name = $"Deployed_{definition.Name}_{_deployed.Count}",
            Definition = definition,
            GlobalPosition = Player.GlobalPosition + forward * 3.0f + Vector3.Down * 0.8f
        };
        _deployed.Add(actor);
        AddChild(actor);
        PublishStatus();
    }

    private void Cycle(int direction)
    {
        var available = AvailableDefinitions();
        if (available.Count == 0)
        {
            _selectedIndex = 0;
            PublishStatus();
            return;
        }

        _selectedIndex = (_selectedIndex + direction) % available.Count;
        if (_selectedIndex < 0) _selectedIndex += available.Count;
        _swingsCompleted = 0;
        PublishStatus();
    }

    private List<RuntimeFortificationDefinition> AvailableDefinitions()
    {
        if (Runtime?.Player is null)
            return [];

        return _catalog
            .Where(x => Runtime.Player.Inventory.GetValueOrDefault(x.Name) > 0)
            .ToList();
    }

    private void PublishStatus()
    {
        if (!_hammerMode)
        {
            StatusChanged?.Invoke("F: HAMMER");
            return;
        }

        var available = AvailableDefinitions();
        if (available.Count == 0)
        {
            StatusChanged?.Invoke("HAMMER | NO FORTIFICATIONS");
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, available.Count - 1);
        var selected = available[_selectedIndex];
        var count = Runtime?.Player?.Inventory.GetValueOrDefault(selected.Name) ?? 0;
        StatusChanged?.Invoke($"HAMMER | {selected.Name} x{count} | WHEEL SELECT | HOLD LMB BUILD");
    }
}

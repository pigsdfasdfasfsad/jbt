using Godot;

namespace Twr.Godot;

public partial class ObjectiveRuntime : Node3D
{
    public string ObjectiveId { get; set; } = "";
    public string Family { get; set; } = "Radio";
    public FirstPersonPlayer? Player { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public Action<ObjectiveRuntime>? Completed { get; set; }
    public Action<string>? StatusChanged { get; set; }

    private readonly List<Node3D> _items = [];
    private CharacterBody3D? _escort;
    private Node3D? _escortDestination;
    private int _required;
    private int _deposited;
    private bool _carrying;
    private bool _completed;
    private bool _interactWasDown;
    private double _secureProgress;
    private float _escortStartDistance;

    // APPROXIMATED interaction pacing: source documents the mechanics and
    // contribution thresholds but not solo completion seconds or escort speed.
    private const double SecureSeconds = 15.0;
    private const float EscortSpeed = 2.4f;

    public override void _Ready()
    {
        BuildObjective();
        PublishStatus();
    }

    public override void _Process(double delta)
    {
        if (_completed || Player is null)
            return;

        var interactDown = Input.IsKeyPressed(Key.E);
        var interactPressed = interactDown && !_interactWasDown;
        _interactWasDown = interactDown;

        switch (Family)
        {
            case "Radio":
            case "Unpack":
                TickSecure(delta, interactDown);
                break;
            case "Load":
            case "Repair":
                if (interactPressed) TickFillInteract();
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_completed || Family != "Escort" || Player is null || _escort is null || _escortDestination is null)
            return;

        var remaining = _escortDestination.GlobalPosition - _escort.GlobalPosition;
        remaining.Y = 0;
        if (remaining.Length() <= 1.5f)
        {
            Complete();
            return;
        }

        if (Player.GlobalPosition.DistanceTo(_escort.GlobalPosition) <= 8f)
        {
            var direction = remaining.Normalized();
            _escort.Velocity = new Vector3(direction.X * EscortSpeed, _escort.Velocity.Y, direction.Z * EscortSpeed);
            if (!_escort.IsOnFloor())
                _escort.Velocity += Vector3.Down * 20f * (float)delta;
            _escort.MoveAndSlide();
        }
        else
        {
            _escort.Velocity = new Vector3(0, _escort.Velocity.Y, 0);
        }

        PublishStatus();
    }

    private void BuildObjective()
    {
        AddChild(MakeLabel(Family.ToUpperInvariant(), new Vector3(0, 2.2f, 0)));

        switch (Family)
        {
            case "Radio":
            case "Unpack":
                AddChild(MakeRing());
                AddChild(MakeBox("SecureDevice", new Vector3(0, 0.55f, 0), new Vector3(0.9f, 1.1f, 0.9f), new Color(0.18f, 0.55f, 0.72f)));
                break;
            case "Load":
                BuildFillItems([
                    "Generator", "Tool Box", "Propane Tank A", "Propane Tank B",
                    "Water Jug A", "Water Jug B", "Water Jug C",
                    "Can Package A", "Can Package B", "Can Package C"
                ]);
                break;
            case "Repair":
                BuildFillItems([
                    "Spark Plug A", "Spark Plug B", "Spark Plug C", "Spark Plug D",
                    "Wheel A", "Wheel B",
                    "Jerry Can A", "Jerry Can B", "Jerry Can C", "Jerry Can D"
                ]);
                break;
            case "Escort":
                BuildEscort();
                break;
        }
    }

    private void BuildFillItems(string[] names)
    {
        _required = names.Length;
        AddChild(MakeBox("DepositTarget", new Vector3(0, 0.75f, 0), new Vector3(2.4f, 1.5f, 2.4f), new Color(0.18f, 0.48f, 0.22f)));

        for (var i = 0; i < names.Length; i++)
        {
            var angle = Mathf.Tau * i / names.Length;
            var radius = 6f + (i % 3) * 1.25f;
            var item = MakeBox(names[i], new Vector3(Mathf.Cos(angle) * radius, 0.35f, Mathf.Sin(angle) * radius), new Vector3(0.55f, 0.7f, 0.55f), new Color(0.74f, 0.58f, 0.18f));
            _items.Add(item);
            AddChild(item);
        }
    }

    private void BuildEscort()
    {
        _escort = new CharacterBody3D
        {
            Name = "EscortSurvivor",
            Position = Vector3.Zero,
            CollisionLayer = 1,
            CollisionMask = 1
        };
        _escort.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, 0.9f, 0),
            Shape = new CapsuleShape3D { Radius = 0.42f, Height = 1.8f }
        });
        _escort.AddChild(new MeshInstance3D
        {
            Position = new Vector3(0, 0.9f, 0),
            Mesh = new CapsuleMesh
            {
                Radius = 0.42f,
                Height = 1.8f,
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.23f, 0.45f, 0.72f) }
            }
        });
        AddChild(_escort);

        _escortDestination = MakeBox("EscortDestination", new Vector3(0, 0.1f, -24f), new Vector3(3f, 0.2f, 3f), new Color(0.15f, 0.65f, 0.28f));
        AddChild(_escortDestination);
        _escortStartDistance = _escort.GlobalPosition.DistanceTo(_escortDestination.GlobalPosition);
    }

    private void TickSecure(double delta, bool interactDown)
    {
        if (Player is null)
            return;

        if (Player.GlobalPosition.DistanceTo(GlobalPosition) <= 3.25f && interactDown)
        {
            _secureProgress = Math.Min(1.0, _secureProgress + delta / SecureSeconds);
            PublishStatus();
            if (_secureProgress >= 1.0)
                Complete();
        }
    }

    private void TickFillInteract()
    {
        if (Player is null)
            return;

        if (_carrying && Player.GlobalPosition.DistanceTo(GlobalPosition) <= 3f)
        {
            _carrying = false;
            _deposited++;
            PublishStatus();
            if (_deposited >= _required)
                Complete();
            return;
        }

        if (_carrying)
            return;

        Node3D? nearest = null;
        var nearestDistance = float.MaxValue;
        foreach (var item in _items)
        {
            if (!GodotObject.IsInstanceValid(item))
                continue;
            var distance = Player.GlobalPosition.DistanceTo(item.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearest = item;
                nearestDistance = distance;
            }
        }

        if (nearest is not null && nearestDistance <= 2.5f)
        {
            _items.Remove(nearest);
            nearest.QueueFree();
            _carrying = true;
            PublishStatus();
        }
    }

    private void Complete()
    {
        if (_completed)
            return;

        _completed = true;
        Runtime?.CompleteObjective(ObjectiveId, Family);
        StatusChanged?.Invoke($"{Family.ToUpperInvariant()} COMPLETE");
        Completed?.Invoke(this);
    }

    private void PublishStatus()
    {
        if (_completed)
            return;

        var status = Family switch
        {
            "Radio" or "Unpack" => $"{Family.ToUpperInvariant()}  HOLD E IN RING  {Math.Round(_secureProgress * 100):0}%",
            "Load" or "Repair" => $"{Family.ToUpperInvariant()}  {_deposited}/{_required} INSERTED" + (_carrying ? "  |  CARRYING ITEM" : ""),
            "Escort" when _escort is not null && _escortDestination is not null && _escortStartDistance > 0 =>
                $"ESCORT  {Math.Clamp((1f - _escort.GlobalPosition.DistanceTo(_escortDestination.GlobalPosition) / _escortStartDistance) * 100f, 0f, 100f):0}%",
            _ => Family.ToUpperInvariant()
        };
        StatusChanged?.Invoke(status);
    }

    private static MeshInstance3D MakeRing() => new()
    {
        Position = new Vector3(0, 0.05f, 0),
        Mesh = new CylinderMesh
        {
            TopRadius = 3.0f,
            BottomRadius = 3.0f,
            Height = 0.08f,
            Material = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.10f, 0.75f, 0.90f, 0.45f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            }
        }
    };

    private static MeshInstance3D MakeBox(string name, Vector3 position, Vector3 size, Color color) => new()
    {
        Name = name,
        Position = position,
        Mesh = new BoxMesh
        {
            Size = size,
            Material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.9f }
        }
    };

    private static Label3D MakeLabel(string text, Vector3 position) => new()
    {
        Text = text,
        Position = position,
        FontSize = 40,
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
    };
}

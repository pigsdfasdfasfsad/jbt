using Godot;

namespace Twr.Godot;

public partial class ObjectiveRuntime : Node3D
{
    public string ObjectiveId { get; set; } = "";
    public string Family { get; set; } = "Radio";
    public bool RequireSameFloorSecure84 { get; set; }
    public bool UseFallbackEscortNavigation89 { get; set; }
    public Pass73GroundNavigationRuntime? EscortGroundNavigator89 { get; set; }
    public int EscortSafeRoutePlans89 { get; private set; }
    public int EscortBlockedRoutePlans89 { get; private set; }
    public int EscortSafeSteps89 { get; private set; }
    public int EscortBlockedSteps89 { get; private set; }
    public bool EscortDestinationAdjusted89 { get; private set; }
    public int EscortGroundedDestinations90 { get; private set; }
    public int EscortRejectedDestinations90 { get; private set; }
    public int EscortRouteInvalidations91 { get; private set; }
    private bool _escortRouteInvalidated91;
    public bool EscortGroundedCompletion90 { get; private set; }
    private Vector3[] _escortRoute89 = [];
    private int _escortRouteIndex89;
    private double _escortRepathSeconds89;
    private bool _escortDestinationResolved89;
    public FirstPersonPlayer? Player { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public Action<ObjectiveRuntime>? Completed { get; set; }
    public Action<string>? StatusChanged { get; set; }

    private readonly List<Node3D> _items = [];
    private readonly RandomNumberGenerator _rng = new();
    private CharacterBody3D? _escort;
    private DamageObjectiveTarget? _damageTarget;
    private Node3D? _escortDestination;
    private int _required;
    private int _deposited;
    private bool _carrying;
    private bool _completed;
    private bool _interactWasDown;
    private double _secureProgress;
    private float _escortStartDistance;
    private float _escortSpeed;
    private bool _escortInjured;

    // FITTED reconstruction from recovered objective directive: solo secure fill
    // uses 1/60 per second; the original retail source did not expose the fill rate.
    private const double SecureSeconds = 60.0;
    // RECOVERED objective reconstruction values.
    private const float SecureRadius = 14.0f;
    private const float SecureYBand = 8.0f;
    // RECOVERED reconstruction values from the objective directive.
    private const float EscortSpeedHealthy = 10f;
    private const float EscortSpeedInjured = 7f;
    private const float EscortProximity = 45f;

    public override void _Ready()
    {
        _rng.Randomize();
        BuildObjective();
        PublishStatus();
    }

    public override void _Process(double delta)
    {
        if (_completed || Player is null || Runtime?.Player?.IsAlive != true)
            return;

        var interactDown = Input.IsKeyPressed(Key.E);
        var interactPressed = interactDown && !_interactWasDown;
        _interactWasDown = interactDown;

        switch (Family)
        {
            case "Radio":
            case "Unpack":
                TickSecure(delta);
                break;
            case "Load":
            case "Repair":
                if (interactPressed) TickFillInteract();
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_completed || Family != "Escort" || Player is null ||
            Runtime?.Player?.IsAlive != true ||
            _escort is null || _escortDestination is null)
            return;

        if (UseFallbackEscortNavigation89)
        {
            TickFallbackEscort89(delta);
            PublishStatus();
            return;
        }

        var remaining = _escortDestination.GlobalPosition - _escort.GlobalPosition;
        remaining.Y = 0;
        if (remaining.Length() <= 1.5f)
        {
            Complete();
            return;
        }

        if (Player.GlobalPosition.DistanceTo(_escort.GlobalPosition) <= EscortProximity)
        {
            var direction = remaining.Normalized();
            _escort.Velocity = new Vector3(direction.X * _escortSpeed, _escort.Velocity.Y, direction.Z * _escortSpeed);
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


    /// <summary>
    /// Pass89: same production Escort survivor body, no scripted motion or
    /// teleports. The fallback-only Pass73 graph supplies collision-checked
    /// waypoints. Every short physical step is rechecked for floor support,
    /// so a stale or disconnected graph causes a safe stop, not a wall clip.
    /// </summary>
    private void TickFallbackEscort89(double delta)
    {
        if (_escort is null || _escortDestination is null || Player is null)
            return;
        var nav=EscortGroundNavigator89;
        if (nav is null || !GodotObject.IsInstanceValid(nav) ||
            !nav.IsGraphReady)
        {
            StopEscortHorizontal89(delta);
            return;
        }

        var actor=_escort;
        var from=actor.GlobalPosition;
        var target=_escortDestination.GlobalPosition;
        // Source-derived and approximate marker positions are NOT
        // guaranteed to equal the NPC's feet-level collision plane.
        // Do not select any route until this real CharacterBody3D has
        // first grounded using its own MoveAndSlide physics.
        if (!actor.IsOnFloor())
        {
            StopEscortHorizontal89(delta);
            return;
        }
        var exclusion90=new global::Godot.Collections.Array<Rid>
        {
            actor.GetRid(),Player.GetRid()
        };
        if (!_escortDestinationResolved89)
        {
            // Reject impossible fixed -24Z locations on approximate maps.
            // Keep the original point whenever it IS safely reachable.
            var options=new[]
            {
                target,
                from+new Vector3(-12,0,-20),
                from+new Vector3(12,0,-20),
                from+new Vector3(-20,0,0),
                from+new Vector3(20,0,0),
                from+new Vector3(0,0,20),
                from+new Vector3(0,0,-16),
                from+new Vector3(-12,0,-12),
                from+new Vector3(12,0,-12)
            };
            foreach(var option in options)
            {
                var grounded=Pass90EscortArrivalSafety.TryGroundDestination(
                    GetWorld3D(),option,exclusion90);
                if (!grounded.HasValue)
                {
                    EscortRejectedDestinations90++;
                    continue;
                }
                var destinationFeet=grounded.Value;
                var candidate=nav.GetVerifiedEscortRoute89(
                    from,destinationFeet,actor.GetRid());
                if(candidate.Length == 0)
                {
                    EscortRejectedDestinations90++;
                    continue;
                }
                // Only the actual local marker or a collision-safe fallback
                // changes. A vertical marker correction never counts as a
                // new retail-original objective destination.
                _escortDestination.GlobalPosition=destinationFeet;
                _escortDestinationResolved89=true;
                EscortDestinationAdjusted89 = option!=target;
                EscortGroundedDestinations90++;
                _escortStartDistance=from.DistanceTo(destinationFeet);
                _escortRoute89=candidate;
                _escortRouteIndex89=0;
                _escortRepathSeconds89=.8;
                EscortSafeRoutePlans89++;
                break;
            }
            if (!_escortDestinationResolved89)
            {
                EscortBlockedRoutePlans89++;
                StopEscortHorizontal89(delta);
                return;
            }
        }

        target=_escortDestination.GlobalPosition;
        var toGoal=target-from;
        toGoal.Y=0;
        if (toGoal.Length() <= 1.5f)
        {
            // Pass90: close in XZ is insufficient when the goal lies
            // behind a wall, on another floor, or the player abandoned
            // the survivor. The goal was physically grounded above.
            if (Pass90EscortArrivalSafety.CanComplete(
                    GetWorld3D(),from,target,actor.IsOnFloor(),
                    Player.GlobalPosition.DistanceTo(from)<=EscortProximity,
                    exclusion90))
            {
                EscortGroundedCompletion90=true;
                StopEscortHorizontal89(delta);
                Complete();
            }
            else
                StopEscortHorizontal89(delta);
            return;
        }

        if (Player.GlobalPosition.DistanceTo(from)>EscortProximity)
        {
            StopEscortHorizontal89(delta);
            return;
        }

        _escortRepathSeconds89-=delta;
        // Pass91: Pass89's 0.8-second unconditional replan caused route
        // oscillation in Bypass around static shell walls: the survivor
        // repeatedly returned to the nearest graph vertex rather than
        // making progress along an already verified corridor. Preserve
        // a completed, collision-audited waypoint chain until consumed
        // or invalidated by a REAL collision probe. This keeps real
        // physical step safety while allowing long detours to finish.
        if (_escortRouteInvalidated91 ||
            _escortRouteIndex89 >= _escortRoute89.Length)
        {
            var newRoute=nav.GetVerifiedEscortRoute89(
                from,target,actor.GetRid());
            _escortRepathSeconds89=.80;
            if (newRoute.Length != 0)
            {
                _escortRoute89=newRoute;
                _escortRouteIndex89=0;
                _escortRouteInvalidated91=false;
                EscortSafeRoutePlans89++;
            }
            else
            {
                EscortBlockedRoutePlans89++;
                // If the last short probe was a false obstruction, never
                // move through it blindly. Stop/recheck next frame.
                // A still-supported non-exhausted route is retained, but
                // ONLY the immediate next physical step can authorize
                // movement (CanTravel below).
                if (_escortRouteIndex89 >= _escortRoute89.Length)
                {
                    StopEscortHorizontal89(delta);
                    return;
                }
            }
        }

        var desired=Vector3.Zero;
        while(_escortRouteIndex89 < _escortRoute89.Length)
        {
            desired=_escortRoute89[_escortRouteIndex89]-from;
            desired.Y=0;
            if(desired.LengthSquared() > .60f*.60f) break;
            _escortRouteIndex89++;
        }
        if(_escortRouteIndex89 >= _escortRoute89.Length)
        {
            StopEscortHorizontal89(delta);
            return;
        }

        var direction=desired.Normalized();
        var exclude=new global::Godot.Collections.Array<Rid>
        {
            actor.GetRid(),Player.GetRid()
        };
        if (!Pass89EscortSurfaceSafety.CanTravel(
                GetWorld3D(),from,from+direction*.55f,exclude))
        {
            EscortBlockedSteps89++;
            EscortRouteInvalidations91++;
            _escortRouteInvalidated91=true;
            _escortRepathSeconds89=0; // next frame re-audits world graph
            StopEscortHorizontal89(delta);
            return;
        }

        EscortSafeSteps89++;
        var velocity=actor.Velocity;
        velocity.X=direction.X*_escortSpeed;
        velocity.Z=direction.Z*_escortSpeed;
        velocity.Y=actor.IsOnFloor()
            ? 0f : velocity.Y-20f*(float)delta;
        actor.Velocity=velocity;
        actor.MoveAndSlide();
    }

    private void StopEscortHorizontal89(double delta)
    {
        if (_escort is null) return;
        var velocity=_escort.Velocity;
        velocity.X=0;
        velocity.Z=0;
        velocity.Y=_escort.IsOnFloor()
            ? 0f : velocity.Y-20f*(float)delta;
        _escort.Velocity=velocity;
        _escort.MoveAndSlide();
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
                BuildRepair();
                break;
            case "Escort":
                BuildEscort();
                break;
            case "Damage":
                BuildDamageTarget();
                break;
        }
    }

    private void BuildRepair()
    {
        // RECOVERED reconstruction variants:
        // Small Silverado = 4 plugs + 2 wheels + 4 cans (10 total).
        // Large Tundra    = 4 plugs + 2 wheels + 6 cans (12 total).
        var large=_rng.RandiRange(1,2)==2;
        var items=new List<string>
        {
            "Spark Plug A","Spark Plug B","Spark Plug C","Spark Plug D",
            "Wheel A","Wheel B",
            "Jerry Can A","Jerry Can B","Jerry Can C","Jerry Can D"
        };
        if(large){items.Add("Jerry Can E");items.Add("Jerry Can F");}
        SetMeta("RepairVariant",large ? "Large" : "Small");
        BuildFillItems(items.ToArray());
    }
    private void BuildDamageTarget()
    {
        _damageTarget = new DamageObjectiveTarget
        {
            Name = "FuelTanker",
            Position = Vector3.Zero
        };
        _damageTarget.ProgressChanged = _ => PublishStatus();
        _damageTarget.Destroyed = position =>
        {
            // DERIVED reconstruction constants from the objective directive;
            // neither blast radius nor blast damage is documented retail data.
            const float blastRadius=60f;
            const float blastDamage=400f;
            foreach(var node in GetTree().GetNodesInGroup("infected"))
            {
                if(node is InfectedAgent infected && GodotObject.IsInstanceValid(infected) &&
                   infected.GlobalPosition.DistanceTo(position)<=blastRadius)
                    infected.ApplyDamage(blastDamage,false,"ObjectiveExplosion");
            }
            Complete();
        };
        AddChild(_damageTarget);
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
        _escortInjured=_rng.RandiRange(1,2)==1;
        _escortSpeed=_escortInjured ? EscortSpeedInjured : EscortSpeedHealthy;
        SetMeta("EscortVariant",_escortInjured ? "Injured" : "Healthy");

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
                Material = new StandardMaterial3D
                {
                    AlbedoColor = _escortInjured
                        ? new Color(0.48f,0.24f,0.22f)
                        : new Color(0.23f,0.45f,0.72f)
                }
            }
        });
        AddChild(_escort);

        _escortDestination = MakeBox("EscortDestination", new Vector3(0, 0.1f, -24f), new Vector3(3f, 0.2f, 3f), new Color(0.15f, 0.65f, 0.28f));
        AddChild(_escortDestination);
        _escortStartDistance = _escort.GlobalPosition.DistanceTo(_escortDestination.GlobalPosition);
    }

    private void TickSecure(double delta)
    {
        if (Player is null)
            return;

        var d=Player.GlobalPosition-GlobalPosition;
        var horizontal=new Vector2(d.X,d.Z).Length();
        if (Math.Abs(d.Y) < SecureYBand && horizontal <= SecureRadius &&
            (!RequireSameFloorSecure84 ||
             Pass84SecureZoneFloorSafety.CanAccumulate(
                 Player.GlobalPosition, GlobalPosition)))
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

        if (_carrying &&
            Pass76InteractionSafety.CanReach(Player, GlobalPosition, 3f))
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

        if (nearest is not null && nearestDistance <= 2.5f &&
            Pass76InteractionSafety.CanReach(Player, nearest.GlobalPosition, 2.5f))
        {
            _items.Remove(nearest);
            nearest.QueueFree();
            _carrying = true;
            PublishStatus();
        }
    }

    private void Complete()
    {
        if (_completed || Runtime?.Player?.IsAlive != true)
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
            "Radio" or "Unpack" => $"{Family.ToUpperInvariant()}  STAY IN SECURE ZONE  {Math.Round(_secureProgress * 100):0}%",
            "Load" or "Repair" => $"{Family.ToUpperInvariant()}  {_deposited}/{_required} INSERTED" + (_carrying ? "  |  CARRYING ITEM" : ""),
            "Escort" when _escort is not null && _escortDestination is not null && _escortStartDistance > 0 =>
                $"ESCORT {(_escortInjured ? "INJURED" : "HEALTHY")}  {Math.Clamp((1f - _escort.GlobalPosition.DistanceTo(_escortDestination.GlobalPosition) / _escortStartDistance) * 100f, 0f, 100f):0}%",
            "Damage" when _damageTarget is not null =>
                $"DAMAGE TANKER  {Math.Ceiling((_damageTarget.Health / _damageTarget.MaxHealth) * 100f):0}%",
            _ => Family.ToUpperInvariant()
        };
        StatusChanged?.Invoke(status);
    }

    private static MeshInstance3D MakeRing() => new()
    {
        Position = new Vector3(0, 0.05f, 0),
        Mesh = new CylinderMesh
        {
            TopRadius = SecureRadius,
            BottomRadius = SecureRadius,
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

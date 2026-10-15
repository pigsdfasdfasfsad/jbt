using Godot;

namespace Twr.Godot;

public partial class FortificationActor : Node3D
{
    public RuntimeFortificationDefinition Definition { get; set; } =
        new("Unknown", 1, 1, null, null, null);
    public float DamageMultiplier {get;set;}=1f;
    public float RangeMultiplier {get;set;}=1f;
    public FirstPersonPlayer? Player {get;set;}
    public LocalSessionNode? Runtime {get;set;}

    private double _timer;
    private int _remainingShots=21;
    private bool _mounted;
    private bool _interactWasDown;
    private bool _qWasDown;
    private bool _toggleAim;
    public bool Mounted76 => _mounted;
    public int ShotsFired76 { get; private set; }

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
            Text = Definition.Name=="50 Cal" ? "50 CAL [E]" : Definition.Name,
            Position = new Vector3(0, 1.7f, 0),
            FontSize = 30,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        });
    }

    public override void _Process(double delta)
    {
        if (Player?.GameplayControlsEnabled != true)
        {
            if (_mounted && Player is not null && GodotObject.IsInstanceValid(Player))
                SetMounted(false);
            return;
        }
        if (IsQueuedForDeletion()) return;
        if (Runtime?.Player?.IsAlive != true)
        {
            if (_mounted) SetMounted(false);
            return;
        }
        _timer=Math.Max(0,_timer-delta);

        if(Definition.Name=="50 Cal")
        {
            TickMounted50Cal();
            return;
        }

        if (Definition.Damage.HasValue && Definition.Radius.HasValue)
            TickAreaDevice();
        else
            TickSlowTrap();
    }

    public override void _ExitTree()
    {
        if(_mounted && Player is not null && GodotObject.IsInstanceValid(Player))
            Player.SetMountedMode(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Player?.GameplayControlsEnabled != true) return;
        // The first actual left click must fire immediately. Holding LMB
        // still uses the existing 0.225-second automatic _Process cadence;
        // the common firing cooldown prevents a double shot in one frame.
        if (!_mounted || Runtime?.Player?.IsAlive != true ||
            @event is not InputEventMouseButton click ||
            !click.Pressed || click.ButtonIndex != MouseButton.Left)
            return;
        Fire50Cal();
    }

    private void TickMounted50Cal()
    {
        if(Player is null || Runtime?.Player?.IsAlive != true) return;

        var eDown=Input.IsKeyPressed(Key.E);
        var ePressed=eDown && !_interactWasDown;
        _interactWasDown=eDown;

        if(ePressed)
        {
            if(_mounted)SetMounted(false);
            else if(Pass76InteractionSafety.CanReach(
                Player, GlobalPosition, 2.8f))SetMounted(true);
        }

        if(!_mounted)return;

        var qDown=Input.IsKeyPressed(Key.Q);
        var qPressed=qDown && !_qWasDown;
        _qWasDown=qDown;
        if(qPressed)_toggleAim=!_toggleAim;
        Player.SetMountedAim(_toggleAim || Input.IsMouseButtonPressed(MouseButton.Right));

        if(_remainingShots>0 && _timer<=0 && Input.IsMouseButtonPressed(MouseButton.Left))
            Fire50Cal();
    }

    private void SetMounted(bool mounted)
    {
        _mounted=mounted;
        _toggleAim=false;
        Player?.SetMountedMode(mounted);
        if(mounted && Player is not null)
        {
            var stand=GlobalPosition-GlobalTransform.Basis.Z*1.15f;
            stand.Y=Player.GlobalPosition.Y;
            Player.GlobalPosition=stand;
            Player.SetMountedAim(false);
        }
    }

    private void Fire50Cal()
    {
        if (Player?.GameplayControlsEnabled != true) return;
        if(Player is null || Runtime?.Player?.IsAlive != true ||
            Definition.Damage is null || !_mounted || _remainingShots <= 0 ||
            _timer > 0)
            return;
        _timer=0.225; // RECOVERED animation timeline: 0.075s + 0.15s.
        _remainingShots--;
        ShotsFired76++;

        var origin=GlobalPosition+Vector3.Up*1.35f;
        var direction=Player.AimDirection.Normalized();
        var remaining=1000f;
        var exclude=new global::Godot.Collections.Array<Rid>{Player.GetRid()};

        for(var penetration=0;penetration<=Math.Max(0,Definition.MaxPen ?? 0);penetration++)
        {
            using var query=PhysicsRayQueryParameters3D.Create(origin,origin+direction*remaining);
            // Pass76: same world + infected + source-server layer mask as
            // actual hand-held weapons. Default Godot layer 1 misses layer-2
            // infected CharacterBody collision completely.
            query.CollisionMask =
                1u | 2u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
            query.Exclude=exclude;
            var hit=GetWorld3D().DirectSpaceState.IntersectRay(query);
            if(hit.Count<=0)return;

            var hitPosition=hit["position"].AsVector3();
            var collider=hit["collider"].AsGodotObject();
            if(collider is DamageObjectiveTarget tanker)
            {
                tanker.ApplyDamage(Definition.Damage.Value*DamageMultiplier,"Bullet");
                return;
            }

            if(collider is not InfectedAgent infected)return;

            var localHit=infected.ToLocal(hitPosition);
            var headshot=localHit.Y>=0.45f; // same APPROXIMATED capsule boundary as handheld guns.
            var damageMultiplier=DamageMultiplier;
            if(Runtime?.HasPerk("Heavy Hitter")==true && localHit.Y>=-0.35f)
                damageMultiplier*=1.2f;
            infected.ApplyDamage(Definition.Damage.Value*damageMultiplier,headshot,"Bullet");

            if(collider is not CollisionObject3D collision || penetration>=Definition.MaxPen)return;
            exclude.Add(collision.GetRid());
            var travelled=origin.DistanceTo(hitPosition);
            remaining-=travelled;
            if(remaining<=0.05f)return;
            origin=hitPosition+direction*0.05f;
        }
    }

    private void TickAreaDevice()
    {
        if (_timer > 0) return;
        var trigger = FindNearest(2.0f);
        if (trigger is null) return;

        // STRONGLY INFERRED: the Clap Bomb exposes Radius but no separate range stat.
        // Enhanced Electronics therefore applies its exact +40% to this radius.
        // Recovered fortification radii are authored in Roblox STUDS.
        // Previous code treated 25/35 studs as metres (3.57x too large).
        var radius = RobloxUnits.Distance(Definition.Radius!.Value) *
            RangeMultiplier;
        foreach (var infected in EnumerateInfected())
            if (Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), GlobalPosition, infected, radius, Player))
                infected.ApplyDamage(Definition.Damage!.Value*DamageMultiplier,
                    false, "Explosion");

        foreach (var node in GetTree().GetNodesInGroup("damage_objective"))
            if (node is DamageObjectiveTarget tanker &&
                Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), GlobalPosition, tanker, radius, Player))
                tanker.ApplyDamage(Definition.Damage!.Value*DamageMultiplier, "Explosive");

        QueueFree();
    }

    private void TickSlowTrap()
    {
        if (_timer > 0) return;

        var touched = false;
        foreach (var infected in EnumerateInfected())
        {
            if (!Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), GlobalPosition, infected, 2.5f, Player))
                continue;

            infected.ApplyDamage(15f, false, "BarbedWire"); // VERIFIED damage.
            // APPROXIMATED: source confirms slowing but not magnitude/duration.
            infected.ApplySlow(0.55f, 0.65);
            touched = true;
        }

        if (touched) _timer = 0.5;
    }

    private InfectedAgent? FindNearest(float maxDistance)
    {
        InfectedAgent? nearest = null;
        var nearestDistance = maxDistance;
        foreach (var infected in EnumerateInfected())
        {
            var distance = GlobalPosition.DistanceTo(infected.GlobalPosition);
            if (distance >= nearestDistance ||
                !Pass75BlastOcclusion.CanReachTarget(
                    GetWorld3D(), GlobalPosition, infected,
                    maxDistance, Player))
                continue;
            nearest = infected;
            nearestDistance = distance;
        }
        return nearest;
    }

    private IEnumerable<InfectedAgent> EnumerateInfected()
    {
        foreach (var node in GetTree().GetNodesInGroup("infected"))
            if (node is InfectedAgent infected &&
                GodotObject.IsInstanceValid(infected) && infected.Health > 0 &&
                !infected.IsQueuedForDeletion())
                yield return infected;
    }
}

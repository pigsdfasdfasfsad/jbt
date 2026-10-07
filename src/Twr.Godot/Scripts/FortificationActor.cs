using Godot;

namespace Twr.Godot;

public partial class FortificationActor : Node3D
{
    public RuntimeFortificationDefinition Definition { get; set; } =
        new("Unknown", 1, 1, null, null, null);
    public float DamageMultiplier {get;set;}=1f;
    public FirstPersonPlayer? Player {get;set;}
    public LocalSessionNode? Runtime {get;set;}

    private double _timer;
    private int _remainingShots=21;
    private bool _mounted;
    private bool _interactWasDown;
    private bool _qWasDown;
    private bool _toggleAim;

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
        if(_mounted)Player?.SetMountedMode(false);
    }

    private void TickMounted50Cal()
    {
        if(Player is null)return;

        var eDown=Input.IsKeyPressed(Key.E);
        var ePressed=eDown && !_interactWasDown;
        _interactWasDown=eDown;

        if(ePressed)
        {
            if(_mounted)SetMounted(false);
            else if(Player.GlobalPosition.DistanceTo(GlobalPosition)<=2.8f)SetMounted(true);
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
        if(Player is null || Definition.Damage is null)return;
        _timer=0.225; // RECOVERED animation timeline: 0.075s + 0.15s.
        _remainingShots--;

        var origin=GlobalPosition+Vector3.Up*1.35f;
        var direction=Player.AimDirection.Normalized();
        var remaining=1000f;
        var exclude=new global::Godot.Collections.Array<Rid>{Player.GetRid()};

        for(var penetration=0;penetration<=Math.Max(0,Definition.MaxPen ?? 0);penetration++)
        {
            var query=PhysicsRayQueryParameters3D.Create(origin,origin+direction*remaining);
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

        var radius = Definition.Radius!.Value;
        foreach (var infected in EnumerateInfected())
            if (GlobalPosition.DistanceTo(infected.GlobalPosition) <= radius)
                infected.ApplyDamage(Definition.Damage!.Value*DamageMultiplier, false, "Explosion");

        foreach (var node in GetTree().GetNodesInGroup("damage_objective"))
            if (node is DamageObjectiveTarget tanker &&
                GlobalPosition.DistanceTo(tanker.GlobalPosition) <= radius)
                tanker.ApplyDamage(Definition.Damage!.Value*DamageMultiplier, "Explosive");

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

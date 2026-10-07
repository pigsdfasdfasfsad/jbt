using Godot;

namespace Twr.Godot;

public partial class InfectedAgent : CharacterBody3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public string InfectedType { get; set; } = "Civilian";
    public float Health { get; set; } = 65;
    public float Damage { get; set; } = 8;
    public float MoveSpeed { get; set; } = 15;
    public Action<InfectedAgent, InfectedDeathContext>? Died { get; set; }
    public Action<InfectedAgent>? SpecialAttackRequested { get; set; }

    private double _attackCooldown;
    private double _specialCooldown;
    private float _slowFactor = 1f;
    private double _slowTime;
    private double _leapTime;
    private double _leapRecovery;
    private Vector3 _leapVelocity;
    private bool _leapHit;
    private double _steerHold;
    private int _steerSign=1;

    public override void _Ready()
    {
        AddToGroup("infected");
        CollisionLayer = 2;
        CollisionMask = 1;

        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.42f, Height = 1.8f }
        });

        AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh
            {
                Radius = 0.42f,
                Height = 1.8f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = ColorForType(InfectedType),
                    Roughness = 0.9f
                }
            }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        _attackCooldown = Math.Max(0, _attackCooldown - delta);
        _specialCooldown = Math.Max(0, _specialCooldown - delta);
        _slowTime = Math.Max(0, _slowTime - delta);
        _steerHold = Math.Max(0, _steerHold - delta);
        if (_slowTime <= 0) _slowFactor = 1f;

        if (Target is null || Runtime?.Player is null || !Runtime.Player.IsAlive)
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
            MoveAndSlide();
            return;
        }

        var deltaToPlayer = Target.GlobalPosition - GlobalPosition;
        var flat = new Vector3(deltaToPlayer.X, 0, deltaToPlayer.Z);
        var distance = flat.Length();

        if (InfectedType == "Bolter" && TickBolterLeap(delta, flat, distance))
            return;

        if (InfectedType == "Bloater" && distance > 4f && distance <= 35f && _specialCooldown <= 0)
        {
            // APPROXIMATED throw cadence/range; source confirms ranged cluster
            // behavior and that it stops throwing near melee range.
            _specialCooldown = 4.0;
            SpecialAttackRequested?.Invoke(this);
        }

        var velocity = Velocity;
        if (!IsOnFloor()) velocity.Y -= 22f * (float)delta;

        if (distance > 1.55f)
        {
            var desired = flat.LengthSquared() > 0.001f ? flat.Normalized() : Vector3.Zero;
            var direction = SteerAroundObstacles(desired);
            velocity.X = direction.X * MoveSpeed * _slowFactor;
            velocity.Z = direction.Z * MoveSpeed * _slowFactor;
            if (direction.LengthSquared() > 0.001f) LookAt(GlobalPosition + direction, Vector3.Up);
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
            if (_attackCooldown <= 0)
            {
                // APPROXIMATED: retail claw cadence is not recovered.
                _attackCooldown = 1.0;
                Runtime.DamagePlayer(Damage, InfectedType);
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private Vector3 SteerAroundObstacles(Vector3 desired)
    {
        if(desired.LengthSquared()<0.001f)return desired;

        if(_steerHold>0)
        {
            var held=desired.Rotated(Vector3.Up,_steerSign*0.72f).Normalized();
            if(!ObstacleAhead(held,1.8f))return held;
        }

        if(!ObstacleAhead(desired,2.2f))return desired;

        var left=desired.Rotated(Vector3.Up,0.78f).Normalized();
        var right=desired.Rotated(Vector3.Up,-0.78f).Normalized();
        var leftBlocked=ObstacleAhead(left,2.0f);
        var rightBlocked=ObstacleAhead(right,2.0f);

        if(!leftBlocked && !rightBlocked)
            _steerSign=(GetInstanceId() & 1UL)==0 ? 1 : -1;
        else if(!leftBlocked)
            _steerSign=1;
        else if(!rightBlocked)
            _steerSign=-1;
        else
            _steerSign=-_steerSign;

        _steerHold=0.9; // APPROXIMATED steering persistence for reconstructed collision geometry.
        return desired.Rotated(Vector3.Up,_steerSign*0.92f).Normalized();
    }

    private bool ObstacleAhead(Vector3 direction,float distance)
    {
        if(direction.LengthSquared()<0.001f)return false;
        var from=GlobalPosition+Vector3.Up*0.75f;
        var query=PhysicsRayQueryParameters3D.Create(from,from+direction.Normalized()*distance);
        query.CollisionMask=1; // World/player layer; infected are on layer 2.
        var exclude=new global::Godot.Collections.Array<Rid>{GetRid()};
        if(Target is not null)exclude.Add(Target.GetRid());
        query.Exclude=exclude;
        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count>0;
    }

    private bool TickBolterLeap(double delta, Vector3 flat, float distance)
    {
        if (_leapTime > 0)
        {
            _leapTime = Math.Max(0, _leapTime - delta);
            _leapVelocity.Y -= 22f * (float)delta;
            Velocity = _leapVelocity;
            MoveAndSlide();

            if (!_leapHit && distance <= 1.7f)
            {
                _leapHit = true;
                Runtime?.DamagePlayer(Math.Max(6f, Damage * 1.5f), "Bolter Leap");
            }

            if (_leapTime <= 0)
            {
                _leapRecovery = 1.0; // APPROXIMATED immobilized recovery window.
                Velocity = Vector3.Zero;
            }
            return true;
        }

        if (_leapRecovery > 0)
        {
            _leapRecovery = Math.Max(0, _leapRecovery - delta);
            Velocity = Vector3.Zero;
            MoveAndSlide();
            return true;
        }

        if (_specialCooldown <= 0 && distance >= 5f && distance <= 18f && flat.LengthSquared() > 0.001f)
        {
            // VERIFIED behavior: fixed-direction leap then temporary immobilization.
            // APPROXIMATED launch speed, duration, and cooldown.
            var direction = flat.Normalized();
            _leapVelocity = direction * 28f + Vector3.Up * 5f;
            _leapTime = 0.55;
            _specialCooldown = 4.0;
            _leapHit = false;
            LookAt(GlobalPosition + direction, Vector3.Up);
            return true;
        }

        return false;
    }

    public void ApplySlow(float factor, double seconds)
    {
        if (factor <= 0 || factor >= 1 || seconds <= 0) return;
        _slowFactor = Math.Min(_slowFactor, factor);
        _slowTime = Math.Max(_slowTime, seconds);
    }

    public void ApplyDamage(float amount, bool headshot = false, string damageKind = "Generic")
    {
        if (amount <= 0 || Health <= 0) return;
        var applied = headshot ? amount * 2.5f : amount; // VERIFIED head multiplier.
        Health = Math.Max(0, Health - applied);
        if (Health > 0) return;
        Died?.Invoke(this, new InfectedDeathContext(headshot, damageKind));
        QueueFree();
    }

    private static Color ColorForType(string type) => type switch
    {
        "Bolter" => new Color(0.55f, 0.17f, 0.14f),
        "Sprinter" => new Color(0.65f, 0.33f, 0.16f),
        "Military" => new Color(0.20f, 0.28f, 0.17f),
        "Hazmat" => new Color(0.72f, 0.68f, 0.15f),
        "Riot" => new Color(0.12f, 0.15f, 0.18f),
        "Burster" => new Color(0.38f, 0.48f, 0.19f),
        "Bloater" => new Color(0.36f, 0.23f, 0.28f),
        _ => new Color(0.32f, 0.36f, 0.30f)
    };
}

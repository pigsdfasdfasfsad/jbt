using Godot;

namespace Twr.Godot;

public partial class InfectedAgent : CharacterBody3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public ExpresswayNavigationRuntime? HighwayNavigator { get; set; }
    public string InfectedType { get; set; } = "Civilian";
    public float Health { get; set; } = 65;
    public float Damage { get; set; } = 8;
    public float MoveSpeed { get; set; } = 15f * RobloxUnits.MetersPerStud;
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
    private InfectedVisualAssembler? _visual;
    private double _navigationRefresh;
    private Vector3[] _navigationRoute = [];
    private int _nextNavigationPoint;

    public override void _Ready()
    {
        AddToGroup("infected");
        CollisionLayer = 2;
        CollisionMask = 1;

        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.30f, Height = 1.6f }
        });

        _visual = new InfectedVisualAssembler
        {
            Name = "InfectedVisual",
            InfectedType = InfectedType
        };
        AddChild(_visual);
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

        if (InfectedType == "Bloater" && distance > 4f && distance <= 35f &&
            Math.Abs(deltaToPlayer.Y) <= 4f && _specialCooldown <= 0 &&
            HasClearAttackPath())
        {
            // APPROXIMATED throw cadence/range; source confirms ranged cluster
            // behavior and that it stops throwing near melee range.
            _specialCooldown = 4.0;
            SpecialAttackRequested?.Invoke(this);
        }

        var velocity = Velocity;
        if (!IsOnFloor()) velocity.Y -= 22f * (float)delta;

        if (distance > 1.55f || Math.Abs(deltaToPlayer.Y) > 1.6f)
        {
            var desired = flat.LengthSquared() > 0.001f ? flat.Normalized() : Vector3.Zero;
            // Shared Expressway road graph handles large static obstructions.
            // Local collision steering remains active for near-field objects.
            if (HighwayNavigator is not null &&
                GodotObject.IsInstanceValid(HighwayNavigator))
            {
                _navigationRefresh -= delta;
                if (_navigationRefresh <= 0)
                {
                    _navigationRoute = HighwayNavigator.GetRoute(
                        GlobalPosition, Target.GlobalPosition);
                    _nextNavigationPoint = 0;
                    _navigationRefresh = 0.75 + (GetInstanceId() % 11UL) * 0.04;
                }
                while (_nextNavigationPoint < _navigationRoute.Length)
                {
                    var waypoint = _navigationRoute[_nextNavigationPoint];
                    var offset = new Vector3(waypoint.X - GlobalPosition.X,
                        0, waypoint.Z - GlobalPosition.Z);
                    if (offset.LengthSquared() > 2.5f) break;
                    _nextNavigationPoint++;
                }
                if (_nextNavigationPoint < _navigationRoute.Length)
                {
                    var waypoint = _navigationRoute[_nextNavigationPoint];
                    var move = new Vector3(waypoint.X - GlobalPosition.X,
                        0, waypoint.Z - GlobalPosition.Z);
                    if (move.LengthSquared() > .01f) desired = move.Normalized();
                }
            }
            var direction = SteerAroundObstacles(desired);
            velocity.X = direction.X * MoveSpeed * _slowFactor;
            velocity.Z = direction.Z * MoveSpeed * _slowFactor;
            if (direction.LengthSquared() > 0.001f) LookAt(GlobalPosition + direction, Vector3.Up);
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
            if (_attackCooldown <= 0 && HasClearAttackPath())
            {
                // Line of sight prevents hits through original walls and
                // floors while maintaining the existing attack cadence.
                // APPROXIMATED: retail claw cadence is not recovered.
                _attackCooldown = 1.0;
                _visual?.Attack();
                Runtime.DamagePlayer(Damage, InfectedType);
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private bool HasClearAttackPath()
    {
        if (Target is null) return false;
        var from = GlobalPosition + Vector3.Up * .10f;
        var to = Target.GlobalPosition + Vector3.Up * 1.10f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 1;
        query.Exclude = new global::Godot.Collections.Array<Rid>
        {
            GetRid(), Target.GetRid()
        };
        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count == 0;
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
        var intensity = InfectedCatalog.SmokeMultiplier(InfectedType);
        if (intensity <= 0) return; // Hazmat/Burster source smoke immunity.
        var effectiveFactor = 1f - (1f - factor) * intensity;
        _slowFactor = Math.Min(_slowFactor, effectiveFactor);
        _slowTime = Math.Max(_slowTime, seconds);
    }

    public void ApplyDamage(float amount, bool headshot = false, string damageKind = "Generic")
    {
        if (amount <= 0 || Health <= 0) return;
        var applied = amount * InfectedCatalog.DamageMultiplier(InfectedType, damageKind);
        if (headshot) applied *= 2.5f; // VERIFIED head multiplier.
        Health = Math.Max(0, Health - applied);
        if (Health > 0) return;
        Died?.Invoke(this, new InfectedDeathContext(headshot, damageKind));
        QueueFree();
    }

}

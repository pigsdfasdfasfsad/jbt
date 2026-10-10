using Godot;

namespace Twr.Godot;

public partial class InfectedAgent : CharacterBody3D
{
    public FirstPersonPlayer? Target { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public ExpresswayNavigationRuntime? HighwayNavigator { get; set; }
    public Pass25SourceNavigationRuntime? SourceNavigator { get; set; }
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
    private bool[] _jumpIntoNavigationPoint = [];
    private double _sourceJumpRemaining;
    private double _sourceJumpElapsed;
    private double _sourceJumpCooldown;
    private Vector3 _sourceJumpPlanarVelocity;
    private Vector3 _sourceJumpExpectedLanding;
    public int SourceJumpAttempts { get; private set; }
    // A landing must pass Pass43JumpLandingPolicy, not merely touch a floor.
    public int SourceJumpLandings { get; private set; }
    public int SourceJumpFailures { get; private set; }
    public bool SourceJumpInProgress => _sourceJumpRemaining > 0;
    public float SourceJumpLastLandingErrorMetres { get; private set; }
    private Vector3 _lastProgressPosition;
    private double _progressSampleTimer;
    private double _stuckDuration;
    private double _recoveryDetour;
    private double _pass32StationarySeconds;
    private double _pass32AgeSeconds;
    private double _pass32LastRecovery = -1000;
    private int _pass32Rescues;
    public int AssistedRecoveryCount => _pass32Rescues;

    public bool NeedsAssistedRecovery(Vector3 playerPosition)
    {
        if (Health <= 0 || Target is null || Runtime?.Player?.IsAlive != true)
            return false;
        var delta = playerPosition - GlobalPosition;
        var distance = new Vector2(delta.X, delta.Z).Length();
        return Pass32RescuePolicy.CanRescue(distance,
            GlobalPosition.Y - playerPosition.Y,
            _pass32StationarySeconds, _pass32Rescues,
            _pass32AgeSeconds - _pass32LastRecovery);
    }

    public bool ApplyAssistedRecovery(Vector3 newPosition, Vector3 playerPosition)
    {
        if (!NeedsAssistedRecovery(playerPosition) ||
            !float.IsFinite(newPosition.X) || !float.IsFinite(newPosition.Y) ||
            !float.IsFinite(newPosition.Z)) return false;
        GlobalPosition = newPosition;
        Velocity = Vector3.Zero;
        _pass32Rescues++;
        _pass32LastRecovery = _pass32AgeSeconds;
        _pass32StationarySeconds = 0;
        _stuckDuration = 0;
        _recoveryDetour = 0;
        _navigationRefresh = 0;
        _navigationRoute = [];
        _jumpIntoNavigationPoint = [];
        _nextNavigationPoint = 0;
        // Explicit accessibility relocation cancels any outstanding jump;
        // never let old ballistic velocity leak into the new supported spot.
        _sourceJumpRemaining = 0;
        _sourceJumpElapsed = 0;
        _sourceJumpPlanarVelocity = Vector3.Zero;
        _progressSampleTimer = .75;
        _lastProgressPosition = newPosition;
        return true;
    }

    public override void _Ready()
    {
        AddToGroup("infected");
        _lastProgressPosition = GlobalPosition;
        _progressSampleTimer = .75;
        CollisionLayer = 2;
        CollisionMask = 1 | Pass35InfectedWallsRuntime.InfectedWallCollisionLayer |
            Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;

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
        _pass32AgeSeconds += Math.Max(0,delta);
        _attackCooldown = Math.Max(0, _attackCooldown - delta);
        _specialCooldown = Math.Max(0, _specialCooldown - delta);
        _slowTime = Math.Max(0, _slowTime - delta);
        _steerHold = Math.Max(0, _steerHold - delta);
        _sourceJumpCooldown = Math.Max(0,_sourceJumpCooldown - delta);
        if (_slowTime <= 0) _slowFactor = 1f;

        if (Target is null || Runtime?.Player is null || !Runtime.Player.IsAlive)
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
            MoveAndSlide();
            return;
        }

        // Source-gated jump action, absent from older reconstructed graphs.
        // The original Lua Pathfind allows jumping, but jump links here are
        // approximations validated against recovered native-Part collision.
        if (TickSourceJump(delta)) return;

        var deltaToPlayer = Target.GlobalPosition - GlobalPosition;
        var flat = new Vector3(deltaToPlayer.X, 0, deltaToPlayer.Z);
        var distance = flat.Length();
        TickStuckRecovery(delta, distance);

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
            var highwayReady = HighwayNavigator is not null &&
                GodotObject.IsInstanceValid(HighwayNavigator);
            var sourceReady = SourceNavigator is not null &&
                GodotObject.IsInstanceValid(SourceNavigator);
            if (highwayReady || sourceReady)
            {
                _navigationRefresh -= delta;
                if (_navigationRefresh <= 0)
                {
                    _navigationRoute = highwayReady
                        ? HighwayNavigator!.GetRoute(GlobalPosition, Target.GlobalPosition)
                        : SourceNavigator!.GetRoute(GlobalPosition, Target.GlobalPosition);
                    _nextNavigationPoint = 0;
                    _jumpIntoNavigationPoint = new bool[_navigationRoute.Length];
                    if (!highwayReady && sourceReady &&
                        SourceNavigator!.Pass42JumpEdgeCount > 0)
                    {
                        for (var point=1;point<_navigationRoute.Length;point++)
                            _jumpIntoNavigationPoint[point] =
                                SourceNavigator.IsJumpLinkBetween(
                                    _navigationRoute[point-1],_navigationRoute[point]);
                    }
                    _navigationRefresh = 0.75 + (GetInstanceId() % 11UL) * 0.04;
                }
                while (_nextNavigationPoint < _navigationRoute.Length)
                {
                    var waypoint = _navigationRoute[_nextNavigationPoint];
                    if (_nextNavigationPoint > 0 &&
                        _jumpIntoNavigationPoint[_nextNavigationPoint])
                    {
                        var takeoff = _navigationRoute[_nextNavigationPoint-1];
                        var landingHorizontal = new Vector2(
                            waypoint.X - GlobalPosition.X,
                            waypoint.Z - GlobalPosition.Z).Length();
                        // The jump landing must not be skipped merely because
                        // both sides are within the old 1.58m waypoint radius.
                        if (IsOnFloor() && landingHorizontal < .45f &&
                            Math.Abs(waypoint.Y + .8f - GlobalPosition.Y) < .85f)
                        {
                            _nextNavigationPoint++;
                            continue;
                        }
                        var takeoffDistance = new Vector2(
                            takeoff.X - GlobalPosition.X,
                            takeoff.Z - GlobalPosition.Z).Length();
                        if (IsOnFloor() && takeoffDistance < 1.05f &&
                            Math.Abs(takeoff.Y + .8f - GlobalPosition.Y) < 1.15f &&
                            _sourceJumpCooldown <= 0 &&
                            TryStartSourceJump(waypoint))
                            return;
                        break;
                    }
                    var offset = new Vector3(waypoint.X - GlobalPosition.X,
                        0, waypoint.Z - GlobalPosition.Z);
                    // Pathfinding heights are at feet level; the infected
                    // body origin is around +0.8m. Never skip waypoint floors
                    // that are metres above or below the current enemy.
                    if (offset.LengthSquared() > 2.5f ||
                        Math.Abs(waypoint.Y + .8f - GlobalPosition.Y) > 1.15f)
                        break;
                    _nextNavigationPoint++;
                }
                if (_nextNavigationPoint < _navigationRoute.Length)
                {
                    var index = _nextNavigationPoint;
                    var waypoint = index > 0 && _jumpIntoNavigationPoint[index]
                        ? _navigationRoute[index - 1] : _navigationRoute[index];
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

    private bool TryStartSourceJump(Vector3 landingWaypoint)
    {
        var toLanding = landingWaypoint + Vector3.Up * .8f - GlobalPosition;
        var flat = new Vector3(toLanding.X,0,toLanding.Z);
        var distance = flat.Length();
        if (distance < .45f || distance > 8f * RobloxUnits.MetersPerStud + .35f ||
            !IsOnFloor()) return false;
        var speed = Mathf.Max(3f,MoveSpeed * _slowFactor);
        var flightTime = Mathf.Clamp(distance/speed,.38f,.78f);
        var vertical = Mathf.Clamp(
            (toLanding.Y + 11f*flightTime*flightTime)/flightTime,5f,9f);
        _sourceJumpPlanarVelocity = flat/flightTime;
        _sourceJumpExpectedLanding = landingWaypoint + Vector3.Up * .8f;
        Velocity = new Vector3(_sourceJumpPlanarVelocity.X,vertical,
            _sourceJumpPlanarVelocity.Z);
        _sourceJumpRemaining = flightTime + .22f;
        _sourceJumpElapsed = 0;
        _sourceJumpCooldown = 2.0;
        SourceJumpAttempts++;
        // Godot MoveAndSlide remains responsible for all actual 3D physics.
        // Neither the route edge nor this action bypasses solid collisions.
        MoveAndSlide();
        return true;
    }

    private bool TickSourceJump(double delta)
    {
        if (_sourceJumpRemaining <= 0) return false;
        _sourceJumpRemaining=Math.Max(0,_sourceJumpRemaining-delta);
        _sourceJumpElapsed+=delta;
        var velocity=Velocity;
        velocity.X=_sourceJumpPlanarVelocity.X;
        velocity.Z=_sourceJumpPlanarVelocity.Z;
        velocity.Y-=22f*(float)delta;
        Velocity=velocity;
        MoveAndSlide();
        var accepted = Pass43JumpLandingPolicy.IsSuccessful(
            IsOnFloor(), _sourceJumpElapsed, GlobalPosition,
            _sourceJumpExpectedLanding);
        if (accepted)
        {
            SourceJumpLastLandingErrorMetres =
                GlobalPosition.DistanceTo(_sourceJumpExpectedLanding);
            SourceJumpLandings++;
            _sourceJumpRemaining=0;
        }
        else if (_sourceJumpRemaining <= 0 ||
            (IsOnFloor() &&
             _sourceJumpElapsed >= Pass43JumpLandingPolicy.MinimumFlightSeconds))
        {
            // A floor contact on the TAKEOFF side is not a successful jump.
            // It must be recorded as an unsuccessful attempt so that source
            // collisions and unsupported custom geometry remain measurable.
            SourceJumpLastLandingErrorMetres =
                GlobalPosition.DistanceTo(_sourceJumpExpectedLanding);
            SourceJumpFailures++;
            _sourceJumpRemaining=0;
        }
        if (_sourceJumpRemaining <= 0)
        {
            _navigationRefresh=0;
            _navigationRoute=[];
            _jumpIntoNavigationPoint=[];
            _nextNavigationPoint=0;
        }
        return true;
    }

    private void TickStuckRecovery(double dt, float distance)
    {
        _progressSampleTimer -= dt;
        _recoveryDetour = Math.Max(0,_recoveryDetour - dt);
        if (_progressSampleTimer > 0) return;
        // A single stalled enemy should eventually try a different side
        // rather than pushing into a source-part wall forever. These are
        // bounded local detours; they do not replace a multi-floor navmesh.
        var travel = GlobalPosition - _lastProgressPosition;
        var planar = new Vector2(travel.X, travel.Z).Length();
        if (distance >= 7f && planar < .22f)
            _pass32StationarySeconds += .75;
        else
            _pass32StationarySeconds = Math.Max(0, _pass32StationarySeconds - 1.5);
        if (distance > 2.2f && planar < .22f)
            _stuckDuration += .75;
        else
            _stuckDuration = Math.Max(0,_stuckDuration - 1.5);
        if (_stuckDuration >= 1.5 && _recoveryDetour <= 0)
        {
            _steerSign = -_steerSign;
            _steerHold = 2.5;
            _recoveryDetour = 2.5;
            _stuckDuration = 0;
        }
        _lastProgressPosition = GlobalPosition;
        _progressSampleTimer = .75;
    }

    private bool HasClearAttackPath()
    {
        if (Target is null) return false;
        var from = GlobalPosition + Vector3.Up * .10f;
        var to = Target.GlobalPosition + Vector3.Up * 1.10f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 1 | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
        query.Exclude = new global::Godot.Collections.Array<Rid>
        {
            GetRid(), Target.GetRid()
        };
        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count == 0;
    }

    private Vector3 SteerAroundObstacles(Vector3 desired)
    {
        if(desired.LengthSquared()<0.001f)return desired;

        if (_recoveryDetour > 0)
        {
            foreach (var angle in new[] { 1.28f, 1.88f, 2.35f })
            {
                var direction = desired.Rotated(
                    Vector3.Up,_steerSign * angle).Normalized();
                if (!ObstacleAhead(direction,1.9f)) return direction;
            }
        }

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
        // Preserve world obstacle steering; use F7 source-wall layer only
        // for infected motion, never for player or damage line of sight.
        query.CollisionMask=1 | Pass35InfectedWallsRuntime.InfectedWallCollisionLayer |
            Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
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

            if (!_leapHit && distance <= 1.7f &&
                Target is not null &&
                Math.Abs(Target.GlobalPosition.Y - GlobalPosition.Y) <= 1.6f &&
                HasClearAttackPath())
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

        if (_specialCooldown <= 0 && distance >= 5f && distance <= 18f &&
            flat.LengthSquared() > 0.001f && Target is not null &&
            Math.Abs(Target.GlobalPosition.Y - GlobalPosition.Y) <= 2.1f &&
            HasClearAttackPath())
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
        _visual?.HitReaction();
        Health = Math.Max(0, Health - applied);
        if (Health > 0) return;
        Died?.Invoke(this, new InfectedDeathContext(headshot, damageKind));
        QueueFree();
    }

}

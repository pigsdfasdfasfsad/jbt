using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class FirstPersonPlayer : CharacterBody3D
{
    public LocalSessionNode? Runtime { get; set; }
    public string EquippedWeaponName => _equippedWeapon;
    public string PrimaryWeaponName => _primaryWeapon;
    public string SecondaryWeaponName => _secondaryWeapon;
    public bool HammerMode { get; private set; }
    public Vector3 AimOrigin => _camera.GlobalPosition;
    public Vector3 AimDirection => -_camera.GlobalTransform.Basis.Z;

    private Camera3D _camera = null!;
    private MeshInstance3D _viewModel = null!;
    private readonly RandomNumberGenerator _rng = new();
    private float _pitch;
    private bool _jumpRequested;
    private double _actionCooldown;
    private double _reloadTimer;
    private double _adrenalineTick;
    private string? _reloadWeapon;
    private string _primaryWeapon = StarterLoadoutService.SawnOff;
    private string _secondaryWeapon = StarterLoadoutService.Glock17;
    private string _meleeWeapon = StarterLoadoutService.TwoByFour;
    private string _equippedWeapon = StarterLoadoutService.Glock17;

    private const float WalkSpeed = 17f;
    private const float SprintSpeed = 24f; // APPROXIMATED: retail absolute sprint speed is not recovered
    private const float JumpVelocity = 7f; // APPROXIMATED
    private const float Gravity = 22f; // reconstruction physics tuning
    private const float MouseSensitivity = 0.0022f;
    private const int FittedShotgunPellets = 8; // APPROXIMATED: surviving modules expose spread, not a universal pellet count.

    public override void _Ready()
    {
        CollisionLayer = 1;
        CollisionMask = 1 | 2;
        _rng.Randomize();

        AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, 0.9f, 0),
            Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f }
        });

        _camera = new Camera3D
        {
            Position = new Vector3(0, 1.55f, 0),
            Current = true,
            Fov = 60
        };
        AddChild(_camera);

        _viewModel = new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.10f, 0.10f, 0.11f),
                    Metallic = 0.65f,
                    Roughness = 0.28f
                }
            }
        };
        _camera.AddChild(_viewModel);

        LoadProfileWeapons();
        PrepareWeaponAmmo(_primaryWeapon);
        PrepareWeaponAmmo(_secondaryWeapon);
        Equip(_secondaryWeapon);

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        _actionCooldown = Math.Max(0, _actionCooldown - delta);

        if (_reloadTimer > 0)
        {
            _reloadTimer -= delta;
            if (_reloadTimer <= 0 && _reloadWeapon is not null)
            {
                var reloadSpec = RuntimeWeaponCatalog.Get(_reloadWeapon);
                Runtime?.ReloadWeapon(_reloadWeapon, reloadSpec.Magazine);
                _reloadWeapon = null;
            }
        }

        if (Runtime?.Player is null || !Runtime.Player.IsAlive)
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
            MoveAndSlide();
            return;
        }

        TickAdrenaline(delta);

        var x = (Input.IsKeyPressed(Key.D) ? 1f : 0f) - (Input.IsKeyPressed(Key.A) ? 1f : 0f);
        var z = (Input.IsKeyPressed(Key.S) ? 1f : 0f) - (Input.IsKeyPressed(Key.W) ? 1f : 0f);
        var local = new Vector3(x, 0, z);
        if (local.LengthSquared() > 1) local = local.Normalized();

        var world = GlobalTransform.Basis * local;
        world.Y = 0;
        if (world.LengthSquared() > 0.001f) world = world.Normalized();

        var sprintMultiplier = Runtime.HasPerk("Speed Demon") ? 1.12f : 1f;
        var speed = Input.IsKeyPressed(Key.Shift) ? SprintSpeed * sprintMultiplier : WalkSpeed;
        var velocity = Velocity;
        velocity.X = world.X * speed;
        velocity.Z = world.Z * speed;

        if (!IsOnFloor()) velocity.Y -= Gravity * (float)delta;
        else if (_jumpRequested)
        {
            velocity.Y = JumpVelocity;
            _jumpRequested = false;
        }

        Velocity = velocity;
        MoveAndSlide();

        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);
        if (!HammerMode && spec.IsAutomatic &&
            Input.MouseMode == Input.MouseModeEnum.Captured &&
            Input.IsMouseButtonPressed(MouseButton.Left))
            TryUseWeapon();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.45f, 1.45f);
            _camera.Rotation = new Vector3(_pitch, 0, 0);
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Space) _jumpRequested = true;
            else if (key.Keycode == Key.R) BeginReload();
            else if (key.Keycode == Key.Key1) Equip(_primaryWeapon);
            else if (key.Keycode == Key.Key2) Equip(_secondaryWeapon);
            else if (key.Keycode == Key.Key3) Equip(_meleeWeapon);
            else if (key.Keycode == Key.Escape) Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
                Input.MouseMode = Input.MouseModeEnum.Captured;
            else if (!HammerMode)
                TryUseWeapon();
        }
    }

    public void SetHammerMode(bool enabled)
    {
        HammerMode = enabled;
        _viewModel.Visible = !enabled;
        _reloadTimer = 0;
        _reloadWeapon = null;
    }

    private void LoadProfileWeapons()
    {
        _primaryWeapon = ValidLoadout("Primary", Runtime?.Profile?.Loadout.GetValueOrDefault("Primary"), StarterLoadoutService.SawnOff);
        _secondaryWeapon = ValidLoadout("Secondary", Runtime?.Profile?.Loadout.GetValueOrDefault("Secondary"), StarterLoadoutService.Glock17);
        _meleeWeapon = ValidLoadout("Melee", Runtime?.Profile?.Loadout.GetValueOrDefault("Melee"), StarterLoadoutService.TwoByFour);
    }

    private static string ValidLoadout(string slot, string? requested, string fallback)
    {
        if (requested is not null && RuntimeWeaponCatalog.BySlot(slot).Any(x => x.Name == requested))
            return requested;
        return fallback;
    }

    private void PrepareWeaponAmmo(string weapon)
    {
        var spec = RuntimeWeaponCatalog.Get(weapon);
        if (spec.IsMelee) return;
        Runtime?.ConfigureWeaponAmmo(spec.Name, spec.Magazine, spec.Reserve);
    }

    private void Equip(string weapon)
    {
        _equippedWeapon = weapon;
        _reloadTimer = 0;
        _reloadWeapon = null;
        _actionCooldown = 0;
        ApplyViewModel();
    }

    private void ApplyViewModel()
    {
        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);
        var box = (BoxMesh)_viewModel.Mesh;

        if (spec.IsMelee)
        {
            _viewModel.Position = new Vector3(0.38f, -0.28f, -0.75f);
            _viewModel.RotationDegrees = new Vector3(-12, 0, -18);
            box.Size = new Vector3(0.10f, 0.78f, 0.10f);
        }
        else if (spec.IsLauncher)
        {
            _viewModel.Position = new Vector3(0.33f, -0.25f, -0.82f);
            _viewModel.RotationDegrees = new Vector3(-3, 2, 0);
            box.Size = new Vector3(0.23f, 0.23f, 1.18f);
        }
        else if (spec.IsShotgun)
        {
            _viewModel.Position = new Vector3(0.30f, -0.24f, -0.75f);
            _viewModel.RotationDegrees = new Vector3(-4, 3, 0);
            box.Size = new Vector3(0.22f, 0.18f, 0.95f);
        }
        else
        {
            _viewModel.Position = new Vector3(0.28f, -0.22f, -0.70f);
            _viewModel.RotationDegrees = new Vector3(-4, 4, 0);
            box.Size = spec.Slot == "Primary"
                ? new Vector3(0.18f, 0.20f, 0.95f)
                : new Vector3(0.16f, 0.18f, 0.62f);
        }
    }

    private void BeginReload()
    {
        if (_reloadTimer > 0 || Runtime?.Player is null) return;
        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);
        if (spec.IsMelee || spec.Magazine <= 0) return;

        var loaded = Runtime.Player.Ammo.GetValueOrDefault(spec.Name);
        var reserve = Runtime.Player.ReserveAmmo.GetValueOrDefault(spec.Name);
        if (loaded >= spec.Magazine || reserve <= 0) return;

        var reloadMultiplier = Runtime?.HasPerk("Brisk") == true ? 0.8 : 1.0;
        _reloadTimer = Math.Max(0.05, spec.ReloadSeconds * reloadMultiplier);
        _reloadWeapon = spec.Name;
    }

    private void TryUseWeapon()
    {
        if (_actionCooldown > 0 || _reloadTimer > 0 || Runtime?.Player is null || !Runtime.Player.IsAlive) return;
        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);

        if (spec.IsMelee)
        {
            _actionCooldown = Math.Max(0.05, spec.ActionSeconds);
            FireHitscan(spec, -_camera.GlobalTransform.Basis.Z, spec.Range, spec.Damage, "Melee", true);
            return;
        }

        if (Runtime.Player.Ammo.GetValueOrDefault(spec.Name) <= 0)
        {
            BeginReload();
            return;
        }

        if (!Runtime.SpendAmmo(spec.Name, 1)) return;
        _actionCooldown = FireInterval(spec);

        if (spec.IsLauncher)
        {
            FireLauncher(spec);
            return;
        }

        if (spec.IsFlamethrower)
        {
            FireHitscan(spec, -_camera.GlobalTransform.Basis.Z, spec.Range, spec.Damage, "Fire", false);
            return;
        }

        if (spec.IsShotgun)
        {
            for (var i = 0; i < FittedShotgunPellets; i++)
                FireHitscan(spec, SpreadDirection(spec.Spread), spec.Range, spec.Damage, "Bullet", true);
            return;
        }

        FireHitscan(spec, -_camera.GlobalTransform.Basis.Z, spec.Range, spec.Damage, "Bullet", true);
    }


    private double FireInterval(RuntimeWeaponDefinition spec)
    {
        var interval=spec.IsLauncher
            ? Math.Max(0.05,spec.ActionSeconds)
            : spec.Rpm>0 ? 60.0/spec.Rpm : Math.Max(0.05,spec.ActionSeconds);

        if(Runtime?.HasPerk("Trigger Finger")==true && TriggerFingerEligible(spec))
            interval/=1.35; // VERIFIED +35% fire rate.
        return interval;
    }

    private static bool TriggerFingerEligible(RuntimeWeaponDefinition spec)
    {
        if(spec.IsAutomatic || spec.IsMelee || spec.IsLauncher || spec.IsFlamethrower)return false;
        return spec.Type.Contains("Semi",StringComparison.OrdinalIgnoreCase) ||
               spec.Type.Contains("Pump",StringComparison.OrdinalIgnoreCase) ||
               spec.Type.Contains("Bolt",StringComparison.OrdinalIgnoreCase) ||
               spec.Type.Equals("Shotgun",StringComparison.OrdinalIgnoreCase);
    }

    private void TickAdrenaline(double delta)
    {
        if(Runtime?.Player is null || !Runtime.HasPerk("Adrenaline Rush") ||
           Runtime.Player.Health>=35f || Runtime.Player.Health<=0)
        {
            _adrenalineTick=0;
            return;
        }

        _adrenalineTick-=delta;
        if(_adrenalineTick>0)return;

        // APPROXIMATED rate: source defines "slowly" and a 35% ceiling but no rate.
        var amount=Math.Min(1f,35f-Runtime.Player.Health);
        if(amount>0)Runtime.HealPlayer(amount);
        _adrenalineTick=1.0;
    }

    private void FireLauncher(RuntimeWeaponDefinition spec)
    {
        var parent = GetParent();
        if (parent is null) return;

        var projectile = new ExplosiveProjectileRuntime
        {
            Name = spec.Name + "_Projectile",
            Owner = this,
            WeaponName = spec.Name,
            Damage = spec.Damage,
            Direction = -_camera.GlobalTransform.Basis.Z
        };
        parent.AddChild(projectile);
        projectile.GlobalPosition = _camera.GlobalPosition + projectile.Direction * 0.6f;
    }

    private Vector3 SpreadDirection(float spreadDegrees)
    {
        var direction = -_camera.GlobalTransform.Basis.Z;
        var yaw = Mathf.DegToRad(_rng.RandfRange(-spreadDegrees, spreadDegrees));
        var pitch = Mathf.DegToRad(_rng.RandfRange(-spreadDegrees, spreadDegrees));
        direction = direction.Rotated(Vector3.Up, yaw);
        direction = direction.Rotated(_camera.GlobalTransform.Basis.X.Normalized(), pitch);
        return direction.Normalized();
    }

    private void FireHitscan(
        RuntimeWeaponDefinition spec,
        Vector3 direction,
        float range,
        float damage,
        string damageKind,
        bool allowHeadshot)
    {
        var dir = direction.Normalized();
        var origin = _camera.GlobalPosition;
        var remaining = Math.Max(0.1f, range);
        var exclude = new global::Godot.Collections.Array<Rid> { GetRid() };

        for (var penetration = 0; penetration <= Math.Max(0, spec.MaxPen); penetration++)
        {
            var query = PhysicsRayQueryParameters3D.Create(origin, origin + dir * remaining);
            query.Exclude = exclude;
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (hit.Count <= 0) return;

            var hitPosition = hit["position"].AsVector3();
            var collider = hit["collider"].AsGodotObject();

            if (collider is DamageObjectiveTarget tanker)
            {
                tanker.ApplyDamage(damage, damageKind == "Fire" ? "Fire" : spec.IsMelee ? "Melee" : damageKind);
                return;
            }

            if (collider is not InfectedAgent infected)
                return;

            var localHit = infected.ToLocal(hitPosition);
            var headshot = false;
            if (allowHeadshot)
            {
                // APPROXIMATED geometric boundary; the x2.5 multiplier itself is VERIFIED.
                headshot = localHit.Y >= 0.45f;
            }

            var appliedDamage=damage;
            // VERIFIED effect: Heavy Hitter adds 20% to upper-body firearm hits.
            // APPROXIMATED boundary: reconstructed capsule has no authored limbs.
            if(Runtime?.HasPerk("Heavy Hitter")==true && damageKind=="Bullet" && localHit.Y>=-0.35f)
                appliedDamage*=1.2f;

            var kind = spec.Bladed && headshot ? "Decapitation" : damageKind;
            infected.ApplyDamage(appliedDamage, headshot, kind);

            if (collider is not CollisionObject3D collision || penetration >= spec.MaxPen)
                return;

            exclude.Add(collision.GetRid());
            var travelled = origin.DistanceTo(hitPosition);
            remaining -= travelled;
            if (remaining <= 0.05f) return;
            origin = hitPosition + dir * 0.05f;
        }
    }
}

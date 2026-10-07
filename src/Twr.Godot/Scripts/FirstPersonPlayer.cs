using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class FirstPersonPlayer : CharacterBody3D
{
    public LocalSessionNode? Runtime { get; set; }
    public string EquippedWeaponName => _equippedWeapon;
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
    private string? _reloadWeapon;
    private string _equippedWeapon = StarterLoadoutService.Glock17;

    private const float WalkSpeed = 17f;
    private const float SprintSpeed = 24f; // APPROXIMATED: retail absolute sprint speed is not recovered
    private const float JumpVelocity = 7f; // APPROXIMATED
    private const float Gravity = 22f; // reconstruction physics tuning
    private const float MouseSensitivity = 0.0022f;

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
        ApplyViewModel();

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
                var spec = StarterWeaponCatalog.Get(_reloadWeapon);
                Runtime?.ReloadWeapon(_reloadWeapon, spec.MagazineCapacity);
                _reloadWeapon = null;
            }
        }

        if (Runtime?.Player is null || !Runtime.Player.IsAlive)
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
            MoveAndSlide();
            return;
        }

        var x = (Input.IsKeyPressed(Key.D) ? 1f : 0f) - (Input.IsKeyPressed(Key.A) ? 1f : 0f);
        var z = (Input.IsKeyPressed(Key.S) ? 1f : 0f) - (Input.IsKeyPressed(Key.W) ? 1f : 0f);
        var local = new Vector3(x, 0, z);
        if (local.LengthSquared() > 1) local = local.Normalized();

        var world = GlobalTransform.Basis * local;
        world.Y = 0;
        if (world.LengthSquared() > 0.001f) world = world.Normalized();

        var speed = Input.IsKeyPressed(Key.Shift) ? SprintSpeed : WalkSpeed;
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
            else if (key.Keycode == Key.Key1) Equip(StarterLoadoutService.SawnOff);
            else if (key.Keycode == Key.Key2) Equip(StarterLoadoutService.Glock17);
            else if (key.Keycode == Key.Key3) Equip(StarterLoadoutService.TwoByFour);
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
        var spec = StarterWeaponCatalog.Get(_equippedWeapon);
        if (spec.Kind == StarterWeaponKind.Melee)
        {
            _viewModel.Position = new Vector3(0.38f, -0.28f, -0.75f);
            _viewModel.RotationDegrees = new Vector3(-12, 0, -18);
            ((BoxMesh)_viewModel.Mesh).Size = new Vector3(0.10f, 0.75f, 0.10f);
        }
        else if (spec.Kind == StarterWeaponKind.Shotgun)
        {
            _viewModel.Position = new Vector3(0.30f, -0.24f, -0.75f);
            _viewModel.RotationDegrees = new Vector3(-4, 3, 0);
            ((BoxMesh)_viewModel.Mesh).Size = new Vector3(0.22f, 0.18f, 0.95f);
        }
        else
        {
            _viewModel.Position = new Vector3(0.28f, -0.22f, -0.62f);
            _viewModel.RotationDegrees = new Vector3(-4, 4, 0);
            ((BoxMesh)_viewModel.Mesh).Size = new Vector3(0.16f, 0.18f, 0.62f);
        }
    }

    private void BeginReload()
    {
        if (_reloadTimer > 0 || Runtime?.Player is null) return;
        var spec = StarterWeaponCatalog.Get(_equippedWeapon);
        if (spec.Kind == StarterWeaponKind.Melee) return;

        var loaded = Runtime.Player.Ammo.GetValueOrDefault(spec.Name);
        var reserve = Runtime.Player.ReserveAmmo.GetValueOrDefault(spec.Name);
        if (loaded >= spec.MagazineCapacity || reserve <= 0) return;

        _reloadTimer = spec.ReloadSeconds;
        _reloadWeapon = spec.Name;
    }

    private void TryUseWeapon()
    {
        if (_actionCooldown > 0 || _reloadTimer > 0 || Runtime?.Player is null || !Runtime.Player.IsAlive) return;
        var spec = StarterWeaponCatalog.Get(_equippedWeapon);

        if (spec.Kind == StarterWeaponKind.Melee)
        {
            _actionCooldown = spec.ActionSeconds;
            FireRay(-_camera.GlobalTransform.Basis.Z, spec.Range, spec.Damage);
            return;
        }

        if (Runtime.Player.Ammo.GetValueOrDefault(spec.Name) <= 0)
        {
            BeginReload();
            return;
        }

        if (!Runtime.SpendAmmo(spec.Name, 1)) return;
        _actionCooldown = spec.Rpm > 0 ? 60.0 / spec.Rpm : spec.ActionSeconds;

        if (spec.Kind == StarterWeaponKind.Shotgun)
        {
            for (var i = 0; i < spec.Pellets; i++)
                FireRay(SpreadDirection(spec.SpreadDegrees), spec.Range, spec.Damage);
        }
        else
        {
            FireRay(-_camera.GlobalTransform.Basis.Z, spec.Range, spec.Damage);
        }
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

    private void FireRay(Vector3 direction, float range, float damage)
    {
        var origin = _camera.GlobalPosition;
        var end = origin + direction.Normalized() * range;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.Exclude = new global::Godot.Collections.Array<Rid> { GetRid() };
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (hit.Count <= 0) return;

        var collider = hit["collider"].AsGodotObject();
        var kind = StarterWeaponCatalog.Get(_equippedWeapon).Kind == StarterWeaponKind.Melee ? "Melee" : "Bullet";
        if (collider is DamageObjectiveTarget tanker)
        {
            tanker.ApplyDamage(damage, kind);
            return;
        }

        if (collider is InfectedAgent infected)
        {
            var hitPosition = hit["position"].AsVector3();
            var localHit = infected.ToLocal(hitPosition);
            // APPROXIMATED geometric boundary; the x2.5 multiplier itself is VERIFIED.
            var headshot = localHit.Y >= 0.45f;
            infected.ApplyDamage(damage, headshot, kind);
        }
    }
}

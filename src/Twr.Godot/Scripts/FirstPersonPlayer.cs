using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class FirstPersonPlayer : CharacterBody3D
{
    public LocalSessionNode? Runtime { get; set; }

    private Camera3D _camera = null!;
    private float _pitch;
    private bool _jumpRequested;
    private double _fireCooldown;
    private double _reloadTimer;

    private const float WalkSpeed = 17f;
    private const float SprintSpeed = 24f; // APPROXIMATED: retail absolute sprint speed is not recovered
    private const float JumpVelocity = 7f; // APPROXIMATED
    private const float Gravity = 22f; // reconstruction physics tuning
    private const float MouseSensitivity = 0.0022f;

    private const string Weapon = StarterLoadoutService.Glock17;
    private const int MagazineCapacity = 18;
    private const float WeaponDamage = 15f;
    private const double FireInterval = 60.0 / 400.0;
    private const double ReloadSeconds = 1.85;
    private const float Range = 1000f;

    public override void _Ready()
    {
        CollisionLayer = 1;
        CollisionMask = 1 | 2;

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

        _camera.AddChild(new MeshInstance3D
        {
            Position = new Vector3(0.28f, -0.22f, -0.62f),
            RotationDegrees = new Vector3(-4, 4, 0),
            Mesh = new BoxMesh
            {
                Size = new Vector3(0.16f, 0.18f, 0.62f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.10f, 0.10f, 0.11f),
                    Metallic = 0.65f,
                    Roughness = 0.28f
                }
            }
        });

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        _fireCooldown = Math.Max(0, _fireCooldown - delta);

        if (_reloadTimer > 0)
        {
            _reloadTimer -= delta;
            if (_reloadTimer <= 0) Runtime?.ReloadWeapon(Weapon, MagazineCapacity);
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
            else if (key.Keycode == Key.Escape) Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured) Input.MouseMode = Input.MouseModeEnum.Captured;
            else TryFire();
        }
    }

    private void BeginReload()
    {
        if (_reloadTimer > 0 || Runtime?.Player is null) return;
        var loaded = Runtime.Player.Ammo.GetValueOrDefault(Weapon);
        var reserve = Runtime.Player.ReserveAmmo.GetValueOrDefault(Weapon);
        if (loaded >= MagazineCapacity || reserve <= 0) return;
        _reloadTimer = ReloadSeconds;
    }

    private void TryFire()
    {
        if (_fireCooldown > 0 || _reloadTimer > 0 || Runtime?.Player is null || !Runtime.Player.IsAlive) return;

        if (Runtime.Player.Ammo.GetValueOrDefault(Weapon) <= 0)
        {
            BeginReload();
            return;
        }

        if (!Runtime.SpendAmmo(Weapon, 1)) return;
        _fireCooldown = FireInterval;

        var origin = _camera.GlobalPosition;
        var end = origin + (-_camera.GlobalTransform.Basis.Z * Range);
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (hit.Count > 0 && hit["collider"].AsGodotObject() is InfectedAgent infected)
            infected.ApplyDamage(WeaponDamage);
    }
}

using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class FirstPersonPlayer : CharacterBody3D
{
    public LocalSessionNode? Runtime { get; set; }
    public OfflineAudioRuntime? Audio { get; set; }
    public string EquippedWeaponName => _equippedWeapon;
    public string PrimaryWeaponName => _primaryWeapon;
    public string SecondaryWeaponName => _secondaryWeapon;
    public string? HeldThrowableName => _heldThrowable;
    public bool HammerMode { get; private set; }
    public bool MountedMode {get;private set;}
    public Camera3D CaptureCamera => _camera;
    public Vector3 AimOrigin => _camera.GlobalPosition;
    public Vector3 AimDirection => -_camera.GlobalTransform.Basis.Z;

    private Camera3D _camera = null!;
    private WeaponViewModelRuntime _viewModel = null!;
    private readonly RandomNumberGenerator _rng = new();
    private float _pitch;
    private bool _jumpRequested;
    private double _actionCooldown;
    private double _reloadTimer;
    private double _equipTimer;
    private double _adrenalineTick;
    private bool _aiming;
    private Vector3 _hipViewPosition;
    private Vector3 _hipViewRotation;
    private string? _reloadWeapon;
    private string _primaryWeapon = StarterLoadoutService.SawnOff;
    private string _secondaryWeapon = StarterLoadoutService.Glock17;
    private string _meleeWeapon = StarterLoadoutService.TwoByFour;
    private string _equippedWeapon = StarterLoadoutService.Glock17;
    private string? _heldThrowable;

    // Roblox game speeds are authored in studs/s; the Godot world uses metres.
    private const float WalkSpeed = 17f * RobloxUnits.MetersPerStud;
    private const float SprintSpeed = 24f * RobloxUnits.MetersPerStud; // APPROXIMATED: retail absolute sprint speed is not recovered
    private const float JumpVelocity = 7f; // APPROXIMATED
    private const float Gravity = 22f; // reconstruction physics tuning
    private const float MouseSensitivity = 0.0022f;
    private const int FittedShotgunPellets = 8; // APPROXIMATED: surviving modules expose spread, not a universal pellet count.

    public override void _Ready()
    {
        CollisionLayer = 1;
        // Layer 4 is the original invisible Client Walls experiment (F8).
        // Infected still scan only regular source collision on layer 1.
        CollisionMask = 1 | 2 | Pass34ClientWallsRuntime.ClientWallCollisionLayer;
        _rng.Randomize();

        AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, 0.9f, 0),
            Shape = new CapsuleShape3D { Radius = 0.29f, Height = 1.8f }
        });

        _camera = new Camera3D
        {
            Position = new Vector3(0, 1.55f, 0),
            Current = true,
            Fov = 60
        };
        AddChild(_camera);

        _viewModel = new WeaponViewModelRuntime { Name = "WeaponViewModel" };
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
        _equipTimer = Math.Max(0, _equipTimer - delta);

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

        if(MountedMode)
        {
            Velocity=Vector3.Zero;
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
        // FITTED TestPlace reconstruction: shared speed authority uses 1.25x
        // while the 30s/40s duration is source-backed by item/perk evidence.
        var drinkMultiplier = Runtime.Player.EnergyDrinkSeconds>0 ? 1.25f : 1f;
        var speed = (Input.IsKeyPressed(Key.Shift) ? SprintSpeed * sprintMultiplier : WalkSpeed) * drinkMultiplier;
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
        UpdateAim(delta,spec);
        if (!HammerMode && _heldThrowable is null && spec.IsAutomatic &&
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

        if(MountedMode)return;

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Space) _jumpRequested = true;
            else if (key.Keycode == Key.R) BeginReload();
            else if (key.Keycode == Key.Key1) Equip(_primaryWeapon);
            else if (key.Keycode == Key.Key2) Equip(_secondaryWeapon);
            else if (key.Keycode == Key.Key3) Equip(_meleeWeapon);
            else if (key.Keycode == Key.Key5) SelectThrowable("Frag");
            else if (key.Keycode == Key.Key6) SelectThrowable("Molotov");
            else if (key.Keycode == Key.Key7) SelectThrowable("Nerve Gas");
            else if (key.Keycode == Key.G) CycleThrowable();
            else if (key.Keycode == Key.Escape) Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
                Input.MouseMode = Input.MouseModeEnum.Captured;
            else if (!HammerMode && _heldThrowable is not null)
                TryThrowThrowable();
            else if (!HammerMode)
                TryUseWeapon();
        }
    }

    public void SetMountedMode(bool enabled)
    {
        MountedMode=enabled;
        HammerMode=false;
        _heldThrowable=null;
        _reloadTimer=0;
        _reloadWeapon=null;
        _viewModel.Visible=!enabled;
        if(!enabled)
        {
            _camera.Fov=60;
            ApplyViewModel();
        }
    }

    public void SetMountedAim(bool aiming)
    {
        if(!MountedMode)return;
        _camera.Fov=aiming ? 60 : 70;
    }

    public void SetHammerMode(bool enabled)
    {
        HammerMode = enabled;
        if(enabled)_heldThrowable=null;
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
        _heldThrowable=null;
        _equippedWeapon = weapon;
        _reloadTimer = 0;
        _reloadWeapon = null;
        _actionCooldown = 0;
        _aiming=false;
        _camera.Fov=60;
        var spec=RuntimeWeaponCatalog.Get(weapon);
        var equipSpeed=Runtime?.HasPerk("Dexterous")==true ? 1.3 : 1.0;
        _equipTimer=Math.Max(0.01,spec.EquipSeconds/equipSpeed);
        ApplyViewModel();
    }

    private void ApplyViewModel()
    {
        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);
        _viewModel.SetWeapon(spec);

        if (spec.IsMelee)
        {
            _viewModel.Position = new Vector3(0.38f, -0.28f, -0.75f);
            _viewModel.RotationDegrees = new Vector3(-12, 0, -18);
        }
        else if (spec.IsLauncher)
        {
            _viewModel.Position = new Vector3(0.33f, -0.25f, -0.82f);
            _viewModel.RotationDegrees = new Vector3(-3, 2, 0);
        }
        else if (spec.IsShotgun)
        {
            _viewModel.Position = new Vector3(0.30f, -0.24f, -0.75f);
            _viewModel.RotationDegrees = new Vector3(-4, 3, 0);
        }
        else
        {
            _viewModel.Position = new Vector3(0.28f, -0.22f, -0.70f);
            _viewModel.RotationDegrees = new Vector3(-4, 4, 0);
        }

        _hipViewPosition = _viewModel.Position;
        _hipViewRotation = _viewModel.Rotation;
    }

    private void SelectThrowable(string type)
    {
        if(Runtime?.Player is null || Runtime.Player.Inventory.GetValueOrDefault(type)<=0)return;
        HammerMode=false;
        _heldThrowable=type;
        _reloadTimer=0;
        _reloadWeapon=null;
        _actionCooldown=0;
        _viewModel.Visible=true;
        _viewModel.SetThrowable(type);
        _viewModel.Position=new Vector3(0.32f,-0.25f,-0.58f);
        _viewModel.RotationDegrees=Vector3.Zero;
    }

    private void CycleThrowable()
    {
        if(Runtime?.Player is null)return;
        var order=new[]{"Frag","Molotov","Nerve Gas"};
        var available=order.Where(x=>Runtime.Player.Inventory.GetValueOrDefault(x)>0).ToArray();
        if(available.Length==0){_heldThrowable=null;ApplyViewModel();return;}

        if(_heldThrowable is null){SelectThrowable(available[0]);return;}
        var current=Array.IndexOf(available,_heldThrowable);
        SelectThrowable(available[(current+1+available.Length)%available.Length]);
    }

    private void TryThrowThrowable()
    {
        if(_heldThrowable is null || Runtime is null || _actionCooldown>0)return;
        var type=_heldThrowable;
        if(!Runtime.ConsumeItem(type)){_heldThrowable=null;ApplyViewModel();return;}

        var projectile=new ThrowableProjectileRuntime
        {
            Name=type.Replace(" ","")+"Projectile",
            ThrowableType=type,
            SourcePlayer=this,
            Direction=AimDirection
        };
        GetParent()?.AddChild(projectile);
        projectile.GlobalPosition=AimOrigin+AimDirection*0.55f;

        _actionCooldown=type=="Molotov" ? 0.4 : 0.7;
        _heldThrowable=null;
        ApplyViewModel();
    }

    private void BeginReload()
    {
        if (_reloadTimer > 0 || Runtime?.Player is null) return;
        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);
        if (spec.IsMelee || spec.Magazine <= 0) return;

        var loaded = Runtime.Player.Ammo.GetValueOrDefault(spec.Name);
        var reserve = Runtime.Player.ReserveAmmo.GetValueOrDefault(spec.Name);
        if (loaded >= spec.Magazine || reserve <= 0) return;

        var reloadSpeed = Runtime?.HasPerk("Brisk") == true ? 1.2 : 1.0;
        _reloadTimer = Math.Max(0.05, spec.ReloadSeconds / reloadSpeed);
        _reloadWeapon = spec.Name;
        _viewModel.Reload(_reloadTimer);
        Audio?.Play("reload");
    }

    private void TryUseWeapon()
    {
        if (_actionCooldown > 0 || _reloadTimer > 0 || _equipTimer > 0 || Runtime?.Player is null || !Runtime.Player.IsAlive) return;
        var spec = RuntimeWeaponCatalog.Get(_equippedWeapon);

        if (spec.IsMelee)
        {
            _actionCooldown = Math.Max(0.05, spec.ActionSeconds);
            _viewModel.Fire();
            Audio?.Play("melee");
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
        _viewModel.Fire();
        Audio?.Play(spec.IsLauncher ? "launcher" :
            spec.IsShotgun ? "shotgun" : "gunshot");
        ApplyRecoil(spec);

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

    private void UpdateAim(double delta,RuntimeWeaponDefinition spec)
    {
        if(_heldThrowable is not null || HammerMode)
        {
            _aiming=false;
            _camera.Fov=Mathf.MoveToward(_camera.Fov,60f,(float)(120.0*delta));
            return;
        }

        var wantsAim = !spec.IsMelee && _equipTimer<=0 && _reloadTimer<=0 &&
            Input.MouseMode==Input.MouseModeEnum.Captured &&
            Input.IsMouseButtonPressed(MouseButton.Right);
        _aiming=wantsAim;

        var aimSpeed=Runtime?.HasPerk("Eagle Eyes")==true ? 1.4 : 1.0;
        var seconds=Math.Max(0.01,(_aiming ? spec.AimSeconds : spec.UnAimSeconds)/aimSpeed);
        var desiredFov=_aiming ? Math.Clamp(spec.AimFov,5f,60f) : 60f;
        var fullTravel=Math.Max(1f,Math.Abs(60f-Math.Clamp(spec.AimFov,5f,60f)));
        _camera.Fov=Mathf.MoveToward(_camera.Fov,desiredFov,(float)(fullTravel/seconds*delta));

        // APPROXIMATED Godot viewmodel alignment. The source AimCF transforms
        // are Roblox CFrames and cannot be transferred directly.
        var desiredPosition=_aiming
            ? new Vector3(0,-0.18f,_hipViewPosition.Z)
            : _hipViewPosition;
        var blend=(float)Math.Clamp(delta/seconds,0.0,1.0);
        _viewModel.Position=_viewModel.Position.Lerp(desiredPosition,blend);
        if(!_aiming)
            _viewModel.Rotation=_viewModel.Rotation.Lerp(_hipViewRotation,blend);
    }

    private void ApplyRecoil(RuntimeWeaponDefinition spec)
    {
        if(spec.VerticalRecoil<=0 && spec.HorizontalRecoil<=0)return;
        var multiplier=Runtime?.HasPerk("Steady Hand")==true ? 0.5f : 1f;
        var horizontal=_rng.RandfRange(-spec.HorizontalRecoil,spec.HorizontalRecoil)*multiplier;
        _pitch=Mathf.Clamp(_pitch-spec.VerticalRecoil*multiplier,-1.45f,1.45f);
        RotateY(horizontal);
        _camera.Rotation=new Vector3(_pitch,0,0);
    }

    private void FireLauncher(RuntimeWeaponDefinition spec)
    {
        var parent = GetParent();
        if (parent is null) return;

        var projectile = new ExplosiveProjectileRuntime
        {
            Name = spec.Name + "_Projectile",
            SourcePlayer = this,
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
        // Recovered weapon Distance/Range values are authored in Roblox studs.
        var remaining = Math.Max(0.1f, range * RobloxUnits.MetersPerStud);
        var exclude = new global::Godot.Collections.Array<Rid> { GetRid() };

        for (var penetration = 0; penetration <= Math.Max(0, spec.MaxPen); penetration++)
        {
            var query = PhysicsRayQueryParameters3D.Create(origin, origin + dir * remaining);
            query.Exclude = exclude;
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (hit.Count <= 0) return;

            var hitPosition = hit["position"].AsVector3();
            var collider = hit["collider"].AsGodotObject();
            if ((damageKind == "Bullet" || damageKind == "Fire") &&
                GetParent() is Node3D environment)
                BallisticImpactRuntime.Spawn(environment,
                    _camera.GlobalPosition, hitPosition,
                    collider is InfectedAgent);

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

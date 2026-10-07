using System.Text.Json;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

public partial class GameplayRoot : Node3D
{
    public LocalSessionNode? Runtime { get; set; }
    public string MapName { get; set; } = "Manor";
    public Action? ExitRequested { get; set; }

    private GameplayHud _hud = null!;
    private RuntimeMapDefinition _mapDefinition = null!;
    private RuntimeMapLayout _mapLayout = null!;
    private FirstPersonPlayer _player = null!;
    private FortificationController _fortifications = null!;
    private readonly List<InfectedAgent> _infected = [];
    private readonly List<ObjectiveRuntime> _objectives = [];
    private readonly List<PickupActor> _pickups = [];
    private readonly List<SupplyDropRuntime> _supplyDrops = [];
    private int _completedObjectivesThisWave;
    private readonly RandomNumberGenerator _rng = new();
    private Stage _stage = Stage.Countdown;
    private double _stageTime = RegularWaveRules.CountdownSeconds;
    private double _spawnTimer;
    private bool _finished;

    private enum Stage { Countdown, Wave, WaveEnd, Intermission, Results }

    public override void _Ready()
    {
        if (Runtime?.Session is null)
            throw new InvalidOperationException("GameplayRoot requires an initialized LocalSessionNode.");

        _rng.Randomize();
        _mapDefinition = MapCatalogRuntime.Get(MapName);
        _mapLayout = MapBlockoutBuilder.Build(this, _mapDefinition);

        _player = new FirstPersonPlayer
        {
            Name = "Player",
            Runtime = Runtime,
            Position = _mapLayout.PlayerSpawn
        };
        AddChild(_player);

        _hud = new GameplayHud { Name = "HUD" };
        AddChild(_hud);
        _hud.SetBanner($"WAVE {Runtime.Match?.Wave ?? 1} BEGINS IN");

        _fortifications = new FortificationController
        {
            Name = "Fortifications",
            Player = _player,
            Runtime = Runtime
        };
        _fortifications.StatusChanged = status => _hud.SetUtility(status);
        AddChild(_fortifications);

        SpawnNaturalPickups();
    }

    public override void _Process(double delta)
    {
        if (Runtime?.Player is null || Runtime.Match is null) return;

        if (!_finished && !Runtime.Player.IsAlive)
        {
            Finish(false, "YOU DIED");
            return;
        }

        if (_finished)
        {
            _hud.UpdateState(Runtime.Player, Runtime.Match, "RESULTS", 0, _player.EquippedWeaponName);
            return;
        }

        _stageTime = Math.Max(0, _stageTime - delta);

        switch (_stage)
        {
            case Stage.Countdown:
                if (_stageTime <= 0) StartWave();
                break;
            case Stage.Wave:
                _spawnTimer -= delta;
                if (_spawnTimer <= 0 && _infected.Count < RegularWaveRules.MaxAlive(Runtime.Match.Wave))
                {
                    SpawnInfected(Runtime.Match.Wave);
                    _spawnTimer = RegularWaveRules.SpawnIntervalSeconds(Runtime.Match.Wave);
                }
                if (_stageTime <= 0) EndWave();
                break;
            case Stage.WaveEnd:
                if (_stageTime <= 0) AdvanceAfterWave();
                break;
            case Stage.Intermission:
                if (_stageTime <= 0) BeginCountdown();
                break;
        }

        _hud.UpdateState(Runtime.Player, Runtime.Match, StageName(), _stageTime, _player.EquippedWeaponName);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_finished || @event is not InputEventKey key || !key.Pressed || key.Echo) return;

        if (key.Keycode is Key.Enter or Key.KpEnter)
        {
            Runtime?.ReturnToLobby();
            ExitRequested?.Invoke();
        }
    }

    private void StartWave()
    {
        if (Runtime?.Match is null) return;
        _stage = Stage.Wave;
        _stageTime = RegularWaveRules.WaveDurationSeconds;
        _spawnTimer = 0;
        _completedObjectivesThisWave = 0;
        SpawnWaveObjectives();
        var currentWave = Runtime.Match.Wave;
        _hud.SetBanner(currentWave == ReleaseRules.MaxWaves ? "FINAL WAVE" : $"WAVE {currentWave}");
    }

    private void EndWave()
    {
        if (Runtime?.Match is null) return;
        _stage = Stage.WaveEnd;
        _stageTime = RegularWaveRules.WaveEndSeconds;
        var survivedWave = Runtime.Match.Wave;
        Runtime.AwardWaveSurvival(survivedWave, _completedObjectivesThisWave, 1);
        Runtime.EndWaveCleanup();
        _hud.SetBanner($"WAVE {survivedWave} SURVIVED");

        // APPROXIMATED boundary behavior: surviving infected are cleared for
        // intermission until exact retail teardown behavior is recovered.
        ClearInfected();
        ClearObjectives();
        _fortifications.ClearDeployed();
    }

    private void AdvanceAfterWave()
    {
        Runtime?.AdvanceWave();
        if (Runtime?.Match is null) return;

        if (Runtime.Match.Phase == MatchPhase.Results)
        {
            Finish(true, "MAP COMPLETED");
            return;
        }

        _stage = Stage.Intermission;
        _stageTime = RegularWaveRules.IntermissionSeconds;
        _hud.SetBanner($"WAVE {Runtime.Match.Wave} PREP");
    }

    private void BeginCountdown()
    {
        if (Runtime?.Match is null) return;
        _stage = Stage.Countdown;
        _stageTime = RegularWaveRules.CountdownSeconds;
        _hud.SetBanner($"WAVE {Runtime.Match.Wave} BEGINS IN");
    }


    private void SpawnWaveObjectives()
    {
        if (Runtime?.Match is null) return;

        ClearObjectives();
        var families = _mapDefinition.Objectives.ToArray();
        if (families.Length == 0) return;
        var wave = Runtime.Match.Wave;

        // VERIFIED: Regular allows a maximum of one or two objectives per wave.
        // APPROXIMATED scheduling: one objective normally, two every third wave.
        var count = wave % 3 == 0 ? 2 : 1;
        for (var i = 0; i < count; i++)
        {
            var family = families[(wave - 1 + i * 2) % families.Length];
            var objective = new ObjectiveRuntime
            {
                Name = $"Objective_{wave}_{family}_{i}",
                ObjectiveId = $"{MapName}:Wave{wave}:{family}:{i}",
                Family = family,
                Player = _player,
                Runtime = Runtime,
                Position = ObjectivePosition(i)
            };
            objective.StatusChanged = status => _hud.SetObjective(status);
            objective.Completed = OnObjectiveCompleted;
            _objectives.Add(objective);
            AddChild(objective);
        }
    }

    private Vector3 ObjectivePosition(int index)
    {
        if (_mapLayout.ObjectivePoints.Count == 0) return Vector3.Zero;
        return _mapLayout.ObjectivePoints[index % _mapLayout.ObjectivePoints.Count];
    }

    private void OnObjectiveCompleted(ObjectiveRuntime objective)
    {
        _completedObjectivesThisWave++;
        _objectives.Remove(objective);

        var completionPosition = objective.GlobalPosition;
        if (objective.Family == "Radio")
        {
            _hud.SetBanner("SUPPLY HELICOPTER EN ROUTE");
            SpawnSupplyDrop();
        }
        else if (objective.Family == "Unpack")
        {
            _hud.SetBanner("SUPPLY CRATE UNPACKED");
            SpawnUnpackContents(completionPosition);
        }

        objective.QueueFree();
        if (_objectives.Count == 0)
            _hud.SetObjective("ALL WAVE OBJECTIVES COMPLETE");
    }

    private void ClearObjectives()
    {
        foreach (var objective in _objectives.ToArray())
            if (GodotObject.IsInstanceValid(objective)) objective.QueueFree();
        _objectives.Clear();
        if (GodotObject.IsInstanceValid(_hud))
            _hud.SetObjective("");
    }

    private readonly record struct FortPickup(string Name, int Count);

    private void SpawnNaturalPickups()
    {
        // APPROXIMATED coordinates: topology rules come from map references,
        // while exact retail item spawn transforms are not recovered.
        var types = new[] { "Bandages", "Ammo", "Body Armor", "Medkit", "Ammo", "Bandages" };
        var count = Math.Min(types.Length, _mapLayout.PickupPoints.Count);
        for (var i = 0; i < count; i++)
            SpawnPickup(types[i], _mapLayout.PickupPoints[i]);
    }

    private void SpawnPickup(string type, Vector3 position, int grantCount = 1)
    {
        var pickup = new PickupActor
        {
            Name = $"Pickup_{type}_{_pickups.Count}",
            PickupType = type,
            GrantCount = Math.Max(1, grantCount),
            Player = _player,
            Runtime = Runtime,
            Position = position
        };
        pickup.Collected = collected => _pickups.Remove(collected);
        _pickups.Add(pickup);
        AddChild(pickup);
    }

    private void SpawnSupplyDrop()
    {
        var destination = new Vector3(_rng.RandfRange(-8f, 8f), 0.3f, _rng.RandfRange(-20f, 20f));
        var pallet = new SupplyDropRuntime
        {
            Name = $"SupplyDrop_{Runtime?.Match?.Wave ?? 0}",
            Position = destination + Vector3.Up * 15f,
            GroundY = destination.Y
        };
        pallet.Landed = position =>
        {
            _supplyDrops.Remove(pallet);
            SpawnSupplyContents(position);
            _hud.SetBanner("SUPPLY DROP LANDED");
        };
        _supplyDrops.Add(pallet);
        AddChild(pallet);
    }

    private void SpawnSupplyContents(Vector3 center)
    {
        // CONTESTED evidence: one source says exactly eight total; another says
        // four item pickups plus four fortification pickups. The 4+4 split is
        // used here as the stronger reconstruction fit while preserving total=8.
        var itemTypes = new[] { "Medkit", "Ammo", "Body Armor", "Bandages" };
        for (var i = 0; i < 4; i++)
            SpawnPickup(itemTypes[i], RingPoint(center, i, 8, 2.3f));

        var forts = LoadFortificationCatalog();
        for (var i = 0; i < 4; i++)
        {
            if (forts.Count == 0)
            {
                SpawnPickup("Ammo", RingPoint(center, i + 4, 8, 2.3f));
                continue;
            }
            var fort = forts[_rng.RandiRange(0, forts.Count - 1)];
            SpawnPickup(fort.Name, RingPoint(center, i + 4, 8, 2.3f), fort.Count);
        }
    }

    private void SpawnUnpackContents(Vector3 center)
    {
        // VERIFIED: both Unpack variants produce exactly eight pickups.
        // APPROXIMATED: absent source for variant scheduling, alternate by wave.
        var ammoVariant = (Runtime?.Match?.Wave ?? 1) % 2 == 1;
        for (var i = 0; i < 8; i++)
        {
            var type = ammoVariant ? "Ammo" : (i % 2 == 0 ? "Bandages" : "Medkit");
            SpawnPickup(type, RingPoint(center, i, 8, 2.5f));
        }
    }

    private List<FortPickup> LoadFortificationCatalog()
    {
        var result = new List<FortPickup>();
        try
        {
            var json = global::Godot.FileAccess.GetFileAsString("res://Content/fortifications/fortifications.json");
            using var document = JsonDocument.Parse(json);
            foreach (var entry in document.RootElement.GetProperty("fortifications").EnumerateArray())
            {
                var name = entry.GetProperty("name").GetString();
                var count = entry.GetProperty("count").GetInt32();
                if (!string.IsNullOrWhiteSpace(name) && count > 0)
                    result.Add(new FortPickup(name, count));
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Fortification catalog unavailable for supply drop: {ex.Message}");
        }
        return result;
    }

    private static Vector3 RingPoint(Vector3 center, int index, int count, float radius)
    {
        var angle = Mathf.Tau * index / count;
        return center + new Vector3(Mathf.Cos(angle) * radius, 0.25f, Mathf.Sin(angle) * radius);
    }

    private void SpawnInfected(int wave)
    {
        var eligible = InfectedCatalog.Active
            .Where(x => wave >= 2 || (x.Name != "Riot" && x.Name != "Bloater"))
            .ToArray();
        if (eligible.Length == 0) return;

        var definition = eligible[_rng.RandiRange(0, eligible.Length - 1)];
        var scale = (float)RegularWaveRules.DifficultyScale(wave);
        var speedScale = (float)RegularWaveRules.InfectedWalkSpeedScale(wave);

        var infected = new InfectedAgent
        {
            Name = definition.Name,
            Target = _player,
            Runtime = Runtime,
            InfectedType = definition.Name,
            Health = (float)definition.Health * scale,
            Damage = (float)definition.Damage * scale,
            MoveSpeed = (float)definition.WalkSpeed * speedScale,
            Position = RandomSpawnPoint()
        };
        infected.Died = OnInfectedDied;
        infected.SpecialAttackRequested = OnInfectedSpecialAttack;
        _infected.Add(infected);
        AddChild(infected);
    }

    private Vector3 RandomSpawnPoint()
    {
        if (_mapLayout.InfectedSpawns.Count == 0) return new Vector3(0,1,-30);
        var basePoint = _mapLayout.InfectedSpawns[_rng.RandiRange(0, _mapLayout.InfectedSpawns.Count - 1)];
        return basePoint + new Vector3(_rng.RandfRange(-2.5f,2.5f), 0, _rng.RandfRange(-2.5f,2.5f));
    }

    private void OnInfectedDied(InfectedAgent infected, InfectedDeathContext context)
    {
        _infected.Remove(infected);
        if (context.DamageKind == "ObjectiveExplosion") return;

        var reward = InfectedCatalog.Reward(infected.InfectedType);
        Runtime?.AwardKill(infected.InfectedType, reward.Credits, reward.Xp);

        var specialKill = context.Headshot ? "Headshot" : context.DamageKind switch
        {
            "Explosion" => "Explosion",
            "Fire" => "Fire",
            "Decapitation" => "Decapitation",
            "BarbedWire" => "BarbedWire",
            _ => null
        };
        if (specialKill is not null)
        {
            var bonus = InfectedCatalog.BonusReward(infected.InfectedType);
            Runtime?.AwardKill(infected.InfectedType + ":" + specialKill, bonus.Credits, bonus.Xp);
        }

        if (infected.InfectedType == "Burster" &&
            !context.Headshot && context.DamageKind != "SlowTrap" && context.DamageKind != "BarbedWire" &&
            context.DamageKind != "Fire" && context.DamageKind != "Decapitation")
        {
            SpawnBursterHazard(infected.GlobalPosition);
        }
    }

    private void OnInfectedSpecialAttack(InfectedAgent infected)
    {
        if (infected.InfectedType != "Bloater") return;
        AddChild(new SporeProjectileRuntime
        {
            Name = "BloaterSpore",
            Runtime = Runtime,
            Target = _player,
            Damage = 26.4f,
            Radius = 15f,
            GlobalPosition = infected.GlobalPosition + Vector3.Up * 1.2f
        });
    }

    private void SpawnBursterHazard(Vector3 position)
    {
        const float blastRadius = 20f;
        if (Runtime?.Player?.IsAlive == true && _player.GlobalPosition.DistanceTo(position) <= blastRadius)
            Runtime.DamagePlayer(27.5f, "Burster detonation");

        AddChild(new SporeCloudRuntime
        {
            Name = "BursterSporeCloud",
            Runtime = Runtime,
            Target = _player,
            Radius = blastRadius,
            TickDamage = 5f,
            DurationSeconds = 4.0,
            GlobalPosition = position
        });
    }

    private void Finish(bool won, string title)
    {
        if (_finished) return;
        if (!won) Runtime?.FailMatch("All players dead");

        _finished = true;
        _stage = Stage.Results;
        ClearInfected();
        _fortifications.ClearDeployed();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _hud.SetBanner($"{title}  -  PRESS ENTER TO RETURN");
    }

    private void ClearInfected()
    {
        foreach (var infected in _infected.ToArray())
            if (GodotObject.IsInstanceValid(infected)) infected.QueueFree();
        _infected.Clear();
    }

    private string StageName()
    {
        switch (_stage)
        {
            case Stage.Countdown: return "COUNTDOWN";
            case Stage.Wave: return "WAVE";
            case Stage.WaveEnd: return "WAVE END";
            case Stage.Intermission: return "INTERMISSION";
            default: return "RESULTS";
        }
    }
}

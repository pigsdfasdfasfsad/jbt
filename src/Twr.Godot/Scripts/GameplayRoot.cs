using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

public partial class GameplayRoot : Node3D
{
    public LocalSessionNode? Runtime { get; set; }
    public string MapName { get; set; } = "Manor";
    public Action? ExitRequested { get; set; }

    private GameplayHud _hud = null!;
    private FirstPersonPlayer _player = null!;
    private readonly List<InfectedAgent> _infected = [];
    private readonly List<ObjectiveRuntime> _objectives = [];
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
        BuildManorBlockout();

        _player = new FirstPersonPlayer
        {
            Name = "Player",
            Runtime = Runtime,
            Position = new Vector3(0, 1.0f, 18)
        };
        AddChild(_player);

        _hud = new GameplayHud { Name = "HUD" };
        AddChild(_hud);
        _hud.SetBanner($"WAVE {Runtime.Match?.Wave ?? 1} BEGINS IN");
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
        _hud.SetBanner($"WAVE {survivedWave} SURVIVED");

        // APPROXIMATED boundary behavior: surviving infected are cleared for
        // intermission until exact retail teardown behavior is recovered.
        ClearInfected();
        ClearObjectives();
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
        var families = new[] { "Radio", "Load", "Unpack", "Repair", "Escort" };
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

    private Vector3 ObjectivePosition(int index) => index == 0
        ? new Vector3(0, 0, -8)
        : new Vector3(10, 0, 12);

    private void OnObjectiveCompleted(ObjectiveRuntime objective)
    {
        _completedObjectivesThisWave++;
        _objectives.Remove(objective);

        if (objective.Family == "Radio")
            _hud.SetBanner("SUPPLY HELICOPTER EN ROUTE");
        else if (objective.Family == "Unpack")
            _hud.SetBanner("SUPPLY CRATE UNPACKED");

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
        _infected.Add(infected);
        AddChild(infected);
    }

    private Vector3 RandomSpawnPoint()
    {
        var side = _rng.RandiRange(0, 3);
        var offset = _rng.RandfRange(-29f, 29f);
        return side switch
        {
            0 => new Vector3(offset, 1, -38),
            1 => new Vector3(offset, 1, 38),
            2 => new Vector3(-33, 1, offset),
            _ => new Vector3(33, 1, offset)
        };
    }

    private void OnInfectedDied(InfectedAgent infected)
    {
        _infected.Remove(infected);
        var reward = InfectedCatalog.Reward(infected.InfectedType);
        Runtime?.AwardKill(infected.InfectedType, reward.Credits, reward.Xp);
    }

    private void Finish(bool won, string title)
    {
        if (_finished) return;
        if (!won) Runtime?.FailMatch("All players dead");

        _finished = true;
        _stage = Stage.Results;
        ClearInfected();
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

    private void BuildManorBlockout()
    {
        // APPROXIMATED geometry. Manor topology follows surviving references,
        // but measurements are not source-surveyed.
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, -25, 0),
            LightEnergy = 1.15f,
            ShadowEnabled = true
        });

        StaticBox("Ground", new Vector3(0, -0.5f, 0), new Vector3(72, 1, 84), new Color(0.12f, 0.13f, 0.12f));
        StaticBox("NorthWall", new Vector3(0, 3, -42), new Vector3(72, 6, 1), new Color(0.19f, 0.18f, 0.17f));
        StaticBox("SouthWall", new Vector3(0, 3, 42), new Vector3(72, 6, 1), new Color(0.19f, 0.18f, 0.17f));
        StaticBox("WestWall", new Vector3(-36, 3, 0), new Vector3(1, 6, 84), new Color(0.19f, 0.18f, 0.17f));
        StaticBox("EastWall", new Vector3(36, 3, 0), new Vector3(1, 6, 84), new Color(0.19f, 0.18f, 0.17f));

        DecorativeBox("WestWing", new Vector3(-20, 2.5f, -6), new Vector3(18, 5, 48), new Color(0.22f, 0.20f, 0.18f));
        DecorativeBox("EastWing", new Vector3(20, 2.5f, -6), new Vector3(18, 5, 48), new Color(0.22f, 0.20f, 0.18f));
        DecorativeBox("FrontHall", new Vector3(0, 2.5f, 29), new Vector3(26, 5, 10), new Color(0.25f, 0.22f, 0.19f));

        for (var z = -24; z <= 18; z += 14)
        {
            DecorativeBox($"CourtyardCoverL{z}", new Vector3(-6, 1, z), new Vector3(2, 2, 5), new Color(0.25f, 0.26f, 0.24f));
            DecorativeBox($"CourtyardCoverR{z}", new Vector3(6, 1, z), new Vector3(2, 2, 5), new Color(0.25f, 0.26f, 0.24f));
        }
    }

    private void StaticBox(string name, Vector3 position, Vector3 size, Color color)
    {
        var body = new StaticBody3D { Name = name, Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        body.AddChild(BoxMeshFor(size, color));
        AddChild(body);
    }

    private void DecorativeBox(string name, Vector3 position, Vector3 size, Color color)
    {
        var mesh = BoxMeshFor(size, color);
        mesh.Name = name;
        mesh.Position = position;
        AddChild(mesh);
    }

    private static MeshInstance3D BoxMeshFor(Vector3 size, Color color) => new()
    {
        Mesh = new BoxMesh
        {
            Size = size,
            Material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.95f }
        }
    };
}

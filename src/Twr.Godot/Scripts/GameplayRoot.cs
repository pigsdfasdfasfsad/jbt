using System.Text.Json;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

public partial class GameplayRoot : Node3D
{
    public LocalSessionNode? Runtime { get; set; }
    public string MapName { get; set; } = "Manor";
    public Action? ExitRequested { get; set; }
    public Action<string>? RestartRequested { get; set; }

    private GameplayHud _hud = null!;
    private RuntimeMapDefinition _mapDefinition = null!;
    private RuntimeMapLayout _mapLayout = null!;
    private FirstPersonPlayer _player = null!;
    private FortificationController _fortifications = null!;
    private readonly List<InfectedAgent> _infected = [];
    private readonly List<ObjectiveRuntime> _objectives = [];
    private readonly List<PickupActor> _pickups = [];
    private readonly List<SupplyDropRuntime> _supplyDrops = [];
    private readonly List<NaturalRespawn> _naturalRespawns = [];
    private int _completedObjectivesThisWave;
    private readonly RandomNumberGenerator _rng = new();
    private Stage _stage = Stage.Countdown;
    private double _stageTime = RegularWaveRules.CountdownSeconds;
    private double _spawnTimer;
    private bool _supplyQueuedForNextWave;
    private double _queuedSupplySeconds=-1;

    // FITTED in the recovered TestPlace directive: Regular restocks item and
    // fortification markers every 20 seconds. The original retail source did
    // not expose this interval directly.
    private const double NaturalRespawnMinSeconds=20.0;
    private const double NaturalRespawnMaxSeconds=20.0;
    private readonly record struct NaturalRespawn(Vector3 Position,string Group,double Remaining);
    private bool _finished;
    private PauseOverlayRuntime? _pauseOverlay;

    private enum Stage { Countdown, Wave, WaveEnd, Intermission, Results }

    public override void _Ready()
    {
        if (Runtime?.Session is null)
            throw new InvalidOperationException("GameplayRoot requires an initialized LocalSessionNode.");

        _rng.Randomize();
        _mapDefinition = MapCatalogRuntime.Get(MapName);
        _mapLayout = MapName == "Laboratory" && LaboratorySourceLoader.TryBuild(this, out var recovered)
            ? recovered
            : MapBlockoutBuilder.Build(this, _mapDefinition);

        _player = new FirstPersonPlayer
        {
            Name = "Player",
            Runtime = Runtime,
            Position = _mapLayout.PlayerSpawn
        };
        AddChild(_player);
        GetNodeOrNull<LaboratoryLightStreamer>("RecoveredLaboratory/LaboratoryLights")
            ?.Track(_player);

        _hud = new GameplayHud { Name = "HUD", Runtime = Runtime };
        AddChild(_hud);
        Runtime.PresentationEvent += OnPresentationEvent;
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

        if(!_finished) TickNaturalRespawns(delta);

        if (!_finished && !Runtime.Player.IsAlive)
        {
            Finish(false, "YOU DIED");
            return;
        }

        if (_finished)
        {
            _hud.UpdateState(Runtime.Player, Runtime.Match, "RESULTS", 0, _player.EquippedWeaponName,_player.HeldThrowableName);
            return;
        }

        _stageTime = Math.Max(0, _stageTime - delta);

        switch (_stage)
        {
            case Stage.Countdown:
                if (_stageTime <= 0) StartWave();
                break;
            case Stage.Wave:
                if(_queuedSupplySeconds>=0)
                {
                    _queuedSupplySeconds-=delta;
                    if(_queuedSupplySeconds<=0)
                    {
                        _queuedSupplySeconds=-1;
                        SpawnSupplyDrop();
                    }
                }
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

        _hud.UpdateState(Runtime.Player, Runtime.Match, StageName(), _stageTime, _player.EquippedWeaponName,_player.HeldThrowableName);
    }

    public override void _ExitTree()
    {
        if(Runtime is not null)
            Runtime.PresentationEvent -= OnPresentationEvent;
    }

    private void OnPresentationEvent(string eventName)
    {
        if(eventName=="PlayerDamagedEvent")
            _hud.FlashDamage();
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        if (!_finished && (key.Keycode is Key.P or Key.Escape))
        {
            OpenPause();
            return;
        }

        if (!_finished) return;

        if (key.Keycode is Key.Enter or Key.KpEnter)
        {
            Runtime?.ReturnToLobby();
            ExitRequested?.Invoke();
        }
        else if (key.Keycode == Key.R)
        {
            Runtime?.ReturnToLobby();
            RestartRequested?.Invoke(MapName);
        }
    }

    private void OpenPause()
    {
        if (_pauseOverlay is not null || _finished) return;

        _pauseOverlay = new PauseOverlayRuntime { Name = "PauseOverlay" };
        _pauseOverlay.ResumeRequested = ResumePause;
        _pauseOverlay.RestartRequested = RestartFromPause;
        _pauseOverlay.QuitRequested = QuitFromPause;
        AddChild(_pauseOverlay);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
    }

    private void ResumePause()
    {
        GetTree().Paused = false;
        _pauseOverlay?.QueueFree();
        _pauseOverlay = null;
        if (!_finished) Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void RestartFromPause()
    {
        GetTree().Paused = false;
        _pauseOverlay?.QueueFree();
        _pauseOverlay = null;
        if (!_finished)
        {
            Runtime?.FailMatch("Player restarted map");
            _finished = true;
        }
        Runtime?.ReturnToLobby();
        RestartRequested?.Invoke(MapName);
    }

    private void QuitFromPause()
    {
        GetTree().Paused = false;
        _pauseOverlay?.QueueFree();
        _pauseOverlay = null;
        if (!_finished)
        {
            Runtime?.FailMatch("Player quit to menu");
            _finished = true;
        }
        Runtime?.ReturnToLobby();
        ExitRequested?.Invoke();
    }

    private void StartWave()
    {
        if (Runtime?.Match is null) return;
        _stage = Stage.Wave;
        _stageTime = RegularWaveRules.WaveDurationSeconds;
        _spawnTimer = 0;
        _completedObjectivesThisWave = 0;
        if(_supplyQueuedForNextWave)
        {
            _supplyQueuedForNextWave=false;
            _queuedSupplySeconds=3.0;
        }
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

        // RECOVERED rule: Regular starts one or two objectives every wave.
        var count = _rng.RandiRange(1, 2);
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
            // RECOVERED bug/behavior: if Radio finishes with under ~20 seconds
            // left, the helicopter is deferred into the next wave.
            if(_stage==Stage.Wave && _stageTime<20)
            {
                _supplyQueuedForNextWave=true;
                _hud.SetBanner("SUPPLY DROP QUEUED FOR NEXT WAVE");
            }
            else
            {
                _hud.SetBanner("SUPPLY HELICOPTER EN ROUTE");
                SpawnSupplyDrop();
            }
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
        // The recovered Laboratory pack supplies distinct original item and
        // fortification marker pools. Keep only a small opening selection,
        // without repeating a marker in the same spawn batch.
        foreach (var position in ChooseUniqueMarkers(_mapLayout.PickupPoints, 4))
            SpawnNaturalAt(position, "Item");

        if (_mapLayout.FortificationPoints.Count > 0)
        {
            foreach (var position in ChooseUniqueMarkers(_mapLayout.FortificationPoints, 2))
                SpawnNaturalAt(position, "Fortification");
        }
        else
        {
            // Legacy blockout maps have no recovered fortification markers.
            // Keep their existing approximate positions until proper source
            // location data is obtained for each individual map.
            var fortCount=Math.Min(2,_mapLayout.PickupPoints.Count);
            for(var i=0;i<fortCount;i++)
            {
                var basePoint=_mapLayout.PickupPoints[i];
                var offset=new Vector3(i%2==0 ? 1.8f : -1.8f,0,(i%3-1)*1.2f);
                SpawnNaturalAt(basePoint+offset,"Fortification");
            }
        }
    }

    private IEnumerable<Vector3> ChooseUniqueMarkers(IReadOnlyList<Vector3> markers, int count)
    {
        var indices = Enumerable.Range(0, markers.Count).ToArray();
        for (var index = 0; index < Math.Min(count, markers.Count); index++)
        {
            var next = _rng.RandiRange(index, indices.Length - 1);
            (indices[index], indices[next]) = (indices[next], indices[index]);
            yield return markers[indices[index]];
        }
    }

    private void SpawnNaturalAt(Vector3 position,string group)
    {
        if(group=="Fortification")
        {
            var forts=new[]{"Barbed Wire","Clap Bomb","Jack"};
            SpawnPickup(forts[_rng.RandiRange(0,forts.Length-1)],position,1,true,group);
            return;
        }

        var items=new[]{"Bandages","Ammo","Body Armor","Medkit","Energy Drink","Gas Mask","Frag","Molotov","Nerve Gas"};
        SpawnPickup(items[_rng.RandiRange(0,items.Length-1)],position,1,true,group);
    }

    private void TickNaturalRespawns(double delta)
    {
        for(var i=_naturalRespawns.Count-1;i>=0;i--)
        {
            var entry=_naturalRespawns[i];
            var remaining=entry.Remaining-delta;
            if(remaining>0)
            {
                _naturalRespawns[i]=entry with { Remaining=remaining };
                continue;
            }

            _naturalRespawns.RemoveAt(i);
            SpawnNaturalAt(entry.Position,entry.Group);
        }
    }

    private void ScheduleNaturalRespawn(Vector3 position,string group)
    {
        var seconds=_rng.RandfRange((float)NaturalRespawnMinSeconds,(float)NaturalRespawnMaxSeconds);
        _naturalRespawns.Add(new NaturalRespawn(position,group,seconds));
    }

    private void SpawnPickup(string type, Vector3 position, int grantCount = 1, bool natural=false, string naturalGroup="")
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
        pickup.Collected = collected =>
        {
            _pickups.Remove(collected);
            if(natural)ScheduleNaturalRespawn(position,naturalGroup);
        };
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
        // RECOVERED payload contract: exactly four Regular slots and four Fort
        // slots. Regular pool is Medkit/Ammo/Body Armor. Fort pool includes
        // 50 Cal/Barbed Wire/Clap Bomb/Jack, with 0..4 50 Cals per drop.
        var regularPool=new[] { "Medkit","Ammo","Body Armor" };
        for(var i=0;i<4;i++)
        {
            var kind=regularPool[_rng.RandiRange(0,regularPool.Length-1)];
            SpawnPickup(kind,RingPoint(center,i,8,2.3f));
        }

        var forts=LoadFortificationCatalog();
        var nonFifty=forts.Where(x=>x.Name!="50 Cal").ToArray();
        var fifty=forts.FirstOrDefault(x=>x.Name=="50 Cal");
        var nFifty=_rng.RandiRange(0,4);
        for(var i=0;i<4;i++)
        {
            FortPickup fort;
            if(i<nFifty && !string.IsNullOrWhiteSpace(fifty.Name))
                fort=fifty;
            else if(nonFifty.Length>0)
                fort=nonFifty[_rng.RandiRange(0,nonFifty.Length-1)];
            else
            {
                SpawnPickup("Ammo",RingPoint(center,i+4,8,2.3f));
                continue;
            }
            SpawnPickup(fort.Name,RingPoint(center,i+4,8,2.3f),fort.Count);
        }
    }

    private void SpawnUnpackContents(Vector3 center)
    {
        // RECOVERED reconstruction contract: random Ammo or Medical variant,
        // always exactly eight payload pickups. The Medical Bandage/Medkit ratio
        // is explicitly undocumented; the directive uses a uniform 0..8 split.
        var ammoVariant=_rng.RandiRange(1,2)==1;
        var bandages=ammoVariant ? 0 : _rng.RandiRange(0,8);
        for(var i=0;i<8;i++)
        {
            var type=ammoVariant ? "Ammo" : (i<bandages ? "Bandages" : "Medkit");
            SpawnPickup(type,RingPoint(center,i,8,2.5f));
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
        // Source markers are foot/contact positions, whereas the infected
        // collider spans 1.8m about its node origin. Lift its centre 0.9m
        // above the exact source marker; never jitter source X/Z coordinates.
        if (_mapLayout.UseExactInfectedSpawns) return basePoint + Vector3.Up * 0.9f;
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
        ClearObjectives();
        _fortifications.ClearDeployed();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _hud.SetBanner($"{title}  -  [R] RESTART  |  [ENTER] MENU");
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

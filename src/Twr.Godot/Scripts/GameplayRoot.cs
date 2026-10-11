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
    private Pass25SourceNavigationRuntime? _sourceNavigator;
    private Pass34ClientWallsRuntime? _originalClientWalls;
    private Pass35InfectedWallsRuntime? _originalInfectedWalls;
    private Pass36SourceFragmentsRuntime? _sourceFragments;
    private Pass36MapPlanOverlay? _sourceMapPlan;
    private Pass37SourceLightingRuntime? _sourceLighting;
    private Pass38SourceSoundscapeRuntime? _sourceSoundscape;
    private Pass39LaboratoryFloorplan? _labFloorplan;
    private OfflineAudioRuntime _audio = null!;
    private RuntimeMapDefinition _mapDefinition = null!;
    private RuntimeMapLayout _mapLayout = null!;
    private FirstPersonPlayer _player = null!;
    private FortificationController _fortifications = null!;
    private readonly List<InfectedAgent> _infected = [];
    private int _pass43RetiredJumpAttempts;
    private int _pass43RetiredJumpLandings;
    private int _pass43RetiredJumpFailures;
    public Pass43FrameWindow Pass43FrameTimes { get; } = new();
    // Counts survive zombie deaths, wave teardown, and the Results screen.
    public int Pass43TotalJumpAttempts =>
        _pass43RetiredJumpAttempts + _infected.Where(GodotObject.IsInstanceValid)
            .Sum(actor => actor.SourceJumpAttempts);
    public int Pass43VerifiedJumpLandings =>
        _pass43RetiredJumpLandings + _infected.Where(GodotObject.IsInstanceValid)
            .Sum(actor => actor.SourceJumpLandings);
    public int Pass43FailedJumpAttempts =>
        _pass43RetiredJumpFailures + _infected.Where(GodotObject.IsInstanceValid)
            .Sum(actor => actor.SourceJumpFailures);
    public int Pass43CurrentlyJumping =>
        _infected.Count(actor => GodotObject.IsInstanceValid(actor) &&
            actor.SourceJumpInProgress);
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
    public ulong MapLoadMilliseconds { get; private set; }
    public int ActiveInfectedCount => _infected.Count(agent => GodotObject.IsInstanceValid(agent));
    public int ActivePickupCount => _pickups.Count(actor => GodotObject.IsInstanceValid(actor));
    public string WaveStage => StageName();
    public int OriginalClientWallCount => _originalClientWalls?.WallCount ?? 0;
    public bool OriginalClientWallsEnabled => _originalClientWalls?.Enabled ?? false;
    public int OriginalInfectedWallCount => _originalInfectedWalls?.WallCount ?? 0;
    public int OriginalInfectedMeshProxyCount => _originalInfectedWalls?.ApproximateMeshProxyCount ?? 0;
    public bool OriginalInfectedWallsEnabled => _originalInfectedWalls?.Enabled ?? false;
    public int Pass36SourceServerWallCount => _sourceFragments?.WallCount ?? 0;
    public int Pass36RenderableOriginalPropCount => _sourceFragments?.NativeObjectCount ?? 0;
    public int Pass36MissingOriginalMeshCount => _sourceFragments?.MissingOriginalCustomObjectCount ?? 0;
    public bool Pass36SourceWallsEnabled => _sourceFragments?.ServerCollisionEnabled ?? false;
    public bool Pass36NativePropsEnabled => _sourceFragments?.NativeObjectPreviewEnabled ?? false;
    public bool Pass36HasOriginalMapPlan => _sourceMapPlan?.HasPlan ?? false;
    public int Pass37SourceLightingEffectCount => _sourceLighting?.SourceEffectCount ?? 0;
    public bool Pass37SourceLightingEnabled => _sourceLighting?.Enabled ?? false;
    public int Pass38SourceSoundEmitterCount => _sourceSoundscape?.SourceEmitterCount ?? 0;
    public int Pass38SourcePositionedEmitterCount => _sourceSoundscape?.SourcePositionedCount ?? 0;
    public int Pass38OfflineWavEmitterCount => _sourceSoundscape?.InstalledWavEmitterCount ?? 0;
    public int Pass38ActiveSourceVoices => _sourceSoundscape?.ActiveVoiceCount ?? 0;
    public bool Pass38SourceSoundEnabled => _sourceSoundscape?.Enabled ?? false;
    public bool Pass39OriginalLabSourceVerified => _labFloorplan?.VerifiedOriginalScene ?? false;
    public int Pass39LabFloorCount => _labFloorplan?.LevelCount ?? 0;
    public int Pass39LabCurrentFloor => _labFloorplan?.ActiveLevel ?? -1;
    public bool Pass40NavigationPlanAvailable => _labFloorplan?.HasNavigationDiagnostic ?? false;
    public bool Pass40NavigationPlanOpen => _labFloorplan?.IsShowingNavigation ?? false;
    public bool AssistedInfectedSpawnsEnabled { get; private set; }
    public int AssistedInfectedSpawnCount { get; private set; }
    public int AssistedInfectedRecoveryCount { get; private set; }
    public int RejectedAssistedSpawnAttempts { get; private set; }
    public int Pass40AdaptiveNearSpawns { get; private set; }
    public int Pass40AdaptiveNearRecoveries { get; private set; }
    public bool Pass40GroundedNavigationVerified =>
        _sourceNavigator?.IsPass40GroundedGraph ?? false;
    public bool Pass41SourceNativeBridgeVerified =>
        _sourceNavigator?.IsPass41SourceNativeGraph ?? false;
    public int Pass41NativeBridgeCount =>
        _sourceNavigator?.Pass41NativeBridgeCount ?? 0;
    public int Pass41RemainingNavigationComponents =>
        _sourceNavigator?.ConnectedComponentCount ?? 0;
    public bool Pass41BridgeDiagramAvailable =>
        _labFloorplan?.HasPass41RepairDiagnostic ?? false;
    public bool Pass42JumpGraphVerified => _sourceNavigator?.IsPass42JumpGraph ?? false;
    public int Pass42JumpLinkCount => _sourceNavigator?.Pass42JumpEdgeCount ?? 0;
    public bool Pass42JumpDiagramAvailable => _labFloorplan?.HasPass42JumpDiagnostic ?? false;
    public int Pass42JumpAttempts => _infected.Where(GodotObject.IsInstanceValid)
        .Sum(infected => infected.SourceJumpAttempts);
    public int Pass42JumpLandings => _infected.Where(GodotObject.IsInstanceValid)
        .Sum(infected => infected.SourceJumpLandings);
    public int Pass44NativePrimitiveCount =>
        GetNodeOrNull<Pass28PrimitiveStreamer>("Recovered" + MapName +
            "/Pass28PrimitiveStream")?.SourceInstanceCount ?? 0;
    public int Pass44NativeDrawBatches =>
        GetNodeOrNull<Pass28PrimitiveStreamer>("Recovered" + MapName +
            "/Pass28PrimitiveStream")?.BatchCount ?? 0;
    public int Pass44VisibleNativeBatches =>
        GetNodeOrNull<Pass28PrimitiveStreamer>("Recovered" + MapName +
            "/Pass28PrimitiveStream")?.VisibleBatchCount ?? 0;
    public bool Pass45ProxyStreamingAvailable =>
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream") is not null;
    public bool Pass45ProxyCullEnabled =>
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream")?.CullEnabled ?? false;
    public int Pass45ProxyBatchCount =>
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream")?.BatchCount ?? 0;
    public int Pass45ProxyVisibleBatches =>
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream")?.VisibleBatchCount ?? 0;
    public int Pass45ProxySourceInstances =>
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream")?.SourceProxyInstanceCount ?? 0;
    public int Pass45ProxyVisibleInstances =>
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream")?.VisibleInstanceCount ?? 0;
    private double _pass32RecoveryScan = 1.5;

    private enum Stage { Countdown, Wave, WaveEnd, Intermission, Results }

    public override void _Ready()
    {
        var startup = Time.GetTicksMsec();
        if (Runtime?.Session is null)
            throw new InvalidOperationException("GameplayRoot requires an initialized LocalSessionNode.");

        _rng.Randomize();
        _mapDefinition = MapCatalogRuntime.Get(MapName);
        // Prefer original loaded-map geometry from each of the ten privately
        // held Roblox place snapshots; retain blockouts as pack-free fallbacks.
        _mapLayout = LaboratorySourceLoader.TryBuild(this, MapName, out var recovered)
            ? recovered
            : MapBlockoutBuilder.Build(this, _mapDefinition);

        // Load independently decoded original SmoothGrid terrain after map
        // geometry; no Roblox Studio, network or editor imports are needed.
        RecoveredTerrainRuntime.TryBuild(this, MapName);
        // Source's invisible Client Walls were placed in Roblox collision
        // group 8. Its collision matrix was not exported; do not assert
        // player-only behavior until F8 explicitly enables testing.
        _originalClientWalls = Pass34ClientWallsRuntime.TryBuild(this,MapName);
        // Source Infected Walls are fragments; original collision group
        // interactions have not been recovered. F7 only, OFF by default.
        _originalInfectedWalls = Pass35InfectedWallsRuntime.TryBuild(this, MapName);
        // The remaining nine maps have CMaps source collision and LOD object
        // positions, but NOT their complete visual/terrain models. Never
        // replace their approximated blockouts without evidence.
        _sourceFragments = Pass36SourceFragmentsRuntime.TryBuild(this, MapName);
        _sourceMapPlan = Pass36MapPlanOverlay.TryBuild(this, MapName);
        // Laboratory has a distinct exact-loaded-source three-height map plan.
        // Unlike the nine-map server-wall fragments, it is SHA-bound to the
        // owner-recovered full Workspace/Map scene, not approximate blockouts.
        _labFloorplan = Pass39LaboratoryFloorplan.TryBuild(this, MapName);
        _sourceLighting = Pass37SourceLightingRuntime.TryBuild(this, MapName);
        _sourceNavigator = Pass25SourceNavigationRuntime.TryBuild(this, MapName);

        _audio = new OfflineAudioRuntime { Name = "OfflineAudio" };
        AddChild(_audio);
        _audio.Play("ambient");

        _player = new FirstPersonPlayer
        {
            Name = "Player",
            Audio = _audio,
            Runtime = Runtime,
            Position = _mapLayout.PlayerSpawn
        };
        AddChild(_player);
        _sourceSoundscape = Pass38SourceSoundscapeRuntime.TryBuild(this, MapName,
            GetNodeOrNull<Node3D>("Recovered" + MapName) is not null);
        _sourceSoundscape?.Track(_player);
        GetNodeOrNull<LaboratoryLightStreamer>("Recovered" + MapName + "/LaboratoryLights")
            ?.Track(_player);
        GetNodeOrNull<Pass28PrimitiveStreamer>("Recovered" + MapName +
            "/Pass28PrimitiveStream")?.Track(_player);
        GetNodeOrNull<Pass45FallbackProxyStreamer>("Recovered" + MapName +
            "/Pass45FallbackProxyStream")?.Track(_player);

        _hud = new GameplayHud { Name = "HUD", Runtime = Runtime };
        AddChild(_hud);
        // Dictionary-backed inventory is shown read-only; grants are enforced
        // by the authoritative LocalSessionNode, never by the UI.
        AddChild(new Pass29InventoryPanel { Name = "InventoryGrid", Runtime = Runtime });
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
        AddChild(new Pass30DiagnosticsHud { Name = "Pass30Diagnostics", Game = this, MapName = MapName });
        MapLoadMilliseconds = Time.GetTicksMsec() - startup;
        GD.Print($"TWR_PASS30_STARTUP map={MapName} milliseconds={MapLoadMilliseconds} " +
            $"source_nav={_sourceNavigator is not null} " +
            $"streamed={GetNodeOrNull<Pass28PrimitiveStreamer>("Recovered" + MapName + "/Pass28PrimitiveStream") is not null} " +
            $"batched_collision={GetNodeOrNull<Node3D>("Recovered" + MapName + "/Pass26Collision") is not null}");
    }

    public override void _Process(double delta)
    {
        Pass43FrameTimes.Record(delta, ActiveInfectedCount);
        if (Runtime?.Player is null || Runtime.Match is null) return;

        if(!_finished)
        {
            TickNaturalRespawns(delta);
            TickAssistedInfectedRecovery(delta);
        }

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
        {
            _hud.FlashDamage();
            _audio.Play("hurt");
        }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        // F1 is a purely visual A/B test; it never changes collision,
        // navigation, original player spawn or infected AI behavior.
        if (key.Keycode == Key.F1)
        {
            var proxy=GetNodeOrNull<Pass45FallbackProxyStreamer>(
                "Recovered" + MapName + "/Pass45FallbackProxyStream");
            if (proxy is null)
                _hud.SetUtility("F1 source proxy batching unavailable for this map");
            else
            {
                proxy.ToggleCull();
                _hud.SetUtility(proxy.CullEnabled
                    ? "F1 SOURCE PROXY CULL ON (source geometry, same collision)"
                    : "F1 SOURCE PROXY CULL OFF (A/B comparison only)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!_finished && key.Keycode == Key.F2)
        {
            if (_sourceSoundscape is null)
                _hud.SetUtility("Offline source soundscape pack unavailable");
            else
            {
                _sourceSoundscape.SetEnabled(!_sourceSoundscape.Enabled);
                _hud.SetUtility(_sourceSoundscape.Enabled
                    ? "F2 MAP SOUND PREVIEW ON (separate WAV substitutions; not original audio)"
                    : "F2 MAP SOUND PREVIEW OFF (existing game sounds unchanged)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!_finished && key.Keycode == Key.F3)
        {
            if (_sourceLighting is null)
                _hud.SetUtility("Original source lighting pack unavailable");
            else
            {
                _sourceLighting.SetEnabled(!_sourceLighting.Enabled);
                _hud.SetUtility(_sourceLighting.Enabled
                    ? "F3 SOURCE LIGHTING PREVIEW ON (approximation; sky images missing)"
                    : "F3 ORIGINAL LIGHTING PREVIEW OFF (baseline restored)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!_finished && key.Keycode == Key.F6)
        {
            if (_sourceFragments is null)
                _hud.SetUtility("Original Server Wall source fragment unavailable");
            else
            {
                _sourceFragments.SetServerCollision(!_sourceFragments.ServerCollisionEnabled);
                _hud.SetUtility(_sourceFragments.ServerCollisionEnabled
                    ? "F6 SOURCE WALLS ON (original positions; blockout may not align)"
                    : "F6 SOURCE WALLS OFF (approximated blockout preserved)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!_finished && key.Keycode == Key.F5)
        {
            if (_sourceFragments is null)
                _hud.SetUtility("Original small prop source unavailable");
            else
            {
                _sourceFragments.SetNativeObjectPreview(!_sourceFragments.NativeObjectPreviewEnabled);
                _hud.SetUtility(_sourceFragments.NativeObjectPreviewEnabled
                    ? "F5 NATIVE ORIGINAL PROPS ON (custom meshes still missing)"
                    : "F5 ORIGINAL PROP PREVIEW OFF");
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!_finished && key.Keycode == Key.F7)
        {
            if (_originalInfectedWalls is null)
                _hud.SetUtility("No recovered Infected Walls for this map");
            else
            {
                _originalInfectedWalls.SetEnabled(!_originalInfectedWalls.Enabled);
                _hud.SetUtility(_originalInfectedWalls.Enabled
                    ? "INFECTED WALLS ON (experimental source fragments, not full map)"
                    : "INFECTED WALLS OFF (legacy collision)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!_finished && key.Keycode == Key.F8)
        {
            if (_originalClientWalls is null)
                _hud.SetUtility("Source client walls unavailable on this map");
            else
            {
                _originalClientWalls.SetEnabled(!_originalClientWalls.Enabled);
                _hud.SetUtility(_originalClientWalls.Enabled
                    ? "SOURCE CLIENT WALLS ON (experimental player-only collision)"
                    : "SOURCE CLIENT WALLS OFF (legacy collision only)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!_finished && key.Keycode == Key.F9)
        {
            if (_sourceNavigator?.IsBridgePackActive != true)
                _hud.SetUtility("Assisted spawns need the Laboratory nav31 pack");
            else
            {
                AssistedInfectedSpawnsEnabled = !AssistedInfectedSpawnsEnabled;
                _hud.SetUtility(AssistedInfectedSpawnsEnabled
                    ? "ASSISTED SPAWNS ON (F9: source approximations, min 24/14/10m)"
                    : "ASSISTED SPAWNS OFF (exact original source markers)");
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!_finished && key.Keycode == Key.F12)
        {
            FidelityCaptureRuntime.TryCapture(GetViewport(), _player.CaptureCamera, MapName);
            GetViewport().SetInputAsHandled();
            return;
        }

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
        _audio.Play("wave");
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
        _audio.Play("wave-clear");

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

        var spawn = RandomSpawnPoint();
        if (AssistedInfectedSpawnsEnabled && _sourceNavigator?.IsBridgePackActive == true)
        {
            var placement = Pass40AdaptiveEntry.TryFind(
                _sourceNavigator, GetWorld3D(), spawn, _player.GlobalPosition, _rng.Randi());
            if (placement.HasValue)
            {
                spawn = placement.Value.Position;
                AssistedInfectedSpawnCount++;
                if (placement.Value.MinimumDistance <
                    Pass32SpawnSafety.MinimumPlayerDistance)
                    Pass40AdaptiveNearSpawns++;
            }
            else RejectedAssistedSpawnAttempts++;
        }

        var infected = new InfectedAgent
        {
            Name = definition.Name,
            Target = _player,
            Runtime = Runtime,
            SourceNavigator = _sourceNavigator,
            HighwayNavigator = GetNodeOrNull<ExpresswayNavigationRuntime>(
                "ExpresswayReconstruction/HighwayNavigation"),
            InfectedType = definition.Name,
            Health = (float)definition.Health * scale,
            Damage = (float)definition.Damage * scale,
            MoveSpeed = (float)definition.WalkSpeed * speedScale * RobloxUnits.MetersPerStud,
            Position = spawn
        };
        infected.Died = OnInfectedDied;
        infected.SpecialAttackRequested = OnInfectedSpecialAttack;
        _infected.Add(infected);
        AddChild(infected);
    }

    private void TickAssistedInfectedRecovery(double delta)
    {
        // Default source-exact behavior never relocates an infected actor.
        if (!AssistedInfectedSpawnsEnabled || _sourceNavigator?.IsBridgePackActive != true)
            return;
        _pass32RecoveryScan -= delta;
        if (_pass32RecoveryScan > 0) return;
        _pass32RecoveryScan = 1.5;
        if (!GodotObject.IsInstanceValid(_player) || !_player.IsInsideTree()) return;
        var target = _player.GlobalPosition;
        var movedThisScan = 0;
        foreach (var infected in _infected)
        {
            if (!GodotObject.IsInstanceValid(infected) ||
                !infected.NeedsAssistedRecovery(target)) continue;
            var result = Pass40AdaptiveEntry.TryFind(
                _sourceNavigator,GetWorld3D(),infected.GlobalPosition,target,_rng.Randi());
            if (!result.HasValue)
            {
                RejectedAssistedSpawnAttempts++;
                continue;
            }
            if (!infected.ApplyAssistedRecovery(result.Value.Position,target)) continue;
            AssistedInfectedRecoveryCount++;
            if (result.Value.MinimumDistance < Pass32SpawnSafety.MinimumPlayerDistance)
                Pass40AdaptiveNearRecoveries++;
            GD.Print($"TWR_PASS32_ASSISTED_RECOVERY map={MapName} " +
                $"count={AssistedInfectedRecoveryCount}");
            if (++movedThisScan >= 2) break;
        }
    }

    private Vector3 RandomSpawnPoint()
    {
        if (_mapLayout.InfectedSpawns.Count == 0) return new Vector3(0,1,-30);
        var basePoint = _mapLayout.InfectedSpawns[_rng.RandiRange(0, _mapLayout.InfectedSpawns.Count - 1)];
        // Source markers are foot/contact positions, whereas the infected
        // collider spans 1.6m about its node origin. Lift its centre 0.8m
        // above the exact source marker; never jitter source X/Z coordinates.
        if (_mapLayout.UseExactInfectedSpawns) return basePoint + Vector3.Up * 0.8f;
        return basePoint + new Vector3(_rng.RandfRange(-2.5f,2.5f), 0, _rng.RandfRange(-2.5f,2.5f));
    }

    private void OnInfectedDied(InfectedAgent infected, InfectedDeathContext context)
    {
        if (_infected.Remove(infected))
            RetirePass43Traversal(infected);
        if (context.DamageKind == "ObjectiveExplosion") return;

        _audio.Play("infected");
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
            Radius = 15f, // Source spore radius is in Roblox studs.
            GlobalPosition = infected.GlobalPosition + Vector3.Up * 1.2f
        });
    }

    private void SpawnBursterHazard(Vector3 position)
    {
        const float blastRadius = 20f; // Source Burster radius, Roblox studs.
        if (Runtime?.Player?.IsAlive == true &&
            _player.GlobalPosition.DistanceTo(position) <= RobloxUnits.Distance(blastRadius))
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

    private void RetirePass43Traversal(InfectedAgent actor)
    {
        if (!GodotObject.IsInstanceValid(actor)) return;
        _pass43RetiredJumpAttempts += actor.SourceJumpAttempts;
        _pass43RetiredJumpLandings += actor.SourceJumpLandings;
        _pass43RetiredJumpFailures += actor.SourceJumpFailures;
    }

    private void ClearInfected()
    {
        foreach (var infected in _infected.ToArray())
        {
            if (!GodotObject.IsInstanceValid(infected)) continue;
            RetirePass43Traversal(infected);
            infected.QueueFree();
        }
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

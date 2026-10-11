using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Local first-person gameplay instrumentation, not an imported Roblox UI.
/// F10 toggles FPS/source-map telemetry; F11 saves an offline JSON snapshot
/// in Godot's per-user data folder for owner-side 15-wave playtest evidence.
/// Read-only: no rendering, collision, navigation or gameplay mutations.
/// </summary>
public partial class Pass30DiagnosticsHud : CanvasLayer
{
    public GameplayRoot? Game { get; set; }
    public string MapName { get; set; } = "";
    private ColorRect _background = null!;
    private Label _label = null!;
    private double _refresh;

    public override void _Ready()
    {
        Layer = 35;
        _background = new ColorRect
        {
            Name = "DiagnosticsBackground",
            AnchorLeft = 1f, AnchorRight = 1f,
            OffsetLeft = -365f, OffsetRight = -15f,
            OffsetTop = 12f, OffsetBottom = 515f,
            Color = new Color(.025f, .035f, .048f, .88f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        AddChild(_background);
        _label = new Label
        {
            OffsetLeft = 12f, OffsetTop = 10f,
            OffsetRight = 335f, OffsetBottom = 500f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _label.AddThemeFontSizeOverride("font_size", 14);
        _label.AddThemeColorOverride("font_color", Colors.White);
        _background.AddChild(_label);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.F10)
        {
            _background.Visible = !_background.Visible;
            _refresh = 0;
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.F11)
        {
            ExportSnapshot();
            GetViewport().SetInputAsHandled();
        }
    }

    private Pass28PrimitiveStreamer? Primitives =>
        Game?.GetNodeOrNull<Pass28PrimitiveStreamer>(
            "Recovered" + MapName + "/Pass28PrimitiveStream");
    private Pass25SourceNavigationRuntime? Navigation =>
        Game?.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
    private int CollisionTiles =>
        Game?.GetNodeOrNull<Node3D>("Recovered" + MapName + "/Pass26Collision")
            ?.GetChildCount() ?? 0;

    public override void _Process(double delta)
    {
        if (!_background.Visible) return;
        _refresh -= delta;
        if (_refresh > 0) return;
        _refresh = .65;
        var mesh = Primitives;
        var frames = Game?.Pass43FrameTimes.Snapshot();
        _label.Text =
            "PASS 47 | Original 3D tools; F1 proxy cull, F4 maps, F9 assist\n" +
            "F10 HUD - F11 report\n" +
            $"Map: {MapName}  Wave: {Game?.Runtime?.Match?.Wave ?? 0}\n" +
            $"FPS: {Engine.GetFramesPerSecond()}  Load: {Game?.MapLoadMilliseconds ?? 0} ms\n" +
            $"Infected: {Game?.ActiveInfectedCount ?? 0}  Pickups: {Game?.ActivePickupCount ?? 0}\n" +
            $"Original nav nodes: {Navigation?.PointCount ?? 0}  P40 graph: {(Game?.Pass40GroundedNavigationVerified == true ? "YES" : "NO")}\n" +
            $"Pass41 bridges: {Game?.Pass41NativeBridgeCount ?? 0}  components: {Game?.Pass41RemainingNavigationComponents ?? 0}\n" +
            $"Pass42 jump links: {Game?.Pass42JumpLinkCount ?? 0} ({(Game?.Pass42JumpGraphVerified == true ? "VERIFIED" : "UNVERIFIED")})\n" +
            $"Jump outcome (match): {Game?.Pass43TotalJumpAttempts ?? 0} launched / " +
            $"{Game?.Pass43VerifiedJumpLandings ?? 0} landed / " +
            $"{Game?.Pass43FailedJumpAttempts ?? 0} failed\n" +
            $"Jump actions in flight: {Game?.Pass43CurrentlyJumping ?? 0}\n" +
            $"Real frame ms (p50/p95/p99): {frames?.MedianMs ?? 0:F1} / " +
            $"{frames?.P95Ms ?? 0:F1} / {frames?.P99Ms ?? 0:F1} " +
            $"({frames?.SampledFrames ?? 0} samples)\n" +
            $"F4/N repaired diagrams: {(Game?.Pass41BridgeDiagramAvailable == true ? "READY" : "NOT VERIFIED")}\n" +
            $"Collision tiles: {CollisionTiles}\n" +
            $"Verified original Lab: {(Game?.Pass39OriginalLabSourceVerified == true ? "YES" : "NO")}  Plans: {Game?.Pass39LabFloorCount ?? 0}\n" +
            $"F4-N nav graph: {(Game?.Pass40NavigationPlanAvailable == true ? "READY" : "N/A")} / {(Game?.Pass40NavigationPlanOpen == true ? "ON" : "OFF")}\n" +
            $"Original client walls: {Game?.OriginalClientWallCount ?? 0} " +
            $"(F8: {(Game?.OriginalClientWallsEnabled == true ? "ON" : "OFF")})\n" +
            $"Original infected walls: {Game?.OriginalInfectedWallCount ?? 0} " +
            $"(F7: {(Game?.OriginalInfectedWallsEnabled == true ? "ON" : "OFF")})\n" +
            $"Missing MeshPart shapes approximated: {Game?.OriginalInfectedMeshProxyCount ?? 0}\n" +
            $"F6 source server walls: {Game?.Pass36SourceServerWallCount ?? 0} " +
            $"({(Game?.Pass36SourceWallsEnabled == true ? "ON" : "OFF")})\n" +
            $"F5 original native props: {Game?.Pass36RenderableOriginalPropCount ?? 0} " +
            $"({(Game?.Pass36NativePropsEnabled == true ? "ON" : "OFF")})\n" +
            $"Original custom props missing: {Game?.Pass36MissingOriginalMeshCount ?? 0}\n" +
            $"Original source weapon: {(Game?.Pass47OriginalWeaponModelActive == true ? "ACTIVE" : "FALLBACK")}" +
            $" (pack SHA verified: {Game?.Pass47OwnerWeaponPackVerified ?? false})\n" +
            $"Weapon visible source parts: {Game?.Pass47WeaponSourceVisibleParts ?? 0} " +
            $"missing triangle proxies: {Game?.Pass47MissingMeshProxies ?? 0}\n" +
            $"Source proxy batches: {Game?.Pass45ProxyVisibleBatches ?? 0} / " +
            $"{Game?.Pass45ProxyBatchCount ?? 0}  source proxies visible: " +
            $"{Game?.Pass45ProxyVisibleInstances ?? 0}/{Game?.Pass45ProxySourceInstances ?? 0}\n" +
            $"F1 proxy culling: {(Game?.Pass45ProxyStreamingAvailable != true ? "UNAVAILABLE" :
                Game.Pass45ProxyCullEnabled ? "ON" : "OFF")}\n" +
            $"Source native instancing: {Game?.Pass44NativePrimitiveCount ?? 0} parts / " +
            $"{Game?.Pass44NativeDrawBatches ?? 0} batches\n" +
            $"Visible native batches: {mesh?.VisibleBatchCount ?? 0}/{mesh?.BatchCount ?? 0}\n" +
            $"Source primitive instances: {mesh?.SourceInstanceCount ?? 0}\n" +
            $"Nav queries: {Navigation?.RouteRequests ?? 0} / A*: {Navigation?.ActualPathSearches ?? 0}\n" +
            $"Cached routes: {Navigation?.RouteCacheHits ?? 0} hits " +
            $"({Navigation?.CachedRouteCount ?? 0} entries)\n" +
            $"Disconnected rejects: {Navigation?.DisconnectedRouteRejects ?? 0}\n" +
            $"F9 spawn assist: {(Game?.AssistedInfectedSpawnsEnabled == true ? "ON" : "OFF")}, " +
            $"redirected: {Game?.AssistedInfectedSpawnCount ?? 0}\n" +
            $"Ground-checked rescues: {Game?.AssistedInfectedRecoveryCount ?? 0}\n" +
            $"F9 near (14/10m): {Game?.Pass40AdaptiveNearSpawns ?? 0} spawns, {Game?.Pass40AdaptiveNearRecoveries ?? 0} rescues\n" +
            $"Unsafe candidates rejected: {Game?.RejectedAssistedSpawnAttempts ?? 0}\n" +
            $"Managed memory: {GC.GetTotalMemory(false) / 1048576} MiB";
    }

    private void ExportSnapshot()
    {
        try
        {
            var mesh = Primitives;
            var frames = Game?.Pass43FrameTimes.Snapshot();
            var payload = new
            {
                format = "twr-pass36-gameplay-diagnostics-v1",
                utc = DateTimeOffset.UtcNow.ToString("O"),
                map = MapName,
                wave = Game?.Runtime?.Match?.Wave ?? 0,
                fps = Engine.GetFramesPerSecond(),
                map_load_ms = Game?.MapLoadMilliseconds ?? 0,
                infected_alive = Game?.ActiveInfectedCount ?? 0,
                world_pickups = Game?.ActivePickupCount ?? 0,
                source_navigation_nodes = Navigation?.PointCount ?? 0,
                source_navigation_edges = Navigation?.EdgeCount ?? 0,
                source_navigation_requests = Navigation?.RouteRequests ?? 0,
                source_navigation_astar_searches = Navigation?.ActualPathSearches ?? 0,
                source_navigation_cache_hits = Navigation?.RouteCacheHits ?? 0,
                source_navigation_cache_entries = Navigation?.CachedRouteCount ?? 0,
                source_navigation_disconnected_rejects = Navigation?.DisconnectedRouteRejects ?? 0,
                source_navigation_distant_anchor_rejects = Navigation?.DistantAnchorRejects ?? 0,
                source_collision_tiles = CollisionTiles,
                pass39_verified_original_laboratory = Game?.Pass39OriginalLabSourceVerified ?? false,
                pass39_laboratory_floorplan_layers = Game?.Pass39LabFloorCount ?? 0,
                pass39_laboratory_floorplan_active_level = Game?.Pass39LabCurrentFloor ?? -1,
                pass40_navigation_floorplan_available = Game?.Pass40NavigationPlanAvailable ?? false,
                pass40_navigation_floorplan_selected = Game?.Pass40NavigationPlanOpen ?? false,
                original_source_client_walls = Game?.OriginalClientWallCount ?? 0,
                original_source_client_walls_opt_in = Game?.OriginalClientWallsEnabled ?? false,
                original_source_infected_walls = Game?.OriginalInfectedWallCount ?? 0,
                source_infected_walls_opt_in = Game?.OriginalInfectedWallsEnabled ?? false,
                source_infected_wall_approximate_meshes = Game?.OriginalInfectedMeshProxyCount ?? 0,
                source_server_wall_count = Game?.Pass36SourceServerWallCount ?? 0,
                source_server_walls_enabled = Game?.Pass36SourceWallsEnabled ?? false,
                source_native_original_objects = Game?.Pass36RenderableOriginalPropCount ?? 0,
                source_original_objects_preview_enabled = Game?.Pass36NativePropsEnabled ?? false,
                source_missing_custom_object_meshes = Game?.Pass36MissingOriginalMeshCount ?? 0,
                original_server_wall_plan_available = Game?.Pass36HasOriginalMapPlan ?? false,
                source_lighting_effect_count = Game?.Pass37SourceLightingEffectCount ?? 0,
                source_lighting_preview_enabled = Game?.Pass37SourceLightingEnabled ?? false,
                source_sound_original_emitter_count = Game?.Pass38SourceSoundEmitterCount ?? 0,
                source_sound_positioned_emitter_count = Game?.Pass38SourcePositionedEmitterCount ?? 0,
                source_sound_installed_wav_emitter_count = Game?.Pass38OfflineWavEmitterCount ?? 0,
                source_sound_active_voices = Game?.Pass38ActiveSourceVoices ?? 0,
                source_sound_preview_enabled = Game?.Pass38SourceSoundEnabled ?? false,
                pass47_owner_weapon_pack_sha_verified = Game?.Pass47OwnerWeaponPackVerified ?? false,
                pass47_original_weapon_assembly_active = Game?.Pass47OriginalWeaponModelActive ?? false,
                pass47_weapon_visible_source_parts = Game?.Pass47WeaponSourceVisibleParts ?? 0,
                pass47_weapon_missing_mesh_proxies = Game?.Pass47MissingMeshProxies ?? 0,
                pass45_fallback_proxy_stream_available = Game?.Pass45ProxyStreamingAvailable ?? false,
                pass45_proxy_cull_enabled = Game?.Pass45ProxyCullEnabled ?? false,
                pass45_total_fallback_batches = Game?.Pass45ProxyBatchCount ?? 0,
                pass45_visible_fallback_batches = Game?.Pass45ProxyVisibleBatches ?? 0,
                pass45_total_fallback_instances = Game?.Pass45ProxySourceInstances ?? 0,
                pass45_visible_fallback_instances = Game?.Pass45ProxyVisibleInstances ?? 0,
                pass44_source_native_stream_loaded = (Game?.Pass44NativePrimitiveCount ?? 0) > 0,
                pass44_source_native_instances = Game?.Pass44NativePrimitiveCount ?? 0,
                pass44_source_native_draw_batches = Game?.Pass44NativeDrawBatches ?? 0,
                pass44_source_visible_batches = Game?.Pass44VisibleNativeBatches ?? 0,
                visible_source_batches = mesh?.VisibleBatchCount ?? 0,
                total_source_batches = mesh?.BatchCount ?? 0,
                source_native_instances = mesh?.SourceInstanceCount ?? 0,
                assisted_infected_spawns_on = Game?.AssistedInfectedSpawnsEnabled ?? false,
                pass42_jump_graph_verified = Game?.Pass42JumpGraphVerified ?? false,
                pass42_jump_edge_count = Game?.Pass42JumpLinkCount ?? 0,
                pass42_jump_overlay_available = Game?.Pass42JumpDiagramAvailable ?? false,
                pass42_infected_jump_attempts = Game?.Pass42JumpAttempts ?? 0,
                pass42_infected_jump_landings = Game?.Pass42JumpLandings ?? 0,
                pass43_jump_launches_match = Game?.Pass43TotalJumpAttempts ?? 0,
                pass43_jump_confirmed_landings_match = Game?.Pass43VerifiedJumpLandings ?? 0,
                pass43_jump_failed_attempts_match = Game?.Pass43FailedJumpAttempts ?? 0,
                pass43_jump_in_flight = Game?.Pass43CurrentlyJumping ?? 0,
                pass43_frame_samples = frames?.SampledFrames ?? 0,
                pass43_frame_median_ms = frames?.MedianMs ?? 0,
                pass43_frame_p95_ms = frames?.P95Ms ?? 0,
                pass43_frame_p99_ms = frames?.P99Ms ?? 0,
                pass43_frame_worst_ms = frames?.WorstMs ?? 0,
                pass43_peak_living_infected = frames?.PeakLivingInfected ?? 0,
                pass43_observed_real_seconds = frames?.ObservedRealSeconds ?? 0,
                pass43_uncontrolled_playtest = true,
                pass41_native_bridge_verified = Game?.Pass41SourceNativeBridgeVerified ?? false,
                pass41_native_bridge_count = Game?.Pass41NativeBridgeCount ?? 0,
                pass41_remaining_navigation_components = Game?.Pass41RemainingNavigationComponents ?? 0,
                pass41_repaired_map_diagrams_available = Game?.Pass41BridgeDiagramAvailable ?? false,
                pass40_grounded_navigation_verified = Game?.Pass40GroundedNavigationVerified ?? false,
                pass40_assisted_close_spawns = Game?.Pass40AdaptiveNearSpawns ?? 0,
                pass40_assisted_close_recoveries = Game?.Pass40AdaptiveNearRecoveries ?? 0,
                assisted_infected_spawn_count = Game?.AssistedInfectedSpawnCount ?? 0,
                assisted_physics_checked_recoveries = Game?.AssistedInfectedRecoveryCount ?? 0,
                unsafe_assisted_spawn_attempts = Game?.RejectedAssistedSpawnAttempts ?? 0,
                managed_memory_bytes = GC.GetTotalMemory(false)
            };
            var path = Path.Combine(OS.GetUserDataDir(), "TWR_Pass33_Diagnostics.json");
            var json=JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path,json); // Legacy latest snapshot path.
            var archival=Path.Combine(OS.GetUserDataDir(),
                "TWR_Pass43_Diagnostics-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".json");
            File.WriteAllText(archival,json); // Distinct captures across waves.
            GD.Print("TWR_PASS43_DIAGNOSTICS_SAVED " + archival);
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS30_DIAGNOSTICS_FAILED: " + error.Message);
        }
    }
}

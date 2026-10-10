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
            OffsetTop = 12f, OffsetBottom = 220f,
            Color = new Color(.025f, .035f, .048f, .88f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        AddChild(_background);
        _label = new Label
        {
            OffsetLeft = 12f, OffsetTop = 10f,
            OffsetRight = 335f, OffsetBottom = 200f,
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
        _label.Text =
            "PASS 30 | F10 toggle - F11 save JSON\n" +
            $"Map: {MapName}  Wave: {Game?.Runtime?.Match?.Wave ?? 0}\n" +
            $"FPS: {Engine.GetFramesPerSecond()}  Load: {Game?.MapLoadMilliseconds ?? 0} ms\n" +
            $"Infected: {Game?.ActiveInfectedCount ?? 0}  Pickups: {Game?.ActivePickupCount ?? 0}\n" +
            $"Original nav nodes: {Navigation?.PointCount ?? 0}\n" +
            $"Collision tiles: {CollisionTiles}\n" +
            $"Visible native batches: {mesh?.VisibleBatchCount ?? 0}/{mesh?.BatchCount ?? 0}\n" +
            $"Source primitive instances: {mesh?.SourceInstanceCount ?? 0}\n" +
            $"Managed memory: {GC.GetTotalMemory(false) / 1048576} MiB";
    }

    private void ExportSnapshot()
    {
        try
        {
            var mesh = Primitives;
            var payload = new
            {
                format = "twr-pass30-gameplay-diagnostics-v1",
                utc = DateTimeOffset.UtcNow.ToString("O"),
                map = MapName,
                wave = Game?.Runtime?.Match?.Wave ?? 0,
                fps = Engine.GetFramesPerSecond(),
                map_load_ms = Game?.MapLoadMilliseconds ?? 0,
                infected_alive = Game?.ActiveInfectedCount ?? 0,
                world_pickups = Game?.ActivePickupCount ?? 0,
                source_navigation_nodes = Navigation?.PointCount ?? 0,
                source_navigation_edges = Navigation?.EdgeCount ?? 0,
                source_collision_tiles = CollisionTiles,
                visible_source_batches = mesh?.VisibleBatchCount ?? 0,
                total_source_batches = mesh?.BatchCount ?? 0,
                source_native_instances = mesh?.SourceInstanceCount ?? 0,
                managed_memory_bytes = GC.GetTotalMemory(false)
            };
            var path = Path.Combine(OS.GetUserDataDir(), "TWR_Pass30_Diagnostics.json");
            File.WriteAllText(path, JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("TWR_PASS30_DIAGNOSTICS_SAVED " + path);
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_PASS30_DIAGNOSTICS_FAILED: " + error.Message);
        }
    }
}

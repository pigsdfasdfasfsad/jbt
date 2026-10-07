using Godot;

namespace Twr.Godot;

public partial class Bootstrap : Node
{
    private LocalSessionNode _runtime = null!;
    private CanvasLayer? _menu;
    private GameplayRoot? _game;

    public override void _Ready()
    {
        _runtime = new LocalSessionNode { Name = "Runtime" };
        AddChild(_runtime);
        ShowMenu();
        if (OS.GetCmdlineUserArgs().Contains("--smoke-play", StringComparer.Ordinal))
            StartGame("Manor");
        GD.Print("TWR Offline: playable Regular-mode reconstruction runtime initialized.");
    }

    private void ShowMenu()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _menu = new CanvasLayer { Name = "MainMenu" };
        AddChild(_menu);

        var background = new ColorRect
        {
            Color = new Color(0.025f, 0.027f, 0.03f),
            AnchorRight = 1,
            AnchorBottom = 1
        };
        _menu.AddChild(background);

        _menu.AddChild(MakeLabel(58, 45, 1160, 62, 40, "THOSE WHO REMAIN - OFFLINE"));
        _menu.AddChild(MakeLabel(60, 106, 1160, 42, 18, "REGULAR  |  15 WAVES  |  LOCAL SINGLE PLAYER"));
        _menu.AddChild(MakeLabel(60, 150, 1160, 72, 16,
            "Select a recovered release map. Geometry is an evidence-guided reconstruction blockout\n" +
            "until original map transforms are recoverable. Gameplay rules remain source-labeled."));
        _menu.AddChild(MakeLabel(60, 650, 1160, 42, 15,
            "WASD move | Shift sprint | Space jump | Mouse aim/fire | R reload | 1/2/3 weapons | F hammer"));

        var maps = MapCatalogRuntime.All();
        for (var i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            var column = i % 2;
            var row = i / 2;
            var left = 64 + column * 590;
            var top = 240 + row * 76;

            var button = new Button
            {
                OffsetLeft = left,
                OffsetTop = top,
                OffsetRight = left + 535,
                OffsetBottom = top + 58,
                Text = map.Name + "  |  " + map.Skybox
            };
            var selected = map.Name;
            button.Pressed += () => StartGame(selected);
            _menu.AddChild(button);
        }
    }

    private static Label MakeLabel(float left, float top, float right, float height, int size, string text)
    {
        var label = new Label
        {
            OffsetLeft = left,
            OffsetTop = top,
            OffsetRight = right,
            OffsetBottom = top + height,
            Text = text
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private void StartGame(string map)
    {
        _menu?.QueueFree();
        _menu = null;

        _runtime.StartMap(map);
        _game = new GameplayRoot
        {
            Name = "Gameplay",
            Runtime = _runtime,
            MapName = map
        };
        _game.ExitRequested = ExitGame;
        AddChild(_game);
    }

    private void ExitGame()
    {
        _game?.QueueFree();
        _game = null;
        ShowMenu();
    }
}

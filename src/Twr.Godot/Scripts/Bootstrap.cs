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

        var title = MakeLabel(70, 70, 1100, 70, 42, "THOSE WHO REMAIN - OFFLINE");
        _menu.AddChild(title);
        _menu.AddChild(MakeLabel(74, 145, 1110, 55, 20, "REGULAR  |  15 WAVES  |  FULLY LOCAL SINGLE PLAYER"));
        _menu.AddChild(MakeLabel(74, 225, 1070, 115, 18,
            "Current playable vertical slice: Manor reconstruction blockout.\n" +
            "Map measurements/art remain provisional until stronger source geometry is recovered.\n" +
            "WASD move | Shift sprint | Space jump | Mouse aim/fire | R reload | 1/2/3 weapons"));

        var play = new Button
        {
            OffsetLeft = 74,
            OffsetTop = 390,
            OffsetRight = 390,
            OffsetBottom = 455,
            Text = "PLAY MANOR"
        };
        play.Pressed += () => StartGame("Manor");
        _menu.AddChild(play);
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

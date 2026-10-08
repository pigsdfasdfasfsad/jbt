using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

public partial class Bootstrap : Node
{
    private LocalSessionNode _runtime = null!;
    private CanvasLayer? _menu;
    private CanvasLayer? _armory;
    private PerkMenuRuntime? _perkMenu;
    private GameplayRoot? _game;

    public override void _Ready()
    {
        _runtime = new LocalSessionNode { Name = "Runtime" };
        AddChild(_runtime);
        var args=OS.GetCmdlineUserArgs();
        if(args.Contains("--smoke-complete",StringComparer.Ordinal))
        {
            RunFullCompletionSmoke();
            return;
        }
        if (args.Contains("--smoke-lab-source", StringComparer.Ordinal))
        {
            RunLaboratorySourceSmoke();
            return;
        }

        ShowMenu();
        if (args.Contains("--smoke-play", StringComparer.Ordinal))
            StartGame("Manor");
        GD.Print("TWR Offline: playable Regular-mode reconstruction runtime initialized.");
    }

    // Unlike the domain-only completion smoke, this test instantiates the
    // packaged Laboratory importer against an explicit synthetic test pack.
    private void RunLaboratorySourceSmoke()
    {
        var testRoot = new Node3D { Name = "LaboratorySourceSmoke" };
        AddChild(testRoot);
        if (!LaboratorySourceLoader.TryBuild(testRoot, out var layout) ||
            layout.InfectedSpawns.Count != 15 ||
            testRoot.GetNodeOrNull<Node3D>("RecoveredLaboratory") is null)
            throw new InvalidOperationException(
                "Laboratory source-pack smoke failed to instantiate the scene.");
        GD.Print("TWR_SMOKE_LAB_SOURCE_OK infected_spawns=" + layout.InfectedSpawns.Count);
        GetTree().Quit(0);
    }

    private void RunFullCompletionSmoke()
    {
        var completed=0;
        foreach(var map in ReleaseRules.Maps)
        {
            _runtime.StartMap(map);
            for(var expectedWave=1;expectedWave<=ReleaseRules.MaxWaves;expectedWave++)
            {
                if(_runtime.Match?.Wave!=expectedWave)
                    throw new InvalidOperationException($"Smoke wave mismatch {map}: expected {expectedWave}, got {_runtime.Match?.Wave}");
                _runtime.AwardWaveSurvival(expectedWave,0,1);
                _runtime.AdvanceWave();
            }

            if(_runtime.Match is null || _runtime.Match.Wave!=ReleaseRules.MaxWaves ||
               _runtime.Match.Phase!=MatchPhase.Results || !_runtime.Match.SaveCommitted ||
               !_runtime.Match.CompletionAwarded)
                throw new InvalidOperationException("Completion smoke failed for " + map);

            GD.Print($"TWR_SMOKE_MAP_OK map={map} wave={_runtime.Match.Wave} save={_runtime.Match.SaveCommitted}");
            _runtime.ReturnToLobby();
            completed++;
        }

        GD.Print($"TWR_SMOKE_COMPLETE_OK maps={completed} waves={completed*ReleaseRules.MaxWaves}");
        GetTree().Quit(0);
    }
    private void ShowMenu()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _menu = new CanvasLayer { Name = "MainMenu" };
        AddChild(_menu);

        AddBackground(_menu);
        _menu.AddChild(MakeLabel(58, 38, 1160, 62, 40, "THOSE WHO REMAIN - OFFLINE"));
        _menu.AddChild(MakeLabel(60, 98, 850, 38, 18, "REGULAR  |  15 WAVES  |  LOCAL SINGLE PLAYER"));
        _menu.AddChild(MakeLabel(60, 142, 1160, 66, 16,
            "Select a recovered release map. Geometry is an evidence-guided reconstruction blockout\n" +
            "until original map transforms are recoverable. Gameplay rules remain source-labeled."));
        _menu.AddChild(MakeLabel(60, 210, 850, 36, 15, ProfileStatusText()));
        _menu.AddChild(MakeLabel(60, 650, 1160, 42, 15,
            "WASD move | Shift sprint | Space jump | Mouse aim/fire | R reload | 1/2/3 weapons | F hammer"));

        var armory = new Button
        {
            OffsetLeft = 930,
            OffsetTop = 95,
            OffsetRight = 1190,
            OffsetBottom = 145,
            Text = "ARMORY / LOADOUT"
        };
        armory.Pressed += ShowArmory;
        _menu.AddChild(armory);

        var perks = new Button
        {
            OffsetLeft = 930,
            OffsetTop = 150,
            OffsetRight = 1190,
            OffsetBottom = 200,
            Text = "PERKS"
        };
        perks.Pressed += ShowPerks;
        _menu.AddChild(perks);

        var maps = MapCatalogRuntime.All();
        for (var i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            var column = i % 2;
            var row = i / 2;
            var left = 64 + column * 590;
            var top = 270 + row * 70;

            var button = new Button
            {
                OffsetLeft = left,
                OffsetTop = top,
                OffsetRight = left + 535,
                OffsetBottom = top + 54,
                Text = map.Name + "  |  " + map.Skybox
            };
            var selected = map.Name;
            button.Pressed += () => StartGame(selected);
            _menu.AddChild(button);
        }
    }

    private void ShowPerks()
    {
        _menu?.QueueFree();
        _menu=null;
        _perkMenu?.QueueFree();
        _perkMenu=new PerkMenuRuntime
        {
            Name="PerkMenu",
            Runtime=_runtime
        };
        _perkMenu.CloseRequested=()=>
        {
            _perkMenu?.QueueFree();
            _perkMenu=null;
            ShowMenu();
        };
        AddChild(_perkMenu);
    }

    private void ShowArmory()
    {
        _menu?.QueueFree();
        _menu = null;
        _armory?.QueueFree();

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _armory = new CanvasLayer { Name = "Armory" };
        AddChild(_armory);
        AddBackground(_armory);

        _armory.AddChild(MakeLabel(58, 34, 1160, 58, 36, "ARMORY / LOADOUT"));
        _armory.AddChild(MakeLabel(60, 92, 1000, 42, 16, ProfileStatusText()));
        _armory.AddChild(MakeLabel(60, 128, 1000, 44, 14,
            "Recovered at-level prices are used. Inflated early-purchase prices are not reconstructed because the source lacks a universal price schedule."));

        var back = new Button
        {
            OffsetLeft = 1010,
            OffsetTop = 42,
            OffsetRight = 1200,
            OffsetBottom = 88,
            Text = "BACK"
        };
        back.Pressed += () =>
        {
            _armory?.QueueFree();
            _armory = null;
            ShowMenu();
        };
        _armory.AddChild(back);

        var scroll = new ScrollContainer
        {
            OffsetLeft = 60,
            OffsetTop = 185,
            OffsetRight = 1210,
            OffsetBottom = 685
        };
        _armory.AddChild(scroll);

        var list = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(1100, 0)
        };
        list.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(list);

        foreach (var spec in RuntimeWeaponCatalog.All())
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(1080, 44),
                Text = ArmoryButtonText(spec),
                Alignment = HorizontalAlignment.Left,
                Disabled = !CanInteractWithArmoryWeapon(spec)
            };
            var selected = spec;
            button.Pressed += () => HandleArmoryWeapon(selected);
            list.AddChild(button);
        }
    }

    private bool CanInteractWithArmoryWeapon(RuntimeWeaponDefinition spec)
    {
        var profile = _runtime.Profile;
        var player = _runtime.Player;
        if (profile is null || player is null) return false;
        if (profile.Unlocks.Contains(spec.Name)) return true;
        return spec.Price > 0 && spec.Level < 999 &&
            player.Level >= spec.Level && player.Credits >= spec.Price;
    }

    private string ArmoryButtonText(RuntimeWeaponDefinition spec)
    {
        var profile = _runtime.Profile;
        var player = _runtime.Player;
        if (profile is null || player is null) return spec.Name;

        var owned = profile.Unlocks.Contains(spec.Name);
        var equipped = profile.Loadout.GetValueOrDefault(spec.Slot) == spec.Name;
        var state = equipped ? "[EQUIPPED]" : owned ? "[OWNED]" :
            spec.Price <= 0 || spec.Level >= 999 ? "[SPECIAL / UNAVAILABLE]" :
            player.Level < spec.Level ? "[LOCKED]" :
            player.Credits < spec.Price ? "[NEED CREDITS]" : "[BUY]";

        var price = spec.Price > 0 ? "  $" + spec.Price.ToString("N0") : "";
        var level = spec.Level < 999 ? "  LVL " + spec.Level : "";
        return state + "  " + spec.Slot.ToUpperInvariant() + "  |  " + spec.Name + level + price;
    }

    private void HandleArmoryWeapon(RuntimeWeaponDefinition spec)
    {
        var profile = _runtime.Profile;
        var player = _runtime.Player;
        if (profile is null || player is null) return;

        if (!profile.Unlocks.Contains(spec.Name))
        {
            if (spec.Price <= 0 || spec.Level >= 999 || player.Level < spec.Level || player.Credits < spec.Price)
                return;
            if (!_runtime.PurchaseWeapon(spec.Name, spec.Level, spec.Price))
                return;
        }

        _runtime.SetLoadout(spec.Slot, spec.Name);
        _armory?.QueueFree();
        _armory = null;
        ShowArmory();
    }

    private string ProfileStatusText()
    {
        var profile = _runtime.Profile;
        var player = _runtime.Player;
        if (profile is null || player is null) return "PROFILE LOADING";

        var primary = profile.Loadout.GetValueOrDefault("Primary") ?? "None";
        var secondary = profile.Loadout.GetValueOrDefault("Secondary") ?? "None";
        var melee = profile.Loadout.GetValueOrDefault("Melee") ?? "None";
        return "LEVEL " + player.Level + "  |  CREDITS $" + player.Credits.ToString("N0") +
            "  |  PRIMARY " + primary + "  |  SECONDARY " + secondary + "  |  MELEE " + melee;
    }

    private static void AddBackground(CanvasLayer layer)
    {
        layer.AddChild(new ColorRect
        {
            Color = new Color(0.025f, 0.027f, 0.03f),
            AnchorRight = 1,
            AnchorBottom = 1
        });
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
        _armory?.QueueFree();
        _armory = null;
        _perkMenu?.QueueFree();
        _perkMenu = null;

        _runtime.StartMap(map);
        _game = new GameplayRoot
        {
            Name = "Gameplay",
            Runtime = _runtime,
            MapName = map
        };
        _game.ExitRequested = ExitGame;
        _game.RestartRequested = RestartGame;
        AddChild(_game);
    }

    private void RestartGame(string map)
    {
        _game?.QueueFree();
        _game = null;
        StartGame(map);
    }

    private void ExitGame()
    {
        _game?.QueueFree();
        _game = null;
        ShowMenu();
    }
}

using Godot;
using Twr.Domain.Model;
using Twr.Domain.Services;

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
        if(args.Contains("--smoke-pass29",StringComparer.Ordinal))
        {
            RunPass29Smoke();
            return;
        }
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
        if (args.Contains("--smoke-all-source-maps", StringComparer.Ordinal))
        {
            RunAllOriginalSourceMapsSmoke();
            return;
        }
        if (args.Contains("--smoke-infected-models", StringComparer.Ordinal))
        {
            RunInfectedModelSmoke();
            return;
        }
        if (args.Contains("--smoke-source-infected", StringComparer.Ordinal))
        {
            RunOriginalInfectedSourceSmoke();
            return;
        }
        if (args.Contains("--smoke-weapon-models", StringComparer.Ordinal))
        {
            RunWeaponModelSmoke();
            return;
        }
        if (args.Contains("--smoke-source-weapons", StringComparer.Ordinal))
        {
            RunOriginalWeaponSourceSmoke();
            return;
        }
        if (args.Contains("--smoke-private-art", StringComparer.Ordinal))
        {
            RunPrivateArtSmoke();
            return;
        }
        if (args.Contains("--smoke-original-terrain", StringComparer.Ordinal))
        {
            RunOriginalTerrainSmoke();
            return;
        }
        if (args.Contains("--smoke-private-audio", StringComparer.Ordinal))
        {
            RunPrivateAudioSmoke();
            return;
        }
        if (args.Contains("--smoke-expressway-scene", StringComparer.Ordinal))
        {
            RunExpresswaySceneSmoke();
            return;
        }

        ShowMenu();
        if (args.Contains("--smoke-play", StringComparer.Ordinal))
            StartGame("Manor");
        GD.Print("TWR Offline: playable Regular-mode reconstruction runtime initialized.");
    }

    /// <summary>
    /// Exercises v2 map import for all ten maps in the exported Windows binary
    /// against generated CI-only scene packs. It is not a visual playtest.
    /// </summary>
    private void RunAllOriginalSourceMapsSmoke()
    {
        var holder = new Node3D { Name = "OriginalMapSourceSmoke" };
        AddChild(holder);
        var loaded = 0;
        foreach (var definition in MapCatalogRuntime.All())
        {
            if (!LaboratorySourceLoader.TryBuild(holder, definition.Name, out var layout))
                throw new InvalidOperationException("Original source map import failed: " + definition.Name);
            var nodeName = "Recovered" + definition.Name;
            var stage = holder.GetNodeOrNull<Node3D>(nodeName)
                ?? throw new InvalidOperationException("Original source map stage missing: " + definition.Name);
            if (layout.InfectedSpawns.Count != 2 || layout.PickupPoints.Count != 5 ||
                layout.FortificationPoints.Count != 3 || stage.GetChildCount() < 1)
                throw new InvalidOperationException("Original source map spawn groups invalid: " + definition.Name);
            loaded++;
            holder.RemoveChild(stage);
            stage.Free();
        }
        if (loaded != 10) throw new InvalidOperationException("Incomplete 10-map source smoke: " + loaded);
        GD.Print("TWR_SMOKE_ALL_SOURCE_MAPS_OK maps=10");
        GetTree().Quit(0);
    }

    // Unlike the domain-only completion smoke, this test instantiates the
    // packaged Laboratory importer against an explicit synthetic test pack.
    private void RunLaboratorySourceSmoke()
    {
        var testRoot = new Node3D { Name = "LaboratorySourceSmoke" };
        AddChild(testRoot);
        if (!LaboratorySourceLoader.TryBuild(testRoot, out var layout) ||
            layout.InfectedSpawns.Count != 15 ||
            layout.PickupPoints.Count != 127 ||
            layout.FortificationPoints.Count != 47 ||
            testRoot.GetNodeOrNull<Node3D>("RecoveredLaboratory") is null)
            throw new InvalidOperationException(
                "Laboratory source-pack smoke failed to instantiate the scene.");
        GD.Print("TWR_SMOKE_LAB_SOURCE_OK infected_spawns=" + layout.InfectedSpawns.Count +
            " item_markers=" + layout.PickupPoints.Count +
            " fortification_markers=" + layout.FortificationPoints.Count);
        GetTree().Quit(0);
    }

    private void RunOriginalInfectedSourceSmoke()
    {
        var stage = new Node3D { Name = "OriginalSourceInfectedSmoke" };
        AddChild(stage);
        var sourceTypes = new[] {
            "Civilian", "Sprinter", "Military", "Hazmat", "Burster", "Bolter"
        };
        foreach (var type in sourceTypes)
        {
            var visual = new InfectedVisualAssembler { Name = type, InfectedType = type };
            stage.AddChild(visual);
            if (!visual.UsingSourceBlueprint ||
                visual.GetNodeOrNull<Node3D>("SourceR6Body") is null)
                throw new InvalidOperationException(
                    "Original source infected assembly failed: " + type);
            if (type == "Civilian" && visual.GetNodeOrNull<MeshInstance3D>(
                "SourceR6Body/ReconstructedR6Head") is null)
                throw new InvalidOperationException(
                    "Missing-R6-head reconstruction failed.");
            visual.Attack();
        }
        GD.Print("TWR_SMOKE_SOURCE_INFECTED_OK types=6");
        GetTree().Quit(0);
    }

    // Exercise all eight no-network infected assembly fallback types in the
    // exported game, rather than only checking model source text.
    private void RunInfectedModelSmoke()
    {
        var stage = new Node3D { Name = "InfectedVisualSmoke" };
        AddChild(stage);
        var types = new[] {
            "Civilian", "Sprinter", "Military", "Hazmat",
            "Riot", "Burster", "Bloater", "Bolter"
        };
        foreach (var type in types)
        {
            var visual = new InfectedVisualAssembler { Name = type, InfectedType = type };
            stage.AddChild(visual);
            if (visual.GetChildCount() == 0)
                throw new InvalidOperationException("Missing offline infected visual: " + type);
        }
        var hazmat = new InfectedAgent { InfectedType = "Hazmat", Health = 100 };
        stage.AddChild(hazmat);
        hazmat.ApplyDamage(10, false, "Fire");
        if (Math.Abs(hazmat.Health - 90f) > 0.01f)
            throw new InvalidOperationException("Hazmat incorrectly immune to fire.");
        var bloater = new InfectedAgent { InfectedType = "Bloater", Health = 100 };
        stage.AddChild(bloater);
        bloater.ApplyDamage(10, false, "Fire");
        if (Math.Abs(bloater.Health - 70f) > 0.01f)
            throw new InvalidOperationException("Bloater source fire modifier not applied.");
        var riot = new InfectedAgent { InfectedType = "Riot", Health = 100 };
        stage.AddChild(riot);
        riot.ApplyDamage(10, false, "Melee");
        if (Math.Abs(riot.Health - 95f) > 0.01f)
            throw new InvalidOperationException("Riot source melee modifier not applied.");
        GD.Print("TWR_SMOKE_INFECTED_MODIFIERS_OK");
        GD.Print("TWR_SMOKE_INFECTED_VISUALS_OK types=" + types.Length);
        GetTree().Quit(0);
    }

    // Exercises the complete reconstructed Expressway scene in the exported
    // executable. This checks scene-node construction and layout integrity;
    // it is NOT a graphical side-by-side or full infected-navigation test.
    private void RunExpresswaySceneSmoke()
    {
        var stage = new Node3D { Name = "ExpresswaySceneSmoke" };
        AddChild(stage);
        var map = new RuntimeMapDefinition("Expressway", "Indianapolis", "Cloudy",
            new[] { "Load", "Repair" });
        var layout = MapBlockoutBuilder.Build(stage, map);
        var scene = stage.GetNodeOrNull<Node3D>("ExpresswayReconstruction")
            ?? throw new InvalidOperationException("Expressway reconstruction not instantiated");
        var bridge = scene.GetNodeOrNull<Node3D>("ElevatedBridge")
            ?? throw new InvalidOperationException("Missing elevated bridge");
        var traffic = scene.GetNodeOrNull<Node3D>("AbandonedTraffic")
            ?? throw new InvalidOperationException("Missing traffic jam");
        var screening = scene.GetNodeOrNull<Node3D>("MedicalScreeningCheckpoint")
            ?? throw new InvalidOperationException("Missing medical screening checkpoint");
        var lights = scene.GetNodeOrNull<Node3D>("HighwayStreetlamps")
            ?? throw new InvalidOperationException("Missing raised expressway lamps");
        var navigator = scene.GetNodeOrNull<ExpresswayNavigationRuntime>(
            "HighwayNavigation") ?? throw new InvalidOperationException(
            "Missing shared Expressway navigation graph");
        var corridorRoute = navigator.GetRoute(
            new Vector3(-6f,1f,-56f), new Vector3(6f,1f,55f));
        if (bridge.GetChildCount() < 75 || traffic.GetChildCount() < 12 ||
            screening.GetChildCount() < 45 || lights.GetChildCount() < 8 ||
            navigator.NavigablePoints < 140 || corridorRoute.Length < 12 ||
            layout.InfectedSpawns.Count != 4 || layout.PickupPoints.Count < 6)
            throw new InvalidOperationException("Incomplete Expressway reconstruction scene layers");
        GD.Print($"TWR_SMOKE_EXPRESSWAY_SCENE_OK bridge={bridge.GetChildCount()} " +
            $"traffic={traffic.GetChildCount()} screening={screening.GetChildCount()} " +
            $"lights={lights.GetChildCount()} nav_points={navigator.NavigablePoints} " +
            $"route_points={corridorRoute.Length} infected_spawns={layout.InfectedSpawns.Count}");
        GetTree().Quit(0);
    }

    private void RunPrivateAudioSmoke()
    {
        var controller = new OfflineAudioRuntime { Name = "PrivateAudioSmoke" };
        AddChild(controller);
        var overrideSound = controller.StreamFor("gunshot");
        var fallback = controller.StreamFor("reload");
        if (overrideSound.Data.Length != 2205 * 2 || fallback.Data.Length < 500 ||
            fallback.MixRate != 22050)
            throw new InvalidOperationException(
                "Offline audio WAV override or procedural fallback invalid.");
        GD.Print("TWR_SMOKE_PRIVATE_AUDIO_OK samples=2205");
        GetTree().Quit(0);
    }

    private void RunOriginalTerrainSmoke()
    {
        var stage = new Node3D { Name = "OriginalTerrainSmoke" };
        AddChild(stage);
        if (!RecoveredTerrainRuntime.TryBuild(stage, "Cabin"))
            throw new InvalidOperationException("Offline original terrain fixture not loaded");
        var terrain = stage.GetNodeOrNull<Node3D>("RecoveredTerrain")
            ?? throw new InvalidOperationException("Terrain node missing");
        var chunk = terrain.GetNodeOrNull<Node3D>("VoxelChunk_0_0_0")
            ?? throw new InvalidOperationException("Terrain chunk missing");
        var land = chunk.GetNodeOrNull<MeshInstance3D>("SolidTerrain");
        var water = chunk.GetNodeOrNull<MeshInstance3D>("SourceWater");
        var groundCollision = chunk.GetNodeOrNull<StaticBody3D>("TerrainCollision");
        if (land?.Mesh is null || water?.Mesh is null ||
            land.Mesh.GetSurfaceCount() != 1 || water.Mesh.GetSurfaceCount() != 1 ||
            groundCollision is null || groundCollision.GetChildCount() != 1)
            throw new InvalidOperationException(
                "Solid land mesh, collision, or separate water surface missing");
        GD.Print("TWR_SMOKE_ORIGINAL_TERRAIN_OK map=Cabin chunks=1 faces=7");
        GetTree().Quit(0);
    }

    private void RunPrivateArtSmoke()
    {
        // Files are generated outside res:// beside the actual exported EXE.
        // The check must exercise the same absolute-path loader as private art.
        var mesh = OfflineAssetResolver.Mesh("99887766");
        var texture = OfflineAssetResolver.Texture("99887766");
        if (mesh is null || mesh.GetSurfaceCount() != 1 ||
            texture is null || texture.GetWidth() != 2 || texture.GetHeight() != 2)
            throw new InvalidOperationException(
                "Post-export private OBJ/PNG was not resolved outside the PCK.");
        GD.Print("TWR_SMOKE_PRIVATE_ART_OK mesh=99887766 texture=99887766");
        GetTree().Quit(0);
    }

    private void RunOriginalWeaponSourceSmoke()
    {
        var stage = new Node3D { Name = "OriginalWeaponSourceSmoke" };
        AddChild(stage);
        var names = new[] {
            "Glock 17", "Sawn Off Shotgun", "AK-47", "RPG-7", "2x4"
        };
        foreach (var name in names)
        {
            var visual = new WeaponViewModelRuntime { Name = name };
            stage.AddChild(visual);
            visual.SetWeapon(RuntimeWeaponCatalog.Get(name));
            if (!visual.UsingOriginalToolAssembly || visual.VisualPartCount < 4)
                throw new InvalidOperationException("Original tool assembly failed: " + name);
            var muzzle = OriginalWeaponSourceRuntime.EstimatedMuzzle(name);
            if (muzzle is null || muzzle.Value.Z > -.10f)
                throw new InvalidOperationException(
                    "Source barrel axis or muzzle alignment invalid: " + name);
            visual.Fire();
            visual.Reload(.5);
        }
        var thrown = new WeaponViewModelRuntime { Name = "Molotov" };
        stage.AddChild(thrown);
        thrown.SetThrowable("Molotov");
        if (!thrown.UsingOriginalToolAssembly || thrown.VisualPartCount < 4)
            throw new InvalidOperationException("Original thrown tool assembly failed.");
        GD.Print("TWR_SMOKE_SOURCE_WEAPONS_OK models=6");
        GetTree().Quit(0);
    }

    private void RunWeaponModelSmoke()
    {
        var stage = new Node3D { Name = "WeaponModelSmoke" };
        AddChild(stage);
        var names = new[] {
            "Glock 17", "Sawn Off Shotgun", "AK-47",
            "RPG-7", "Flamethrower", "2x4"
        };
        foreach (var name in names)
        {
            var spec = RuntimeWeaponCatalog.Get(name);
            if (spec.Name != name)
                throw new InvalidOperationException("Weapon catalog missing: " + name);
            var model = new WeaponViewModelRuntime { Name = name };
            stage.AddChild(model);
            model.SetWeapon(spec);
            if (model.VisualPartCount < 2)
                throw new InvalidOperationException("Weapon model missing parts: " + name);
            model.Fire();
            model.Reload(Math.Max(0.2, spec.ReloadSeconds));
        }
        var throwable = new WeaponViewModelRuntime { Name = "Throwable" };
        stage.AddChild(throwable);
        throwable.SetThrowable("Molotov");
        if (throwable.VisualPartCount < 2)
            throw new InvalidOperationException("Throwable model missing components.");
        var metadata = FidelityCaptureRuntime.MetadataJson(
            "Laboratory", Vector3.Zero, Basis.Identity, 60f, "test.png");
        using (var json = global::System.Text.Json.JsonDocument.Parse(metadata))
        {
            if (json.RootElement.GetProperty("format").GetString() != "twr-fidelity-camera-v1" ||
                json.RootElement.GetProperty("map").GetString() != "Laboratory")
                throw new InvalidOperationException("Fidelity camera metadata contract failed.");
        }
        GD.Print("TWR_SMOKE_FIDELITY_METADATA_OK");
        var impacts = new Node3D { Name = "BallisticSmoke" };
        stage.AddChild(impacts);
        BallisticImpactRuntime.Spawn(impacts,
            new Vector3(0,2,0), new Vector3(0,2,-6), false);
        var effect = impacts.GetNodeOrNull<BallisticImpactRuntime>(
            "OfflineBulletImpact");
        if (effect is null || effect.GetChildCount() != 2 ||
            effect.GetNodeOrNull<MeshInstance3D>("ShotTrace") is null ||
            effect.GetNodeOrNull<MeshInstance3D>("WallImpact") is null)
            throw new InvalidOperationException("Ballistic effect smoke failed.");
        GD.Print("TWR_SMOKE_BALLISTIC_FX_OK");
        GD.Print("TWR_SMOKE_WEAPON_VISUALS_OK categories=6 throwables=1");
        GetTree().Quit(0);
    }

    private void RunPass29Smoke()
    {
        // This executes INSIDE the exported Windows Godot C# game, not Python.
        var bag = new Dictionary<string,int>(StringComparer.Ordinal);
        for (var i = 0; i < 16; i++) bag["item" + i] = 1;
        if (Pass24LootCapacityPolicy.CanGrant(bag, "new-item", 1, 2))
            throw new InvalidOperationException("Inventory overflow was accepted");
        if (!Pass24LootCapacityPolicy.CanGrant(bag, "item0", 1, 2))
            throw new InvalidOperationException("Existing-stack merge was rejected");
        bag["item15"] = 0;
        if (!Pass24LootCapacityPolicy.CanGrant(bag, "new-item", 1, 2))
            throw new InvalidOperationException("Zero-quantity slot was not reusable");
        if (Pass24LootCapacityPolicy.CanGrant(bag, "item0", int.MaxValue, int.MaxValue))
            throw new InvalidOperationException("Overflowing item grant was accepted");

        var clock = new Pass24GasTickClock();
        if (clock.Advance(1.1, 2.0) != 2 || clock.Expired)
            throw new InvalidOperationException("Spore gas tick cadence invalid");
        if (clock.Advance(4.0, 2.0) != 2 || !clock.Expired ||
            clock.Advance(1.0, 2.0) != 0)
            throw new InvalidOperationException("Spore gas expiry invalid");
        if (Math.Abs(Pass24SporeImpact.ClusterDamage(26.4f, 0, 4, false) - 26.4f) > .001f ||
            Pass24SporeImpact.ClusterDamage(26.4f, 0, 4, true) != 0 ||
            Pass24SporeImpact.GasDamage(5, true, false, true) != 0)
            throw new InvalidOperationException("Spore impact rules invalid");
        GD.Print("TWR_SMOKE_PASS29_RUNTIME_OK inventory=16 spore_clock=4 splash=occluded");
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

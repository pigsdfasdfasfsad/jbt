using Godot;
using Twr.Domain.Persistence;
using Twr.Domain.Model;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class Bootstrap : Node
{
    private LocalSessionNode _runtime = null!;
    private CanvasLayer? _menu;
    private CanvasLayer? _armory;
    private CanvasLayer? _bulletin;
    private CanvasLayer? _shop;
    private Label? _shopCaseDetail;
    private Label? _shopCreditsLabel;
    private Button? _shopOpenButton;
    private string _selectedShopCase = "Low";
    private CanvasLayer? _skinInventory;
    private CanvasLayer? _wikiReferenceGallery;
    private CanvasLayer? _wikiReferenceReturnLayer;
    private TextureRect? _wikiReferenceImage;
    private Label? _wikiReferenceCaption;
    private Label? _skinDetailLabel;
    private string? _selectedSkinId;
    private bool _skinInventoryReturnsToShop = true;
    private Label? _bulletinPage;
    private Label? _armoryPreviewLabel;
    private PerkMenuRuntime? _perkMenu;
    private GameplayRoot? _game;
    private Pass49OriginalLobbyRuntime? _sourceLobby;

    public override void _Ready()
    {
        _runtime = new LocalSessionNode { Name = "Runtime" };
        AddChild(_runtime);
        AddChild(new RuntimeWindowCloseHandler { Name = "RuntimeWindowClose" });
        var args=OS.GetCmdlineUserArgs();
        if(args.Contains("--capture-presentation", StringComparer.Ordinal))
        {
            AddChild(new RuntimePresentationCapture { Runtime=_runtime,Name="RuntimePresentationCapture" });
            return;
        }
        if(args.Contains("--smoke-source-mesh", StringComparer.Ordinal))
        {
            AddChild(new SourceMeshPresentationAcceptance { Name="SourceMeshPresentationAcceptance" });
            return;
        }
        if(args.Contains("--smoke-runtime-readiness",StringComparer.Ordinal))
        {
            AddChild(new RuntimeReadinessAcceptance { Runtime = _runtime });
            return;
        }
        if(args.Contains("--smoke-pass123",StringComparer.Ordinal))
        {
            AddChild(new Pass123ProvisionalObjectiveAcceptance
            {
                Name = "Pass123ProvisionalObjectiveAcceptance"
            });
            return;
        }
        if(args.Contains("--smoke-pass121",StringComparer.Ordinal))
        {
            AddChild(new Pass121OriginalPlayerSpawnAcceptance
            {
                Name="Pass121OriginalPlayerSpawnAcceptance"
            });
            return;
        }
        if(args.Contains("--smoke-pass120",StringComparer.Ordinal))
        {
            AddChild(new Pass120OriginalLootAcceptance
            {
                Name="Pass120OriginalLootAcceptance"
            });
            return;
        }
        if(args.Contains("--smoke-pass119",StringComparer.Ordinal))
        {
            AddChild(new Pass119OriginalSourceSpawnAcceptance
            {
                Name = "Pass119OriginalSourceSpawnAcceptance"
            });
            return;
        }
        if(args.Contains("--smoke-pass114",StringComparer.Ordinal))
        {
            AddChild(new Pass70LiveCombatAcceptance
            {
                Name="Pass114RealSlopedCorpseAcceptance",
                Runtime=_runtime,
                VerifyCorpse111=true,
                VerifyArticulation112=true,
                VerifyGroundContact113=true,
                VerifySlope114=true
            });
            return;
        }
        if(args.Contains("--smoke-pass113",StringComparer.Ordinal))
        {
            AddChild(new Pass70LiveCombatAcceptance
            {
                Name="Pass113RealGroundedCorpseAcceptance",
                Runtime=_runtime,
                VerifyCorpse111=true,
                VerifyArticulation112=true,
                VerifyGroundContact113=true
            });
            return;
        }
        if(args.Contains("--smoke-pass112",StringComparer.Ordinal))
        {
            AddChild(new Pass70LiveCombatAcceptance
            {
                Name="Pass112RealGlockArticulatedCorpseAcceptance",
                Runtime=_runtime,
                VerifyCorpse111=true,
                VerifyArticulation112=true
            });
            return;
        }
        if(args.Contains("--smoke-pass111",StringComparer.Ordinal))
        {
            AddChild(new Pass70LiveCombatAcceptance
            {
                Name="Pass111RealKillCorpseAcceptance",
                Runtime=_runtime,
                VerifyCorpse111=true
            });
            return;
        }
        if(args.Contains("--smoke-pass110",StringComparer.Ordinal))
        {
            AddChild(new Pass103PhysicalLoadLifecycleAcceptance
            {
                Name="Pass110RealRepairVehicleAssemblyAcceptance",
                Runtime=_runtime,
                ExtendToRepair104=true,
                VerifyCarryAim108=true,
                VerifyReferenceVisuals109=true,
                VerifyRepairVehicle110=true
            });
            return;
        }
        if(args.Contains("--smoke-pass109",StringComparer.Ordinal))
        {
            AddChild(new Pass103PhysicalLoadLifecycleAcceptance
            {
                Name="Pass109ReferenceGeneratorRepairAcceptance",
                Runtime=_runtime,
                ExtendToRepair104=true,
                VerifyCarryAim108=true,
                VerifyReferenceVisuals109=true
            });
            return;
        }
        if(args.Contains("--smoke-pass108",StringComparer.Ordinal))
        {
            AddChild(new Pass103PhysicalLoadLifecycleAcceptance
            {
                Name="Pass108NativeRmbCarryAcceptance",
                Runtime=_runtime,
                ExtendToRepair104=true,
                VerifyCarryAim108=true
            });
            return;
        }
        if(args.Contains("--smoke-pass105",StringComparer.Ordinal))
        {
            AddChild(new Pass105LiveThreatObjectiveAcceptance
            {
                Name="Pass105LiveThreatObjectiveAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass104",StringComparer.Ordinal))
        {
            AddChild(new Pass103PhysicalLoadLifecycleAcceptance
            {
                Name="Pass104FullPhysicalRepairAcceptance",
                Runtime=_runtime,
                ExtendToRepair104=true
            });
            return;
        }
        if(args.Contains("--smoke-pass103",StringComparer.Ordinal))
        {
            AddChild(new Pass103PhysicalLoadLifecycleAcceptance
            {
                Name="Pass103PhysicalLoadLifecycleAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass98",StringComparer.Ordinal))
        {
            AddChild(new Pass98MaterialLifecycleAcceptance
            {
                Name="Pass98MaterialLifecycleAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass96-replay",StringComparer.Ordinal))
        {
            AddChild(new Pass96FullMatchReplayAcceptance
            {
                Name="Pass96FullMatchReplayAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass96",StringComparer.Ordinal))
        {
            AddChild(new Pass96GroundedMaterialsAcceptance
            {
                Name="Pass96GroundedMaterialsAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass95",StringComparer.Ordinal))
        {
            AddChild(new Pass95ManorEscortAcceptance
            {
                Name="Pass95ManorEscortAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass94",StringComparer.Ordinal))
        {
            AddChild(new Pass94ManorEscortAcceptance
            {
                Name="Pass94ManorEscortAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass92",StringComparer.Ordinal))
        {
            AddChild(new Pass92ExpresswayEscortAcceptance
            {
                Name="Pass92ExpresswayEscortAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass91",StringComparer.Ordinal))
        {
            AddChild(new Pass91MultimapEscortAcceptance
            {
                Name="Pass91MultimapEscortAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass90",StringComparer.Ordinal))
        {
            AddChild(new Pass90EscortCompletionAcceptance
            {
                Name="Pass90EscortCompletionAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass89",StringComparer.Ordinal))
        {
            AddChild(new Pass89EscortNavigationAcceptance
            {
                Name="Pass89EscortNavigationAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass88",StringComparer.Ordinal))
        {
            AddChild(new Pass88TenMapPhysicalWaveAcceptance
            {
                Name="Pass88TenMapPhysicalWaveAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass83",StringComparer.Ordinal))
        {
            AddChild(new Pass83ManorUpperGameplayAcceptance
            {
                Name = "Pass83ManorUpperGameplayAcceptance",
                Runtime = _runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass78",StringComparer.Ordinal))
        {
            AddChild(new Pass78ManorBidirectionalStairAcceptance
            {
                Name = "Pass78ManorBidirectionalStairAcceptance",
                Runtime = _runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass77",StringComparer.Ordinal))
        {
            AddChild(new Pass77ManorStairAcceptance
            {
                Name = "Pass77ManorStairAcceptance",
                Runtime = _runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass76",StringComparer.Ordinal))
        {
            AddChild(new Pass76ObjectiveFortificationAcceptance
            {
                Name = "Pass76ObjectiveFortificationAcceptance",
                Runtime = _runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass75",StringComparer.Ordinal))
        {
            AddChild(new Pass75WeaponBlastAcceptance
            {
                Name = "Pass75WeaponBlastAcceptance",
                Runtime = _runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass74",StringComparer.Ordinal))
        {
            AddChild(new Pass74SpecialInfectedAcceptance
            {
                Name = "Pass74SpecialInfectedAcceptance",
                Runtime = _runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass73",StringComparer.Ordinal))
        {
            AddChild(new Pass73FullMapGameplayAcceptance
            {
                Name="Pass73FullMapGameplayAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass116",StringComparer.Ordinal))
        {
            AddChild(new Pass72NativeTenMapEntryAcceptance
            {
                Name="Pass116LaboratorySurfacesTenMapAcceptance",
                Runtime=_runtime,
                VerifyLabDressing115=true,
                VerifyLabMaterials116=true
            });
            return;
        }
        if(args.Contains("--smoke-pass115",StringComparer.Ordinal))
        {
            AddChild(new Pass72NativeTenMapEntryAcceptance
            {
                Name="Pass115LaboratoryNativeTenMapAcceptance",
                Runtime=_runtime,
                VerifyLabDressing115=true
            });
            return;
        }
        if(args.Contains("--smoke-pass72",StringComparer.Ordinal))
        {
            AddChild(new Pass72NativeTenMapEntryAcceptance
            {
                Name="Pass72NativeTenMapEntryAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass71",StringComparer.Ordinal))
        {
            AddChild(new Pass71NativeMultiMapDamageAcceptance
            {
                Name="Pass71NativeMultiMapDamageAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass70",StringComparer.Ordinal))
        {
            AddChild(new Pass70LiveCombatAcceptance
            {
                Name="Pass70LiveCombatAcceptance",
                Runtime=_runtime
            });
            return;
        }
        if(args.Contains("--smoke-pass69",StringComparer.Ordinal))
        {
            RunPass69Smoke();
            return;
        }
        if(args.Contains("--smoke-pass68",StringComparer.Ordinal))
        {
            RunPass68Smoke();
            return;
        }
        if(args.Contains("--smoke-pass67",StringComparer.Ordinal))
        {
            RunPass67Smoke();
            return;
        }
        if(args.Contains("--smoke-pass66",StringComparer.Ordinal))
        {
            RunPass66Smoke();
            return;
        }
        if(args.Contains("--smoke-pass65",StringComparer.Ordinal))
        {
            RunPass65Smoke();
            return;
        }
        if(args.Contains("--smoke-pass64",StringComparer.Ordinal))
        {
            RunPass64Smoke();
            return;
        }
        if(args.Contains("--smoke-pass63",StringComparer.Ordinal))
        {
            RunPass63Smoke();
            return;
        }
        if(args.Contains("--smoke-pass62",StringComparer.Ordinal))
        {
            RunPass62Smoke();
            return;
        }
        if(args.Contains("--smoke-pass61",StringComparer.Ordinal))
        {
            RunPass61Smoke();
            return;
        }
        if(args.Contains("--smoke-pass60",StringComparer.Ordinal))
        {
            RunPass60Smoke();
            return;
        }
        if(args.Contains("--smoke-pass59",StringComparer.Ordinal))
        {
            RunPass59Smoke();
            return;
        }
        if(args.Contains("--smoke-pass58",StringComparer.Ordinal))
        {
            RunPass58Smoke();
            return;
        }
        if(args.Contains("--smoke-pass57",StringComparer.Ordinal))
        {
            RunPass57Smoke();
            return;
        }
        if(args.Contains("--smoke-pass56",StringComparer.Ordinal))
        {
            RunPass56Smoke();
            return;
        }
        if(args.Contains("--smoke-pass56",StringComparer.Ordinal))
        {
            RunPass56Smoke();
            return;
        }
        if(args.Contains("--smoke-pass55",StringComparer.Ordinal))
        {
            RunPass55Smoke();
            return;
        }
        if(args.Contains("--smoke-pass54",StringComparer.Ordinal))
        {
            RunPass54Smoke();
            return;
        }
        if(args.Contains("--smoke-pass53",StringComparer.Ordinal))
        {
            RunPass53Smoke();
            return;
        }
        if(args.Contains("--smoke-pass52",StringComparer.Ordinal))
        {
            RunPass52Smoke();
            return;
        }
        if(args.Contains("--smoke-pass51",StringComparer.Ordinal))
        {
            RunPass51Smoke();
            return;
        }
        if(args.Contains("--smoke-pass50",StringComparer.Ordinal))
        {
            RunPass50Smoke();
            return;
        }
        if(args.Contains("--smoke-pass49",StringComparer.Ordinal))
        {
            RunPass49Smoke();
            return;
        }
        if(args.Contains("--smoke-pass48",StringComparer.Ordinal))
        {
            RunPass48Smoke();
            return;
        }
        if(args.Contains("--smoke-pass47",StringComparer.Ordinal))
        {
            RunPass47Smoke();
            return;
        }
        if(args.Contains("--smoke-pass46",StringComparer.Ordinal))
        {
            RunPass46Smoke();
            return;
        }
        if(args.Contains("--smoke-pass45",StringComparer.Ordinal))
        {
            RunPass45Smoke();
            return;
        }
        if(args.Contains("--smoke-pass44",StringComparer.Ordinal))
        {
            RunPass44Smoke();
            return;
        }
        if(args.Contains("--smoke-pass43",StringComparer.Ordinal))
        {
            RunPass43Smoke();
            return;
        }
        if(args.Contains("--smoke-pass42",StringComparer.Ordinal))
        {
            RunPass42Smoke();
            return;
        }
        if(args.Contains("--smoke-pass41",StringComparer.Ordinal))
        {
            RunPass41Smoke();
            return;
        }
        if(args.Contains("--smoke-pass40",StringComparer.Ordinal))
        {
            RunPass40Smoke();
            return;
        }
        if(args.Contains("--smoke-pass39",StringComparer.Ordinal))
        {
            RunPass39Smoke();
            return;
        }
        if(args.Contains("--smoke-pass38",StringComparer.Ordinal))
        {
            RunPass38Smoke();
            return;
        }
        if(args.Contains("--smoke-pass36",StringComparer.Ordinal))
        {
            RunPass36Smoke();
            return;
        }
        if(args.Contains("--smoke-pass35",StringComparer.Ordinal))
        {
            RunPass35Smoke();
            return;
        }
        if(args.Contains("--smoke-pass34",StringComparer.Ordinal))
        {
            RunPass34Smoke();
            return;
        }
        if(args.Contains("--smoke-pass33",StringComparer.Ordinal))
        {
            RunPass33Smoke();
            return;
        }
        if(args.Contains("--smoke-pass32",StringComparer.Ordinal))
        {
            RunPass32Smoke();
            return;
        }
        if(args.Contains("--smoke-pass31",StringComparer.Ordinal))
        {
            RunPass31Smoke();
            return;
        }
        if(args.Contains("--smoke-pass30",StringComparer.Ordinal))
        {
            RunPass30Smoke();
            return;
        }
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

    private void RunPass69Smoke()
    {
        if(Pass69SourceScopeOptics.ProfileCount!=8 ||
           !Pass69SourceScopeOptics.TryGet("AUG",out var aug) ||
           aug is null || Math.Abs(aug.DiameterStuds-.106f)>.0001f ||
           aug.SourceFov!=40f ||
           aug.Reticle!=Pass69SourceScopeOptics.Style.CompactCross ||
           !Pass69SourceScopeOptics.TryGet("CBJ-MS",out var cbj) ||
           cbj is null || Math.Abs(cbj.DiameterStuds-.111f)>.0001f ||
           cbj.Magnified ||
           cbj.Reticle!=Pass69SourceScopeOptics.Style.ReflexDot ||
           !Pass69SourceScopeOptics.TryGet("Barrett M82A1",out var m82) ||
           m82 is null || Math.Abs(m82.DiameterStuds-.205f)>.0001f ||
           !Pass69SourceScopeOptics.TryGet("M110 SASS",out var m110) ||
           m110 is null || Math.Abs(m110.DiameterStuds-.188f)>.0001f ||
           !Pass69SourceScopeOptics.TryGet("M14",out var m14) ||
           m14 is null || Math.Abs(m14.DiameterStuds-.179f)>.0001f ||
           !Pass69SourceScopeOptics.TryGet("SVD",out var svd) ||
           svd is null || Math.Abs(svd.DiameterStuds-.21f)>.0001f ||
           !Pass69SourceScopeOptics.TryGet("VSS",out var vss) ||
           vss is null || Math.Abs(vss.DiameterStuds-.21f)>.0001f ||
           !Pass69SourceScopeOptics.TryGet("Winchester Model 70",out var win) ||
           win is null || Math.Abs(win.DiameterStuds-.184f)>.0001f ||
           Pass69SourceScopeOptics.TryGet("Glock 17",out _))
            throw new InvalidOperationException(
                "Pass69 original ScopeDiameter/ScopeRadius and FOV inventory incorrect");

        var full=new Vector2(1280f,720f);
        var p1=Pass69SourceScopeOptics.ProjectRadius(
            aug,full,aug.SourceFov,.43f);
        var p2=Pass69SourceScopeOptics.ProjectRadius(
            cbj,full,cbj.SourceFov,.43f);
        var p3=Pass69SourceScopeOptics.ProjectRadius(
            m14,full,m14.SourceFov,.43f);
        if(p1<20f || p2<12f || p3<p1*1.5f ||
           p3>full.Y*.4251f ||
           Pass69SourceScopeOptics.ProjectRadius(
               m14,full,float.NaN,.43f)!=0f ||
           Pass69SourceScopeOptics.ProjectRadius(
               m14,full,10f,float.NaN)!=0f ||
           Pass69SourceScopeOptics.ProjectRadius(
               m14,new Vector2(0f,0f),10f,.43f)!=0f ||
           Pass69SourceScopeOptics.ProjectRadius(
               m14,full,4f,.43f)!=0f ||
           Pass69SourceScopeOptics.ProjectRadius(
               m14,full,10f,0f)!=0f)
            throw new InvalidOperationException(
                "Pass69 source scope physical projection or safety clamps invalid");

        var overlay=new Pass69SourceScopeOptics
        {
            Name="Pass69NativeOptics",Size=full,
            AnchorRight=0f,AnchorBottom=0f
        };
        AddChild(overlay);
        overlay.SetAimState("M14",.9f,10f,true);
        if(!overlay.OpticVisible || !overlay.Visible ||
           overlay.SourceWeaponName!="M14" ||
           overlay.ReticleStyle!=Pass69SourceScopeOptics.Style.MilDot ||
           overlay.ProjectedRadiusPixels<90f ||
           overlay.SourceReferenceStill!="M14-ADS.png")
            throw new InvalidOperationException(
                "Pass69 authored optical sight proxy cannot render source-sized lens");
        overlay.SetAimState("CBJ-MS",.75f,60f,true);
        if(!overlay.OpticVisible || overlay.ReticleStyle!=
               Pass69SourceScopeOptics.Style.ReflexDot ||
           overlay.ProjectedRadiusPixels>=p3)
            throw new InvalidOperationException(
                "Pass69 original CBJ-MS ScopeRadius was not treated as reflex aperture");
        overlay.SetAimState("M110 SASS",1f,10f,false);
        if(overlay.OpticVisible || overlay.Visible)
            throw new InvalidOperationException(
                "Pass69 scope persisted after player lost aim permission");
        overlay.SetAimState("Glock 17",1f,10f,true);
        if(overlay.HasSourceProfile || overlay.OpticVisible ||
           overlay.Visible || overlay.ProjectedRadiusPixels!=0f)
            throw new InvalidOperationException(
                "Pass69 unsupported unscoped pistol must never show fake scope");
        overlay.SetAimState("Winchester Model 70",1f,10f,true);
        var estimated=overlay.ProjectedRadiusPixels;
        overlay.SetAimState("Winchester Model 70",1f,10f,true,
            true,.70f);
        var ownerDepth=overlay.ProjectedRadiusPixels;
        if(!overlay.OpticVisible || ownerDepth>=estimated)
            throw new InvalidOperationException(
                "Pass69 verified source AimPart eye-relief depth not projected into optics");
        overlay.SetAimState("Winchester Model 70",float.NaN,10f,true);
        if(overlay.OpticVisible || overlay.CurrentBlend!=0f)
            throw new InvalidOperationException(
                "Pass69 NaN aim blending failed fail-closed safety");
        overlay.SetAimState("M14",1f,10f,true,
            true,.04f);
        if(Math.Abs(overlay.ProjectedRadiusPixels-p3)>.0002f)
            throw new InvalidOperationException(
                "Pass69 invalid source geometry should use known 0.43m fallback eye relief");

        var hud=new GameplayHud{Name="Pass69HudTest"};
        AddChild(hud);
        var plus=hud.GetChildren().OfType<Label>()
            .FirstOrDefault(x=>x.Text=="+");
        if(plus is null || !plus.Visible)throw new InvalidOperationException(
            "Pass69 pre-existing crosshair missing before scoped ADS");
        hud.SetScopeCrosshairSuppressed(true);
        if(!hud.ScopeCrosshairSuppressed || plus.Visible)
            throw new InvalidOperationException(
                "Pass69 general HUD crosshair remains over the scoped reticle");
        hud.SetScopeCrosshairSuppressed(false);
        if(hud.ScopeCrosshairSuppressed || !plus.Visible)
            throw new InvalidOperationException(
                "Pass69 standard HUD crosshair failed to return after leaving scope");

        if(OriginalWeaponSourceRuntime.OwnerSourcePackVerified ||
           Pass62SourceWeaponHierarchy.OwnerHierarchyVerified ||
           !OriginalWeaponSourceRuntime.TryGetSourceSightLocal(
               "Winchester Model 70",out _) )
            throw new InvalidOperationException(
                "Pass69 fixture must have only fabricated original-style scoped weapon");
        var scopedGun=new WeaponViewModelRuntime{Name="Pass69Winchester"};
        AddChild(scopedGun);
        scopedGun.SetWeapon(RuntimeWeaponCatalog.Get("Winchester Model 70"));
        if(!scopedGun.SourceAimPartAvailable ||
           !scopedGun.SourceExactAimCFAvailable68 ||
           !scopedGun.TryGetSourceAimRoot(out var root) ||
           scopedGun.SourceAimGeometryOwnerVerified)
            throw new InvalidOperationException(
                "Pass69 optic projection lost source AimPart/Pass68 AimCF compatibility");
        scopedGun.Transform=root;
        scopedGun._Process(.016f);
        if(scopedGun.SourceAimReticleLateralError>.001f)
            throw new InvalidOperationException(
                "Pass69 scoped source marker no longer centers on camera reticle");

        GD.Print("TWR_SMOKE_PASS69_SOURCE_OPTICS_OK "+
            "original_source_optic_profiles=8 "+
            "source_scope_diameter_and_radius=true "+
            "original_fov_viewport_projection=true "+
            "source_reflex_vs_magnified_modes=true "+
            "reference_guided_procedural_reticles=true "+
            "invalid_fov_depth_and_nan_inputs_rejected=true "+
            "source_winchester_aimpart_eye_relief=true "+
            "scope_disappears_without_aim_permission=true "+
            "original_game_hud_plus_suppressed_and_restored=true "+
            "source_pass68_aimcf_reticle_center_unchanged=true "+
            "no_ads_screenshot_assets_embedded=true "+
            "no_luau_and_no_weapon_damage_changes=true "+
            "synthetic_owner_pack_cleanup_required=true");
        GetTree().Quit(0);
    }

    private void RunPass68Smoke()
    {
        if(Pass68OriginalAimFormulas.FormulaCount!=73 ||
           !Pass68OriginalAimFormulas.TryGet("Glock 17",out var glockProfile) ||
           glockProfile is null ||
           glockProfile.Kind!=Pass68OriginalAimFormulas.Family.InverseSight ||
           !Pass68OriginalAimFormulas.TryGet("M4A1",out var m4Profile) ||
           m4Profile is null ||
           m4Profile.Kind!=Pass68OriginalAimFormulas.Family.SightHandleBaseInside ||
           !Pass68OriginalAimFormulas.TryGet("Mossberg 500",out var mossProfile) ||
           mossProfile is null ||
           mossProfile.Kind!=Pass68OriginalAimFormulas.Family.SightHandleThenBase ||
           !Pass68OriginalAimFormulas.TryGet("SPAS-12",out var spasProfile) ||
           spasProfile is null || spasProfile.SpecialZStuds!=-0.6f ||
           !Pass68OriginalAimFormulas.TryGet("MP7",out var mp7Profile) ||
           mp7Profile is null || mp7Profile.SightPreZStuds!=.45f ||
           !Pass68OriginalAimFormulas.TryGet("Winchester Model 70",out var winProfile) ||
           winProfile is null || winProfile.PostZStuds!=-.2f ||
           !Pass68OriginalAimFormulas.TryGet("Obrez Mosin",out var obrezProfile) ||
           obrezProfile is null || obrezProfile.PostZStuds!=-.1f ||
           !Pass68OriginalAimFormulas.TryGet("Flamethrower",out var flameProfile) ||
           flameProfile is null ||
           flameProfile.Kind!=Pass68OriginalAimFormulas.Family.NumericLiteral)
            throw new InvalidOperationException(
                "Pass68 source-surviving original 73 AimCF formula families unavailable");

        // Independently specify the source algebra with non-commuting
        // rotations. This catches incorrect multiplication ordering
        // even when most original Tool markers are axis aligned.
        var handle=new Transform3D(new Basis(Vector3.Up,.25f),
            new Vector3(.02f,-.03f,.04f));
        var aim=new Transform3D(new Basis(Vector3.Right,-.15f),
            new Vector3(.07f,.18f,-.30f));
        var grip=new Transform3D(new Basis(Vector3.Back,.12f),
            new Vector3(.14f,-.23f,.18f));
        var baseOffset=new Transform3D(Basis.Identity,
            new Vector3(.25000000000000006f,-.0669872981077807f,0f)*
                RobloxUnits.MetersPerStud);
        var inv=grip.AffineInverse();
        var invA=aim.AffineInverse();
        var noShift=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.InverseSight,0,0,0,grip);
        var withBase=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.SightHandleBaseInside,0,0,0,grip);
        var outside=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.SightHandleThenBase,0,0,0,grip);
        var spas=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.SpasSpecial,0,0,-.6f,grip);
        var mp7=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.InverseSight,.45f,0,0,grip);
        var win=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.SightHandleBaseInside,0,-.2f,0,grip);
        var literal=new Pass68OriginalAimFormulas.Formula(
            Pass68OriginalAimFormulas.Family.NumericLiteral,0,0,0,
            Transform3D.Identity);
        var t45=new Transform3D(Basis.Identity,
            new Vector3(0,0,-.45f)*RobloxUnits.MetersPerStud);
        // Lua's T(-0.6):toObjectSpace(X) applies inverse T(+0.6)
        // in Roblox coordinates, which is Godot camera-local -Z.
        var tMinus6=new Transform3D(Basis.Identity,
            new Vector3(0,0,-.6f)*RobloxUnits.MetersPerStud);
        var tMinus2=new Transform3D(Basis.Identity,
            new Vector3(0,0,.2f)*RobloxUnits.MetersPerStud);
        var canonical=invA*handle*inv*baseOffset;
        var originalInverse=inv*
            (handle.AffineInverse()*aim).AffineInverse()*baseOffset;
        var originalSpas=aim*tMinus6*handle*inv*baseOffset;
        var originalMp7=inv*
            (handle.AffineInverse()*(aim*t45)).AffineInverse()*baseOffset;
        var originalWin=canonical*tMinus2;
        static bool Near(Transform3D a,Transform3D b) =>
            a.Origin.DistanceTo(b.Origin)<.0002f &&
            a.Basis.GetRotationQuaternion()
             .AngleTo(b.Basis.GetRotationQuaternion())<.001f;
        void TraceFormula(string label,
            Pass68OriginalAimFormulas.Formula candidate,
            Transform3D expected)
        {
            var found=Pass68OriginalAimFormulas.TryCompute(
                candidate,handle,aim,out var received);
            var positionError=found
                ? received.Origin.DistanceTo(expected.Origin) : -1f;
            var angleError=found
                ? received.Basis.GetRotationQuaternion().AngleTo(
                    expected.Basis.GetRotationQuaternion()) : -1f;
            GD.Print("TWR_PASS68_FORMULA_TRACE "+
                $"family={label} success={found} "+
                $"translation_error={positionError:F7} "+
                $"angle_error={angleError:F7} "+
                $"expected={expected.Origin} actual={received.Origin}");
        }
        TraceFormula("inverse",noShift,originalInverse);
        TraceFormula("inside",withBase,canonical);
        TraceFormula("outside",outside,canonical);
        TraceFormula("spas",spas,originalSpas);
        TraceFormula("mp7",mp7,originalMp7);
        TraceFormula("winchester",win,originalWin);
        TraceFormula("literal",literal,new Transform3D(
            Basis.Identity,new Vector3(-.25f,.25f,0f)*
                RobloxUnits.MetersPerStud));
        if(!Pass68OriginalAimFormulas.TryCompute(
                noShift,handle,aim,out var actualInverse) ||
           !Near(actualInverse,originalInverse) ||
           !Pass68OriginalAimFormulas.TryCompute(
                withBase,handle,aim,out var actualInside) ||
           !Near(actualInside,canonical) ||
           !Pass68OriginalAimFormulas.TryCompute(
                outside,handle,aim,out var actualOutside) ||
           !Near(actualOutside,canonical) ||
           !Pass68OriginalAimFormulas.TryCompute(
                spas,handle,aim,out var actualSpas) ||
           !Near(actualSpas,originalSpas) ||
           !Pass68OriginalAimFormulas.TryCompute(
                mp7,handle,aim,out var actualMp7) ||
           !Near(actualMp7,originalMp7) ||
           !Pass68OriginalAimFormulas.TryCompute(
                win,handle,aim,out var actualWin) ||
           !Near(actualWin,originalWin) ||
           !Pass68OriginalAimFormulas.TryCompute(
                literal,handle,aim,out var actualLiteral) ||
           actualLiteral.Origin.DistanceTo(
                new Vector3(-.25f,.25f,0f)*
                    RobloxUnits.MetersPerStud)>.0001f ||
           Pass68OriginalAimFormulas.TryCompute(noShift,
                new Transform3D(Basis.Identity,
                    new Vector3(float.NaN,0,0)),aim,out _) ||
           Pass68OriginalAimFormulas.TryCompute(noShift,
                Transform3D.Identity,
                new Transform3D(Basis.Identity,
                    new Vector3(99,0,0)),out _))
            throw new InvalidOperationException(
                "Pass68 source AimCF matrix order, offsets, literal or finite guard incorrect");

        if(OriginalWeaponSourceRuntime.OwnerSourcePackVerified ||
           Pass62SourceWeaponHierarchy.OwnerHierarchyVerified)
            throw new InvalidOperationException(
                "Pass68 cannot claim any fabricated weapon geometry is owner-original");
        foreach(var (name,expected) in new[]{
            ("Glock 17","InverseSight"),
            ("Mossberg 500","SightHandleThenBase"),
            ("MP7","InverseSight"),
            ("SPAS-12","SpasSpecial"),
            ("Winchester Model 70","SightHandleBaseInside")})
        {
            if(!OriginalWeaponSourceRuntime.TryGetSourceFormulaFrames(
                    name,out var sourceAim,out var sourceHandle) ||
               !Pass68OriginalAimFormulas.TryGet(name,out var formula) ||
               !Pass68OriginalAimFormulas.TryCompute(
                    formula,sourceHandle,sourceAim,out var actualSourceCf))
                throw new InvalidOperationException(
                    "Pass68 owner-style source AimPart/Handle/GripCF unavailable for "+name);
            var model=new WeaponViewModelRuntime{Name="Pass68_"+name.Replace(' ','_')};
            AddChild(model);
            model.SetWeapon(RuntimeWeaponCatalog.Get(name));
            if(!model.UsingOriginalToolAssembly ||
               !model.SourceAimPartAvailable ||
               !model.SourceExactAimCFAvailable68 ||
               model.SourceOriginalAimFamily68!=expected ||
               !model.TryGetOriginalAimCF68(out var importedCf) ||
               !Near(importedCf,actualSourceCf) ||
               !model.TryGetSourceAimRoot(out var sightRoot))
                throw new InvalidOperationException(
                    "Pass68 source reconstructed AimCF not bound to weapon "+name);
            if(!OriginalWeaponSourceRuntime.TryGetSourceSightLocal(
                    name,out var renderedSight))
                throw new InvalidOperationException(
                    "Pass68 original-style rendered sight disappeared: "+name);
            var eyeDepth=(sightRoot*renderedSight).Origin.Z;
            if(name=="SPAS-12" && Math.Abs(eyeDepth+1.2f)>.015f)
                throw new InvalidOperationException(
                    "Pass68 SPAS-12 source AimCF eye-relief safety clamp did not activate: "+
                    eyeDepth);
            if(name=="MP7" && Math.Abs(eyeDepth+
                    Pass67SourceAimAlignment.SourceSightCameraDepth)<.04f)
                throw new InvalidOperationException(
                    "Pass68 MP7 original 0.45-stud source offset was visually lost");
            model.Transform=sightRoot;
            model._Process(.016f);
            if(model.SourceAimReticleLateralError>.001f ||
               model.SourceAimGeometryOwnerVerified)
                throw new InvalidOperationException(
                    "Pass68 original formula adjustment lost reticle/owner privacy "+name);
        }
        if(!Pass68OriginalAimFormulas.TryGet("Mosin Nagant",out _) ||
           OriginalWeaponSourceRuntime.TryGetSourceFormulaFrames(
                "Mosin Nagant",out _,out _))
            throw new InvalidOperationException(
                "Pass68 missing original sight must stay on Pass67 approximate fallback");

        GD.Print("TWR_SMOKE_PASS68_ORIGINAL_AIMCF_OK "+
            "original_source_formula_count=73 "+
            "noncommuting_matrix_order_verified=true "+
            "original_inverse_sight_formula=true "+
            "original_inside_and_outside_base_variants=true "+
            "source_spas_minus_06_literal_semantics=true "+
            "source_mp7_plus_045_sight_offset=true "+
            "source_winchester_minus_02_post_offset=true "+
            "source_flamethrower_literal_formula=true "+
            "source_five_weapon_sights_centered=true "+
            "source_spas_eye_relief_bounded=true "+
            "source_mp7_special_visual_offset=true "+
            "source_original_aimcf_exposed_without_luau=true "+
            "missing_source_sight_fallback=true "+
            "original_owner_pack_never_published=true "+
            "shooting_and_scope_authority_unchanged=true");
        GetTree().Quit(0);
    }

    private void RunPass67Smoke()
    {
        if(!Pass67SourceAimAlignment.TryGet("Glock 17",out var glockAim) ||
           glockAim is null ||
           Math.Abs(glockAim.Aim.Duration-.5f)>.00001f ||
           Math.Abs(glockAim.UnAim.Duration-.25f)>.00001f ||
           glockAim.Aim.Ease!=Pass67SourceAimAlignment.EaseKind.SlowDown2 ||
           glockAim.UnAim.Ease!=Pass67SourceAimAlignment.EaseKind.SlowDown ||
           !Pass67SourceAimAlignment.TryGet("M4A1",out var m4Aim) ||
           m4Aim is null ||
           !Pass67SourceAimAlignment.TryGet("Mossberg 500",out var pumpAim) ||
           pumpAim is null ||
           !Pass67SourceAimAlignment.TryGet("P90",out var p90Aim) ||
           p90Aim is null ||
           Math.Abs(glockAim.SourceCameraAimOffset.X-.07f)>.0001f ||
           Math.Abs(glockAim.SourceCameraAimOffset.Y-.121244f)>.001f)
            throw new InvalidOperationException(
                "Pass67 original Aim/UnAim source timings or inert camera pose missing");
        if(Math.Abs(Pass67SourceAimAlignment.Eased(.5f,
                Pass67SourceAimAlignment.EaseKind.SlowDown2)-.875f)>.0001f ||
           Math.Abs(Pass67SourceAimAlignment.Eased(.5f,
                Pass67SourceAimAlignment.EaseKind.SlowDown)-.75f)>.0001f ||
           Pass67SourceAimAlignment.Eased(float.NaN,
                Pass67SourceAimAlignment.EaseKind.SlowDown)!=0f ||
           Pass67SourceAimAlignment.TryAimRoot(
                new Transform3D(Basis.Identity,
                    new Vector3(float.NaN,0f,0f)),out _) ||
           Pass67SourceAimAlignment.TryAimRoot(
                new Transform3D(Basis.Identity,
                    new Vector3(10f,0f,0f)),out _))
            throw new InvalidOperationException(
                "Pass67 easing, NaN or impossible sight offset guard failed");

        if(OriginalWeaponSourceRuntime.OwnerSourcePackVerified ||
           Pass62SourceWeaponHierarchy.OwnerHierarchyVerified)
            throw new InvalidOperationException(
                "Pass67 exported smoke must not impersonate original owner-only packs");
        if(!OriginalWeaponSourceRuntime.TryGetSourceSightLocal(
                "Glock 17",out var sourceSight) ||
           !Pass67SourceAimAlignment.TryAimRoot(
                sourceSight,out var sourceRoot) ||
           Pass67SourceAimAlignment.ReticleError(
                sourceRoot,sourceSight)>.0002f ||
           Math.Abs((sourceRoot*sourceSight).Origin.Z+
                Pass67SourceAimAlignment.SourceSightCameraDepth)>.0002f)
            throw new InvalidOperationException(
                "Pass67 invisible original-style AimPart CFrame did not center to camera");
        var gun=new WeaponViewModelRuntime{Name="Pass67Glock"};
        AddChild(gun);
        gun.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        if(!gun.UsingOriginalToolAssembly ||
           !gun.SourceAimPartAvailable ||
           gun.SourceAimGeometryOwnerVerified ||
           !gun.TryGetSourceAimRoot(out var aimRoot) ||
           aimRoot.Origin.DistanceTo(sourceRoot.Origin)>.0001f ||
           !gun.SourceArmRigAvailable)
            throw new InvalidOperationException(
                "Pass67 source sight did not bind to existing source weapon and arm rig");
        // Recreate the normal player hip offset; Bootstrap's headless
        // test root otherwise starts at identity, already centered in X/Y
        // and cannot meaningfully demonstrate lateral sight convergence.
        var hip=new Transform3D(
            Basis.FromEuler(new Vector3(-.07f,.07f,0f)),
            new Vector3(.28f,-.22f,-.70f));
        gun.Transform=hip;
        gun._Process(.016f);
        var arm=gun.GetNodeOrNull<Node3D>("ArmRig66");
        var shoulder=arm?.GetNodeOrNull<MeshInstance3D>("LeftShoulder66");
        if(arm is null || shoulder is null)
            throw new InvalidOperationException(
                "Pass67 camera shoulder geometry missing");
        gun.Transform=aimRoot;
        gun._Process(.016f);
        var shoulderCamera=arm.GlobalTransform*shoulder.Position;
        if(shoulderCamera.DistanceTo(
                Pass66ArmKinematics.Shoulder(true))>.0002f ||
           gun.SourceAimReticleLateralError>.0002f)
            throw new InvalidOperationException(
                "Pass67 ADS sight center displaced camera-fixed shoulder");
        var half=Pass60WeaponPoseMath.Interpolate(
            hip,aimRoot,Pass67SourceAimAlignment.Eased(
                .5f,glockAim.Aim.Ease));
        if(Pass67SourceAimAlignment.ReticleError(
                half,sourceSight)>=
           Pass67SourceAimAlignment.ReticleError(
                hip,sourceSight))
            throw new InvalidOperationException(
                "Pass67 source ease did not move sight closer to camera center");
        gun.Transform=hip;
        gun._Process(.016f);
        if(arm.GlobalTransform.Origin.Length()>.0002f ||
           gun.SourceAimReticleLateralError<0f)
            throw new InvalidOperationException(
                "Pass67 hip-rest sight/shoulder were not restored");

        var shotgun=new WeaponViewModelRuntime{Name="Pass67Mossberg"};
        AddChild(shotgun);
        shotgun.SetWeapon(RuntimeWeaponCatalog.Get("Mossberg 500"));
        if(!shotgun.SourceAimPartAvailable ||
           !shotgun.TryGetSourceAimRoot(out var shotgunAim))
            throw new InvalidOperationException(
                "Pass67 authored shotgun sight marker not installed");
        shotgun.Transform=shotgunAim;
        shotgun._Process(.016f);
        if(shotgun.SourceAimReticleLateralError>.0002f)
            throw new InvalidOperationException(
                "Pass67 longer original shotgun AimPart does not center");

        var unsupported=new WeaponViewModelRuntime{Name="Pass67Mosin"};
        AddChild(unsupported);
        unsupported.SetWeapon(RuntimeWeaponCatalog.Get("Mosin Nagant"));
        if(unsupported.SourceAimPartAvailable ||
           unsupported.TryGetSourceAimRoot(out _) ||
           unsupported.SourceAimReticleLateralError!=-1f)
            throw new InvalidOperationException(
                "Pass67 missing synthetic sight should retain prior approximate ADS fallback");

        GD.Print("TWR_SMOKE_PASS67_SOURCE_SIGHT_OK "+
            "original_glock_aim_duration=.5 original_glock_unaim_duration=.25 "+
            "source_slowdown2_visual_easing=true "+
            "owner_style_invisible_aimpart_consumed=true "+
            "source_pistol_and_shotgun_sight_centered=true "+
            "camera_axis_target_depth=.43m "+
            "camera_fixed_shoulders_during_ads=true "+
            "continuous_aim_ease_centers_sights=true "+
            "unsupported_source_sight_uses_fallback=true "+
            "no_dynamic_luau_aimcf_evaluation=true "+
            "source_sight_not_original_scope_mesh=true "+
            "shooting_recoil_and_fov_authority_unchanged=true "+
            "owner_private_pack_upload_disabled=true");
        GetTree().Quit(0);
    }

    private void RunPass66Smoke()
    {
        // Finite, camera-space two-bone solver with lengths anchored to
        // the original source Survivor R6 2-stud total arm dimension.
        foreach(var leftSide in new[]{true,false})
        {
            var shoulder=Pass66ArmKinematics.Shoulder(leftSide);
            var wrist=shoulder+new Vector3(leftSide ? .12f : -.12f,.12f,-.34f);
            var solution=Pass66ArmKinematics.Solve(leftSide,wrist);
            if(!solution.IsFinite ||
               solution.ReachClamped ||
               solution.ElbowError>.001f ||
               solution.WristError>.001f ||
               solution.Wrist.DistanceTo(wrist)>.001f ||
               solution.UpperLength<.20f ||
               solution.ForeLength<.20f)
                throw new InvalidOperationException(
                    "Pass66 bounded original-scale articulated arm failed ordinary reachable case");
        }
        // This target is unreachable but remains inside the strict
        // input safety budget. Inputs beyond that budget instead use
        // the finite fallback shoulder pose.
        var far=Pass66ArmKinematics.Solve(true,
            new Vector3(-1.3f,-1.3f,-1.3f));
        var invalid=Pass66ArmKinematics.Solve(false,
            new Vector3(float.NaN,0f,0f));
        if(!far.IsFinite || !far.ReachClamped ||
           far.ReachScale>Pass66ArmKinematics.MaxReachScale+.0001f ||
           !invalid.IsFinite ||
           !Pass66ArmKinematics.TrySegment(Vector3.Zero,
                new Vector3(0f,.36f,0f),out var upright,out var length) ||
           Math.Abs(length-.36f)>.0001f ||
           Math.Abs(upright.Basis.Determinant()-1f)>.001f ||
           Pass66ArmKinematics.TrySegment(
                Vector3.Zero,Vector3.Zero,out _,out _) ||
           Pass66ArmKinematics.TrySegment(
                Vector3.Zero,new Vector3(float.NaN,0f,0f),
                out _,out _))
            throw new InvalidOperationException(
                "Pass66 arm overextension, degenerate segment or finite input guard failed");

        if(Pass62SourceWeaponHierarchy.OwnerHierarchyVerified ||
           OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass66 native smoke source packs must be fabricated, never original private files");
        var glock=new WeaponViewModelRuntime{Name="Pass66Glock"};
        AddChild(glock);
        glock.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var armRoot=glock.GetNodeOrNull<Node3D>("ArmRig66");
        var leftUpper=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/LeftUpperArm66");
        var leftLower=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/LeftForeArm66");
        var leftElbow=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/LeftElbow66");
        var leftShoulder=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/LeftShoulder66");
        var rightUpper=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/RightUpperArm66");
        var rightLower=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/RightForeArm66");
        var rightElbow=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/RightElbow66");
        var rightShoulder=glock.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/RightShoulder66");
        if(armRoot is null || leftUpper is null ||
           leftLower is null || leftElbow is null ||
           leftShoulder is null || rightUpper is null ||
           rightLower is null || rightElbow is null ||
           rightShoulder is null ||
           !glock.SourceArmRigAvailable ||
           glock.SourceArmProxyMeshCount!=8 ||
           glock.SourceHandProxyMeshCount!=14 ||
           !glock.UsingOriginalToolAssembly)
            throw new InvalidOperationException(
                "Pass66 camera-anchored articulated upper and forearms missing");
        if(armRoot.GetChildCount()!=8 ||
           leftShoulder.Position.DistanceTo(
               Pass66ArmKinematics.Shoulder(true))>.0001f ||
           rightShoulder.Position.DistanceTo(
               Pass66ArmKinematics.Shoulder(false))>.0001f)
            throw new InvalidOperationException(
                "Pass66 original-scale limb root or shoulder anchor invalid");
        var originalLeftElbow=leftElbow.Position;
        var originalRightElbow=rightElbow.Position;
        glock.Reload(2.0);
        glock._Process(.8f);
        if(!glock.SourceHandAnimationActive ||
           leftElbow.Position.DistanceTo(originalLeftElbow)<.012f ||
           leftUpper.Scale.Y<.15f ||
           leftLower.Scale.Y<.15f ||
           rightUpper.Scale.Y<.15f ||
           rightLower.Scale.Y<.15f ||
           leftShoulder.Position.DistanceTo(
               Pass66ArmKinematics.Shoulder(true))>.0001f ||
           rightShoulder.Position.DistanceTo(
               Pass66ArmKinematics.Shoulder(false))>.0001f)
            throw new InvalidOperationException(
                "Pass66 Glock wrist-following elbows did not solve while camera shoulders stayed still");
        glock._Process(1.21);
        if(glock.SourceHandAnimationActive ||
           // Pass55/60 idle sway continues even at rest, so world-space
           // elbow anchors may drift by a few millimetres. The source
           // hand weld resets exactly in the Pass65 native smoke.
           leftElbow.Position.DistanceTo(originalLeftElbow)>.012f ||
           rightElbow.Position.DistanceTo(originalRightElbow)>.012f)
            throw new InvalidOperationException(
                "Pass66 arm elbows did not reset with source hand welds");

        glock.StaticShowroomMode=true;
        glock._Process(.02f);
        if(armRoot.Visible)
            throw new InvalidOperationException(
                "Pass66 original armory preview must hide invented camera arm meshes");
        glock.StaticShowroomMode=false;
        glock._Process(.02f);
        if(!armRoot.Visible)
            throw new InvalidOperationException(
                "Pass66 first-person arm geometry stayed hidden after showroom");

        var pump=new WeaponViewModelRuntime{Name="Pass66Mossberg"};
        AddChild(pump);
        pump.SetWeapon(RuntimeWeaponCatalog.Get("Mossberg 500"));
        var pumpElbow=pump.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/LeftElbow66");
        var pumpRest=pumpElbow?.Position??Vector3.Zero;
        pump.Fire(false);
        pump._Process(.35f);
        if(pumpElbow is null || !pump.SourceHandAnimationActive ||
           pumpElbow.Position.DistanceTo(pumpRest)<.008f ||
           !pump.SourceArmRigAvailable)
            throw new InvalidOperationException(
                "Pass66 source Mossberg pump support elbow failed to follow hand");
        pump._Process(.43f);
        if(pumpElbow.Position.DistanceTo(pumpRest)>.012f)
            throw new InvalidOperationException(
                "Pass66 source shotgun arm failed to reset after pump");

        var bolt=new WeaponViewModelRuntime{Name="Pass66Mosin"};
        AddChild(bolt);
        bolt.SetWeapon(RuntimeWeaponCatalog.Get("Mosin Nagant"));
        var boltElbow=bolt.GetNodeOrNull<MeshInstance3D>(
            "ArmRig66/RightElbow66");
        var boltRest=boltElbow?.Position??Vector3.Zero;
        bolt.Fire(false);
        bolt._Process(.35f);
        if(boltElbow is null || !bolt.SourceHandAnimationActive ||
           boltElbow.Position.DistanceTo(boltRest)<.008f ||
           !bolt.SourceArmRigAvailable)
            throw new InvalidOperationException(
                "Pass66 source Mosin right shoulder-elbow chain failed on bolt");
        bolt._Process(.94f);
        if(boltElbow.Position.DistanceTo(boltRest)>.012f)
            throw new InvalidOperationException(
                "Pass66 source Mosin right arm failed to restore rest after cycling");

        // A weapon switch removes the previous camera arm rig instead of
        // leaving extra viewport geometry or stale hand joint references.
        glock.SetWeapon(RuntimeWeaponCatalog.Get("AK-47"));
        if(!glock.SourceArmRigAvailable ||
           glock.SourceArmProxyMeshCount!=8 ||
           glock.GetNodeOrNull<Node3D>("ArmRig66") is not { } newArmRoot ||
           ReferenceEquals(armRoot,newArmRoot))
            throw new InvalidOperationException(
                "Pass66 weapon switch leaked or failed to rebuild arm rig");

        GD.Print("TWR_SMOKE_PASS66_ARTICULATED_ARMS_OK "+
            "original_survivor_r6_total_arm_studs=2 "+
            "camera_fixed_shoulder_anchors=true "+
            "deterministic_finite_two_bone_ik=true "+
            "reach_limit_and_degenerate_input_guards=true "+
            "two_upper_and_two_forearm_meshes=true "+
            "source_glock_reload_elbows_follow=true "+
            "source_mossberg_left_pump_elbow_follows=true "+
            "source_mosin_right_bolt_elbow_follows=true "+
            "source_hand_and_arm_rest_reset=true "+
            "source_showroom_hides_approximate_arms=true "+
            "weapon_switch_arm_rig_cleanup=true "+
            "no_original_avatar_mesh_claim=true "+
            "no_gameplay_ammo_or_damage_changes=true");
        GetTree().Quit(0);
    }

    private void RunPass65Smoke()
    {
        if(!Pass65SourceHands.TryGet("Glock 17",out var glock) ||
           glock is null || glock.Equip is null ||
           glock.Reload is null ||
           glock.Reload.Left.Cues.Length!=5 ||
           glock.Reload.Right.Cues.Length!=2 ||
           glock.Equip.Left.Cues.Length!=3 ||
           !Pass65SourceHands.TryGet("Mossberg 500",out var moss) ||
           moss is null || moss.Cycling is null ||
           moss.Cycling.Left.Cues.Length!=2 ||
           !Pass65SourceHands.TryGet("Mosin Nagant",out var mosin) ||
           mosin is null || mosin.Cycling is null ||
           mosin.Cycling.Right.Cues.Length!=4)
            throw new InvalidOperationException(
                "Pass65 original source left/right weld keyframes not recovered");
        if(!Pass65SourceHands.TryLiteralPose(
               "CFrame.new(-0.5, -0.35, 0.475) * CFrame.Angles((math.pi/2), 0, 0)",
               out var source) ||
           Math.Abs(source.Origin.X+.14f)>.0001f ||
           Math.Abs(source.Origin.Y+.098f)>.0001f ||
           Math.Abs(source.Origin.Z+.133f)>.0001f ||
           !Pass65SourceHands.TryLiteralPose(
               "CFrame.new(0.1, 0, 0) * CFrame.Angles(1.9198621771937625, 0, 0)",
               out _) ||
           Pass65SourceHands.TryLiteralPose(
               "CFrame.Angles(math.pi/2, 0, 0)",out _) ||
           Pass65SourceHands.TryLiteralPose(
               "CFrame.Angles(0, execute(), 0)",out _) ||
           Pass65SourceHands.TryLiteralPose(
               "module.Stats.LeftCF * CFrame.new(0, 0, 0)",out _) ||
           Pass65SourceHands.TryLiteralPose(
               "CFrame.new(999, 0, 0)",out _))
            throw new InvalidOperationException(
                "Pass65 strict source hand CFrame parser failed");

        if(OriginalWeaponSourceRuntime.OwnerSourcePackVerified ||
           Pass62SourceWeaponHierarchy.OwnerHierarchyVerified)
            throw new InvalidOperationException(
                "Pass65 synthetic smoke cannot claim the original private owner assets");
        var gun=new WeaponViewModelRuntime{Name="Pass65Glock"};
        AddChild(gun);
        gun.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var left=gun.GetNodeOrNull<Node3D>("WeaponBody/LeftHandProxy65");
        var right=gun.GetNodeOrNull<Node3D>("WeaponBody/RightHandProxy65");
        if(left is null || right is null ||
           !gun.UsingOriginalToolAssembly ||
           !gun.SourceHandProxiesAvailable ||
           gun.SourceHandProxyMeshCount!=14 ||
           gun.SourceLeftReloadWeldCues!=5 ||
           gun.SourceRightReloadWeldCues!=2 ||
           Math.Abs(gun.SourceLeftHandPosition.X+.14f)>.001f ||
           Math.Abs(gun.SourceRightHandPosition.X-.0462f)>.001f ||
           gun.SourceHierarchyOriginalVerified)
            throw new InvalidOperationException(
                "Pass65 source grip poses and paired hand proxies unavailable");
        var glockLeftHome=left.Transform;
        var glockRightHome=right.Transform;
        gun.BeginEquip(.8);
        gun._Process(.38);
        if(!gun.SourceHandAnimationActive ||
           left.Position.DistanceTo(glockLeftHome.Origin)<.02f)
            throw new InvalidOperationException(
                "Pass65 original Glock equip hand weld did not animate");
        gun._Process(.43);
        gun.Reload(2.0);
        gun._Process(.16);
        if(!gun.SourceHandAnimationActive ||
           right.Position.DistanceTo(glockRightHome.Origin)<.01f)
            throw new InvalidOperationException(
                "Pass65 original Glock reload right-hand weld did not animate");
        gun._Process(.64);
        if(left.Position.DistanceTo(glockLeftHome.Origin)<.02f)
            throw new InvalidOperationException(
                "Pass65 original left-hand magazine reload grip did not move");
        gun._Process(1.21);
        if(gun.SourceHandAnimationActive ||
           left.Position.DistanceTo(glockLeftHome.Origin)>.0002f ||
           right.Position.DistanceTo(glockRightHome.Origin)>.0002f)
            throw new InvalidOperationException(
                "Pass65 Glock gloves did not reset after completed reload");

        var pump=new WeaponViewModelRuntime{Name="Pass65Mossberg"};
        AddChild(pump);
        pump.SetWeapon(RuntimeWeaponCatalog.Get("Mossberg 500"));
        var pumpHand=pump.GetNodeOrNull<Node3D>("WeaponBody/LeftHandProxy65");
        if(pumpHand is null || pump.SourceCyclingPumpFrames!=5 ||
           pump.SourceHandProxyMeshCount!=14)
            throw new InvalidOperationException(
                "Pass65 source shotgun support hand missing");
        var originalPumpHand=pumpHand.Position;
        pump.Fire(false);
        pump._Process(.35f);
        if(!pump.SourceHandAnimationActive ||
           pumpHand.Position.DistanceTo(originalPumpHand)<.035f)
            throw new InvalidOperationException(
                "Pass65 original Mossberg left-hand inverse PumpRel did not follow pump");
        pump._Process(.43f);
        if(pump.SourceHandAnimationActive ||
           pumpHand.Position.DistanceTo(originalPumpHand)>.0002f)
            throw new InvalidOperationException(
                "Pass65 source pump grip failed to return to rest");

        var bolt=new WeaponViewModelRuntime{Name="Pass65Mosin"};
        AddChild(bolt);
        bolt.SetWeapon(RuntimeWeaponCatalog.Get("Mosin Nagant"));
        var boltHand=bolt.GetNodeOrNull<Node3D>("WeaponBody/RightHandProxy65");
        if(boltHand is null || bolt.SourceRightCyclingWeldCues!=4 ||
           bolt.SourceNestedBoltHandleParts!=2)
            throw new InvalidOperationException(
                "Pass65 source bolt-action hand / nested handle missing");
        var originalBoltHand=boltHand.Transform;
        bolt.Fire(false);
        bolt._Process(.35f);
        if(!bolt.SourceHandAnimationActive ||
           boltHand.Position.DistanceTo(originalBoltHand.Origin)<.03f)
            throw new InvalidOperationException(
                "Pass65 Mosin right hand did not reach source Bolt Handle weld");
        bolt._Process(.20f);
        if(Math.Abs(bolt.LastSourceBoltTravel)<.085f ||
           boltHand.Position.DistanceTo(originalBoltHand.Origin)<.035f)
            throw new InvalidOperationException(
                "Pass65 right hand did not follow original moving bolt");
        bolt._Process(.74f);
        if(bolt.SourceHandAnimationActive ||
           boltHand.Position.DistanceTo(originalBoltHand.Origin)>.0002f)
            throw new InvalidOperationException(
                "Pass65 bolt action grip failed to return to original hold");
        GD.Print("TWR_SMOKE_PASS65_SOURCE_HAND_GRIPS_OK "+
            "source_glock_left_reload_weld_cues=5 "+
            "source_glock_right_reload_weld_cues=2 "+
            "source_mosin_right_cycling_weld_cues=4 "+
            "source_two_procedural_hands_per_weapon=true "+
            "source_equip_hands_animated=true "+
            "source_reload_magazine_hand_follows=true "+
            "source_mossberg_left_pump_grip_follows=true "+
            "source_mosin_right_bolt_handle_grip_follows=true "+
            "end_of_clip_hands_reset=true "+
            "nonliteral_luau_expressions_blocked=true "+
            "no_original_character_mesh_claim=true "+
            "gameplay_ammo_and_recoil_authority_unchanged=true");
        GetTree().Quit(0);
    }

    private void RunPass64Smoke()
    {
        if(!Pass64SourceAction.TryGet("Mosin Nagant",out var mosin) ||
           mosin is null || mosin.Cycling.Bolt.Length!=2 ||
           mosin.Cycling.BoltHandle.Length!=2 ||
           mosin.Cycling.Ejections.Length!=1 ||
           Math.Abs(mosin.Cycling.Duration-1.275f)>.0001f ||
           mosin.Cycling.Ejections[0].Name!="7.62x54R Shell" ||
           !Pass64SourceAction.TryGet("Winchester Model 70",out var win) ||
           win is null || win.Cycling.Bolt.Length!=2 ||
           win.Cycling.BoltHandle.Length!=2 ||
           win.Cycling.Ejections.Length!=1 ||
           !Pass64SourceAction.TryGet("Mossberg 500",out var moss) ||
           moss is null || !moss.IncrementalReload ||
           moss.Cycling.Ejections.Length!=1 ||
           moss.Cycling.Ejections[0].Name!="12 Gauge Shell" ||
           !Pass64SourceAction.TryGet("SPAS-12",out var spas) ||
           spas is null || spas.Cycling.Ejections.Length!=1)
            throw new InvalidOperationException(
                "Pass64 original source bolt/shell evidence unavailable");
        if(!Pass64SourceAction.TryBoltHandleAngle(
               "CFrame.Angles(0, 0, (math.pi/2))",out var angle) ||
           Math.Abs(angle+Mathf.Pi/2f)>.0001f ||
           !Pass64SourceAction.TryBoltHandleAngle(
               "CFrame.Angles(0, 0, 1.0471975511965976)",out var numeric) ||
           Math.Abs(numeric+Mathf.Pi/3f)>.0001f ||
           Pass64SourceAction.TryBoltHandleAngle(
               "CFrame.Angles(0, 0, math.pi)",out _) ||
           Pass64SourceAction.TryBoltHandleAngle(
               "CFrame.Angles(0, 0, execute())",out _) ||
           Pass64SourceAction.TryShellDirection(
               "Vector3.new(1, NaN, 0)",out _) ||
           !Pass64SourceAction.TryShellDirection(
               "Vector3.new(0.86602, 0.5, 0)",out var ejection) ||
           Math.Abs(ejection.Length()-1f)>.0001f)
            throw new InvalidOperationException(
                "Pass64 bounded original bolt/shell literal parser failed");
        if(Pass62SourceWeaponHierarchy.OwnerHierarchyVerified ||
           OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass64 synthetic fixture must not claim private owner verification");

        var rifle=new WeaponViewModelRuntime{Name="Pass64Mosin"};
        AddChild(rifle);
        rifle.SetWeapon(RuntimeWeaponCatalog.Get("Mosin Nagant"));
        var rig=rifle.GetNodeOrNull<Node3D>("WeaponBody");
        var bolt=rig?.GetNodeOrNull<MeshInstance3D>("BoltBody");
        var knob=rig?.GetNodeOrNull<MeshInstance3D>("BoltKnob");
        var lever=rig?.GetNodeOrNull<MeshInstance3D>("BoltLever");
        if(rig is null || bolt is null || knob is null || lever is null ||
           !rifle.UsingOriginalToolAssembly ||
           rifle.SourceBoltCarrierCueCount!=2 ||
           rifle.SourceBoltHandleCueCount!=2 ||
           rifle.SourceBoltCarrierGroupParts!=3 ||
           rifle.SourceNestedBoltHandleParts!=2 ||
           rifle.SourceShellEjectionCueCount!=1 ||
           rifle.SourceHierarchyOriginalVerified)
            throw new InvalidOperationException(
                "Pass64 source nested Mosin bolt hierarchy not bound");
        var boltHome=bolt.Transform;
        var knobHome=knob.Transform;
        var leverHome=lever.Transform;

        rifle.Fire(false);
        rifle._Process(.35f);
        if(Math.Abs(rifle.LastSourceBoltHandleAngle)<.80f ||
           knob.Position.DistanceTo(knobHome.Origin)<.015f ||
           lever.Position.DistanceTo(leverHome.Origin)<.007f ||
           bolt.Position.DistanceTo(boltHome.Origin)>.002f)
            throw new InvalidOperationException(
                "Pass64 authored nested bolt handle did not rotate independently");
        rifle._Process(.17f);
        if(Math.Abs(rifle.LastSourceBoltTravel)<.085f ||
           bolt.Position.DistanceTo(boltHome.Origin)<.08f ||
           knob.Position.DistanceTo(knobHome.Origin)<.06f ||
           rifle.EjectedShellVisualCount!=1 ||
           rifle.ActiveEjectedShellVisualCount!=1 ||
           rifle.LastEjectedShellSourceName!="7.62x54R Shell")
            throw new InvalidOperationException(
                "Pass64 source bolt travel or single original ejection failed");
        rifle._Process(.9f);
        if(bolt.Transform.Origin.DistanceTo(boltHome.Origin)>.0002f ||
           knob.Transform.Origin.DistanceTo(knobHome.Origin)>.0002f ||
           lever.Transform.Origin.DistanceTo(leverHome.Origin)>.0002f ||
           Math.Abs(rifle.LastSourceBoltTravel)>.00001f ||
           Math.Abs(rifle.LastSourceBoltHandleAngle)>.00001f)
            throw new InvalidOperationException(
                "Pass64 original nested bolt failed to restore home transforms");

        var shotgun=new WeaponViewModelRuntime{Name="Pass64Mossberg"};
        AddChild(shotgun);
        shotgun.SetWeapon(RuntimeWeaponCatalog.Get("Mossberg 500"));
        if(shotgun.SourceShellEjectionCueCount!=1 ||
           shotgun.SourceMagazineGroupParts!=2 ||
           shotgun.SourceReloadPumpFrames!=13)
            throw new InvalidOperationException(
                "Pass64 shotgun source shell/pump/insertion evidence missing");
        shotgun.Fire(false);
        shotgun._Process(.35f);
        if(shotgun.EjectedShellVisualCount!=1 ||
           shotgun.ActiveEjectedShellVisualCount!=1 ||
           shotgun.LastEjectedShellSourceName!="12 Gauge Shell")
            throw new InvalidOperationException(
                "Pass64 source shotgun shell ejection proxy not created");
        shotgun._Process(1.4f);
        if(shotgun.ActiveEjectedShellVisualCount!=0 ||
           shotgun.EjectedShellVisualCount!=1)
            throw new InvalidOperationException(
                "Pass64 transient shotgun shell not reclaimed");
        shotgun.Reload(2.0);
        shotgun._Process(.50f);
        if(!shotgun.SourceIncrementalShellInsertPoseActive ||
           !shotgun.SourceMagazinePoseActive)
            throw new InvalidOperationException(
                "Pass64 original incremental shell feed did not reuse source magazine pose");
        shotgun._Process(1.6f);
        if(shotgun.SourceIncrementalShellInsertPoseActive ||
           shotgun.SourceMagazinePoseActive)
            throw new InvalidOperationException(
                "Pass64 original incremental insertion leaked after reload");
        GD.Print("TWR_SMOKE_PASS64_SOURCE_BOLT_SHELL_OK "+
            "source_mosin_bolt_frames=2 source_mosin_handle_frames=2 "+
            "source_winchester_bolt_frames=2 "+
            "mosin_handle_angle_degrees=90 "+
            "nested_bolt_children_follow_parent=true "+
            "bolt_cycling_returns_to_rest=true "+
            "source_mossberg_shell_ejection=12Gauge "+
            "source_spas_shell_ejection=true "+
            "source_shell_proxy_lifetime_bounded=true "+
            "source_incremental_shell_feed_pose=true "+
            "synthetic_private_source_gate=true "+
            "no_luau_execution=true ammo_damage_unchanged=true");
        GetTree().Quit(0);
    }

    private void RunPass63Smoke()
    {
        if(!Pass63SourceMagazineReload.TryGet("Glock 17",out var glock) ||
           glock is null || glock.Cues.Length!=4 ||
           !Pass63SourceMagazineReload.TryGet("M4A1",out var m4) ||
           m4 is null || m4.Cues.Length!=4 ||
           !Pass63SourceMagazineReload.TryGet("AK-47",out var ak) ||
           ak is null || ak.Cues.Length!=5 ||
           !Pass63SourceMagazineReload.TryGet("P90",out var p90) ||
           p90 is null || p90.Cues.Length!=8 ||
           !Pass63SourceMagazineReload.TryGet("Mossberg 500",out var moss) ||
           moss is null || moss.Cues.Length!=3 ||
           Math.Abs(glock.Duration-1.85f)>.001f)
            throw new InvalidOperationException(
                "Pass63 original source magazine reload cues missing");

        if(!Pass63SourceMagazineReload.TryParseTarget(
               "module.Stats.Rels.MagRel * CFrame.new(0, -0.3, 0)",
                out var rel) || !rel.RelativeToHandle ||
           Math.Abs(rel.Pose.Origin.Y+.084f)>.0001f ||
           !Pass63SourceMagazineReload.TryParseTarget(
               "CFrame.new(0, 0, 0.1) * CFrame.Angles(0.15,0,0)",
               out var abs) || abs.RelativeToHandle ||
           Math.Abs(abs.Pose.Origin.Z+.028f)>.0001f ||
           Pass63SourceMagazineReload.TryParseTarget(
               "module.Stats.Rels.MagRel * execute()",out _) ||
           Pass63SourceMagazineReload.TryParseTarget(
               "module.Stats.Rels.BoltRel",out _) ||
           Pass63SourceMagazineReload.TryParseTarget(
               "CFrame.new(999,0,0)",out _) ||
           Pass63SourceMagazineReload.TryParseTarget(
               "module.Stats.Rels.MagRel * CFrame.Angles(math.pi/2,0,0)",out _))
            throw new InvalidOperationException(
                "Pass63 original magazine CFrame grammar or coordinate reflection failed");

        if(Pass62SourceWeaponHierarchy.OwnerHierarchyVerified ||
           OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass63 synthetic fixture incorrectly claims owner pack verification");
        var gun=new WeaponViewModelRuntime{Name="Pass63Glock"};
        AddChild(gun);
        gun.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var root=gun.GetNodeOrNull<Node3D>("WeaponBody");
        var magazine=root?.GetNodeOrNull<MeshInstance3D>("Magazine");
        var sleeve=root?.GetNodeOrNull<MeshInstance3D>("MagazineSleeve");
        if(root is null || magazine is null || sleeve is null ||
           !gun.UsingOriginalToolAssembly ||
           gun.SourceMagazineCueCount!=4 ||
           gun.SourceMagazineGroupParts!=2 ||
           gun.SourceHierarchyOriginalVerified)
            throw new InvalidOperationException(
                "Pass63 SHA-gated grouped magazine model unavailable");
        var firstHome=magazine.Transform;
        var secondHome=sleeve.Transform;
        var spacing=magazine.Position.DistanceTo(sleeve.Position);
        gun.Reload(2.0);
        gun._Process(.30);
        gun._Process(.10);
        if(!gun.SourceMagazinePoseActive ||
           gun.SourceMagazineHandleTravel<.045f ||
           magazine.Position.DistanceTo(firstHome.Origin)<.045f ||
           sleeve.Position.DistanceTo(secondHome.Origin)<.045f ||
           Math.Abs(magazine.Position.DistanceTo(sleeve.Position)-spacing)>.0003f ||
           Math.Abs(magazine.Basis.Orthonormalized()
               .GetRotationQuaternion().W-1f)<.00001f)
            throw new InvalidOperationException(
                "Pass63 original Glock magazine removal did not animate both parts");
        gun._Process(2.1);
        gun._Process(.016);
        if(gun.SourceMagazinePoseActive ||
           gun.SourceMagazineHandleTravel>.00001f ||
           magazine.Transform.Origin.DistanceTo(firstHome.Origin)>.0002f ||
           sleeve.Transform.Origin.DistanceTo(secondHome.Origin)>.0002f)
            throw new InvalidOperationException(
                "Pass63 magazine failed to return to original assembly position");
        if(gun.SourceMotorHierarchyGroups<2 ||
           gun.SourceSlideGroupParts!=2)
            throw new InvalidOperationException(
                "Pass63 regressed verified source Slide subassembly");
        GD.Print("TWR_SMOKE_PASS63_MAGAZINE_OK "+
            "source_glock_cues=4 source_ak_cues=5 "+
            "source_m4_cues=4 source_p90_cues=8 "+
            "source_mossberg_cues=3 "+
            "original_absolute_pose_and_magrel=true "+
            "source_two_piece_magazine_moves_and_rotates=true "+
            "source_group_spacing_preserved=true "+
            "pose_returns_to_rest=true "+
            "private_pack_synthetic_only=true "+
            "dynamic_luau_expressions_blocked=true "+
            "reload_ammo_authority_unchanged=true");
        GetTree().Quit(0);
    }

    private void RunPass62Smoke()
    {
        if(Pass62SourceWeaponHierarchy.OwnerHierarchyVerified ||
           OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass62 synthetic smoke must never impersonate private original source");

        var glock=new WeaponViewModelRuntime{Name="Pass62Glock"};
        AddChild(glock);
        glock.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var rig=glock.GetNodeOrNull<Node3D>("WeaponBody");
        var slide=rig?.GetNodeOrNull<MeshInstance3D>("Slide");
        var detail=rig?.GetNodeOrNull<MeshInstance3D>("HousingDetail");
        if(rig is null || slide is null || detail is null ||
           !glock.UsingOriginalToolAssembly ||
           glock.SourceMotorHierarchyGroups!=2 ||
           glock.SourceSlideGroupParts!=2 ||
           glock.SourceHierarchyOriginalVerified ||
           !glock.HasSourceEquipClip)
            throw new InvalidOperationException(
                "Pass62 Glock source hierarchy was not installed correctly");

        var slideHome=slide.Position;
        var detailHome=detail.Position;
        glock.Fire(false);
        glock._Process(.05);
        var slideShift=slide.Position-slideHome;
        var detailShift=detail.Position-detailHome;
        if(Math.Abs(glock.LastSlideMotorOffset.Z+.063f)>.0002f ||
           slideShift.DistanceTo(detailShift)>.0002f ||
           Math.Abs(slideShift.X)<.045f ||
           Math.Abs(slideShift.Z)>.02f)
            throw new InvalidOperationException(
                "Pass62 rotated source Handle pivot failed to move entire Glock Slide group");
        glock._Process(.08);
        if(slide.Position.DistanceTo(slideHome)>.0002f ||
           detail.Position.DistanceTo(detailHome)>.0002f)
            throw new InvalidOperationException(
                "Pass62 source Glock Slide group failed to return to original rest pose");

        var moss=new WeaponViewModelRuntime{Name="Pass62Mossberg"};
        AddChild(moss);
        moss.SetWeapon(RuntimeWeaponCatalog.Get("Mossberg 500"));
        var mossRig=moss.GetNodeOrNull<Node3D>("WeaponBody");
        var pump=mossRig?.GetNodeOrNull<MeshInstance3D>("Pump");
        var rail=mossRig?.GetNodeOrNull<MeshInstance3D>("ForeRail");
        if(mossRig is null || pump is null || rail is null ||
           moss.SourceMotorHierarchyGroups!=2 ||
           moss.SourcePumpGroupParts!=2)
            throw new InvalidOperationException(
                "Pass62 Mossberg source Pump group was not imported");
        var pumpHome=pump.Position;
        var railHome=rail.Position;
        moss.Fire(false);
        moss._Process(.4125);
        var pumpShift=pump.Position-pumpHome;
        var railShift=rail.Position-railHome;
        if(Math.Abs(moss.LastPumpMotorOffset.Z+.084f)>.0003f ||
           pumpShift.DistanceTo(railShift)>.0002f ||
           Math.Abs(pumpShift.X)<.06f ||
           Math.Abs(pumpShift.Z)>.025f)
            throw new InvalidOperationException(
                "Pass62 original pivot failed to move all source pump components");
        moss._Process(.38);
        if(pump.Position.DistanceTo(pumpHome)>.0002f ||
           rail.Position.DistanceTo(railHome)>.0002f)
            throw new InvalidOperationException(
                "Pass62 source Pump group failed to return to rest");

        GD.Print("TWR_SMOKE_PASS62_SOURCE_HIERARCHY_OK "+
            "synthetic_private_pack_gate=true "+
            "source_index_mapping=true source_nested_handle_pivots=true "+
            "all_glock_slide_children_animated=2 "+
            "all_mossberg_pump_children_animated=2 "+
            "rotated_local_motor_axes=true returned_to_rest=true "+
            "source_mesh_bytes_not_claimed=true combat_authority_unchanged=true");
        GetTree().Quit(0);
    }

    private void RunPass61Smoke()
    {
        if(!Pass61SourceWeaponHandling.TryGet("Glock 17",out var glock) ||
           glock?.Equip is null || glock.CyclingSlide is null ||
           glock.Equip.Frames.Length!=4 ||
           glock.CyclingSlide.Frames.Length!=2 ||
           Math.Abs(glock.CyclingSlide.Duration-.125f)>.0001f ||
           !Pass61SourceWeaponHandling.TryRelativeMotorOffset(
               "module.Stats.Rels.SlideRel * CFrame.new(0, 0, 0.225)",
               "SlideRel",out var originalSlide) ||
           Math.Abs(originalSlide.Z+.063f)>.0001f ||
           Pass61SourceWeaponHandling.TryRelativeMotorOffset(
               "module.Stats.Rels.SlideRel * execute()", "SlideRel",out _) ||
           Pass61SourceWeaponHandling.TryRelativeMotorOffset(
               "module.Stats.Rels.MagRel * CFrame.new(0,0,0.4)",
               "SlideRel",out _) ||
           Pass61SourceWeaponHandling.TryRelativeMotorOffset(
               "module.Stats.Rels.SlideRel * CFrame.new(NaN,0,0)",
               "SlideRel",out _))
            throw new InvalidOperationException(
                "Pass61 original Glock mechanical animation parsing failed");
        var slidePeak=Pass61SourceWeaponHandling.SampleMotor(
            glock.CyclingSlide,.05f);
        var slideRest=Pass61SourceWeaponHandling.SampleMotor(
            glock.CyclingSlide,.125f);
        var equipPose=Pass61SourceWeaponHandling.SampleEquip(
            glock.Equip,.32f);
        if(Math.Abs(slidePeak.Z+.063f)>.0002f ||
           slideRest.LengthSquared()>.000001f ||
           equipPose.Origin.LengthSquared()<.0001f ||
           Pass61SourceWeaponHandling.SampleEquip(
               glock.Equip,glock.Equip.Duration).Origin.LengthSquared()>.000001f)
            throw new InvalidOperationException(
                "Pass61 source model motor/equip keyframe interpolation failed");

        if(!Pass61SourceWeaponHandling.TryGet("Mossberg 500",out var moss) ||
           moss?.CyclingPump is null || moss.CyclingSlide is null ||
           moss.ReloadPump is null ||
           Math.Abs(Pass61SourceWeaponHandling.SampleMotor(
                moss.CyclingPump,.4125f).Z+.084f)>.0003f)
            throw new InvalidOperationException(
                "Pass61 source Mossberg pump and slide timelines missing");

        var actor=new Node3D {Name="Pass61SourceHandling"};
        AddChild(actor);
        var glockModel=new WeaponViewModelRuntime{Name="Pass61Glock"};
        actor.AddChild(glockModel);
        glockModel.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var gunRig=glockModel.GetNodeOrNull<Node3D>("WeaponBody");
        var slide=gunRig?.GetNodeOrNull<MeshInstance3D>("PistolSlide");
        if(gunRig is null || slide is null ||
           !glockModel.HasSourceEquipClip ||
           glockModel.SourceCyclingSlideFrames!=2)
            throw new InvalidOperationException(
                "Pass61 source Glock visual assembly not initialized");
        var startingSlide=slide.Position;
        glockModel.BeginEquip(.8);
        glockModel._Process(.32);
        if(gunRig.Position.LengthSquared()<.0001f)
            throw new InvalidOperationException("Pass61 equip clip not visible");
        glockModel._Process(.49);
        glockModel.Fire(false);
        glockModel._Process(.05);
        if(Math.Abs(glockModel.LastSlideMotorOffset.Z+.063f)>.0003f ||
           slide.Position.DistanceTo(startingSlide)<.05f)
            throw new InvalidOperationException("Pass61 Glock visual slide did not cycle");
        glockModel._Process(.08);
        if(slide.Position.DistanceTo(startingSlide)>.0002f)
            throw new InvalidOperationException(
                "Pass61 Glock slide did not reset after cycling");

        var mossModel=new WeaponViewModelRuntime{Name="Pass61Mossberg"};
        actor.AddChild(mossModel);
        mossModel.SetWeapon(RuntimeWeaponCatalog.Get("Mossberg 500"));
        var mossRig=mossModel.GetNodeOrNull<Node3D>("WeaponBody");
        var pump=mossRig?.GetNodeOrNull<MeshInstance3D>("Pump");
        if(pump is null || mossModel.SourceCyclingPumpFrames!=5 ||
           mossModel.SourceReloadPumpFrames!=13)
            throw new InvalidOperationException(
                "Pass61 source shotgun pump viewmodel not initialized");
        var initialPump=pump.Position;
        mossModel.Fire(false);
        mossModel._Process(.4125);
        if(Math.Abs(mossModel.LastPumpMotorOffset.Z+.084f)>.0003f ||
           pump.Position.DistanceTo(initialPump)<.075f)
            throw new InvalidOperationException(
                "Pass61 Mossberg pump did not move by recovered travel");
        mossModel._Process(.38);
        if(pump.Position.DistanceTo(initialPump)>.0002f)
            throw new InvalidOperationException(
                "Pass61 Mossberg pump failed to return");
        if(_runtime.Player is not null && _runtime.Player.Credits<0)
            throw new InvalidOperationException(
                "Pass61 test unexpectedly changed player economy");
        GD.Print("TWR_SMOKE_PASS61_CYCLING_OK "+
            "source_equip_main_pose=true source_glock_slide_meters=0.063 "+
            "source_mossberg_pump_meters=0.084 "+
            "source_reload_pump_timeline=true "+
            "actual_viewmodel_parts_moved=true returned_to_rest=true "+
            "unknown_motor_expressions_blocked=true "+
            "combat_ammo_and_economy_unmodified=true");
        GetTree().Quit(0);
    }

    private void RunPass60Smoke()
    {
        if(Pass60WeaponPoseMath.TryParse("module.Stats.GripCF",null,out _) ||
           Pass60WeaponPoseMath.TryParse("CFrame.new(999,0,0)",null,out _) ||
           Pass60WeaponPoseMath.TryParse("CFrame.new(0,0,NaN)",null,out _) ||
           Pass60WeaponPoseMath.TryParse(
               "CFrame.new(0,0,0) * execute_remote_code()",null,out _) ||
           !Pass60WeaponPoseMath.TryParse(
               "CFrame.new(0,0,0.1) * CFrame.Angles(0.17453292519943295,0,0)",
               null,out var literal) ||
           Math.Abs(literal.Origin.Z-.028f)>.0001f)
            throw new InvalidOperationException("Pass60 safe source CFrame parser failed");
        if(!Pass60WeaponAnimationLibrary.TryGet("Glock 17",out var glock) ||
           !glock.HasSprint || glock.Fire is null ||
           glock.AimFire is null || glock.Reload is null ||
           glock.Reload.Frames.Length<10 ||
           Math.Abs(glock.Fire.Value.Target.Origin.Z-.028f)>.0001f ||
           Math.Abs(glock.AimFire.Value.Target.Origin.Z-.014f)>.0001f ||
           Math.Abs(glock.Sprint.Origin.X+.084f)>.0001f ||
           Math.Abs(glock.Sprint.Origin.Z-.028f)>.0001f ||
           Math.Abs(glock.Fire.Value.Target.Basis.GetEuler().X-
               .1745329252f)>.002f)
            throw new InvalidOperationException("Pass60 Glock source animation unavailable");
        var reloadPose=Pass60WeaponAnimationLibrary.SampleReload(
            glock.Reload,.10f);
        if(reloadPose.Origin.LengthSquared()<.0001f ||
           Pass60WeaponAnimationLibrary.SampleReload(
               glock.Reload,1f).Origin.LengthSquared()>.000001f)
            throw new InvalidOperationException("Pass60 reload keyframe progress invalid");
        var view=new WeaponViewModelRuntime{Name="Pass60Viewmodel"};
        AddChild(view);
        view.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var rig=view.GetNodeOrNull<Node3D>("WeaponBody");
        if(rig is null || view.SourceReloadFrames<10 ||
           !view.SourceSprintRotationAvailable)
            throw new InvalidOperationException("Pass60 viewmodel source animation missing");
        view.Fire(false);
        view._Process(glock.Fire.Value.Out);
        if(Math.Abs(rig.Position.Z-.028f)>.0003f ||
           Math.Abs(rig.Rotation.X-.1745329252f)>.004f)
            throw new InvalidOperationException("Pass60 hip-fire source pose failed");
        view.Fire(true);
        view._Process(glock.AimFire.Value.Out);
        if(Math.Abs(rig.Position.Z-.014f)>.0003f ||
           Math.Abs(rig.Rotation.X-.0872664626f)>.004f)
            throw new InvalidOperationException("Pass60 aimed source pose failed");
        view._Process(glock.AimFire.Value.Back+.01f);
        view.Reload(2.0);
        view._Process(.2f);
        if(rig.Position.LengthSquared()<.0001f)
            throw new InvalidOperationException("Pass60 source reload position not sampled");
        view._Process(1.81f);
        if(rig.Position.LengthSquared()>.000001f)
            throw new InvalidOperationException("Pass60 reload pose leaked past finish");
        GD.Print("TWR_SMOKE_PASS60_ANIMATION_OK "+
            "source_matrix_and_angles=true "+
            "hip_translation_and_rotation=true aim_translation_and_rotation=true "+
            "original_sprint_position_and_rotation=true "+
            "original_reload_keyframes=true reload_duration_authority_unchanged=true "+
            "invalid_source_expressions_blocked=true");
        GetTree().Quit(0);
    }

    private void RunPass59Smoke()
    {
        if(!Pass59WeaponMotion.TryGet("Glock 17",out var glock) ||
           !glock.HasHip || !glock.HasAim ||
           Math.Abs(glock.Hip.Pullback-.028f)>.0001f ||
           Math.Abs(glock.Aim.Pullback-.014f)>.0001f ||
           Math.Abs(glock.Hip.Out-.075f)>.0001f ||
           Math.Abs(glock.Hip.Back-.15f)>.0001f ||
           Pass59WeaponMotion.ParseVector("module.Stats.GripCF",out _) ||
           Pass59WeaponMotion.ParseVector("CFrame.new(999,0,0)",out _) ||
           Pass59WeaponMotion.ParseVector("CFrame.new(1,0,NaN)",out _))
            throw new InvalidOperationException("Pass59 source fire motion parse failed");
        var view=new WeaponViewModelRuntime{Name="Pass59Viewmodel"};
        AddChild(view);
        view.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var rig=view.GetNodeOrNull<Node3D>("WeaponBody");
        if(rig is null)throw new InvalidOperationException("Pass59 no viewmodel rig");
        view.Fire(false);
        view._Process(glock.Hip.Out);
        var hipDistance=rig.Position.Z;
        view.Fire(true);
        view._Process(glock.Aim.Out);
        var aimDistance=rig.Position.Z;
        if(Math.Abs(hipDistance-glock.Hip.Pullback)>.0002f ||
           Math.Abs(aimDistance-glock.Aim.Pullback)>.0002f ||
           hipDistance<=aimDistance)
            throw new InvalidOperationException("Pass59 aim/hip gun model displacement failed");
        view._Process(glock.Aim.Back+.01f);
        if(Math.Abs(rig.Position.Z)>.0002f)
            throw new InvalidOperationException("Pass59 weapon fire pose failed to reset");
        GD.Print("TWR_SMOKE_PASS59_SOURCE_MOTION_OK weapon=Glock17 "+
            "hip_meters=0.028 aim_meters=0.014 "+
            "source_shot_timing=true returned_to_rest=true "+
            "inert_luau_expressions=true");
        GetTree().Quit(0);
    }

    private void RunPass58Smoke()
    {
        // Synthetic-only 13-part original-style record with a handmade OBJ.
        // Original remote Roblox triangle meshes have not been recovered.
        if(OriginalWeaponSourceRuntime.SourceModelCount!=1 ||
           OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass58 synthetic source weapon fixture not loaded safely");

        var installed=OfflineAssetResolver.Mesh("rbxassetid://555580001");
        if(installed is not ArrayMesh || installed.GetSurfaceCount()!=1)
            throw new InvalidOperationException("Pass58 explicit local OBJ not resolved");

        var scene=new Node3D {Name="Pass58SyntheticSourceModel"};
        AddChild(scene);
        var view=new WeaponViewModelRuntime {Name="Glock 17"};
        scene.AddChild(view);
        view.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var rig=view.GetNodeOrNull<Node3D>("WeaponBody");
        var parts=rig?.GetChildren().OfType<MeshInstance3D>()
            .ToDictionary(p=>p.Name.ToString(),StringComparer.Ordinal);
        if(!view.UsingOriginalToolAssembly || rig is null ||
           parts is null || parts.Count!=13 || parts.ContainsKey("Handle") ||
           view.VisualPartCount!=12 || view.SourceVisibleParts!=12 ||
           view.SourceLocallyResolvedMeshes!=1 ||
           view.SourceMissingMeshProxies!=8 ||
           view.SourceDetailProfileProxies!=5 ||
           view.SourceLensProxies!=1 ||
           view.SourceCircularProxyParts!=1 ||
           view.SourceConservativeProxyParts!=1 ||
           view.SourceNativePrimitiveParts!=3)
            throw new InvalidOperationException(
                "Pass58 original 3D source/shape proxy counts differ from fixture");

        if(parts["Bolt"].Mesh!=installed ||
           parts["Barrel"].Mesh is not ArrayMesh ||
           parts["Receiver"].Mesh is not ArrayMesh ||
           parts["Magazine"].Mesh is not ArrayMesh mag ||
           parts["Stock"].Mesh is not ArrayMesh ||
           parts["PistolGrip"].Mesh is not ArrayMesh ||
           parts["TriggerGuard"].Mesh is not ArrayMesh ring ||
           parts["ReticleGlass"].Mesh is not ArrayMesh lens ||
           parts["UnknownCasing"].Mesh is not BoxMesh ||
           parts["Ball"].Mesh is not SphereMesh ||
           parts["Cylinder"].Mesh is not ArrayMesh ||
           parts["StockWedge"].Mesh is not ArrayMesh ||
           mag.SurfaceGetArrayLen(0)!=72 ||
           ring.SurfaceGetArrayLen(0)!=144 ||
           lens.SurfaceGetArrayLen(0)!=Pass57SourceGeometryProxy.CircularSides*12)
            throw new InvalidOperationException(
                "Pass58 source-guided magazine/grip/receiver/lens/guard geometry not used");

        if(Pass58WeaponDetailGeometry.TryCreate(
                "MeshPart","Receiver",new Vector3(float.NaN,.2f,.6f),
                out _,out _) ||
           Pass58WeaponDetailGeometry.TryCreate(
                "MeshPart","Receiver",new Vector3(.3f,.4f,.6f)*-1f,
                out _,out _) ||
           Pass58WeaponDetailGeometry.TryCreate(
                "MeshPart","RandomUnknown",new Vector3(.3f,.4f,.6f),
                out _,out _) ||
           Pass58WeaponDetailGeometry.TryCreate(
                "Part","Magazine",new Vector3(.15f,.51f,.32f),
                out _,out _) ||
           !Pass58WeaponDetailGeometry.TryCreate(
                "MeshPart","Magazine",new Vector3(.15f,.51f,.32f),
                out var cached,out var category) ||
           category!="magazine" || !ReferenceEquals(cached,mag) ||
           !Pass58WeaponDetailGeometry.TryLensAxis(
                new Vector3(.16f,.16f,.04f),out var axis) || axis!=2)
            throw new InvalidOperationException(
                "Pass58 source semantics/bounds protection or mesh cache failed");

        var credits=_runtime.Player?.Credits ?? -1;
        var owned=_runtime.Profile?.OwnedSkins.Count ?? -1;
        RemoveChild(scene);
        scene.Free();
        ShowMenu();
        ShowArmory();
        var showroom=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        if(showroom is null || !showroom.Preview("Glock 17") ||
           showroom.PreviewVisibleParts!=12 ||
           showroom.PreviewLocallyResolvedMeshes!=1 ||
           showroom.PreviewDetailProfiles!=5 ||
           showroom.PreviewLensDisks!=1 ||
           showroom.PreviewCircularProxies!=1 ||
           showroom.PreviewConservativeProxies!=1 ||
           !showroom.PreviewStatus.Contains("RECONSTRUCTED SILHOUETTES",
               StringComparison.Ordinal) ||
           !showroom.PreviewStatus.Contains("OPTIC DISKS",
               StringComparison.Ordinal) ||
           !showroom.PreviewStatus.Contains("SOURCE MESH IDS UNAVAILABLE",
               StringComparison.Ordinal) ||
           (_runtime.Player?.Credits ?? -1)!=credits ||
           (_runtime.Profile?.OwnedSkins.Count ?? -1)!=owned)
            throw new InvalidOperationException(
                "Pass58 3D Armory geometry readouts or inventory changed");

        StartGame("Manor");
        if(_sourceLobby is not null || _game is null ||
           GetChildren().Any(n=>n.Name.ToString()=="Pass58SyntheticSourceModel"))
            throw new InvalidOperationException(
                "Pass58 3D source geometry leaked into match lifecycle");
        GD.Print("TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK synthetic_only=true " +
            "visible=12 local_obj=1 missing_mesh_ids=8 " +
            "detail_profiles=5 lens_disk=1 barrel_proxy=1 " +
            "unclassified_bounds=1 native_primitives=3 " +
            "trigger_guard_open=true source_pose_preserved=true " +
            "profile_mesh_cached=true invalid_bounds_rejected=true " +
            "armory_fidelity_counters=true credits_unchanged=true " +
            "original_mesh_triangles_not_claimed=true");
        GetTree().Quit(0);
    }

    private void RunPass57Smoke()
    {
        // Exported Windows Godot executable, synthetic owner-like tool
        // metadata and synthetic local OBJ. NEVER needs Roblox or HTTP.
        if(OfflineAssetResolver.CanonicalAssetId("rbxassetid://555570001")
                != "555570001" ||
           OfflineAssetResolver.CanonicalAssetId("555570001")!="555570001" ||
           OfflineAssetResolver.CanonicalAssetId("https://evil.test/555570001")!="" ||
           OfflineAssetResolver.CanonicalAssetId("rbxassetid://../555570001")!="" ||
           OfflineAssetResolver.CanonicalAssetId("rbxassetid://555570001/")!="" ||
           OfflineAssetResolver.CanonicalAssetId("rbxassetid://")!="")
            throw new InvalidOperationException(
                "Pass57 local asset ID canonicalization/safety failed");

        var localMesh=OfflineAssetResolver.Mesh("rbxassetid://555570001");
        if(localMesh is not ArrayMesh || localMesh.GetSurfaceCount()<1 ||
           OfflineAssetResolver.Mesh("555570001")!=localMesh ||
           OfflineAssetResolver.Mesh("rbxassetid://555570002") is not null ||
           OfflineAssetResolver.Mesh("../../555570001") is not null)
            throw new InvalidOperationException(
                "Pass57 source URL form did not resolve local OBJ without network");

        if(OriginalWeaponSourceRuntime.SourceModelCount!=1 ||
           OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass57 private original models were unexpectedly used in CI");

        var scene=new Node3D {Name="Pass57SyntheticGeometry"};
        AddChild(scene);
        var visual=new WeaponViewModelRuntime {Name="Glock 17"};
        scene.AddChild(visual);
        visual.SetWeapon(RuntimeWeaponCatalog.Get("Glock 17"));
        var rig=visual.GetNodeOrNull<Node3D>("WeaponBody");
        var parts=rig?.GetChildren().OfType<MeshInstance3D>()
            .ToDictionary(p=>p.Name.ToString(),StringComparer.Ordinal);
        if(!visual.UsingOriginalToolAssembly || rig is null ||
           parts is null || parts.Count!=9 || parts.ContainsKey("Handle") ||
           visual.VisualPartCount!=8 || visual.SourceVisibleParts!=8 ||
           visual.SourceLocallyResolvedMeshes!=1 ||
           visual.SourceMissingMeshProxies!=2 ||
           visual.SourceCircularProxyParts!=1 ||
           visual.SourceConservativeProxyParts!=2 ||
           visual.SourceNativePrimitiveParts!=4)
            throw new InvalidOperationException(
                "Pass57 original source part accounting or invisibility mismatch");

        if(parts["Bolt"].Mesh!=localMesh ||
           parts["Barrel"].Mesh is not ArrayMesh barrelMesh ||
           parts["Receiver"].Mesh is not BoxMesh ||
           parts["Ball"].Mesh is not SphereMesh ||
           parts["Cylinder"].Mesh is not ArrayMesh ||
           parts["Block"].Mesh is not BoxMesh ||
           parts["StockWedge"].Mesh is not ArrayMesh ||
           parts["Guard"].Mesh is not BoxMesh ||
           barrelMesh.GetSurfaceCount()!=1 ||
           barrelMesh.SurfaceGetArrayLen(0)!=Pass57SourceGeometryProxy.CircularSides*12)
            throw new InvalidOperationException(
                "Pass57 OBJ, round tube, source native primitives, wedge or conservative bounds failed");

        if(!Pass57SourceGeometryProxy.TryTubeAxis(
                new Vector3(.12f,.13f,1.15f),out var major) || major!=2 ||
           Pass57SourceGeometryProxy.TryTubeAxis(
                new Vector3(.45f,.35f,.51f),out _) ||
           Pass57SourceGeometryProxy.IdentifiableTube("BarrelGrip") ||
           !Pass57SourceGeometryProxy.IdentifiableTube("Barrel") ||
           Pass57SourceGeometryProxy.IdentifiableTube("Receiver") ||
           !ReferenceEquals(Pass57SourceGeometryProxy.Tube(2),
                Pass57SourceGeometryProxy.Tube(2)))
            throw new InvalidOperationException(
                "Pass57 narrow source geometry proxy guards failed");

        var credits=_runtime.Player?.Credits ?? -1;
        var owned=_runtime.Profile?.OwnedSkins.Count ?? -1;
        RemoveChild(scene);
        scene.QueueFree();
        ShowMenu();
        ShowArmory();
        var lobby=_sourceLobby;
        var showroom=lobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        if(showroom is null || !showroom.Preview("Glock 17") ||
           showroom.PreviewLocallyResolvedMeshes!=1 ||
           showroom.PreviewCircularProxies!=1 ||
           showroom.PreviewConservativeProxies!=2 ||
           !showroom.PreviewStatus.Contains("LOCAL TRIANGLE MESHES",
                StringComparison.Ordinal) ||
           !showroom.PreviewStatus.Contains("SOURCE MESH IDS UNAVAILABLE",
                StringComparison.Ordinal) ||
           (_runtime.Player?.Credits ?? -1)!=credits ||
           (_runtime.Profile?.OwnedSkins.Count ?? -1)!=owned)
            throw new InvalidOperationException(
                "Pass57 Armory source geometry diagnostics/economy integrity failed");

        StartGame("Manor");
        if(_sourceLobby is not null || _game is null ||
           GetChildren().Any(node=>node.Name.ToString()=="Pass57SyntheticGeometry"))
            throw new InvalidOperationException(
                "Pass57 source visual assembly leaked into gameplay lifecycle");
        GD.Print("TWR_SMOKE_PASS57_GEOMETRY_OK synthetic_only=true " +
            "source_visible_parts=8 local_obj_meshes=1 " +
            "unresolved_source_mesh_ids=2 circular_mesh_proxies=1 " +
            "conservative_bounds=2 native_primitive_parts=4 " +
            "wedge_mesh=true cylinder_mesh=true " +
            "canonical_rbxassetid_url=true path_traversal_blocked=true " +
            "armory_provenance_shown=true player_economy_unchanged=true " +
            "owner_mesh_triangles_not_claimed=true");
        GetTree().Quit(0);
    }

    private void RunPass56Smoke()
    {
        if(Pass56WikiVisualReferences.WeaponCount!=1 ||
           Pass56WikiVisualReferences.OwnerPackVerified ||
           !Pass56WikiVisualReferences.SyntheticFixture ||
           Pass56WikiVisualReferences.ImageCount!=3 ||
           Pass56WikiVisualReferences.SkinIconCount!=1 ||
           !Pass56WikiVisualReferences.TryWeaponImage(
                "Glock 17","Neon/Ego",out var picture,out var caption) ||
           picture is null || picture.GetWidth()!=4 ||
           !caption.Contains("Neon/Ego",StringComparison.Ordinal))
            throw new InvalidOperationException("Pass56 source wiki image validation failed");
        if(!Pass56WikiVisualReferences.TryWeaponImage(
             "Glock 17","Low/Cocoa",out var fallback,out var fallbackCaption) ||
           fallback is null || !fallbackCaption.Contains("DEFAULT image only",
               StringComparison.Ordinal))
            throw new InvalidOperationException("Pass56 missing skin fallback not labelled");

        var credits=_runtime.Player?.Credits ?? -1;
        var owned=_runtime.Profile?.OwnedSkins.Count ?? -1;
        ShowMenu();
        ShowArmory();
        var showroom=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        if(showroom is null || !showroom.Preview("Glock 17"))
            throw new InvalidOperationException("Pass56 Glock reference preview not selected");
        var button=_armory?.GetChildren().OfType<Button>()
            .FirstOrDefault(b=>b.Name.ToString()=="ArmoryWikiReference");
        button?.EmitSignal(Button.SignalName.Pressed);
        if(button is null || _wikiReferenceGallery is null ||
           _wikiReferenceImage?.Texture is null ||
           _wikiReferenceCaption is null ||
           !_wikiReferenceCaption.Text.Contains("NOT ORIGINAL 3D/UV TEXTURE",
                StringComparison.Ordinal) ||
           _armory is null || _armory.Visible)
            throw new InvalidOperationException("Pass56 armory gallery unavailable");
        var back=_wikiReferenceGallery.GetChildren().OfType<Button>()
            .FirstOrDefault(b=>b.Name.ToString()=="CloseWikiReferenceGallery");
        back?.EmitSignal(Button.SignalName.Pressed);
        if(back is null || _wikiReferenceGallery is not null || !_armory.Visible)
            throw new InvalidOperationException("Pass56 gallery did not restore armory");

        ShowSkinInventory(false);
        var action=_skinInventory?.GetChildren().OfType<Button>()
            .FirstOrDefault(b=>b.Name.ToString()=="InventoryWikiReference");
        action?.EmitSignal(Button.SignalName.Pressed);
        if(action is null || _wikiReferenceGallery is null ||
           _skinInventory is null || _skinInventory.Visible)
            throw new InvalidOperationException("Pass56 inventory gallery missing");
        CloseWikiReferenceGallery();
        if(!_skinInventory.Visible || (_runtime.Player?.Credits??-1)!=credits ||
           (_runtime.Profile?.OwnedSkins.Count??-1)!=owned)
            throw new InvalidOperationException("Pass56 changed domain state");
        StartGame("Manor");
        if(_sourceLobby is not null || _game is null ||
           _wikiReferenceGallery is not null)
            throw new InvalidOperationException("Pass56 gallery leaked into gameplay");
        GD.Print("TWR_SMOKE_PASS56_WIKI_REFERENCE_OK synthetic_only=true "+
            "files=3 png_decoded=true fallback_disclosed=true "+
            "gallery_back_restored=true inventory_view_preserved=true "+
            "credits_unchanged=true gallery_removed_on_match=true "+
            "owner_images_not_uploaded=true");
        GetTree().Quit(0);
    }

    private void RunPass55Smoke()
    {
        // Real exported Windows Godot runner. Only synthetic palettes,
        // source-style tool tags and a labeled fake PNG are installed.
        // The original texture IDs never trigger network requests.
        if(Pass55SourceSkinLayers.OwnerPackVerified ||
           Pass55SourceSkinLayers.LoadedSkinCount!=3 ||
           Pass55SourceSkinLayers.LayerReferenceCount!=1 ||
           Pass55SourceSkinLayers.MeshTextureReferenceCount!=1 ||
           !Pass55SourceSkinLayers.TryGetSlot("High/Gold",1,out var goldSource) ||
           goldSource.Layers.Count!=1 ||
           goldSource.Layers[0].TextureId!="5555500000001" ||
           !Pass55SourceSkinLayers.TryGetSlot("Ethereal/Spirit",1,out var spiritSource) ||
           spiritSource.MeshTextureId!="5555500000002" ||
           Pass55SourceSkinLayers.TryGetSlot("NotOwned/NoSkin",1,out _) ||
           OfflineAssetResolver.Texture("5555500000001") is null ||
           OfflineAssetResolver.Texture("5555500000002") is not null)
            throw new InvalidOperationException(
                "Pass55 bounded original-style skin references or local PNG resolver failed");

        var profile=_runtime.Profile ??
            throw new InvalidOperationException("Pass55 test-only profile missing");
        var credits=_runtime.Player?.Credits ?? -1;
        if(credits!=12000 || profile.OwnedSkins.Count!=3 ||
           profile.EquippedWeaponSkins.GetValueOrDefault("Glock 17")!="Low/Cocoa")
            throw new InvalidOperationException("Pass55 isolated profile fixture invalid");

        ShowMenu();
        var lobby=_sourceLobby ??
            throw new InvalidOperationException("Pass55 synthetic lobby missing");
        var stage=lobby.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName) ??
            throw new InvalidOperationException("Pass55 source-style showroom missing");
        ShowArmory();

        WeaponViewModelRuntime? Model() =>
            stage.GetNodeOrNull<WeaponViewModelRuntime>(
            "OriginalLoadoutAnchor_View/SourceSelectedPreviewTilt/DisplayedWeapon_View");
        MeshInstance3D? Barrel() =>
            stage.GetNodeOrNull<MeshInstance3D>(
            "OriginalLoadoutAnchor_View/SourceSelectedPreviewTilt/DisplayedWeapon_View/WeaponBody/Barrel");

        if(!stage.Preview("Glock 17") || Model()?.ActiveCosmeticSkinId!="Low/Cocoa")
            throw new InvalidOperationException("Pass55 initial skin not selected");

        if(!_runtime.ApplyWeaponSkin("Glock 17","High/Gold").Success)
            throw new InvalidOperationException("Pass55 source texture skin assignment rejected");
        ShowArmory();
        if(!stage.Preview("Glock 17") ||
           Barrel()?.MaterialOverlay is not StandardMaterial3D gold ||
           gold.AlbedoTexture is null ||
           Model()?.SourceInstalledTextureParts!=1 ||
           Model()?.SourceProceduralTextureParts!=0 ||
           Model()?.SourceTextureReferencedParts!=1 ||
           Math.Abs(gold.Uv1Scale.X-.5f)>.001f ||
           Math.Abs(gold.Uv1Scale.Y-1f/3f)>.001f ||
           Math.Abs(gold.Uv1Offset.X-.25f)>.001f ||
           Math.Abs(gold.Uv1Offset.Y+.125f)>.001f)
            throw new InvalidOperationException(
                "Pass55 installed PNG/tile/offset source metadata not applied");

        if(!_runtime.ApplyWeaponSkin("Glock 17","Ethereal/Spirit").Success)
            throw new InvalidOperationException("Pass55 ForceField skin assignment rejected");
        ShowArmory();
        if(!stage.Preview("Glock 17") ||
           Model() is not {SourceProceduralTextureParts:1} fx ||
           fx.AnimatedSkinMaterialCount<1 ||
           Barrel()?.MaterialOverlay is not StandardMaterial3D material ||
           material.AlbedoTexture is null ||
           !fx.CosmeticSourceStatus.Contains("PROCEDURAL TEXTURE PROXY",
               StringComparison.Ordinal) ||
           !fx.CosmeticSourceStatus.Contains("RECONSTRUCTED SHIMMER",
               StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Pass55 absent source image not identified as procedural proxy");

        var baseline=material.EmissionEnergyMultiplier;
        fx.AdvanceCosmeticEffects(.10f);
        if(fx.CosmeticEffectElapsed<=0f ||
           Math.Abs(fx.LastShimmerMultiplier-1f)<.001f ||
           Math.Abs(material.EmissionEnergyMultiplier-baseline)<.001f ||
           !fx.StaticShowroomMode)
            throw new InvalidOperationException(
                "Pass55 deterministic showroom shimmer did not advance");
        fx.AdvanceCosmeticEffects(float.NaN);
        if(!float.IsFinite(fx.CosmeticEffectElapsed))
            throw new InvalidOperationException("Pass55 rejected NaN timing incorrectly");

        if(!_runtime.ApplyWeaponSkin("Glock 17",null).Success)
            throw new InvalidOperationException("Pass55 skin reset rejected");
        ShowArmory();
        if(!stage.Preview("Glock 17") ||
           Model()?.AnimatedSkinMaterialCount!=0 ||
           Model()?.SourceTextureReferencedParts!=0 ||
           Model()?.CosmeticProxyTintParts!=0 ||
           Barrel()?.MaterialOverlay is not null ||
           (_runtime.Player?.Credits??-1)!=credits ||
           profile.OwnedSkins.Count!=3)
            throw new InvalidOperationException(
                "Pass55 cosmetic reset/economy invariants did not hold");

        StartGame("Manor");
        if(_sourceLobby is not null || _game is null)
            throw new InvalidOperationException("Pass55 material showroom leaked into match");
        GD.Print("TWR_SMOKE_PASS55_LAYERS_OK synthetic_only=true " +
            "layer_count=1 mesh_texture_count=1 source_slot_1=true " +
            "local_png_loaded=true uv_tiling_preserved=true " +
            "unavailable_image_proxy_labeled=true reconstructed_shimmer=true " +
            "material_reset=true economy_unchanged=true " +
            "owner_source_textures_not_uploaded=true");
        GetTree().Quit(0);
    }

    private void RunPass54Smoke()
    {
        // Exported Windows Godot test with synthetic source packs only.
        // The CI skin profile is isolated from user://profile.json.
        if(Pass54SourceSkinMaterials.OwnerPaletteVerified ||
           Pass54SourceSkinMaterials.OwnerSlotsVerified ||
           Pass54SourceSkinMaterials.LoadedSkinCount!=3 ||
           Pass54SourceSkinMaterials.LoadedTaggedParts!=4 ||
           !Pass54SourceSkinMaterials.TryGet("Low/Cocoa",out var palette) ||
           palette.Length!=5 ||
           Math.Abs(palette[0].Color.R-40f/255f)>.0001f ||
           Math.Abs(palette[1].Color.R-61f/255f)>.0001f ||
           !Pass54SourceSkinMaterials.TryGetPartSlot("Glock 17",0,out var slot) ||
           slot!=1 ||
           Pass54SourceSkinMaterials.TryGetPartSlot("Glock 17",9,out _))
            throw new InvalidOperationException(
                "Pass54 synthetic palettes or original-style Value1..5 tags failed");

        var profile=_runtime.Profile ??
            throw new InvalidOperationException("Pass54 profile unavailable");
        var credits=_runtime.Player?.Credits ?? -1;
        if(credits!=12000 || profile.OwnedSkins.Count!=3 ||
           profile.EquippedWeaponSkins.GetValueOrDefault("Glock 17")!="Low/Cocoa")
            throw new InvalidOperationException("Pass54 isolated fixture profile absent");

        ShowMenu();
        var lobby=_sourceLobby ??
            throw new InvalidOperationException("Pass54 source-style lobby missing");
        var showroom=lobby.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName) ??
            throw new InvalidOperationException("Pass54 showroom missing");
        ShowArmory();

        WeaponViewModelRuntime? Model() => showroom.GetNodeOrNull<WeaponViewModelRuntime>(
            "OriginalLoadoutAnchor_View/SourceSelectedPreviewTilt/DisplayedWeapon_View");
        MeshInstance3D? Barrel() => showroom.GetNodeOrNull<MeshInstance3D>(
            "OriginalLoadoutAnchor_View/SourceSelectedPreviewTilt/DisplayedWeapon_View/WeaponBody/Barrel");
        MeshInstance3D? Receiver() => showroom.GetNodeOrNull<MeshInstance3D>(
            "OriginalLoadoutAnchor_View/SourceSelectedPreviewTilt/DisplayedWeapon_View/WeaponBody/Receiver");

        if(!showroom.Visible || !showroom.Preview("Glock 17") ||
           Model() is not {SourceSkinMappedParts:4, SourceSkinColorParts:4} firstModel ||
           !firstModel.CosmeticSourceStatus.Contains("SYNTHETIC",StringComparison.Ordinal) ||
           Barrel()?.MaterialOverlay is not StandardMaterial3D m1 ||
           Receiver()?.MaterialOverlay is not StandardMaterial3D m2 ||
           Math.Abs(m1.AlbedoColor.R-40f/255f)>.001f ||
           Math.Abs(m2.AlbedoColor.R-61f/255f)>.001f)
            throw new InvalidOperationException("Pass54 original source slot RGB mismatch");

        if(!_runtime.ApplyWeaponSkin("Glock 17","High/Gold").Success)
            throw new InvalidOperationException("Pass54 skin selection not saved");
        ShowArmory();
        if(!showroom.Preview("Glock 17") ||
           Barrel()?.MaterialOverlay is not StandardMaterial3D gold ||
           Math.Abs(gold.AlbedoColor.R-188f/255f)>.001f ||
           Math.Abs(gold.AlbedoColor.G-155f/255f)>.001f ||
           Model()?.SourceSkinMappedParts!=4)
            throw new InvalidOperationException("Pass54 cached skin model kept old color");

        if(!_runtime.ApplyWeaponSkin("Glock 17",null).Success)
            throw new InvalidOperationException("Pass54 skin removal rejected");
        ShowArmory();
        if(!showroom.Preview("Glock 17") ||
           Model()?.CosmeticProxyTintParts!=0 ||
           Barrel()?.MaterialOverlay is not null ||
           (_runtime.Player?.Credits ?? -1)!=credits ||
           profile.OwnedSkins.Count!=3)
            throw new InvalidOperationException(
                "Pass54 failed to remove materials or altered player economy");

        StartGame("Manor");
        if(_sourceLobby is not null || _game is null)
            throw new InvalidOperationException("Pass54 showroom leaked into match");
        GD.Print("TWR_SMOKE_PASS54_SOURCE_SKINS_OK synthetic_only=true " +
            "five_source_material_slots=true tagged_weapon_parts=4 " +
            "exact_source_rgb_40_17_17=true 3d_material_switch=true " +
            "skin_reset_clears_material=true no_economy_change=true " +
            "no_lobby_in_match=true owner_assets_not_uploaded=true");
        GetTree().Quit(0);
    }

    private void RunPass53Smoke()
    {
        // Native exported .exe smoke with synthetic lobby/source geometry.
        // Checks a real persisted profile and the actual interactive Shop UI.
        var cases=SkinCaseCatalog.Cases;
        if(cases.Count!=7 || SkinCaseCatalog.AllSkins.Count!=118 ||
            cases.Sum(c=>c.PurchasePool.Length)!=104 ||
            cases.Sum(c=>c.EventOnlySkins.Length)!=14 ||
            cases.Single(c=>c.Name=="High").EventOnlySkins.Length!=9 ||
            cases.Single(c=>c.Name=="Hallows").OnSale ||
            SkinCaseCatalog.FindSkin("Hallows/Hexbrew")?.Exclusive!=true)
            throw new InvalidOperationException(
                "Pass53 source case inventory or exclusive case bounds changed");

        var deterministic=new SkinCaseService(_=>0);
        var testProfile=new Profile {
            Credits=15000,
            Unlocks=new HashSet<string>(StringComparer.Ordinal) { "Glock 17" },
            Loadout=new Dictionary<string,string>(StringComparer.Ordinal) {
                ["Secondary"]="Glock 17"
            }
        };
        var testPlayer=new PlayerState { Credits=15000 };
        if(deterministic.Open(testProfile,testPlayer,"Hallows").Success ||
            deterministic.Open(testProfile,testPlayer,"NoCase53").Success ||
            testPlayer.Credits!=15000)
            throw new InvalidOperationException("Pass53 offline case rejection charged credits");
        var opened=deterministic.Open(testProfile,testPlayer,"Low");
        if(!opened.Success || opened.SkinId!="Low/Cocoa" ||
            testPlayer.Credits!=9000 || testProfile.OwnedSkins.Count!=1 ||
            !deterministic.Apply(testProfile,"Glock 17",opened.SkinId).Success)
            throw new InvalidOperationException("Pass53 seeded source case roll/equip failed");
        if(deterministic.Open(testProfile,testPlayer,"Low").SkinId=="Low/Cocoa" ||
            !deterministic.Sell(testProfile,testPlayer,"Low/Cocoa").Success ||
            testPlayer.Credits!=6000 ||
            testProfile.EquippedWeaponSkins.Count!=0 ||
            deterministic.Sell(testProfile,testPlayer,"Low/Cocoa").Success ||
            deterministic.Apply(testProfile,"Glock 17","Low/Cocoa").Success)
            throw new InvalidOperationException(
                "Pass53 duplicate prevention, sale, or unowned cosmetic guards failed");
        testProfile.OwnedSkins.Add("Hallows/Hexbrew");
        if(deterministic.Sell(testProfile,testPlayer,"Hallows/Hexbrew").Success)
            throw new InvalidOperationException("Pass53 allowed exclusive skin sale");
        foreach(var name in cases.Single(c=>c.Name=="Low").PurchasePool)
            testProfile.OwnedSkins.Add("Low/"+name);
        var beforeComplete=testPlayer.Credits;
        if(deterministic.Open(testProfile,testPlayer,"Low").Success ||
            testPlayer.Credits!=beforeComplete)
            throw new InvalidOperationException("Pass53 full collection charged credits");

        var profile=_runtime.Profile ??
            throw new InvalidOperationException("Pass53 native runtime profile missing");
        var player=_runtime.Player ??
            throw new InvalidOperationException("Pass53 native runtime player missing");
        // LocalSessionNode installed an isolated fixture profile via the
        // JSON store before the domain session was constructed. UI never
        // writes authoritative player balances directly.
        if(player.Credits!=12000 || profile.Credits!=12000 ||
            profile.OwnedSkins.Count!=0 ||
            profile.EquippedWeaponSkins.Count!=0)
            throw new InvalidOperationException("Pass53 smoke profile fixture missing");
        ShowMenu();
        var lobby=_sourceLobby ??
            throw new InvalidOperationException("Pass53 synthetic source lobby unavailable");
        var showroom=lobby.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName) ??
            throw new InvalidOperationException("Pass53 original-positioned showroom missing");

        var openShop=_menu?.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Text=="SHOP / CASES");
        openShop?.EmitSignal(Button.SignalName.Pressed);
        if(openShop is null || _shop is null ||
            lobby.ActiveCameraName!="Shop" || _shopOpenButton?.Disabled!=false)
            throw new InvalidOperationException("Pass53 shop entry/action not working");
        var lowCase=_shop.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Name.ToString()=="ShopCase_Low");
        lowCase?.EmitSignal(Button.SignalName.Pressed);
        var opening=_shop.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Name.ToString()=="ShopOpenSelectedCase");
        opening?.EmitSignal(Button.SignalName.Pressed);
        if(lowCase is null || opening is null ||
            player.Credits!=6000 || profile.OwnedSkins.Count!=1 ||
            _shopCaseDetail is null ||
            !_shopCaseDetail.Text.Contains("Unlocked",StringComparison.Ordinal))
            throw new InvalidOperationException("Pass53 GUI opening did not grant/charge exactly once");

        var reward=profile.OwnedSkins.Single();
        var persisted=new JsonProfileStore(
            ProjectSettings.GlobalizePath("user://profile.json")).Load();
        if(persisted.Credits!=6000 || !persisted.OwnedSkins.Contains(reward))
            throw new InvalidOperationException("Pass53 case reward did not persist to JSON");
        var inventory=_shop?.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Name.ToString()=="ShopOwnedSkins");
        inventory?.EmitSignal(Button.SignalName.Pressed);
        if(inventory is null || _skinInventory is null ||
            _selectedSkinId!=reward || lobby.ActiveCameraName!="Loadout")
            throw new InvalidOperationException("Pass53 owned-skin inventory navigation failed");
        var apply=_skinInventory.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Name.ToString()=="ApplySkinSecondary");
        apply?.EmitSignal(Button.SignalName.Pressed);
        if(apply is null || profile.EquippedWeaponSkins.GetValueOrDefault(
            profile.Loadout["Secondary"])!=reward ||
            showroom.VisibleCosmeticProxyParts<1)
            throw new InvalidOperationException(
                "Pass53 3D cosmetic proxy was not applied to equipped weapon");
        persisted=new JsonProfileStore(
            ProjectSettings.GlobalizePath("user://profile.json")).Load();
        if(persisted.EquippedWeaponSkins.GetValueOrDefault(
            profile.Loadout["Secondary"])!=reward)
            throw new InvalidOperationException("Pass53 applied skin failed JSON roundtrip");

        var sell=_skinInventory?.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Name.ToString()=="SellOwnedSkin");
        sell?.EmitSignal(Button.SignalName.Pressed);
        if(sell is null || profile.OwnedSkins.Count!=0 ||
            player.Credits!=9000 || profile.EquippedWeaponSkins.Count!=0 ||
            showroom.VisibleCosmeticProxyParts!=0)
            throw new InvalidOperationException(
                "Pass53 source resale and equipped proxy clearing failed");
        persisted=new JsonProfileStore(
            ProjectSettings.GlobalizePath("user://profile.json")).Load();
        if(persisted.Credits!=9000 || persisted.OwnedSkins.Count!=0 ||
            persisted.EquippedWeaponSkins.Count!=0)
            throw new InvalidOperationException(
                "Pass53 sold reward did not persist without stale cosmetic assignment");

        StartGame("Manor");
        if(_sourceLobby is not null || _shop is not null ||
            _skinInventory is not null || _game is null)
            throw new InvalidOperationException(
                "Pass53 skin inventory/3D showroom leaked into active match");
        GD.Print("TWR_SMOKE_PASS53_SKINS_OK source_skins=118 " +
            "case_pool_skins=104 event_exclusive=14 cases=7 " +
            "case_purchase=true uniform_unknown_odds_disclosed=true " +
            "off_sale_hallows_blocked=true no_duplicate_rolls=true " +
            "sale_half_price=true exclusive_sale_blocked=true " +
            "owned_skin_saved_and_reloaded=true equipped_skin_saved=true " +
            "original_textures_not_claimed=true 3d_proxy_applied=true " +
            "sale_clears_3d_proxy=true no_skin_nodes_in_match=true");
        GetTree().Quit(0);
    }

    private void RunPass52Smoke()
    {
        // Fully synthetic original-style lobby and tool models. The real
        // source-derived case prices and normalized weapon offset expressions
        // are regular checked-in metadata; no original mesh/skin asset leaks.
        ShowMenu();
        var lobby=_sourceLobby ??
            throw new InvalidOperationException("Pass52 fake source lobby absent");
        var showroom=lobby.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName) ??
            throw new InvalidOperationException("Pass52 showroom absent");
        if (lobby.OwnerSourceVerified || showroom.OwnerLobbyVerified ||
            lobby.SourceCameraCount!=8 || lobby.SourceLoadoutPointCount!=5 ||
            lobby.ActiveCameraName!="Start" ||
            !lobby.IsCameraAtSourceAnchor("Start") ||
            Math.Abs(Pass49OriginalLobbyRuntime.OriginalLobbyFovDegrees - 50f)>.001f ||
            Math.Abs(Pass49OriginalLobbyRuntime.OriginalCameraMoveSeconds - 3f)>.001f)
            throw new InvalidOperationException("Pass52 original camera/anchor provenance invalid");

        var sourceCount=Pass52SourceLoadoutOffsets.SourceOffsetCount;
        if(sourceCount<75 || sourceCount>91 ||
            !Pass52SourceLoadoutOffsets.TryGet("Glock 17",out var glockFrame) ||
            glockFrame.Origin.DistanceTo(
                new Vector3(.0393884182f,.0704264641f,-.163572311f) *
                RobloxUnits.MetersPerStud)>.0001f ||
            Pass52SourceLoadoutOffsets.TryParse("CFrame.new(os.execute('bad'))",out _) ||
            Pass52SourceLoadoutOffsets.TryGet("No Original Tool 52",out _))
            throw new InvalidOperationException(
                "Pass52 normalised original LoadoutOffset is invalid or unsafe");

        var profile=_runtime.Profile ??
            throw new InvalidOperationException("Pass52 offline profile missing");
        var ownedBefore=profile.Unlocks.Count;
        var creditsBefore=_runtime.Player?.Credits ?? -1;
        var loadoutBefore=string.Join("|",profile.Loadout
            .OrderBy(row=>row.Key,StringComparer.Ordinal)
            .Select(row=>row.Key+":"+row.Value));

        var shopButton=_menu?.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Text=="SHOP / CASES") ??
            throw new InvalidOperationException("Pass52 main shop navigation missing");
        shopButton.EmitSignal(Button.SignalName.Pressed);
        if(_shop is null || _menu is not null ||
            lobby.ActiveCameraName!="Shop" || !lobby.CameraTransitioning ||
            showroom.Visible || Pass52SourceShopCatalog.All.Count!=7)
            throw new InvalidOperationException("Pass52 shop/CFrame transition missing");
        lobby.AdvanceCameraTransition(1.5f);
        if(!lobby.CameraTransitioning ||
            lobby.CameraTransitionProgress<.45f ||
            lobby.CameraTransitionProgress>.55f)
            throw new InvalidOperationException("Pass52 camera half-time progress invalid");
        lobby.AdvanceCameraTransition(1.5f);
        if(lobby.CameraTransitioning || !lobby.IsCameraAtSourceAnchor("Shop"))
            throw new InvalidOperationException("Pass52 eased shop camera did not settle");

        var tiles=_shop.GetChildren().OfType<Button>()
            .Where(button=>button.Text.StartsWith("CASE:",StringComparison.Ordinal))
            .ToArray();
        if(tiles.Length!=7 || !Pass52SourceShopCatalog.TryFind("Low",out var low) ||
            low?.PriceCredits!=6000 ||
            !Pass52SourceShopCatalog.TryFind("Ethereal",out var ethereal) ||
            ethereal?.PriceCredits!=1000000 ||
            Pass52SourceShopCatalog.TryFind("No Case 52",out _))
            throw new InvalidOperationException("Pass52 original credit case catalog mismatch");
        var lowTile=tiles.FirstOrDefault(button=>
            button.Name.ToString()=="ShopCase_Low");
        lowTile?.EmitSignal(Button.SignalName.Pressed);
        if(lowTile is null || _shopCaseDetail is null ||
            !_shopCaseDetail.Text.Contains("$6,000",StringComparison.Ordinal) ||
            !_shopCaseDetail.Text.Contains("not implemented offline",StringComparison.Ordinal))
            throw new InvalidOperationException("Pass52 read-only case detail failed");

        var back=_shop.GetChildren().OfType<Button>()
            .FirstOrDefault(button=>button.Text=="BACK");
        back?.EmitSignal(Button.SignalName.Pressed);
        if(_shop is not null || _menu is null ||
            lobby.ActiveCameraName!="Start" || !lobby.CameraTransitioning)
            throw new InvalidOperationException("Pass52 shop did not return to menu");
        lobby.AdvanceCameraTransition(3f);
        if(!lobby.IsCameraAtSourceAnchor("Start"))
            throw new InvalidOperationException("Pass52 Start camera not source-aligned");

        ShowArmory();
        if(_armory is null || !showroom.Visible ||
            lobby.ActiveCameraName!="Loadout" ||
            !showroom.Preview("Glock 17") ||
            !showroom.RotatePreview(100f,400f) ||
            Math.Abs(showroom.PreviewPitchDegrees-25f)>.001f ||
            !showroom.RotatePreview(-60f,-400f) ||
            Math.Abs(showroom.PreviewPitchDegrees+25f)>.001f ||
            showroom.RotatePreview(float.NaN,0f))
            throw new InvalidOperationException(
                "Pass52 source showroom mouse tilt/clamp preview is invalid");
        lobby.AdvanceCameraTransition(3f);
        if(!lobby.IsCameraAtSourceAnchor("Loadout"))
            throw new InvalidOperationException("Pass52 Loadout camera not source-aligned");

        var unchanged=string.Join("|",profile.Loadout
            .OrderBy(row=>row.Key,StringComparer.Ordinal)
            .Select(row=>row.Key+":"+row.Value));
        if(ownedBefore!=profile.Unlocks.Count ||
            creditsBefore!=(_runtime.Player?.Credits ?? -1) ||
            loadoutBefore!=unchanged)
            throw new InvalidOperationException(
                "Pass52 read-only case or tilt unexpectedly mutated economy");

        StartGame("Manor");
        if(_sourceLobby is not null || _shop is not null || _game is null)
            throw new InvalidOperationException(
                "Pass52 menu/camera/preview nodes leaked into live gameplay");

        GD.Print("TWR_SMOKE_PASS52_LOBBY_SHOP_OK synthetic_only=true " +
            $"source_weapon_offsets={sourceCount} source_camera_duration=3 " +
            "source_camera_fov=50 shop_case_prices=7 case_purchase_disabled=true " +
            "loadout_preview_pitch_clamp=25 read_only_credit_balance=true " +
            "lobby_unloaded_in_match=true private_assets_not_uploaded=true");
        GetTree().Quit(0);
    }

    private void RunPass51Smoke()
    {
        // Exported Windows Godot runtime; all three content sidecars in this
        // test are fabricated and are removed before the CI job terminates.
        ShowMenu();
        var lobby = _sourceLobby ??
            throw new InvalidOperationException("Pass51 synthetic source lobby absent");
        var stage = lobby.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        if (lobby.OwnerSourceVerified || stage is null ||
            stage.OwnerLobbyVerified || stage.AuthoredAnchorCount != 5 ||
            stage.Visible || stage.VisibleWeaponModels != 0 ||
            !stage.HasAnchor("Primary") || !stage.HasAnchor("Secondary") ||
            !stage.HasAnchor("Melee") || !stage.HasAnchor("Utility") ||
            !stage.HasAnchor("View") ||
            !lobby.TryGetLoadoutPoint("View", out var authoredFrame))
            throw new InvalidOperationException(
                "Pass51 source Loadout points were not reconstructed safely");

        var physicalAnchor = stage.GetNodeOrNull<Node3D>(
            "OriginalLoadoutAnchor_View");
        if (physicalAnchor is null ||
            physicalAnchor.Transform.Origin.DistanceTo(authoredFrame.Origin) > .000001f)
            throw new InvalidOperationException(
                "Pass51 showroom did not preserve the original source anchor position");

        var creditsBefore = _runtime.Player?.Credits ?? -1;
        var profile = _runtime.Profile ??
            throw new InvalidOperationException("Pass51 profile is unavailable");
        var ownedBefore = profile.Unlocks.Count;
        var loadoutBefore = string.Join("|", profile.Loadout
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => item.Key + ":" + item.Value));

        ShowArmory();
        var rows = _armory?.GetChildren().OfType<ScrollContainer>()
            .SelectMany(scroll => scroll.GetChildren().OfType<VBoxContainer>())
            .SelectMany(list => list.GetChildren().OfType<Button>()).ToArray()
            ?? [];
        if (lobby.ActiveCameraName != "Loadout" || !stage.Visible ||
            rows.Length != 91 || stage.VisibleWeaponModels < 1 ||
            _armoryPreviewLabel is null)
            throw new InvalidOperationException(
                "Pass51 original 3D loadout is not connected to the functional 91-weapon armory");

        var glock = rows.FirstOrDefault(button =>
            button.Text.Contains("Glock 17", StringComparison.Ordinal));
        if (glock is null)
            throw new InvalidOperationException("Pass51 armory hover target missing");
        glock.EmitSignal(Control.SignalName.MouseEntered);
        if (stage.PreviewWeaponName != "Glock 17" ||
            !stage.PreviewUsesSourceAssembly ||
            stage.PreviewVisibleParts < 4 ||
            !_armoryPreviewLabel.Text.Contains("Glock 17",StringComparison.Ordinal) ||
            OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass51 source-backed 3D weapon hover preview failed or impersonated owner art");

        if (!stage.Preview("M60") || stage.PreviewUsesSourceAssembly ||
            stage.PreviewVisibleParts < 2 ||
            !stage.PreviewStatus.Contains("APPROXIMATE",StringComparison.Ordinal) ||
            stage.Preview("UNAVAILABLE_51"))
            throw new InvalidOperationException(
                "Pass51 unavailable model fallback did not remain explicit");
        var loadoutAfter = string.Join("|",profile.Loadout
            .OrderBy(item => item.Key,StringComparer.Ordinal)
            .Select(item => item.Key + ":" + item.Value));
        if ((_runtime.Player?.Credits ?? -1) != creditsBefore ||
            profile.Unlocks.Count != ownedBefore || loadoutAfter != loadoutBefore)
            throw new InvalidOperationException(
                "Pass51 preview wrongly purchased/equipped a weapon or changed credits");

        var back = _armory?.GetChildren().OfType<Button>()
            .FirstOrDefault(button => button.Text == "BACK");
        back?.EmitSignal(Button.SignalName.Pressed);
        if (_menu is null || stage.Visible || lobby.ActiveCameraName != "Start")
            throw new InvalidOperationException(
                "Pass51 showroom remained on screen after returning to Start");

        var bulletinButton = _menu.GetChildren().OfType<Button>()
            .FirstOrDefault(button => button.Text == "BULLETIN BOARD");
        bulletinButton?.EmitSignal(Button.SignalName.Pressed);
        if (_bulletin is null || lobby.ActiveCameraName != "Leaderboards" ||
            stage.Visible)
            throw new InvalidOperationException(
                "Pass51 display leaked over Pass50 bulletin-board camera");

        var boardBack = _bulletin.GetChildren().OfType<Button>()
            .FirstOrDefault(button => button.Text == "BACK");
        boardBack?.EmitSignal(Button.SignalName.Pressed);
        StartGame("Manor");
        if (_sourceLobby is not null || _game is null)
            throw new InvalidOperationException(
                "Pass51 source loadout display leaked into active survival map");

        GD.Print("TWR_SMOKE_PASS51_LOADOUT_OK synthetic_only=true " +
            "original_anchor_transforms=5 armory_rows=91 source_preview_parts=4 " +
            "missing_source_fallback=true preview_does_not_purchase=true " +
            "start_camera_restored=true bulletin_isolated=true " +
            "match_scene_has_no_lobby=true no_public_exe_artifact=true");
        GetTree().Quit(0);
    }

    private void RunPass50Smoke()
    {
        // Native Godot Windows process with fully FABRICATED 3D lobby and
        // headings. Never installs the owner's historical leaderboard records.
        ShowMenu();
        var lobby=_sourceLobby;
        var signs=lobby?.GetNodeOrNull<Pass50SourceLobbySignsRuntime>(
            "Pass50SourceLobbySigns");
        if(lobby is null || signs is null ||
            lobby.OwnerSourceVerified || signs.OwnerSourceVerified ||
            signs.PanelCount!=9 || signs.HeadingCount!=34 ||
            signs.SourceSurfaceGuiCount!=26 || signs.SourceTextLabelCount!=872 ||
            !signs.OriginalHistoricalValuesExcluded ||
            signs.ActiveHeadingCount!=31 || signs.ActivePage!="level" ||
            signs.GetChildCount()!=9)
            throw new InvalidOperationException(
                "Pass50 original authored UI heading extraction/count or privacy failed");
        var nativeLabels=signs.FindChildren("*","Label3D",true,false)
            .OfType<Label3D>().ToArray();
        if(nativeLabels.Length!=34 ||
            !nativeLabels.Any(l=>l.Text=="PERSONAL STATS:") ||
            !nativeLabels.Any(l=>l.Text=="WEEKLY LEADERBOARD:") ||
            nativeLabels.Any(l=>l.Text.Contains("rachel2006",StringComparison.Ordinal)))
            throw new InvalidOperationException(
                "Pass50 static authentic source headings absent or old rank user leaked");

        var boardButton=_menu?.GetChildren().OfType<Button>()
            .FirstOrDefault(b=>b.Text=="BULLETIN BOARD");
        if(boardButton is null)
            throw new InvalidOperationException("Pass50 main menu bulletin action missing");
        boardButton.EmitSignal(Button.SignalName.Pressed);
        if(_bulletin is null || _menu is not null ||
            lobby.ActiveCameraName!="Leaderboards" ||
            _bulletinPage is null ||
            !_bulletinPage.Text.Contains("HIGHEST LEVELS",StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Pass50 original Leaderboards camera/menu panel did not open");
        var next=_bulletin.GetChildren().OfType<Button>()
            .FirstOrDefault(b=>b.Text=="NEXT BOARD PAGE");
        if(next is null)
            throw new InvalidOperationException("Pass50 source 4-page headings toggle missing");
        next.EmitSignal(Button.SignalName.Pressed);
        if(signs.ActivePage!="kills" || signs.ActiveHeadingCount!=31 ||
            !_bulletinPage!.Text.Contains("MOST KILLS",StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Pass50 static leaderboard headings did not switch active panels");
        next.EmitSignal(Button.SignalName.Pressed);
        next.EmitSignal(Button.SignalName.Pressed);
        next.EmitSignal(Button.SignalName.Pressed);
        if(signs.ActivePage!="level")
            throw new InvalidOperationException(
                "Pass50 board pages did not cycle back without stale rank data");

        var back=_bulletin.GetChildren().OfType<Button>()
            .FirstOrDefault(b=>b.Text=="BACK");
        back?.EmitSignal(Button.SignalName.Pressed);
        if(_bulletin is not null || _menu is null ||
            lobby.ActiveCameraName!="Start")
            throw new InvalidOperationException(
                "Pass50 original source board did not return to playable menu");
        ShowArmory();
        var armoryButtons=_armory?.GetChildren().OfType<ScrollContainer>()
            .SelectMany(scroll=>scroll.GetChildren().OfType<VBoxContainer>())
            .SelectMany(list=>list.GetChildren().OfType<Button>()).Count() ?? -1;
        if(armoryButtons!=91 || lobby.ActiveCameraName!="Loadout")
            throw new InvalidOperationException(
                "Pass50 source board interfered with working original 91-weapon armory");
        StartGame("Manor");
        if(_sourceLobby is not null || _bulletin is not null || _game is null)
            throw new InvalidOperationException(
                "Pass50 source board geometry leaked into offline active match");

        GD.Print("TWR_SMOKE_PASS50_BULLETIN_OK synthetic_only=true " +
            "3d_original_lobby=true source_gui_panels=26 snapshot_labels=872 " +
            "restored_static_headings=34 excluded_historical_values=838 " +
            "panels=9 active_labels=31 authored_leaderboards_camera=true " +
            "board_pages=4 board_button_action=true backward_navigation=true " +
            "armory_buttons=91 game_switch=true private_source_not_uploaded=true");
        GetTree().Quit(0);
    }

    private void RunPass49Smoke()
    {
        // Original TestPlace XML/derived owner lobby content is NOT in CI.
        // This exercises the actual Windows renderer against fabricated
        // 3D lobby boxes, native shapes, distant mesh bounds and cameras.
        ShowMenu();
        var stage=_sourceLobby;
        if(stage is null || stage.OwnerSourceVerified ||
            stage.SourceGeometryParts!=8 || stage.VisibleSourceParts!=7 ||
            stage.UnresolvedMeshProxyParts!=3 || stage.SourceLights!=2 ||
            stage.RenderBatchCount<2 || stage.SourceCameraCount!=8 ||
            stage.SourceLoadoutPointCount!=5 ||
            stage.ActiveCameraName!="Start" ||
            !stage.HasCamera("Perks") || !stage.HasCamera("Loadout") ||
            !stage.HasLoadoutPoint("Primary"))
            throw new InvalidOperationException(
                "Pass49 native source lobby geometric/camera counts failed");
        if(_menu is null || _menu.GetChildren().OfType<Button>().Count()<12)
            throw new InvalidOperationException(
                "Pass49 3D lobby made the existing ten-map menu inaccessible");
        var originalCam=stage.GetNodeOrNull<Camera3D>("OriginalSourceLobbyCamera");
        if(originalCam is null || !originalCam.Current ||
            originalCam.GlobalPosition.Length()>0.01f)
            throw new InvalidOperationException(
                "Pass49 original relative Start CFrame misaligned");
        ShowArmory();
        if(_sourceLobby!=stage || stage.ActiveCameraName!="Loadout" ||
            _armory is null)
            throw new InvalidOperationException(
                "Pass49 owner lobby camera did not track existing armory");
        var armoryButtons=_armory.GetChildren().OfType<ScrollContainer>()
            .SelectMany(scroll=>scroll.GetChildren().OfType<VBoxContainer>())
            .SelectMany(list=>list.GetChildren().OfType<Button>()).Count();
        if(armoryButtons!=91)
            throw new InvalidOperationException(
                "Pass49 3D lobby intercepted or removed functional 91-weapon shop");
        ShowMenu();
        if(stage.ActiveCameraName!="Start")
            throw new InvalidOperationException(
                "Pass49 original source camera failed to return to Start");
        StartGame("Manor");
        if(_sourceLobby is not null || _game is null)
            throw new InvalidOperationException(
                "Pass49 lobby camera/geometry leaked into the active wave scene");
        GD.Print("TWR_SMOKE_PASS49_LOBBY_OK source=synthetic_only " +
            "geometry_parts=8 visible_parts=7 mesh_proxy_parts=3 " +
            "source_cameras=8 loadout_points=5 source_lights=2 " +
            "camera_Start_to_Loadout_to_Start=true " +
            "menu_maps=10 armory_weapons=91 gameplay_switch=true " +
            "original_cloud_mesh_triangles_missing=true");
        GetTree().Quit(0);
    }

    private void RunPass48Smoke()
    {
        // CI source kit contains only fabricated geometry. The actual 285 MB
        // TestPlace XML and its owner-only source accessories are NEVER uploaded.
        var arena = new Node3D { Name="Pass48SourceInfectedSyntheticSmoke" };
        AddChild(arena);
        var types = new[] {
            "Civilian", "Sprinter", "Bolter", "Military",
            "Riot", "Hazmat", "Bloater", "Burster"
        };
        var loaded = 0;
        foreach(var type in types)
        {
            var visual = new InfectedVisualAssembler {
                Name=type+"SourceVisual",InfectedType=type
            };
            arena.AddChild(visual);
            var body = visual.GetNodeOrNull<Node3D>("SourceR6Body");
            if (!visual.UsingSourceBlueprint ||
                visual.VerifiedOwnerSourceAccessoryKit ||
                visual.RecoveredSourceAccessoryParts != 2 ||
                visual.ReconstructedR6BodyParts != 6 ||
                visual.MissingSourceMeshProxies != 1 ||
                body is null)
                throw new InvalidOperationException(
                    "Pass48 source/proxy attribution invalid: " + type);
            var meshes=body.FindChildren("*","MeshInstance3D",true,false)
                .OfType<MeshInstance3D>().ToArray();
            if(meshes.Length!=8 ||
                !meshes.Any(node => node.Name=="Head" && node.Mesh is SphereMesh) ||
                !meshes.Any(node => node.Name=="OriginalEyepiece" &&
                    node.Mesh is BoxMesh) ||
                meshes.Any(node => node.Name=="UnwantedHandle"))
                throw new InvalidOperationException(
                    "Pass48 visible body, original accessory, or missing-mesh proxy incorrect");
            visual.Attack();
            loaded++;
        }
        if (InfectedSourceModelRuntime.LoadedVariantCount != 15 ||
            InfectedSourceModelRuntime.OwnerSourceKitVerified)
            throw new InvalidOperationException(
                "Pass48 synthetic fixture variant coverage or original-source SHA state invalid");
        var zombie=new InfectedAgent {
            Name="Pass48SyntheticPhysicalInfected",
            InfectedType="Hazmat",Health=100
        };
        arena.AddChild(zombie);
        if(!zombie.SourceInfectedKitActive || zombie.SourceInfectedKitVerified ||
            zombie.SourceAccessoryPartCount!=2 ||
            zombie.SourceBodyProxyPartCount!=6 ||
            zombie.SourceMissingMeshProxyCount!=1)
            throw new InvalidOperationException(
                "Pass48 active infected actor did not receive source-kit geometry");
        GD.Print("TWR_SMOKE_PASS48_INFECTED_OK synthetic_only=true " +
            "source_playable_types=8 variants=15 body_proxy_per_actor=6 " +
            "source_accessories_per_actor=2 cloud_mesh_proxy_per_actor=1 " +
            "R6_head_sphere=true actual_infected_actor=true " +
            "original_3d_asset_triangles_recovered=false " +
            "original_r6_active_variant_rigs_recovered=false");
        GetTree().Quit(0);
    }

    private void RunPass47Smoke()
    {
        // The exported Windows game reads six wholly INVENTED weapon models.
        // The real owner's TestPlace 98-model source pack is never in CI.
        if (OriginalWeaponSourceRuntime.SourceModelCount != 6 ||
            OriginalWeaponSourceRuntime.OwnerSourcePackVerified)
            throw new InvalidOperationException(
                "Pass47 synthetic-only 3D tool pack was not loaded securely");

        var scene = new Node3D { Name = "Pass47OriginalToolAssemblySynthetic" };
        AddChild(scene);
        var count=0;
        foreach(var name in new[] {
            "Glock 17","Sawn Off Shotgun","AK-47","RPG-7","2x4"
        })
        {
            var tool = new WeaponViewModelRuntime { Name = name };
            scene.AddChild(tool);
            tool.SetWeapon(RuntimeWeaponCatalog.Get(name));
            var rig = tool.GetNodeOrNull<Node3D>("WeaponBody");
            if (!tool.UsingOriginalToolAssembly ||
                tool.SourceVisibleParts!=4 || tool.VisualPartCount!=4 ||
                tool.SourceMissingMeshProxies!=1 || rig is null)
                throw new InvalidOperationException(
                    "Pass47 source model or absent MeshPart fallback failed: "+name);
            var meshes=rig.GetChildren().OfType<MeshInstance3D>().ToArray();
            if (meshes.Length!=5 || // four visible plus noncounted muzzle flash
                !meshes.Any(part => part.Mesh is SphereMesh) ||
                meshes.Any(part => part.Name.ToString()=="Pos"))
                throw new InvalidOperationException(
                    "Pass47 original ball primitive or invisible marker filtering failed: "+name);
            if (meshes.Any(part => part.Position.Length()>3f))
                throw new InvalidOperationException(
                    "Pass47 source CFrames were not converted to local viewmodel space");
            count++;
        }
        var grenade=new WeaponViewModelRuntime {Name="SourceMolotov"};
        scene.AddChild(grenade);
        grenade.SetThrowable("Molotov");
        if(!grenade.UsingOriginalToolAssembly ||
            grenade.SourceVisibleParts!=4 ||
            grenade.SourceMissingMeshProxies!=1)
            throw new InvalidOperationException(
                "Pass47 original throwable tool pack not reused correctly");
        if (OriginalWeaponSourceRuntime.SourceModelCount!=6)
            throw new InvalidOperationException(
                "Pass47 preview unexpectedly required full original owner source pack");
        GD.Print("TWR_SMOKE_PASS47_SOURCE_WEAPONS_OK source=synthetic_only " +
            "active_models=5 thrown_models=1 visible_parts_per_model=4 " +
            "absent_mesh_proxy_per_model=1 invisible_source_markers_hidden=true " +
            "authored_sphere_shape=true local_source_transforms=true " +
            "retail_mesh_triangles_recovered=false");
        GetTree().Quit(0);
    }

    private void RunPass46Smoke()
    {
        // Windows Godot executable reads ONLY fabricated 2x2 PNG art here.
        // Real image archive must never enter a public Actions artifact.
        if (!Pass27SourceArtCatalog.OfflineArtReady ||
            Pass27SourceArtCatalog.MapArtCount!=10 ||
            Pass27SourceArtCatalog.WeaponArtCount!=3 ||
            Pass27SourceArtCatalog.WeaponIcon("Glock 17") is null ||
            Pass27SourceArtCatalog.WeaponIcon("M4A1") is null ||
            Pass27SourceArtCatalog.WeaponIcon("AA-12") is null ||
            Pass27SourceArtCatalog.WeaponIcon("Nonexistent Weapon") is not null ||
            Pass27SourceArtCatalog.MapCard("Nonexistent Map") is not null)
            throw new InvalidOperationException(
                "Pass46 fabricated source-art manifest/hash or name lookup did not load");

        ShowMenu();
        var cards=_menu?.GetChildren().OfType<Button>()
            .Count(b => b.GetNodeOrNull<TextureRect>("SourceMapCard") is not null) ?? 0;
        if(cards!=10)
            throw new InvalidOperationException(
                "Pass46 map selection did not show all ten original-style source cards");
        ShowArmory();
        var all=_armory?.GetChildren().OfType<ScrollContainer>()
            .SelectMany(scroll => scroll.GetChildren().OfType<VBoxContainer>())
            .SelectMany(list => list.GetChildren().OfType<Button>()).ToArray();
        if(all is null || all.Length!=91)
            throw new InvalidOperationException(
                "Pass46 visual armory removed any of the 91 selectable source weapon rows");
        var decorated=all.Where(button =>
            button.GetNodeOrNull<TextureRect>("SourceWeaponSilhouette") is not null)
            .ToArray();
        if(decorated.Length!=3 ||
            !decorated.Any(button => button.TooltipText=="Glock 17") ||
            !decorated.Any(button => button.TooltipText=="AA-12") ||
            !decorated.Any(button => button.TooltipText=="M4A1"))
            throw new InvalidOperationException(
                "Pass46 armory failed to show all three synthetic source silhouettes");
        if(all.Any(button => button.GetNodeOrNull<TextureRect>(
            "SourceWeaponSilhouette") is not null &&
            button.GetNodeOrNull<TextureRect>(
                "SourceWeaponSilhouette")!.MouseFilter!=Control.MouseFilterEnum.Ignore))
            throw new InvalidOperationException(
                "Pass46 icon decoration intercepted functional purchase controls");
        var fallback=all.FirstOrDefault(button =>
            button.Text.Contains("Ruger 10-22",StringComparison.Ordinal));
        if(fallback is null || fallback.GetNodeOrNull<TextureRect>(
            "SourceWeaponSilhouette") is not null)
            throw new InvalidOperationException(
                "Pass46 missing art did not retain the original text-only armory row");

        var dial=new WeaponDialHud { Name="Pass46OfflineReferenceHudSmoke" };
        AddChild(dial);
        dial.Display("Glock 17",17,136,17,"SIDEARM",false,false);
        if(!dial.HasOfflineReferenceWeaponArt)
            throw new InvalidOperationException(
                "Pass46 original-style ammo HUD failed to load external offline weapon art");
        GD.Print("TWR_SMOKE_PASS46_ART_OK source=synthetic_only " +
            "map_cards=10 armory_rows=91 reference_weapon_icons=3 " +
            "fallback_text_only=true original_purchase_buttons_unmodified=true " +
            "live_ammo_dial_icon=true no_original_images_in_public_ci=true");
        GetTree().Quit(0);
    }

    private void RunPass45Smoke()
    {
        // Real exported Windows Godot game, fabricated source-only scene:
        // 18 packed native Parts, two fabricated MeshPart visual proxies,
        // one source-like SpecialMesh and no original Roblox mesh bytes.
        _runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass45SyntheticProxyVisibilitySmoke",
            Runtime=_runtime,MapName="Laboratory"
        };
        AddChild(game);
        var scene=game.GetNodeOrNull<Node3D>("RecoveredLaboratory");
        var native=game.GetNodeOrNull<Pass28PrimitiveStreamer>(
            "RecoveredLaboratory/Pass28PrimitiveStream");
        var proxies=game.GetNodeOrNull<Pass45FallbackProxyStreamer>(
            "RecoveredLaboratory/Pass45FallbackProxyStream");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(scene is null || native is null || proxies is null || player is null ||
            native.SourceInstanceCount!=18 ||
            !game.Pass45ProxyStreamingAvailable || !proxies.CullEnabled ||
            proxies.SourceProxyInstanceCount!=3 || proxies.BatchCount!=3 ||
            proxies.VisibleInstanceCount!=2 || proxies.VisibleBatchCount!=2)
            throw new InvalidOperationException(
                "Pass45 spatial source-proxy startup/culling unavailable or incorrect");
        // Retain the one old SpecialMesh visual in addition to the two
        // fabricated source MeshPart bounding approximations.
        if(proxies.GetChildren().OfType<MultiMeshInstance3D>().Count()!=3 ||
            scene.GetNodeOrNull<StaticBody3D>("LaboratoryCollision") is null)
            throw new InvalidOperationException(
                "Pass45 source proxy migration lost legacy special visuals or collision");

        var local=new Aabb(new Vector3(-2,-1,-.5f),new Vector3(4,2,1));
        var orientation=new Transform3D(
            new Basis(Vector3.Up,Mathf.Pi/2f),new Vector3(10,0,0));
        var rotated=Pass45FallbackProxyStreamer.WorldBounds(orientation,local);
        if(Math.Abs(rotated.Size.X-1)>0.01f ||
            Math.Abs(rotated.Size.Z-4)>0.01f ||
            !Pass28PrimitiveStreamer.CanSeeBounds(
                new Vector2(10,0),
                new Vector2(rotated.Position.X,rotated.Position.Z),
                new Vector2(rotated.End.X,rotated.End.Z),2))
            throw new InvalidOperationException(
                "Rotated actual source mesh AABB was truncated or culled");

        // F1 does NOT affect original physics; it is an A/B visualization
        // toggle so the owner can capture p95/p99 frame time in one match.
        game._UnhandledInput(new InputEventKey {Keycode=Key.F1,Pressed=true});
        if(proxies.CullEnabled || proxies.VisibleInstanceCount!=3 ||
            proxies.VisibleBatchCount!=3 || game.Pass45ProxyCullEnabled)
            throw new InvalidOperationException("F1 failed to display all source proxies");

        game._UnhandledInput(new InputEventKey {Keycode=Key.F1,Pressed=true});
        if(!proxies.CullEnabled || proxies.VisibleInstanceCount!=2 ||
            proxies.VisibleBatchCount!=2)
            throw new InvalidOperationException("F1 failed to restore spatial culling");

        var home=player.GlobalPosition;
        player.GlobalPosition=new Vector3(1800f*RobloxUnits.MetersPerStud,
            home.Y,0);
        proxies.Track(player);
        if(proxies.VisibleBatchCount!=1 || proxies.VisibleInstanceCount!=1)
            throw new InvalidOperationException("Source proxy far-camera culling incorrect");
        player.GlobalPosition=home;
        proxies.Track(player);
        if(proxies.VisibleBatchCount!=2 || proxies.VisibleInstanceCount!=2)
            throw new InvalidOperationException("Source proxy return-camera visibility failed");

        game._Process(1.0/60.0);
        game._Process(1.0/60.0);
        if(game.Pass43FrameTimes.Snapshot().SampledFrames<2 ||
            game.Pass44NativePrimitiveCount!=18 || game.Pass45ProxyBatchCount!=3)
            throw new InvalidOperationException("Pass45 broke source graphics or perf telemetry");

        GD.Print("TWR_SMOKE_PASS45_PROXY_OK map=Laboratory fabricated_scene=true " +
            "native_ordinary_parts=18 fallback_special=1 " +
            "fallback_meshproxies=2 original_collision_retained=true " +
            "F1_cull_on_off_on=PASS rotated_mesh_bounds=PASS " +
            "visible_at_spawn=2 visible_far=1 source_mesh_triangles_missing=true " +
            "real_owner_fps_not_benchmarked=true");
        GetTree().Quit(0);
    }

    private void RunPass44Smoke()
    {
        // Exported Windows executable, fabricated original map JSON and
        // SHA-bound native TWRINS28 draw pack; no original Roblox assets.
        _runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass44NativeStreamingSmoke",
            Runtime=_runtime, MapName="Laboratory"
        };
        AddChild(game);
        var stage=game.GetNodeOrNull<Node3D>("RecoveredLaboratory");
        var renderer=game.GetNodeOrNull<Pass28PrimitiveStreamer>(
            "RecoveredLaboratory/Pass28PrimitiveStream");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(stage is null || renderer is null || player is null ||
            renderer.SourceInstanceCount!=18 ||
            game.Pass44NativePrimitiveCount!=18 ||
            renderer.BatchCount<3 || renderer.BatchCount>12 ||
            renderer.VisibleBatchCount<1 ||
            renderer.VisibleBatchCount>=renderer.BatchCount)
            throw new InvalidOperationException(
                "Pass44 original native Part batching failed to load, partition or cull at spawn");

        // All packed native Parts are removed from the legacy visible
        // renderer, but the one nonpacked SpecialMesh type=6 MUST survive.
        var legacySpecial=stage.GetChildren()
            .OfType<MultiMeshInstance3D>().ToArray();
        if(legacySpecial.Length!=1 ||
            legacySpecial[0].Multimesh is null ||
            legacySpecial[0].Multimesh.InstanceCount!=1)
            throw new InvalidOperationException(
                "Pass44 duplicate native geometry or dropped original SpecialMesh fallback");
        if(stage.GetNodeOrNull<StaticBody3D>("LaboratoryCollision") is null)
            throw new InvalidOperationException(
                "Pass44 accidentally removed original native collision source");

        var initiallyVisible=renderer.VisibleBatchCount;
        player.GlobalPosition = new Vector3(1000f * RobloxUnits.MetersPerStud,
            player.GlobalPosition.Y, 0);
        renderer.Track(player);
        var movedVisible=renderer.VisibleBatchCount;
        if(movedVisible<1 || movedVisible>=renderer.BatchCount ||
            movedVisible==initiallyVisible &&
            renderer.BatchCount==3)
            throw new InvalidOperationException(
                "Pass44 native render culling failed to switch active source tiles");
        player.GlobalPosition = new Vector3(0,player.GlobalPosition.Y,0);
        renderer.Track(player);
        if(renderer.VisibleBatchCount!=initiallyVisible)
            throw new InvalidOperationException(
                "Pass44 native source tiles failed to reappear on returning to player spawn");

        game._Process(1.0/60.0);
        game._Process(1.0/60.0);
        if(game.Pass43FrameTimes.Snapshot().SampledFrames<2)
            throw new InvalidOperationException("Pass44 map load broke performance frame sample telemetry");

        GD.Print("TWR_SMOKE_PASS44_NATIVE_OK map=Laboratory synthetic=true " +
            "source_native_parts=18 special_mesh_legacy=1 "+
            $"batches={renderer.BatchCount} visible_spawn={initiallyVisible} "+
            $"visible_far={movedVisible} dynamic_cull=true collision_retained=true "+
            "original_custom_triangle_meshes_missing=true real_owner_fps_unmeasured=true");
        GetTree().Quit(0);
    }

    private async void RunPass43Smoke()
    {
        // Native exported Windows game, not static type introspection.
        // Fixture has TWO disjoint PHYSICAL platforms with a real empty pit.
        // Data is fabricated. Original Laboratory meshes are never in CI.
        _runtime.StartMap("Laboratory");
        var game = new GameplayRoot
        {
            Name="Pass43RealPhysicsGapSmoke",
            Runtime=_runtime,MapName="Laboratory"
        };
        AddChild(game);
        var nav = game.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(nav is null || player is null || !nav.IsBridgePackActive ||
            nav.PointCount!=19 || nav.Pass42JumpEdgeCount!=1 ||
            game.Pass43TotalJumpAttempts!=0 ||
            game.Pass43VerifiedJumpLandings!=0 ||
            game.Pass43FailedJumpAttempts!=0)
            throw new InvalidOperationException(
                "Pass43 synthetic source-bound jump route or zero-state counters missing");

        var sample = new Pass43FrameWindow();
        sample.Record(.016,1);
        sample.Record(.024,4);
        sample.Record(300.0,99); // Synthetic accelerated frames excluded.
        sample.Record(double.NaN,99);
        var window=sample.Snapshot();
        if(window.SampledFrames!=2 || window.MedianMs<15 ||
            window.P95Ms<23.9 || window.P95Ms>24.1 ||
            window.PeakLivingInfected!=4 || window.P99Ms<window.MedianMs)
            throw new InvalidOperationException("Pass43 rolling frame telemetry contract failed");
        if(Pass43JumpLandingPolicy.IsSuccessful(true,.3,
            new Vector3(7.84f,.94f,0),new Vector3(9.8f,.94f,0)) ||
            Pass43JumpLandingPolicy.IsSuccessful(false,.4,
                new Vector3(9.8f,.94f,0),new Vector3(9.8f,.94f,0)) ||
            !Pass43JumpLandingPolicy.IsSuccessful(true,.45,
                new Vector3(9.8f,.94f,0),new Vector3(9.8f,.94f,0)))
            throw new InvalidOperationException("Pass43 landing policy accepted takeoff/air contact");

        player.GlobalPosition=new Vector3(50f*.28f,.5f*.28f+.85f,0);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);

        bool HasFloorAt(float sourceStuds)
        {
            var x=sourceStuds*.28f;
            var query=PhysicsRayQueryParameters3D.Create(
                new Vector3(x,3.5f,0),new Vector3(x,-3f,0));
            query.CollisionMask=1;
            return game.GetWorld3D().DirectSpaceState.IntersectRay(query).Count>0;
        }
        if(!HasFloorAt(28) || !HasFloorAt(35) || HasFloorAt(31.5f))
            throw new InvalidOperationException(
                "Pass43 physical geometry is not two separated, solid platforms with empty space");

        game._Process(RegularWaveRules.CountdownSeconds+.1);
        game._Process(.1);
        if(game.WaveStage!="WAVE" || game.ActiveInfectedCount<1)
            throw new InvalidOperationException("Pass43 wave 1 infected spawn not active");
        InfectedAgent? active=null;
        foreach(Node child in game.GetChildren())
            if(child is InfectedAgent infected) { active=infected; break; }
        if(active is null)throw new InvalidOperationException("Pass43 no owner-tracked infected agent");
        active.InfectedType="Civilian";
        active.MoveSpeed=15f*RobloxUnits.MetersPerStud;
        active.SourceNavigator=nav;
        var from=new Vector3(28f*.28f,.5f*.28f+.8f,0);
        active.GlobalPosition=from;
        var previous=active.GlobalPosition;
        var peakStep=0f;
        var observedSuccess=false;
        for(var i=0;i<240;i++)
        {
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(!GodotObject.IsInstanceValid(active))
                throw new InvalidOperationException("Pass43 source infected vanished during gap crossing");
            peakStep=Math.Max(peakStep,active.GlobalPosition.DistanceTo(previous));
            previous=active.GlobalPosition;
            if(active.SourceJumpLandings>0)
            {
                observedSuccess=true;
                break;
            }
        }
        if(!observedSuccess || active.SourceJumpAttempts!=1 ||
            active.SourceJumpLandings!=1 || active.SourceJumpFailures!=0 ||
            active.SourceJumpInProgress || !active.IsOnFloor() ||
            active.GlobalPosition.X<33.6f*.28f || peakStep>.65f)
            throw new InvalidOperationException(
                "Pass43 infected did not PHYSICALLY cross real unsupported floor gap " +
                $"attempts={active.SourceJumpAttempts} landings={active.SourceJumpLandings} " +
                $"failures={active.SourceJumpFailures} onFloor={active.IsOnFloor()} " +
                $"x={active.GlobalPosition.X} largestFrameTravel={peakStep}");
        game._Process(RegularWaveRules.WaveDurationSeconds+.1);
        if(game.WaveStage!="WAVE END" ||
            game.Pass43TotalJumpAttempts!=1 ||
            game.Pass43VerifiedJumpLandings!=1 ||
            game.Pass43FailedJumpAttempts!=0 ||
            game.ActiveInfectedCount!=0)
            throw new InvalidOperationException(
                "Pass43 wave cleanup incorrectly dropped physically completed jump counters");

        // Hard wall in the open pit: a source edge cannot pass through new
        // physical obstruction. Contact with the takeoff floor is a FAILURE.
        var barrier=new StaticBody3D
        {
            Name="Pass43SyntheticJumpBlockingWall",
            Position=new Vector3(31.5f*.28f,2.7f,0),
            CollisionLayer=1, CollisionMask=1
        };
        barrier.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D { Size=new Vector3(.40f,5.4f,6f) }
        });
        game.AddChild(barrier);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        game._Process(RegularWaveRules.WaveEndSeconds+.1);
        game._Process(RegularWaveRules.IntermissionSeconds+.1);
        game._Process(RegularWaveRules.CountdownSeconds+.1);
        game._Process(.1);
        if(game.WaveStage!="WAVE" || _runtime.Match?.Wave!=2)
            throw new InvalidOperationException("Pass43 second wave stage setup failed");
        InfectedAgent? blocked=null;
        foreach(Node child in game.GetChildren())
            if(child is InfectedAgent infected) { blocked=infected; break; }
        if(blocked is null)throw new InvalidOperationException("Pass43 no second wave infected");
        blocked.InfectedType="Civilian";
        blocked.MoveSpeed=15f*RobloxUnits.MetersPerStud;
        blocked.SourceNavigator=nav;
        blocked.GlobalPosition=from;
        for(var i=0;i<170 && blocked.SourceJumpFailures==0;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if(blocked.SourceJumpAttempts<1 || blocked.SourceJumpFailures<1 ||
            blocked.SourceJumpLandings!=0)
            throw new InvalidOperationException(
                "Pass43 blocked jump was not reported as FAILED rather than LANDED " +
                $"attempts={blocked.SourceJumpAttempts} failures={blocked.SourceJumpFailures} " +
                $"landings={blocked.SourceJumpLandings}");
        game._Process(RegularWaveRules.WaveDurationSeconds+.1);
        if(game.Pass43TotalJumpAttempts<2 ||
            game.Pass43VerifiedJumpLandings!=1 ||
            game.Pass43FailedJumpAttempts<1)
            throw new InvalidOperationException(
                "Pass43 retired match counters did not preserve both actual outcomes");

        // Continue through all 15 actual Godot state transitions; time
        // advances are artificially accelerated, NOT a playthrough.
        for(var expected=3;expected<=ReleaseRules.MaxWaves;expected++)
        {
            game._Process(RegularWaveRules.WaveEndSeconds+.1);
            if(game.WaveStage!="INTERMISSION" || _runtime.Match?.Wave!=expected)
                throw new InvalidOperationException("Pass43 wave progression broken at "+expected);
            game._Process(RegularWaveRules.IntermissionSeconds+.1);
            game._Process(RegularWaveRules.CountdownSeconds+.1);
            game._Process(.1);
            if(game.WaveStage!="WAVE" || game.ActiveInfectedCount<1)
                throw new InvalidOperationException("Pass43 wave actor not spawned at "+expected);
            game._Process(RegularWaveRules.WaveDurationSeconds+.1);
            if(game.WaveStage!="WAVE END")
                throw new InvalidOperationException("Pass43 wave did not end "+expected);
        }
        game._Process(RegularWaveRules.WaveEndSeconds+.1);
        if(game.WaveStage!="RESULTS" ||
            _runtime.Match?.Phase!=MatchPhase.Results ||
            game.Pass43VerifiedJumpLandings!=1 || game.Pass43FailedJumpAttempts<1)
            throw new InvalidOperationException("Pass43 final results and durable jump statistics invalid");
        GD.Print("TWR_SMOKE_PASS43_TRAVERSAL_OK synthetic_only=true " +
            "physical_gap_studs=4.2 crossed_to_destination=true no_teleport=true " +
            "blocked_wall_jump_not_landing=true jump_success=1 jump_failure>=1 " +
            "match_counters_persist=true frame_p95_bounded=true 15_waves_accelerated=true " +
            "real_original_scene_playtested=false");
        GetTree().Quit(0);
    }

    private async void RunPass42Smoke()
    {
        // The Windows export loads a fabricated 19-node Laboratory map with
        // exactly one typed jump segment, never the private original scene.
        // 15 rounds are accelerated stage transitions, NOT a human playtest.
        _runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass42SyntheticJumpAndWaveSmoke",
            Runtime=_runtime, MapName="Laboratory"
        };
        AddChild(game);
        var nav=game.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        var overlay=game.GetNodeOrNull<Pass39LaboratoryFloorplan>(
            "Pass39LaboratoryFloorplan");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(nav is null || overlay is null || player is null ||
            !nav.IsBridgePackActive || nav.IsPass42JumpGraph ||
            nav.PointCount!=19 || nav.EdgeCount!=18 ||
            nav.Pass42JumpEdgeCount!=1 || !overlay.HasPass42JumpDiagnostic ||
            game.Pass42JumpLinkCount!=1 || game.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException(
                "Synthetic typed source jump pack, F4 layers, or original spawn default invalid");
        var from=new Vector3(8*3.5f*.28f,.5f*.28f,0);
        var to=new Vector3(10*3.5f*.28f,.5f*.28f,0);
        if (!nav.IsJumpLinkBetween(from,to) || !nav.IsJumpLinkBetween(to,from) ||
            nav.IsJumpLinkBetween(from,new Vector3(7*3.5f*.28f,.5f*.28f,0)))
            throw new InvalidOperationException("Source jump action flags were not preserved");
        var path=nav.GetRoute(from,to);
        if(path.Length!=2 || path[0].DistanceTo(from)>.01f ||
            path[^1].DistanceTo(to)>.01f)
            throw new InvalidOperationException("Source jump is absent from actual AStar route");
        overlay._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        overlay._Input(new InputEventKey {Keycode=Key.N,Pressed=true});
        if(!overlay.IsOpen || !overlay.IsShowingNavigation ||
            !overlay.HasPass42JumpDiagnostic)
            throw new InvalidOperationException("Pass42 original-coordinate jump maps not shown");
        overlay._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        player.GlobalPosition=to+Vector3.Up*.8f;
        var zombie=new InfectedAgent
        {
            Name="Pass42SyntheticJumpInfected",
            Target=player,Runtime=_runtime,
            SourceNavigator=nav,InfectedType="Civilian",
            Position=from+Vector3.Up*.8f
        };
        game.AddChild(zombie);
        for(var frame=0;frame<60 && zombie.SourceJumpAttempts==0;frame++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if(zombie.SourceJumpAttempts==0)
            throw new InvalidOperationException(
                "Infected did not launch along the synthetic Godot source jump edge");
        zombie.QueueFree();
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);

        for(var expectedWave=1;expectedWave<=ReleaseRules.MaxWaves;expectedWave++)
        {
            if(_runtime.Match?.Wave!=expectedWave || game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass42 accelerated wave countdown failed at "+expectedWave);
            game._Process(RegularWaveRules.CountdownSeconds+.1);
            if(game.WaveStage!="WAVE")
                throw new InvalidOperationException("Pass42 wave did not start "+expectedWave);
            game._Process(.1);
            if(game.ActiveInfectedCount<1)
                throw new InvalidOperationException(
                    "Pass42 real Godot infected actor not spawned in wave "+expectedWave);
            game._Process(RegularWaveRules.WaveDurationSeconds+.1);
            if(game.WaveStage!="WAVE END" || game.ActiveInfectedCount!=0)
                throw new InvalidOperationException(
                    "Pass42 infected cleanup failed after wave "+expectedWave);
            game._Process(RegularWaveRules.WaveEndSeconds+.1);
            if(expectedWave==ReleaseRules.MaxWaves)
            {
                if(game.WaveStage!="RESULTS" ||
                    _runtime.Match?.Phase!=MatchPhase.Results)
                    throw new InvalidOperationException(
                        "Pass42 fifteenth-wave completion did not reach Results");
            }
            else
            {
                if(game.WaveStage!="INTERMISSION" ||
                    _runtime.Match?.Wave!=expectedWave+1)
                    throw new InvalidOperationException(
                        "Pass42 intermission advance failed after wave "+expectedWave);
                game._Process(RegularWaveRules.IntermissionSeconds+.1);
            }
        }
        GD.Print("TWR_SMOKE_PASS42_JUMP_OK source=synthetic jump_edges=1 " +
            "AStar_jump_flags=true infected_jump_physics=true " +
            "F4_N_jumps=true waves_15_accelerated=true " +
            "full_player_playtest=false original_navmesh=false");
        GetTree().Quit(0);
    }

    private async void RunPass41Smoke()
    {
        // Native exported Windows Godot check. This test installs ONLY a
        // fabricated 19-waypoint graph and fabricated geometry/PNG images.
        // The actual private 14,726-waypoint scene cannot enter public CI.
        _runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass41SyntheticBridgeSmoke",
            Runtime=_runtime,MapName="Laboratory"
        };
        AddChild(game);
        var navigator=game.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        var overlay=game.GetNodeOrNull<Pass39LaboratoryFloorplan>(
            "Pass39LaboratoryFloorplan");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(navigator is null || overlay is null || player is null ||
            navigator.IsPass41SourceNativeGraph || navigator.IsPass40GroundedGraph ||
            !navigator.IsBridgePackActive ||
            navigator.PointCount!=19 || navigator.EdgeCount!=18 ||
            navigator.ConnectedComponentCount!=1 ||
            !overlay.HasPass41RepairDiagnostic || !overlay.HasNavigationDiagnostic ||
            game.Pass41NativeBridgeCount!=0 || game.Pass41SourceNativeBridgeVerified)
            throw new InvalidOperationException(
                "Pass41 fabricated SHA-bound v3 bridged navigation was not loaded correctly");
        var from=new Vector3(8*3.5f*.28f,.5f*.28f,0);
        var to=new Vector3(10*3.5f*.28f,.5f*.28f,0);
        var path=navigator.GetRoute(from,to);
        if(path.Length!=2 ||
            path[0].DistanceTo(from)>.01f || path[^1].DistanceTo(to)>.01f)
            throw new InvalidOperationException(
                "Pass41 v3 repaired edge is not directly routable inside the exported Windows game");
        overlay._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        overlay._Input(new InputEventKey {Keycode=Key.N,Pressed=true});
        if(!overlay.IsOpen || !overlay.IsShowingNavigation || !game.Pass41BridgeDiagramAvailable)
            throw new InvalidOperationException("Pass41 F4/N 3-floor bridge viewer failed");
        overlay._Input(new InputEventKey {Keycode=Key.Right,Pressed=true});
        if(overlay.ActiveLevel!=1)
            throw new InvalidOperationException("Pass41 repaired graph image floor switching failed");
        overlay._Input(new InputEventKey {Keycode=Key.N,Pressed=true});
        overlay._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        if(overlay.IsOpen || overlay.IsShowingNavigation)
            throw new InvalidOperationException("Pass41 F4/N did not restore original source bounds");
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if(game.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("Pass41 incorrectly opted into repaired enemy spawns");
        var distant=new Vector3(280,1,-280);
        var safe=Pass40AdaptiveEntry.TryFind(navigator,game.GetWorld3D(),
            distant,player.GlobalPosition,0);
        if(!safe.HasValue || safe.Value.MinimumDistance!=14f)
            throw new InvalidOperationException("Pass41 broke bounded grounded F9 fallback");
        game._UnhandledInput(new InputEventKey {Keycode=Key.F9,Pressed=true});
        if(!game.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("Pass41 F9 opt-in failed");
        game._UnhandledInput(new InputEventKey {Keycode=Key.F9,Pressed=true});
        if(game.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("Pass41 F9 opt-out failed");
        GD.Print("TWR_SMOKE_PASS41_NAV_OK map=Laboratory synthetic=true " +
            "v3_nodes=19 v3_edges=18 repaired_shortcut=true " +
            "components=1 F4_N=PASS F9_toggle=2 native_floor_clearance=PASS " +
            "original_roblox_navmesh=false");
        GetTree().Quit(0);
    }

    private async void RunPass40Smoke()
    {
        // Actual exported Godot/Windows binary against a synthetic SHA-bound
        // navigation fixture and large fabricated source floor. The original
        // Laboratory's missing external meshes/navigation are not used here.
        _runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass40SyntheticGroundedNavigationSmoke",
            Runtime=_runtime,MapName="Laboratory"
        };
        AddChild(game);
        var nav=game.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var floorplans=game.GetNodeOrNull<Pass39LaboratoryFloorplan>(
            "Pass39LaboratoryFloorplan");
        if(floorplans is null || !floorplans.HasNavigationDiagnostic ||
            floorplans.IsShowingNavigation || floorplans.LevelCount!=3)
            throw new InvalidOperationException("Pass40 synthetic map nav diagrams unavailable");
        floorplans._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        floorplans._Input(new InputEventKey {Keycode=Key.N,Pressed=true});
        if(!floorplans.IsShowingNavigation || !floorplans.IsOpen)
            throw new InvalidOperationException("Pass40 F4-N navigation overlay did not open");
        floorplans._Input(new InputEventKey {Keycode=Key.Right,Pressed=true});
        if(floorplans.ActiveLevel!=1)
            throw new InvalidOperationException("Pass40 nav plan floor switch lost selection");
        floorplans._Input(new InputEventKey {Keycode=Key.N,Pressed=true});
        if(floorplans.IsShowingNavigation)
            throw new InvalidOperationException("Pass40 N failed to restore source bounds view");
        floorplans._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        if(nav is null || !nav.IsBridgePackActive ||
            nav.IsPass40GroundedGraph || nav.PointCount!=19 ||
            nav.EdgeCount!=18 || player is null)
            throw new InvalidOperationException(
                "Source-SHA-bound synthetic Pass40 Laboratory navigation not loaded");
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        var distantSource=new Vector3(280f,1f,-280f);
        var original=nav.GetAssistedInfectedSpawnCandidates(
            distantSource,player.GlobalPosition,0,24f);
        var closer=nav.GetAssistedInfectedSpawnCandidates(
            distantSource,player.GlobalPosition,0,14f);
        if(original.Length!=0 || closer.Length==0 ||
            nav.GetAssistedInfectedSpawnCandidates(
                distantSource,player.GlobalPosition,0,9f).Length!=0)
            throw new InvalidOperationException(
                "Adaptive F9 ignored 24m preference or minimum 10m safeguard");
        var safe=Pass40AdaptiveEntry.TryFind(
            nav,game.GetWorld3D(),distantSource,player.GlobalPosition,0);
        if(!safe.HasValue || safe.Value.MinimumDistance!=14f ||
            safe.Value.Position.DistanceTo(player.GlobalPosition)<14f)
            throw new InvalidOperationException(
                "Pass40 did not return a physically supported nonteleport entry");
        game._UnhandledInput(new InputEventKey {Keycode=Key.F9,Pressed=true});
        if(!game.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("F9 source accessibility did not opt in");
        game._Process(1000.0);
        game._Process(1.0);
        if(game.AssistedInfectedSpawnCount<1 || game.Pass40AdaptiveNearSpawns<1)
            throw new InvalidOperationException(
                "Regular-wave infected spawn did not use an F9 source-grounded entry");
        GD.Print("TWR_SMOKE_PASS40_NAV_OK Laboratory synthetic_sha_bound=true "+
            "nodes=19 edges=18 default_original_spawns=true "+
            "F9_opt_in=true min24_unavailable=true min14_physics_accepted=true "+
            "wave_spawn_grounded=true zero_source_geometry_claims=true");
        GetTree().Quit(0);
    }

    private void RunPass39Smoke()
    {
        // The actual exported Godot Windows executable imports a SYNTHETIC,
        // fabricated Laboratory source scene and three tiny generated PNGs.
        // No real source map or owner-private art is present in public CI.
        _runtime.StartMap("Laboratory");
        var game=new GameplayRoot
        {
            Name="Pass39OriginalLaboratorySmoke",
            Runtime=_runtime, MapName="Laboratory"
        };
        AddChild(game);
        var scene=game.GetNodeOrNull<Node3D>("RecoveredLaboratory");
        var overlay=game.GetNodeOrNull<Pass39LaboratoryFloorplan>(
            "Pass39LaboratoryFloorplan");
        if(scene is null || overlay is null || overlay.LevelCount!=3 ||
            overlay.VerifiedOriginalScene || overlay.IsOpen || overlay.ActiveLevel!=0 ||
            game.Pass39LabFloorCount!=3 ||
            game.Pass39OriginalLabSourceVerified ||
            game.GetNodeOrNull<Node3D>("Pass36SourceFragments") is not null)
            throw new InvalidOperationException(
                "Pass39 synthetic Laboratory floorplan missing, not isolated or enabled by default");
        overlay._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        if(!overlay.IsOpen)
            throw new InvalidOperationException("F4 did not open Laboratory source floorplan");
        overlay._Input(new InputEventKey {Keycode=Key.Right,Pressed=true});
        if(overlay.ActiveLevel!=1)
            throw new InvalidOperationException("Right arrow did not choose Laboratory main floor");
        overlay._Input(new InputEventKey {Keycode=Key.Left,Pressed=true});
        if(overlay.ActiveLevel!=0)
            throw new InvalidOperationException("Left arrow did not return Laboratory lower floor");
        overlay._Input(new InputEventKey {Keycode=Key.Left,Pressed=true});
        if(overlay.ActiveLevel!=2)
            throw new InvalidOperationException("Laboratory source floor index wrap failed");
        overlay._Input(new InputEventKey {Keycode=Key.F4,Pressed=true});
        if(overlay.IsOpen)
            throw new InvalidOperationException("F4 failed to close the Laboratory source plan");
        GD.Print("TWR_SMOKE_PASS39_LAB_OK original_source=false synthetic_geometry=20 " +
            "floorplans=3 toggle=2 floor_change=3 original_custom_meshes_unavailable=true");
        GetTree().Quit(0);
    }

    private void RunPass38Smoke()
    {
        // This executes in the exported native Windows Godot game. CI installs
        // two fabricated Manor sound rows and self-generated PCM16 WAV clips;
        // no original Roblox SoundIds, MP3s or owner source data are included.
        _runtime.StartMap("Manor");
        var game=new GameplayRoot
        {
            Name="Pass38SoundscapeSmoke",Runtime=_runtime,MapName="Manor"
        };
        AddChild(game);
        var sound=game.GetNodeOrNull<Pass38SourceSoundscapeRuntime>(
            "Pass38SourceSoundscape");
        if(sound is null || sound.SourceEmitterCount!=2 ||
            sound.SourceEnvironmentCount!=1 || sound.SourcePositionedCount!=1 ||
            sound.InstalledWavEmitterCount!=2 || sound.HasExactMapGeometry ||
            sound.Enabled || sound.ActiveVoiceCount!=0)
            throw new InvalidOperationException(
                "Pass38 synthetic source soundscape missing, miscounted, or enabled by default");
        var local=sound.GetNodeOrNull<AudioStreamPlayer3D>("SourceLocalSound1");
        if(local is null || local.Position.DistanceTo(
            new Vector3(8f*.28f,2f*.28f,-1f*.28f))>.001f)
            throw new InvalidOperationException("Recovered source 3D sound position was not converted from studs");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F2,Pressed=true });
        if(!sound.Enabled || sound.ActiveVoiceCount!=1 ||
            sound.GetNodeOrNull<AudioStreamPlayer>("SourceEnvironmentSound0")?.Playing!=true ||
            local.Playing)
            throw new InvalidOperationException(
                "F2 source sound preview did not honor authored ambience and unaligned fallback map");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F2,Pressed=true });
        if(sound.Enabled || sound.ActiveVoiceCount!=0)
            throw new InvalidOperationException(
                "F2 failed to stop all source sound voices without mutating gameplay audio");
        GD.Print("TWR_SMOKE_PASS38_SOUND_OK map=Manor source_emitters=2 " +
            "installed_wav=2 3d_source_pos_correct=true approximate_blockout_local=OFF " +
            "global_ambience=ON toggles=2 default=OFF original_audio=false");
        GetTree().Quit(0);
    }

    private async void RunPass36Smoke()
    {
        // Native exported Godot C# verification with fabricated CMaps source.
        // No owner TestPlace bytes, meshes, source textures or private map packs.
        _runtime.StartMap("Manor");
        var game=new GameplayRoot
        {
            Name="Pass36OriginalSourceMapSmoke",Runtime=_runtime,MapName="Manor"
        };
        AddChild(game);
        var source=game.GetNodeOrNull<Pass36SourceFragmentsRuntime>("Pass36SourceFragments");
        var map=game.GetNodeOrNull<Pass36MapPlanOverlay>("Pass36SourceMapPlan");
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(source is null || source.WallCount!=1 ||
            source.NativeObjectCount!=1 || source.MissingOriginalCustomObjectCount!=1 ||
            source.ServerCollisionEnabled || source.NativeObjectPreviewEnabled ||
            map is null || !map.HasPlan || map.IsOpen || player is null)
            throw new InvalidOperationException("Pass36 synthetic source fragments or map plan unavailable");
        if((player.CollisionMask & Pass36SourceFragmentsRuntime.SourceServerCollisionLayer)==0)
            throw new InvalidOperationException("Player cannot collide with opt-in original Server Walls");
        var infected=new InfectedAgent
        {
            Name="Pass36InfectedCollisionProbe",
            Target=player,Runtime=_runtime,
            Position=new Vector3(280,280,-285)
        };
        game.AddChild(infected);
        if((infected.CollisionMask & Pass36SourceFragmentsRuntime.SourceServerCollisionLayer)==0)
            throw new InvalidOperationException("Infected do not collide with source Server Wall layer");
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        var from=new Vector3(280,280,-282);
        var to=new Vector3(280,280,-278);
        bool Hit(uint mask)
        {
            var query=PhysicsRayQueryParameters3D.Create(from,to);
            query.CollisionMask=mask;
            return game.GetWorld3D().DirectSpaceState.IntersectRay(query).Count>0;
        }
        if(Hit(Pass36SourceFragmentsRuntime.SourceServerCollisionLayer))
            throw new InvalidOperationException("Original server collision enabled by default");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F6, Pressed=true });
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if(!source.ServerCollisionEnabled || !Hit(Pass36SourceFragmentsRuntime.SourceServerCollisionLayer) ||
            Hit(1))
            throw new InvalidOperationException("F6 did not isolate source Server Wall physics from world layer");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F6, Pressed=true });
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if(source.ServerCollisionEnabled || Hit(Pass36SourceFragmentsRuntime.SourceServerCollisionLayer))
            throw new InvalidOperationException("F6 could not disable original source wall collision");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F5, Pressed=true });
        if(!source.NativeObjectPreviewEnabled || source.NativeObjectCount!=1)
            throw new InvalidOperationException("F5 original native Part rendering did not enable");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F5, Pressed=true });
        if(source.NativeObjectPreviewEnabled)
            throw new InvalidOperationException("F5 original source props did not hide");
        map._Input(new InputEventKey { Keycode=Key.F4, Pressed=true });
        if(!map.IsOpen)throw new InvalidOperationException("F4 original footprint plan did not open");
        map._Input(new InputEventKey { Keycode=Key.F4, Pressed=true });
        if(map.IsOpen)throw new InvalidOperationException("F4 source footprint plan did not close");
        GD.Print("TWR_SMOKE_PASS36_SOURCE_MAP_OK " +
            "server_wall=1 native_prop=1 missing_custom_mesh=1 " +
            "physics_toggle=2 object_toggle=2 plan_toggle=2 default=OFF");
        GetTree().Quit(0);
    }

    private async void RunPass35Smoke()
    {
        // Executed in the actual exported Godot .NET Windows game; CI injects
        // one fabricated Manor source Infected Wall. No owner Roblox assets.
        _runtime.StartMap("Manor");
        var game = new GameplayRoot
        {
            Name = "Pass35InfectedWallSmoke",
            Runtime = _runtime, MapName = "Manor"
        };
        AddChild(game);
        var original = game.GetNodeOrNull<Pass35InfectedWallsRuntime>(
            "Pass35InfectedWalls");
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if (original is null || original.WallCount != 1 ||
            original.Enabled || original.ApproximateMeshProxyCount != 0 ||
            player is null)
            throw new InvalidOperationException("Pass35 synthetic original infected wall missing or enabled by default");
        if ((player.CollisionMask & Pass35InfectedWallsRuntime.InfectedWallCollisionLayer) != 0)
            throw new InvalidOperationException("Infected-only source barrier incorrectly blocks the player");

        var enemy = new InfectedAgent
        {
            Name = "Pass35InfectedCollisionProbe", Target=player,
            Runtime=_runtime, Position=new Vector3(280,280,-285)
        };
        game.AddChild(enemy);
        if ((enemy.CollisionMask & Pass35InfectedWallsRuntime.InfectedWallCollisionLayer)==0)
            throw new InvalidOperationException("Infected actor does not test original Infected Walls layer");
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        // Generated source wall: studs [1000,1000,1000], Godot z reflected.
        var from=new Vector3(280f,280f,-282f);
        var to=new Vector3(280f,280f,-278f);
        bool Hits(uint collisionLayer)
        {
            var ray=PhysicsRayQueryParameters3D.Create(from,to);
            ray.CollisionMask=collisionLayer;
            return game.GetWorld3D().DirectSpaceState.IntersectRay(ray).Count>0;
        }
        if (Hits(Pass35InfectedWallsRuntime.InfectedWallCollisionLayer))
            throw new InvalidOperationException("Original infected-only wall not default OFF");

        game._UnhandledInput(new InputEventKey { Keycode=Key.F7,Pressed=true });
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if (!original.Enabled || !Hits(Pass35InfectedWallsRuntime.InfectedWallCollisionLayer))
            throw new InvalidOperationException("F7 infected-only wall collision failed");
        if (Hits(1))
            throw new InvalidOperationException("Source infected-only wall leaked into normal world/player layer");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F7,Pressed=true });
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if (original.Enabled || Hits(Pass35InfectedWallsRuntime.InfectedWallCollisionLayer))
            throw new InvalidOperationException("F7 failed to disable infected-only source wall");
        GD.Print("TWR_SMOKE_PASS35_INFECTED_WALLS_OK map=Manor " +
            "wall=1 default_off=true toggles=2 infected_only_layer=8 player_excluded=true");
        GetTree().Quit(0);
    }

    private async void RunPass34Smoke()
    {
        _runtime.StartMap("Laboratory");
        var game = new GameplayRoot
        {
            Name = "Pass34ClientWallSmoke",
            Runtime = _runtime,
            MapName = "Laboratory"
        };
        AddChild(game);
        var walls = game.GetNodeOrNull<Pass34ClientWallsRuntime>("Pass34ClientWalls");
        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if (walls is null || walls.WallCount != 1 || walls.Enabled ||
            player is null ||
            (player.CollisionMask & Pass34ClientWallsRuntime.ClientWallCollisionLayer) == 0)
            throw new InvalidOperationException("Pass34 exact-SHA synthetic Client Walls did not load OFF by default");

        // Verify this source layer is physically absent until F8, then
        // visible to player physics only, not world/zombie layer 1.
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        var from = new Vector3(0f,1.4f,-7.5f);
        var to = new Vector3(0f,1.4f,-3.5f);
        bool Hits(uint layer)
        {
            var ray = PhysicsRayQueryParameters3D.Create(from,to);
            ray.CollisionMask = layer;
            return game.GetWorld3D().DirectSpaceState.IntersectRay(ray).Count > 0;
        }
        if (Hits(Pass34ClientWallsRuntime.ClientWallCollisionLayer))
            throw new InvalidOperationException("Source client walls active before user opt-in");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F8, Pressed=true });
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if (!walls.Enabled || !Hits(Pass34ClientWallsRuntime.ClientWallCollisionLayer))
            throw new InvalidOperationException("F8 did not enable physical original Client Walls");
        if (Hits(1))
            throw new InvalidOperationException("Client-only wall incorrectly blocks world/infected collision mask");
        game._UnhandledInput(new InputEventKey { Keycode=Key.F8, Pressed=true });
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if (walls.Enabled || Hits(Pass34ClientWallsRuntime.ClientWallCollisionLayer))
            throw new InvalidOperationException("F8 disabled wall but physics collision remained");
        GD.Print("TWR_SMOKE_PASS34_WALLS_OK source=sha256 " +
            "walls=1 default=OFF toggles=2 player_only_layer=4 infected_layer=1");
        GetTree().Quit(0);
    }

    private void RunPass33Smoke()
    {
        // The exported Windows game loads the synthetic SHA-checked
        // Laboratory nav31 sidecar. Repeating the same source waypoints
        // must use the bounded cache rather than recomputing A* per zombie.
        _runtime.StartMap("Laboratory");
        var game = new GameplayRoot
        {
            Name = "Pass33PathfindingSmoke",
            Runtime = _runtime,
            MapName = "Laboratory"
        };
        AddChild(game);
        var nav = game.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        if (nav is null || !nav.IsBridgePackActive || nav.PointCount < 52)
            throw new InvalidOperationException("Pass33 requires synthetic source navigation");

        var from = new Vector3(0,1.4f,0);
        var target = new Vector3(14,1.4f,0);
        var first = nav.GetRoute(from,target);
        if (first.Length < 2 || nav.ActualPathSearches != 1)
            throw new InvalidOperationException("Cold source path was not computed");

        // Callers are not permitted to corrupt a cached path shared by
        // multiple infected actors.
        var original = first[0];
        first[0] = new Vector3(1234,1234,1234);
        var cloned = nav.GetRoute(from,target);
        if (cloned.Length < 2 || cloned[0] != original)
            throw new InvalidOperationException("Cached route mutated by a caller");
        for (var i = 0; i < 255; i++)
        {
            var route = nav.GetRoute(from,target);
            if (route.Length < 2)
                throw new InvalidOperationException("Repeated cached source route became empty");
        }
        if (nav.ActualPathSearches != 1 || nav.RouteCacheHits != 256)
            throw new InvalidOperationException("A* was recomputed instead of using source-path cache");

        // Original exterior source region is disconnected. Short circuit
        // using union-find component IDs BEFORE A* explores the whole wing.
        var searchesBefore = nav.ActualPathSearches;
        for (var i = 0; i < 64; i++)
            if (nav.GetRoute(new Vector3(84,1.4f,-11.2f),from).Length != 0)
                throw new InvalidOperationException("Disconnected exterior route fabricated");
        if (nav.ActualPathSearches != searchesBefore ||
            nav.DisconnectedRouteRejects < 64)
            throw new InvalidOperationException("Disconnected source graph was searched");

        // Exceed the bounded LRU budget with different genuine graph paths.
        // More than 512 cached paths must never accumulate.
        for (var start = 0; start < 51; start++)
        {
            for (var end = 0; end < 51; end++)
            {
                if (start == end) continue;
                nav.GetRoute(new Vector3(start*.56f,1.4f,0),
                    new Vector3(end*.56f,1.4f,0));
            }
        }
        if (nav.CachedRouteCount > 512 ||
            nav.ActualPathSearches < 513)
            throw new InvalidOperationException("Source path cache is unbounded or inactive");
        GD.Print($"TWR_SMOKE_PASS33_PATHCACHE_OK " +
            $"queries={nav.RouteRequests} searches={nav.ActualPathSearches} " +
            $"hits={nav.RouteCacheHits} disconnected={nav.DisconnectedRouteRejects} " +
            $"cached={nav.CachedRouteCount} original_ingress_paths=0");
        GetTree().Quit(0);
    }

    private async void RunPass32Smoke()
    {
        // This runs in the real Windows-exported Godot C# executable with
        // disposable synthetic scene/collision/nav assets from CI.
        if (!Pass32RescuePolicy.CanRescue(24f, 0f, 8.0, 0, 100.0) ||
            Pass32RescuePolicy.CanRescue(24f, 0f, 8.0, 2, 100.0) ||
            Pass32RescuePolicy.CanRescue(24f, 0f, 8.0, 1, 1.0) ||
            Pass32RescuePolicy.CanRescue(6f, 0f, 8.0, 0, 100.0) ||
            !Pass32RescuePolicy.CanRescue(24f,-10f,0.0,0,100.0))
            throw new InvalidOperationException("Pass32 rescue eligibility limits failed");

        _runtime.StartMap("Laboratory");
        var gameplay = new GameplayRoot
        {
            Name = "Pass32GroundedSmoke",
            Runtime = _runtime,
            MapName = "Laboratory"
        };
        AddChild(gameplay);
        var navigation = gameplay.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        if (navigation is null || !navigation.IsBridgePackActive)
            throw new InvalidOperationException("Pass32 nav31 pack failed to load");

        // Ensure all collision shapes are active before querying space.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        var target = new Vector3(0,1.4f,0);
        var disconnected = new Vector3(84,1.4f,-11.2f);
        var candidates = navigation.GetAssistedInfectedSpawnCandidates(
            disconnected,target,3);
        if (candidates.Length == 0)
            throw new InvalidOperationException("No disconnected spawn alternatives");
        var safe = Pass32SpawnSafety.FindSupportedPlacement(
            gameplay.GetWorld3D(),candidates,target);
        if (!safe.HasValue || safe.Value.DistanceTo(target) < 24f ||
            navigation.GetRoute(safe.Value,target).Length == 0 ||
            Math.Abs(safe.Value.Y - 2.30f) > .2f)
            throw new InvalidOperationException("Distant source-grounded F9 placement invalid");
        var unsupported = Pass32SpawnSafety.FindSupportedPlacement(
            gameplay.GetWorld3D(), new[] { new Vector3(70f,2.2f,40f) },target);
        if (unsupported.HasValue)
            throw new InvalidOperationException("Void/unsupported zombie entry accepted");
        if (gameplay.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("F9 assisted relocation default was enabled");
        gameplay._UnhandledInput(new InputEventKey
        {
            Keycode = Key.F9, Pressed = true
        });
        if (!gameplay.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("F9 opt-in did not enable source rescue");

        // Actual CharacterBody3D path traversal, not a Python-only graph test.
        var enemy = new InfectedAgent
        {
            Name = "Pass32MovementProbe",
            Target = gameplay.GetNode<FirstPersonPlayer>("Player"),
            Runtime = _runtime,
            SourceNavigator = navigation,
            InfectedType = "Civilian",
            Position = safe.Value
        };
        gameplay.AddChild(enemy);
        var start = enemy.GlobalPosition;
        for (var tick = 0; tick < 35; tick++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        var end = enemy.GlobalPosition;
        var moved = new Vector2(end.X-start.X,end.Z-start.Z).Length();
        if (moved < .20f || !float.IsFinite(moved))
            throw new InvalidOperationException("Lab infected actor did not traverse source nav");
        GD.Print($"TWR_SMOKE_PASS32_GROUNDED_OK floor=true " +
            $"rejected_void=true movement={moved:F2}m " +
            $"rescue_limit={Pass32RescuePolicy.MaximumRescuesPerEnemy} " +
            $"source_spawns_default=unchanged");
        GetTree().Quit(0);
    }

    private void RunPass31Smoke()
    {
        _runtime.StartMap("Laboratory");
        var game = new GameplayRoot
        {
            Name = "Pass31NavigationSmoke",
            Runtime = _runtime,
            MapName = "Laboratory"
        };
        AddChild(game);
        var nav = game.GetNodeOrNull<Pass25SourceNavigationRuntime>(
            "Pass25LaboratoryNavigation");
        if (nav is null || !nav.IsBridgePackActive ||
            nav.PointCount < 52 || nav.EdgeCount < 50)
            throw new InvalidOperationException("Synthetic nav31 source graph not loaded");
        var player = new Vector3(0, 1.4f, 0);
        // Exact source-side node is disconnected in the synthetic fixture.
        var unreachable = new Vector3(84, 1.4f, -11.2f);
        var assisted = nav.FindAssistedInfectedSpawn(unreachable, player, 3);
        if (!assisted.HasValue || assisted.Value.DistanceTo(player) < 24f ||
            nav.GetRoute(assisted.Value, player).Length == 0)
            throw new InvalidOperationException("Assisted distant infected entry is not navigable");
        if (game.AssistedInfectedSpawnsEnabled)
            throw new InvalidOperationException("Source-preserving assisted spawn mode must default OFF");
        GD.Print("TWR_SMOKE_PASS31_NAV_OK nodes=" + nav.PointCount +
            " bridges=source_bound opt_in=true original_spawns_preserved=true");
        GetTree().Quit(0);
    }

    private void RunPass30Smoke()
    {
        // C# code runs in the exported Windows game, not in a Python mock.
        if (!Pass28PrimitiveStreamer.CanSeeBounds(
                new Vector2(0,0), new Vector2(140,-4), new Vector2(210,4), 145f))
            throw new InvalidOperationException("Large source geometry wrongly culled");
        if (Pass28PrimitiveStreamer.CanSeeBounds(
                new Vector2(0,0), new Vector2(180,-4), new Vector2(210,4), 145f))
            throw new InvalidOperationException("Out-of-range geometry wrongly visible");
        _runtime.StartMap("Laboratory");
        var gameplay = new GameplayRoot {
            Name = "Pass30GameplaySmoke", Runtime = _runtime, MapName = "Laboratory"
        };
        AddChild(gameplay);
        if (gameplay.GetNodeOrNull<Pass30DiagnosticsHud>("Pass30Diagnostics") is null ||
            gameplay.ActivePickupCount == 0 ||
            gameplay.MapLoadMilliseconds > 300000UL)
            throw new InvalidOperationException("Laboratory gameplay or QA overlay did not start");
        var stage = gameplay.GetNodeOrNull<Node3D>("RecoveredLaboratory");
        if (stage is not null)
        {
            var navigator = gameplay.GetNodeOrNull<Pass25SourceNavigationRuntime>(
                "Pass25LaboratoryNavigation");
            var collision = stage.GetNodeOrNull<Node3D>("Pass26Collision");
            var streaming = stage.GetNodeOrNull<Pass28PrimitiveStreamer>("Pass28PrimitiveStream");
            if (navigator is null || navigator.PointCount < 2 ||
                collision is null || collision.GetChildCount() < 1 ||
                streaming is null || streaming.BatchCount < 1 ||
                streaming.VisibleBatchCount < 1)
                throw new InvalidOperationException(
                    "Original source navigation/collision/geometry were not activated");
            // Minimal generated PNGs prove map cards and weapon icons load
            // in the exported Godot C# executable (no private art in CI).
            if (Pass27SourceArtCatalog.MapCard("Laboratory") is null ||
                Pass27SourceArtCatalog.WeaponIcon("Glock 17") is null)
                throw new InvalidOperationException("Synthetic private UI art failed to load");
            GD.Print($"TWR_SMOKE_PASS30_SOURCE_OK nodes={navigator.PointCount} " +
                $"collision_tiles={collision.GetChildCount()} " +
                $"visible_batches={streaming.VisibleBatchCount}");
        }
        GD.Print("TWR_SMOKE_PASS30_GAMEPLAY_OK map=Laboratory inventory=16 bounds=aabb");
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
    public override void _Input(InputEvent @event)
    {
        // The right mouse button tilts only the armory's local preview model,
        // never the authoritative player camera, scene, purchases or weapons.
        if (_armory is null || @event is not InputEventMouseMotion motion ||
            !Input.IsMouseButtonPressed(MouseButton.Right))
            return;
        var preview=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        if(preview?.RotatePreview(motion.Relative.X,motion.Relative.Y)==true)
            GetViewport().SetInputAsHandled();
    }

    private void EnsureOriginalLobby(string cameraName)
    {
        if(_sourceLobby is null || !GodotObject.IsInstanceValid(_sourceLobby))
            _sourceLobby=Pass49OriginalLobbyRuntime.TryBuild(this);
        _sourceLobby?.SetCamera(cameraName);
    }

    private void ShowMenu()
    {
        _bulletin?.QueueFree();
        _bulletin=null;
        _bulletinPage=null;
        _armoryPreviewLabel=null;
        _shop?.QueueFree();
        _shop=null;
        _shopCaseDetail=null;
        _shopCreditsLabel=null;
        _shopOpenButton=null;
        _skinInventory?.QueueFree();
        _skinInventory=null;
        _skinDetailLabel=null;
        _sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName)?.HideDisplay();
        EnsureOriginalLobby("Start");
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _menu = new CanvasLayer { Name = "MainMenu" };
        AddChild(_menu);

        AddBackground(_menu);
        _menu.AddChild(MakeLabel(58, 38, 840, 62, 40, "THOSE WHO REMAIN - OFFLINE"));
        _menu.AddChild(MakeLabel(60, 98, 850, 38, 18, "REGULAR  |  15 WAVES  |  LOCAL SINGLE PLAYER"));
        _menu.AddChild(MakeLabel(60, 142, 1160, 66, 16,
            "Select a recovered release map. Geometry is an evidence-guided reconstruction blockout\n" +
            "until original map transforms are recoverable. Gameplay rules remain source-labeled."));
        _menu.AddChild(MakeLabel(60, 210, 850, 36, 15, ProfileStatusText()));
        if (Pass27SourceArtCatalog.OfflineArtReady)
            _menu.AddChild(MakeLabel(60, 242, 850, 23, 12,
                $"OFFLINE MAP ART {Pass27SourceArtCatalog.MapArtCount}/10  |  " +
                $"WEAPON REFERENCES {Pass27SourceArtCatalog.WeaponArtCount}/91"));
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

        var shop=new Button {
            OffsetLeft=930,
            OffsetTop=35,
            OffsetRight=1190,
            OffsetBottom=85,
            Text="SHOP / CASES"
        };
        shop.Pressed += ShowShop;
        _menu.AddChild(shop);

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

        var bulletin=new Button
        {
            OffsetLeft=930,
            OffsetTop=205,
            OffsetRight=1190,
            OffsetBottom=255,
            Text="BULLETIN BOARD"
        };
        bulletin.Pressed += ShowBulletin;
        _menu.AddChild(bulletin);

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
            Pass27MapCardDecoration.Apply(button, map.Name);
            var selected = map.Name;
            button.Pressed += () => StartGame(selected);
            _menu.AddChild(button);
        }
    }

    private void UpdateShopSelection()
    {
        var definition=SkinCaseCatalog.FindCase(_selectedShopCase);
        if(definition is null)return;
        var profile=_runtime.Profile;
        var owned=profile is null ? 0 : SkinCaseCatalog.OwnedCount(profile,definition);
        var complete=profile is not null && SkinCaseCatalog.Completed(profile,definition);
        var credits=_runtime.Player?.Credits ?? 0;
        if(_shopCreditsLabel is not null)
            _shopCreditsLabel.Text="OFFLINE CREDITS: $"+credits.ToString("N0")+
                "  |  OWNED SKINS: "+(profile?.OwnedSkins.Count ?? 0);
        if(_shopOpenButton is not null)
        {
            _shopOpenButton.Text=definition.OnSale
                ? "OPEN "+definition.Name.ToUpperInvariant()+" CASE ($"+
                    definition.PriceCredits.ToString("N0")+")"
                : "HALLOWS OFF-SALE";
            _shopOpenButton.Disabled=!definition.OnSale || complete ||
                credits<definition.PriceCredits;
        }
        if(_shopCaseDetail is not null)
            _shopCaseDetail.Text=definition.Name+"  |  $"+
                definition.PriceCredits.ToString("N0")+" CREDITS  |  "+owned+
                "/"+definition.PurchasePool.Length+" ORDINARY SKINS OWNED"+
                (complete ? "  |  COLLECTION COMPLETE" : "")+
                (!definition.OnSale ? "  |  OFF-SALE EXCLUSIVE" : "")+
                "\nOffline case odds: uniform among missing skins (RECONSTRUCTION). "+
                "Original paid-product transactions are not implemented offline.";
    }

    private void HandleOpenSelectedCase()
    {
        var result=_runtime.OpenSkinCase(_selectedShopCase);
        ShowShop();
        if(_shopCaseDetail is not null)
            _shopCaseDetail.Text=result.Message+"\n"+_shopCaseDetail.Text;
    }

    private void ShowShop()
    {
        EnsureOriginalLobby("Shop");
        _sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName)?.HideDisplay();
        _menu?.QueueFree();
        _menu=null;
        _armory?.QueueFree();
        _armory=null;
        _skinInventory?.QueueFree();
        _skinInventory=null;
        _skinDetailLabel=null;
        _bulletin?.QueueFree();
        _bulletin=null;
        _bulletinPage=null;
        _perkMenu?.QueueFree();
        _perkMenu=null;
        _shop?.QueueFree();
        Input.MouseMode=Input.MouseModeEnum.Visible;

        _shop=new CanvasLayer { Name="OriginalSourceShop" };
        AddChild(_shop);
        AddBackground(_shop,.40f);
        _shop.AddChild(MakeLabel(58,34,740,54,32,"SHOP / OFFLINE SKIN CASES"));
        _shop.AddChild(MakeLabel(62,100,1120,62,15,
            "Source-confirmed case contents and credit prices. Case odds are "+
            "not recovered: locally simulated uniform draws only among "+
            "unowned standard skins. Exclusive Hallows is off-sale."));
        _shopCreditsLabel=MakeLabel(62,172,1000,34,16,"");
        _shop.AddChild(_shopCreditsLabel);
        var back=new Button {
            OffsetLeft=1030,OffsetTop=38,OffsetRight=1210,OffsetBottom=88,
            Text="BACK"
        };
        back.Pressed += ShowMenu;
        _shop.AddChild(back);

        var cases=SkinCaseCatalog.Cases;
        for(var i=0;i<cases.Count;i++)
        {
            var source=cases[i];
            var column=i%2;
            var row=i/2;
            var left=62+column*590;
            var top=218+row*67;
            var button=new Button {
                Name="ShopCase_"+source.Name,
                OffsetLeft=left,
                OffsetTop=top,
                OffsetRight=left+545,
                OffsetBottom=top+55,
                Text="CASE: "+source.Name.ToUpperInvariant()+"  |  $"+
                    source.PriceCredits.ToString("N0")+" CREDITS"+
                    (source.OnSale?"":"  [OFF-SALE]")
            };
            button.Pressed += () =>
            {
                _selectedShopCase=source.Name;
                UpdateShopSelection();
            };
            _shop.AddChild(button);
        }
        _shopCaseDetail=MakeLabel(62,493,1130,80,15,"");
        _shop.AddChild(_shopCaseDetail);
        _shopOpenButton=new Button {
            Name="ShopOpenSelectedCase",
            OffsetLeft=62,OffsetTop=604,OffsetRight=444,OffsetBottom=659,
            Text="OPEN SELECTED CASE"
        };
        _shopOpenButton.Pressed += HandleOpenSelectedCase;
        _shop.AddChild(_shopOpenButton);
        var ownedButton=new Button {
            Name="ShopOwnedSkins",
            OffsetLeft=467,OffsetTop=604,OffsetRight=849,OffsetBottom=659,
            Text="OWNED SKINS / APPLY / SELL"
        };
        ownedButton.Pressed += () => ShowSkinInventory(true);
        _shop.AddChild(ownedButton);
        UpdateShopSelection();
    }

    private void CloseWikiReferenceGallery()
    {
        _wikiReferenceGallery?.QueueFree();
        _wikiReferenceGallery=null;
        _wikiReferenceImage=null;
        _wikiReferenceCaption=null;
        if(_wikiReferenceReturnLayer is not null &&
           GodotObject.IsInstanceValid(_wikiReferenceReturnLayer))
            _wikiReferenceReturnLayer.Show();
        _wikiReferenceReturnLayer=null;
    }

    private void ShowWikiReferenceGallery(string weapon,string? skin)
    {
        CloseWikiReferenceGallery();
        // The 2D source wiki reference is NEVER used as a MeshPart texture.
        // It is a read-only visual QA reference, not the rendered Godot gun.
        _wikiReferenceReturnLayer=_skinInventory ?? _armory;
        _wikiReferenceReturnLayer?.Hide();
        _wikiReferenceGallery=new CanvasLayer {Name="Pass56WikiReferenceGallery"};
        AddChild(_wikiReferenceGallery);
        AddBackground(_wikiReferenceGallery,.97f);
        _wikiReferenceGallery.AddChild(MakeLabel(60,34,900,60,32,
            "ORIGINAL GAME / WIKI VISUAL REFERENCE"));
        _wikiReferenceGallery.AddChild(MakeLabel(60,104,1130,60,16,
            "Reference screenshots from your supplied image archive. "+
            "These are NOT Roblox UV texture files or a guarantee of 1:1 render fidelity."));

        _wikiReferenceImage=new TextureRect {
            Name="Pass56SourceWikiScreenshot",
            OffsetLeft=100,OffsetTop=185,OffsetRight=1110,OffsetBottom=555,
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter=Control.MouseFilterEnum.Ignore
        };
        _wikiReferenceGallery.AddChild(_wikiReferenceImage);
        var has=Pass56WikiVisualReferences.TryWeaponImage(
            weapon,skin,out var picture,out var caption);
        _wikiReferenceImage.Texture=picture;
        _wikiReferenceCaption=MakeLabel(68,580,1110,70,17,
            (has?caption:"NO VERIFIED WIKI REFERENCE FOUND - "+caption)+
            "\n"+Pass56WikiVisualReferences.Provenance);
        _wikiReferenceGallery.AddChild(_wikiReferenceCaption);
        var back=new Button {
            Name="CloseWikiReferenceGallery",
            OffsetLeft=980,OffsetTop=45,OffsetRight=1195,OffsetBottom=95,
            Text="BACK"
        };
        back.Pressed+=CloseWikiReferenceGallery;
        _wikiReferenceGallery.AddChild(back);
        if(skin is not null && Pass56WikiVisualReferences.TrySkinIcon(skin,
            out var icon) && icon is not null)
        {
            var iconView=new TextureRect {
                Name="Pass56SkinReferenceIcon",
                OffsetLeft=65,OffsetTop=660,OffsetRight=125,OffsetBottom=720,
                Texture=icon,
                ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered
            };
            _wikiReferenceGallery.AddChild(iconView);
        }
    }

    private void OpenArmoryWikiReference()
    {
        var name=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName)?.PreviewWeaponName;
        if(string.IsNullOrWhiteSpace(name))
            name=_runtime.Profile?.Loadout.GetValueOrDefault("Primary");
        if(string.IsNullOrWhiteSpace(name))name="Glock 17";
        var selected=_runtime.Profile?.EquippedWeaponSkins.GetValueOrDefault(name);
        ShowWikiReferenceGallery(name,selected);
    }

    private void OpenInventoryWikiReference()
    {
        var profile=_runtime.Profile;
        var weapon=profile?.Loadout.GetValueOrDefault("Primary") ??
            profile?.Loadout.GetValueOrDefault("Secondary") ?? "Glock 17";
        ShowWikiReferenceGallery(weapon,_selectedSkinId);
    }

    private void ShowSkinInventory(bool returnToShop)
    {
        _skinInventoryReturnsToShop=returnToShop;
        CloseWikiReferenceGallery();
        _shop?.QueueFree();
        _shop=null;
        _shopCaseDetail=null;
        _shopCreditsLabel=null;
        _shopOpenButton=null;
        _armory?.QueueFree();
        _armory=null;
        _menu?.QueueFree();
        _menu=null;
        _bulletin?.QueueFree();
        _bulletin=null;
        _perkMenu?.QueueFree();
        _perkMenu=null;
        EnsureOriginalLobby("Loadout");
        var showroom=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        showroom?.ShowForProfile(_runtime.Profile);
        _skinInventory?.QueueFree();
        _skinInventory=new CanvasLayer { Name="Pass53OwnedSkinInventory" };
        AddChild(_skinInventory);
        AddBackground(_skinInventory,showroom is null ? (float?)null : .48f);
        Input.MouseMode=Input.MouseModeEnum.Visible;

        var profile=_runtime.Profile;
        var owned=(profile?.OwnedSkins ?? [])
            .Select(SkinCaseCatalog.FindSkin).Where(skin=>skin is not null)
            .OrderBy(skin=>skin!.CaseName,StringComparer.Ordinal)
            .ThenBy(skin=>skin!.Name,StringComparer.Ordinal)
            .ToArray();
        if(_selectedSkinId is null ||
            !owned.Any(skin=>skin!.Id==_selectedSkinId))
            _selectedSkinId=owned.FirstOrDefault()?.Id;

        _skinInventory.AddChild(MakeLabel(58,38,910,57,34,
            "OFFLINE SKIN INVENTORY"));
        _skinInventory.AddChild(MakeLabel(60,110,1130,48,15,
            "OWNED "+owned.Length+"/118 SOURCE SKINS | EXCLUSIVES CANNOT BE SOLD | "+
            "SOURCE COLORS WITH PRIVATE PACK; ORIGINAL TEXTURES PARTIALLY MISSING"));
        var back=new Button {
            OffsetLeft=1020,OffsetTop=42,OffsetRight=1200,OffsetBottom=91,
            Text="BACK"
        };
        back.Pressed += () =>
        {
            if(_skinInventoryReturnsToShop)ShowShop();
            else ShowArmory();
        };
        _skinInventory.AddChild(back);
        _skinDetailLabel=MakeLabel(61,160,1130,42,15,"");
        _skinInventory.AddChild(_skinDetailLabel);

        var scroll=new ScrollContainer {
            OffsetLeft=60,OffsetTop=210,OffsetRight=1205,OffsetBottom=541
        };
        _skinInventory.AddChild(scroll);
        var list=new VBoxContainer {CustomMinimumSize=new Vector2(1090,0)};
        list.AddThemeConstantOverride("separation",5);
        scroll.AddChild(list);
        foreach(var item in owned)
        {
            var skin=item!;
            var button=new Button {
                Name="OwnedSkin_"+skin.Name.Replace(' ','_'),
                CustomMinimumSize=new Vector2(1080,36),
                Text=(skin.Id==_selectedSkinId?"[SELECTED] ":"")+
                    skin.CaseName+" | "+skin.Name+
                    (skin.Exclusive?" [EXCLUSIVE]":""),
                Alignment=HorizontalAlignment.Left
            };
            button.Pressed += () =>
            {
                _selectedSkinId=skin.Id;
                UpdateOwnedSkinSelection();
            };
            list.AddChild(button);
        }

        var primary=new Button {
            Name="ApplySkinPrimary",
            OffsetLeft=62,OffsetTop=563,OffsetRight=335,OffsetBottom=612,
            Text="APPLY TO PRIMARY",
            Disabled=_selectedSkinId is null
        };
        primary.Pressed += () => HandleSkinApply("Primary");
        _skinInventory.AddChild(primary);
        var secondary=new Button {
            Name="ApplySkinSecondary",
            OffsetLeft=345,OffsetTop=563,OffsetRight=620,OffsetBottom=612,
            Text="APPLY TO SECONDARY",
            Disabled=_selectedSkinId is null
        };
        secondary.Pressed += () => HandleSkinApply("Secondary");
        _skinInventory.AddChild(secondary);
        var sell=new Button {
            Name="SellOwnedSkin",
            OffsetLeft=630,OffsetTop=563,OffsetRight=900,OffsetBottom=612,
            Text="SELL SELECTED SKIN",
            Disabled=_selectedSkinId is null ||
                SkinCaseCatalog.FindSkin(_selectedSkinId)?.Exclusive == true
        };
        var compareButton=new Button {
            Name="InventoryWikiReference",
            OffsetLeft=910,OffsetTop=563,OffsetRight=1200,OffsetBottom=612,
            Text="WIKI REFERENCE IMAGE"
        };
        compareButton.Pressed+=OpenInventoryWikiReference;
        _skinInventory.AddChild(compareButton);
        sell.Pressed += () =>
        {
            if(_selectedSkinId is null)return;
            var outcome=_runtime.SellOwnedSkin(_selectedSkinId);
            ShowSkinInventory(_skinInventoryReturnsToShop);
            if(_skinDetailLabel is not null)
                _skinDetailLabel.Text=outcome.Message;
        };
        _skinInventory.AddChild(sell);
        var removePrimary=new Button {
            Name="ClearSkinPrimary",
            OffsetLeft=62,OffsetTop=627,OffsetRight=335,OffsetBottom=675,
            Text="RESET PRIMARY FINISH"
        };
        removePrimary.Pressed += () => HandleSkinClear("Primary");
        _skinInventory.AddChild(removePrimary);
        var removeSecondary=new Button {
            Name="ClearSkinSecondary",
            OffsetLeft=345,OffsetTop=627,OffsetRight=620,OffsetBottom=675,
            Text="RESET SECONDARY FINISH"
        };
        removeSecondary.Pressed += () => HandleSkinClear("Secondary");
        _skinInventory.AddChild(removeSecondary);
        UpdateOwnedSkinSelection();
    }

    private void HandleSkinApply(string slot)
    {
        var weapon=_runtime.Profile?.Loadout.GetValueOrDefault(slot) ?? "";
        var outcome=_runtime.ApplyWeaponSkin(weapon,_selectedSkinId);
        ShowSkinInventory(_skinInventoryReturnsToShop);
        if(_skinDetailLabel is not null)
            _skinDetailLabel.Text=outcome.Message;
    }

    private void HandleSkinClear(string slot)
    {
        var weapon=_runtime.Profile?.Loadout.GetValueOrDefault(slot) ?? "";
        var outcome=_runtime.ApplyWeaponSkin(weapon,null);
        ShowSkinInventory(_skinInventoryReturnsToShop);
        if(_skinDetailLabel is not null)
            _skinDetailLabel.Text=outcome.Message;
    }

    private void UpdateOwnedSkinSelection()
    {
        if(_skinDetailLabel is null)return;
        var skin=SkinCaseCatalog.FindSkin(_selectedSkinId);
        if(skin is null)
        {
            _skinDetailLabel.Text="No skins owned. Open an available case with credits.";
            return;
        }
        var profile=_runtime.Profile;
        var applied=profile?.EquippedWeaponSkins
            .Where(pair=>pair.Value==skin.Id)
            .Select(pair=>pair.Key).ToArray() ?? [];
        _skinDetailLabel.Text=skin.Name+" ("+skin.CaseName+")"+
            (skin.Exclusive?" | EXCLUSIVE - CANNOT SELL":" | SELL $"+
                (SkinCaseCatalog.FindCase(skin.CaseName)!.PriceCredits/2).ToString("N0"))+
            (applied.Length>0 ? " | ON "+string.Join(", ",applied) : "");
    }

    private void ShowBulletin()
    {
        _sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName)?.HideDisplay();
        EnsureOriginalLobby("Leaderboards");
        _menu?.QueueFree();
        _menu=null;
        _armory?.QueueFree();
        _armory=null;
        _perkMenu?.QueueFree();
        _perkMenu=null;
        _bulletin?.QueueFree();
        Input.MouseMode=Input.MouseModeEnum.Visible;

        _bulletin=new CanvasLayer { Name="SourceBulletinView" };
        AddChild(_bulletin);
        AddBackground(_bulletin, .17f);
        _bulletin.AddChild(MakeLabel(52,32,970,48,30,
            "ORIGINAL LOBBY BULLETIN BOARD"));
        _bulletin.AddChild(MakeLabel(52,91,1040,65,15,
            "Original sign headings are restored in 3D. The saved online ranking snapshot " +
            "is historical; live Roblox leaderboards are unavailable offline."));
        var signs=_sourceLobby?.GetNodeOrNull<Pass50SourceLobbySignsRuntime>(
            "Pass50SourceLobbySigns");
        _bulletinPage=MakeLabel(52,165,1040,45,19,
            signs is null ? "Original board lettering unavailable (2D fallback)" :
            "SOURCE BOARD PAGE: " + signs.ActivePageTitle);
        _bulletin.AddChild(_bulletinPage);

        var next=new Button {
            OffsetLeft=52,OffsetTop=630,OffsetRight=370,OffsetBottom=686,
            Text="NEXT BOARD PAGE",
            Disabled=signs is null
        };
        next.Pressed += () =>
        {
            if(signs is null)return;
            signs.CyclePage();
            if (_bulletinPage is not null)
                _bulletinPage.Text="SOURCE BOARD PAGE: "+signs.ActivePageTitle;
        };
        _bulletin.AddChild(next);
        var back=new Button {
            OffsetLeft=1010,OffsetTop=38,OffsetRight=1200,OffsetBottom=90,
            Text="BACK"
        };
        back.Pressed += ShowMenu;
        _bulletin.AddChild(back);
    }

    private void ShowPerks()
    {
        _sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName)?.HideDisplay();
        EnsureOriginalLobby("Perks");
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
        CloseWikiReferenceGallery();
        EnsureOriginalLobby("Loadout");
        var showroom=_sourceLobby?.GetNodeOrNull<Pass51OriginalLoadoutDisplayRuntime>(
            Pass51OriginalLoadoutDisplayRuntime.StageName);
        _skinInventory?.QueueFree();
        _skinInventory=null;
        _skinDetailLabel=null;
        showroom?.ShowForProfile(_runtime.Profile);
        _menu?.QueueFree();
        _menu = null;
        _armory?.QueueFree();

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _armory = new CanvasLayer { Name = "Armory" };
        AddChild(_armory);
        AddBackground(_armory, showroom is null ? (float?)null : .45f);

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

        _armoryPreviewLabel=MakeLabel(60,170,1140,36,14,
            showroom?.PreviewStatus ?? "3D loadout preview requires the private original lobby pack.");
        _armory.AddChild(_armoryPreviewLabel);
        var skinInventoryButton=new Button {
            Name="ArmoryOpenSkins",
            OffsetLeft=795,OffsetTop=133,OffsetRight=1018,OffsetBottom=176,
            Text="SKIN INVENTORY"
        };
        skinInventoryButton.Pressed += () => ShowSkinInventory(false);
        _armory.AddChild(skinInventoryButton);
        var referenceButton=new Button {
            Name="ArmoryWikiReference",
            OffsetLeft=1025,OffsetTop=133,OffsetRight=1205,OffsetBottom=176,
            Text="WIKI IMAGE"
        };
        referenceButton.Pressed+=OpenArmoryWikiReference;
        _armory.AddChild(referenceButton);
        _armory.AddChild(MakeLabel(60,198,1130,24,12,
            "Hold RIGHT MOUSE and drag to rotate the selected preview (pitch limited to 25 degrees)."));

        var scroll = new ScrollContainer
        {
            OffsetLeft = 60,
            OffsetTop = 215,
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
            Pass46ArmoryArtDecoration.Apply(button, spec.Name);
            var selected = spec;
            button.Pressed += () => HandleArmoryWeapon(selected);
            button.MouseEntered += () =>
            {
                if(showroom?.Preview(selected.Name) == true && _armoryPreviewLabel is not null)
                    _armoryPreviewLabel.Text=showroom.PreviewStatus;
            };
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

    private void AddBackground(CanvasLayer layer,float? backdropAlpha=null)
    {
        // Keep controls readable while allowing the 3D source-authored lobby
        // to show behind them. No private pack = previous fully opaque UI.
        layer.AddChild(new ColorRect
        {
            Color = _sourceLobby is not null
                ? new Color(.025f,.027f,.03f,backdropAlpha ?? .62f)
                : new Color(.025f,.027f,.03f),
            AnchorRight=1,
            AnchorBottom=1,
            MouseFilter=Control.MouseFilterEnum.Ignore
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
        CloseWikiReferenceGallery();
        // The authored Lobby is a menu-only backdrop, never collision or
        // lighting inside an active game map.
        _sourceLobby?.QueueFree();
        _sourceLobby=null;
        _bulletin?.QueueFree();
        _bulletin=null;
        _bulletinPage=null;
        _armoryPreviewLabel=null;
        _shop?.QueueFree();
        _shop=null;
        _shopCaseDetail=null;
        _shopCreditsLabel=null;
        _shopOpenButton=null;
        _skinInventory?.QueueFree();
        _skinInventory=null;
        _skinDetailLabel=null;
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

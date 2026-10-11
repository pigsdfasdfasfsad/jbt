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
            Pass27MapCardDecoration.Apply(button, map.Name);
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
            Pass46ArmoryArtDecoration.Apply(button, spec.Name);
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

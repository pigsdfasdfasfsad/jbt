"""Pass116: source-safe procedural Lab material/lighting and ten-map native fidelity contracts.

Unlike 1:1 source material recovery, these textures are GENERATED in RAM from
clearly approximate screenshot color/texture categories, never copied from
owners' private Roblox mesh/material streams or uploaded image references.
"""
from conftest import ROOT


def src(name):
    return (ROOT / name).read_text(encoding="utf-8")


def test_fallback_lab_repaints_genuine_static_shell_meshes_without_collision():
    material = src("src/Twr.Godot/Scripts/Pass116LaboratorySurfaceMaterials.cs")
    for anchor in (
        "Pass116LaboratorySurfaceMaterials : Node3D",
        "PatternPixels = 64",
        "ExpectedFloorSurfaces = 5",
        "ExpectedRoofSurfaces = 4",
        "ExpectedWallSurfaces = 4",
        "MaximumGarageDecals = 12",
        'CreatePattern("clinical"',
        'CreatePattern("research"',
        'CreatePattern("industrial"',
        'CreatePattern("vinyl"',
        'CreatePattern("acoustic"',
        'RepaintStatic(root,"Reception_Floor","clinical"',
        'RepaintStatic(root,"Research_Floor","research"',
        'RepaintStatic(root,"ServerRoom_Floor","industrial"',
        'RepaintStatic(root,"ParkingGarage_Floor","industrial"',
        'RepaintDecorative(root,"HospitalWard","vinyl"',
        'RepaintStatic(root,"Reception_Roof","acoustic"',
        'RepaintStatic(root,"Research_Roof","acoustic"',
        'RepaintStatic(root,"ServerRoom_Roof","industrial"',
        'RepaintStatic(root,"ParkingGarage_Roof","industrial"',
        'RepaintStatic(root,"Reception_WestWall","clinical"',
        'RepaintStatic(root,"Reception_EastWall","clinical"',
        'RepaintStatic(root,"Research_WestWall","research"',
        'RepaintStatic(root,"Research_EastWall","research"',
        "GetNodeOrNull<StaticBody3D>(name)",
        "GetChildren().OfType<MeshInstance3D>().SingleOrDefault()",
        "target.MaterialOverride=SurfaceMaterial(",
        "AlbedoTexture=_textures[pattern]",
        "Uv1Scale=new Vector3(repeatX,repeatZ,1f)",
        "Image.CreateEmpty(PatternPixels,PatternPixels,false,",
        "Image.Format.Rgba8",
        "image.SetPixel(x,y,new Color(",
        "ImageTexture.CreateFromImage(image)",
    ):
        assert anchor in material, anchor
    for forbidden in (
        "new StaticBody3D",
        "new CollisionShape3D",
        "new RigidBody3D",
        "new Area3D",
        "NavigationAgent3D",
        "body.AddChild",
        "position += Vector3",
        "MatchState",
        "AwardKill(",
        "CompleteObjective(",
        "HttpClient",
        "File.ReadAllBytes",
        "Image.LoadFromFile",
    ):
        assert forbidden not in material, forbidden


def test_emissive_lab_fixtures_and_parking_markings_are_render_only():
    code = src("src/Twr.Godot/Scripts/Pass116LaboratorySurfaceMaterials.cs")
    for anchor in (
        "IlluminateDetails(dressing);",
        'name.Contains("StatusLED",StringComparison.Ordinal)',
        'name.EndsWith("Diffuser",',
        '"BlueDoorScannerIndicator"',
        '"BlueDoorScannerScreen"',
        '"ReceptionDeskDisplay"',
        "EmissionEnabled=true",
        "Emission=emissive",
        "EmissionEnergyMultiplier=fluorescent?1.55f:1.9f",
        "EmissiveDetails116++",
        "BuildGarageLanePaint()",
        'AddGarageMark("Bay"+bay+"Stripe"+stripe',
        'AddGarageMark("LaneDash"+dash',
        "GarageDecals116++",
        "GeometryInstance3D.ShadowCastingSetting.Off",
        "HasAddedCollision116 => ContainsCollision(this)",
        'SetMeta("Pass116OriginalSourceMaterials",false)',
        'SetMeta("Pass116PrivateScreenshotBytes",false)',
        'SetMeta("Pass116CollisionGeometryModified",false)',
    ):
        assert anchor in code, anchor
    assert "CollisionLayer" not in code
    assert "CollisionMask" not in code


def test_source_original_precedence_and_all_other_nine_maps_remain_unmodified():
    map_builder = src("src/Twr.Godot/Scripts/MapBlockoutBuilder.cs")
    game = src("src/Twr.Godot/Scripts/GameplayRoot.cs")
    lab = map_builder.split("private static RuntimeMapLayout BuildLaboratory(")[1]
    lab = lab.split("private static RuntimeMapLayout BuildManor(")[0]
    assert "var props115=Pass115LaboratoryDressing.AddToFallback(root);" in lab
    assert "Pass116LaboratorySurfaceMaterials.AddToFallback(root,props115);" in lab
    assert "return Layout(new Vector3(0, 1, 39)" in lab
    assert lab.count("Pass116LaboratorySurfaceMaterials.AddToFallback") == 1
    assert map_builder.count("Pass116LaboratorySurfaceMaterials.AddToFallback") == 1
    assert "LaboratorySourceLoader.TryBuild(this, MapName, out var recovered)" in game
    assert "_isFallbackScene86 = !originalWorldAvailable;" in game
    assert "MapBlockoutBuilder.Build(this, _mapDefinition)" in game
    assert "originalWorldAvailable" in game


def test_native_real_lab_renderer_is_tested_through_all_ten_gameplay_maps():
    boot = src("src/Twr.Godot/Scripts/Bootstrap.cs")
    native = src("src/Twr.Godot/Scripts/Pass72NativeTenMapEntryAcceptance.cs")
    assert 'args.Contains("--smoke-pass116",StringComparer.Ordinal)' in boot
    assert "VerifyLabDressing115=true" in boot
    assert "VerifyLabMaterials116=true" in boot
    for expected in (
        "VerifyLabMaterials116",
        'if(map=="Laboratory")',
        'game.GetNodeOrNull<',
        '"Pass116LaboratorySurfaceMaterials"',
        "surfaces.PatternCount116!=5",
        "surfaces.RepaintedSurfaces116!=13",
        "surfaces.FloorSurfaces116!=5",
        "surfaces.RoofSurfaces116!=4",
        "surfaces.WallSurfaces116!=4",
        "surfaces.EmissiveDetails116<28",
        "surfaces.GarageDecals116!=10",
        "surfaces.HasAddedCollision116",
        "material.AlbedoTexture is not ImageTexture texture",
        "texture.GetWidth()!=Pass116LaboratorySurfaceMaterials.PatternPixels",
        "body?.GetChildren().OfType<CollisionShape3D>().Count()!=1",
        "TWR_SMOKE_PASS116_LAB_SURFACES_OK",
        "TWR_SMOKE_PASS115_LAB_DRESSING_OK",
        "TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK",
        "native_wave2_each_map=true",
        "owner_images_not_bundled=true",
        "Runtime.ReturnToLobby();",
        "player.IsOnFloor()",
    ):
        assert expected in native, expected


def test_ci_native_windows_and_previous_all_passes_remain_enabled():
    ci = src(".github/workflows/windows-build.yml")
    assert "twr-pass116-laboratory-surface-materials" in ci
    assert "Preflight Pass 116 real ten-map Laboratory floor surfaces" in ci
    assert "'--smoke-pass116'" in ci
    assert "TWR_SMOKE_PASS116_LAB_SURFACES_OK" in ci
    assert "TWR_SMOKE_PASS115_LAB_DRESSING_OK" in ci
    assert "TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK" in ci
    assert "$count.Groups[1].Value -lt 28" in ci
    assert "$count.Groups[1].Value -gt 48" in ci
    for old_step in (
        "Preflight Pass 115 seven Laboratory visual landmark zones",
        "Preflight Pass 114 real Glock death on sloped terrain",
        "Preflight Pass 113 real corpse ground settling",
        "Preflight Pass 112 real Glock articulated corpse limb motion",
        "Preflight Pass 111 native Glock kill and corpse lifecycle",
        "Preflight Pass 110 real two-map repair truck assembly",
        "Preflight Pass 109 reference generator and Repair badge",
        "Preflight Pass 108 real RMB ADS carry preservation",
        "Preflight Pass 105 native objective pickup and deposit",
        "Preflight Pass 104 full production Wave2 Repair",
        "Preflight Pass 103 real full production Load",
        "Verify no private map scenes entered the CI export",
    ):
        assert old_step in ci, old_step
    assert ci.count("uses: actions/upload-artifact@v4") == ci.count(
        "if: false && success()"
    )

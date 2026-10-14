using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Pass72: actual native Godot world construction, grounded first-person
/// entry and Wave 1/2 actor lifecycle on all TEN release maps. Unlike the
/// earlier domain-only maps=10/waves=150 test, this loads real gameplay
/// scenes, colliders, pickup actors and infected. It does not verify the
/// owner-only original retail geometry or a human-played full match.
/// </summary>
public partial class Pass72NativeTenMapEntryAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public const string SuccessMarker = "TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK";
    private int _verified;
    public bool VerifyLabDressing115 { get; set; }
    public bool VerifyLabMaterials116 { get; set; }
    private int _labMaterialScenes116;
    private int _labMaterialSurfaces116;
    private int _labEmissiveDetails116;
    private int _labGarageLines116;
    private int _labDressed115;
    private int _labMeshCount115;
    private int _labLights115;

    public override async void _Ready()
    {
        try
        {
            if (Runtime.Session is null ||
                Runtime.Match?.Phase != MatchPhase.Lobby ||
                ReleaseRules.Maps.Length != 10 ||
                MapCatalogRuntime.All().Count != 10)
                throw new InvalidOperationException(
                    "Pass72 requires ten catalogued maps and isolated lobby session");
            foreach (var map in ReleaseRules.Maps)
                await ValidateMap(map);

            if (_verified != ReleaseRules.Maps.Length)
                throw new InvalidOperationException(
                    "Pass72 did not complete all ten native map lifecycles");

            if(VerifyLabDressing115)
            {
                if(_labDressed115!=1 || _labMeshCount115<140 ||
                   _labMeshCount115>Pass115LaboratoryDressing.MaxVisualMeshes ||
                   _labLights115<8)
                    throw new InvalidOperationException(
                        "Pass115 Laboratory screenshot-guided scene missing"+
                        " lab_scenes="+_labDressed115+
                        " meshes="+_labMeshCount115+
                        " local_lights="+_labLights115);
                GD.Print("TWR_SMOKE_PASS115_LAB_DRESSING_OK"+
                    " maps=10"+
                    " lab_fallback_scenes="+_labDressed115+
                    " landmark_zones=7"+
                    " visual_meshes="+_labMeshCount115+
                    " local_lights="+_labLights115+
                    " other_maps_undressed=9"+
                    " zero_new_colliders=true"+
                    " two_native_waves_each=true"+
                    " original_owner_assets_excluded=true"+
                    " screenshot_exact_fidelity_unverified=true");
            }
            if(VerifyLabMaterials116)
            {
                if(!VerifyLabDressing115 ||
                   _labMaterialScenes116!=1 ||
                   _labMaterialSurfaces116!=13 ||
                   _labEmissiveDetails116<28 ||
                   _labGarageLines116!=10)
                    throw new InvalidOperationException(
                        "Pass116 procedural Laboratory surface verification incomplete");
                GD.Print("TWR_SMOKE_PASS116_LAB_SURFACES_OK"+
                    " maps=10"+
                    " lab_material_scenes="+_labMaterialScenes116+
                    " procedural_patterns=5"+
                    " repainted_structural_surfaces="+_labMaterialSurfaces116+
                    " emissive_fixture_parts="+_labEmissiveDetails116+
                    " garage_decal_meshes="+_labGarageLines116+
                    " other_maps_unchanged=9"+
                    " zero_new_colliders=true"+
                    " original_geometry_unmodified=true"+
                    " native_wave2_each_map=true"+
                    " owner_images_not_bundled=true"+
                    " pixel_parity_unverified=true");
            }
            GD.Print(SuccessMarker + " maps=" + _verified +
                " actual_gameplay_roots=true" +
                " native_player_grounding=true" +
                " active_pickup_actors=true" +
                " real_first_wave_infected=true" +
                " actual_wave2_actor_spawns=true" +
                " authoritative_wave_survival_rewards=true" +
                " roundtrip_to_isolated_lobby=true" +
                " source_mesh_parity_unverified=true" +
                " no_manual_fifteen_wave_claim=true");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS72_TEN_MAP_ENTRY_FAILED " +
                "verified=" + _verified + " " + ex.GetType().Name +
                " " + ex.Message + "\n" + ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task PhysicsFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async Task ValidateMap(string map)
    {
        if (Runtime.Match?.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass72 previous game failed to restore lobby before " + map);

        Runtime.StartMap(map);
        var game = new GameplayRoot
        {
            Name = "Pass72Native" + map,
            Runtime = Runtime,
            MapName = map
        };
        AddChild(game);

        if (VerifyLabDressing115)
        {
            var visual=game.GetNodeOrNull<Pass115LaboratoryDressing>(
                "Pass115LaboratoryDressing");
            if(map=="Laboratory")
            {
                if(visual is null ||
                   visual.LandmarkCount!=Pass115LaboratoryDressing.ExpectedLandmarks ||
                   visual.VisualMeshCount<140 ||
                   visual.VisualMeshCount>Pass115LaboratoryDressing.MaxVisualMeshes ||
                   visual.LocalLightCount<8 ||
                   visual.HasAnyCollisionObjects ||
                   !visual.HasMeta("Pass115OwnerOriginalGeometry") ||
                   visual.GetMeta("Pass115OwnerOriginalGeometry").AsBool() ||
                   !visual.HasMeta("Pass115SourceReferencesBundled") ||
                   visual.GetMeta("Pass115SourceReferencesBundled").AsBool())
                    throw new InvalidOperationException(
                        "Pass115 source-absent Laboratory should have readable"+
                        " seven-zone noncolliding render-only scene");
                foreach(var landmark in new []
                {
                    "ReceptionDressing","ServerDressing","ResearchDressing",
                    "ExaminationDressing","GarageDressing","WardDressing",
                    "CafeteriaDressing"
                })
                {
                    if(!visual.VisualMeshesByLandmark.TryGetValue(
                           landmark,out var count) || count<12 ||
                       visual.GetNodeOrNull<Node3D>(landmark) is null)
                        throw new InvalidOperationException(
                            "Pass115 missing readable Laboratory landmark "+landmark);
                }
                _labDressed115++;
                _labMeshCount115=visual.VisualMeshCount;
                _labLights115=visual.LocalLightCount;
            }
            else if(visual is not null)
                throw new InvalidOperationException(
                    "Pass115 Laboratory props leaked into "+map);
        }

        if(VerifyLabMaterials116)
        {
            var surfaces=game.GetNodeOrNull<
                Pass116LaboratorySurfaceMaterials>(
                "Pass116LaboratorySurfaceMaterials");
            if(map=="Laboratory")
            {
                if(surfaces is null ||
                   surfaces.PatternCount116!=5 ||
                   surfaces.RepaintedSurfaces116!=13 ||
                   surfaces.FloorSurfaces116!=5 ||
                   surfaces.RoofSurfaces116!=4 ||
                   surfaces.WallSurfaces116!=4 ||
                   surfaces.EmissiveDetails116<28 ||
                   surfaces.EmissiveDetails116>48 ||
                   surfaces.GarageDecals116!=10 ||
                   surfaces.HasAddedCollision116 ||
                   !surfaces.HasMeta("Pass116OriginalSourceMaterials") ||
                   surfaces.GetMeta("Pass116OriginalSourceMaterials").AsBool() ||
                   !surfaces.HasMeta("Pass116PrivateScreenshotBytes") ||
                   surfaces.GetMeta("Pass116PrivateScreenshotBytes").AsBool() ||
                   !surfaces.HasMeta("Pass116CollisionGeometryModified") ||
                   surfaces.GetMeta("Pass116CollisionGeometryModified").AsBool())
                    throw new InvalidOperationException(
                        "Pass116 missing source-safe Laboratory tile/glow/markings");
                foreach(var floor in new []{
                    "Reception_Floor","Research_Floor","ServerRoom_Floor",
                    "ParkingGarage_Floor"
                })
                {
                    var body=game.GetNodeOrNull<StaticBody3D>(floor);
                    var renderer=body?.GetChildren()
                        .OfType<MeshInstance3D>().SingleOrDefault();
                    if(renderer?.MaterialOverride is not StandardMaterial3D material ||
                       material.AlbedoTexture is not ImageTexture texture ||
                       texture.GetWidth()!=Pass116LaboratorySurfaceMaterials.PatternPixels ||
                       texture.GetHeight()!=Pass116LaboratorySurfaceMaterials.PatternPixels ||
                       body?.GetChildren().OfType<CollisionShape3D>().Count()!=1)
                        throw new InvalidOperationException(
                            "Pass116 original Lab floor collider or procedural UV"+
                            " material missing: "+floor);
                }
                _labMaterialScenes116++;
                _labMaterialSurfaces116=surfaces.RepaintedSurfaces116;
                _labEmissiveDetails116=surfaces.EmissiveDetails116;
                _labGarageLines116=surfaces.GarageDecals116;
            }
            else if(surfaces is not null)
                throw new InvalidOperationException(
                    "Pass116 Lab-only surface renderer leaked to "+map);
        }

        var player = game.GetNodeOrNull<FirstPersonPlayer>("Player");
        var hud = game.GetNodeOrNull<GameplayHud>("HUD");
        if (player is null || player.CaptureCamera is null ||
            hud is null || game.GetNodeOrNull<Node>("Fortifications") is null ||
            Runtime.Match?.MapName != map || Runtime.Match.Wave != 1 ||
            Runtime.Player is null || !Runtime.Player.IsAlive ||
            game.ActivePickupCount < 1)
            throw new InvalidOperationException(
                "Pass72 gameplay actor, HUD, pickup or Wave 1 startup missing: " + map);

        // There are no synthetic platforms or teleports in this test.
        // The actual configured map spawn must settle on its existing
        // Godot collider, not fall through a missing base floor.
        await PhysicsFrames(48);
        var at = player.GlobalPosition;
        if (!float.IsFinite(at.X) || !float.IsFinite(at.Y) ||
            !float.IsFinite(at.Z) || !player.IsOnFloor() ||
            !Runtime.Player.IsAlive)
            throw new InvalidOperationException(
                "Pass72 first-person source/fallback spawn is not physically grounded: " +
                map + " position=" + at + " grounded=" + player.IsOnFloor());

        // Fast-forward only the wave STAGING clock. Actor creation, cleanup,
        // camera/physics and authoritative rewards remain production paths.
        game._Process(RegularWaveRules.CountdownSeconds + .05);
        game._Process(.05);
        var wave1 = game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(x => GodotObject.IsInstanceValid(x));
        if (game.WaveStage != "WAVE" ||
            wave1 is null || wave1.Target != player ||
            game.ActiveInfectedCount < 1 ||
            Runtime.Match.Wave != 1)
            throw new InvalidOperationException(
                "Pass72 native Wave 1 failed to instantiate actual infected: " + map);
        await PhysicsFrames(3);

        game._Process(RegularWaveRules.WaveDurationSeconds + .2);
        if (game.WaveStage != "WAVE END" ||
            game.ActiveInfectedCount != 0 ||
            Runtime.Match.Wave != 1)
            throw new InvalidOperationException(
                "Pass72 Wave 1 survival cleanup was not executed: " + map);

        game._Process(RegularWaveRules.WaveEndSeconds + .2);
        if (game.WaveStage != "INTERMISSION" || Runtime.Match.Wave != 2)
            throw new InvalidOperationException(
                "Pass72 real domain and scene failed Wave 2 advance: " + map);

        game._Process(RegularWaveRules.IntermissionSeconds + .2);
        if (game.WaveStage != "COUNTDOWN")
            throw new InvalidOperationException(
                "Pass72 Wave 2 countdown missing: " + map);
        game._Process(RegularWaveRules.CountdownSeconds + .2);
        game._Process(.05);
        var wave2 = game.GetChildren().OfType<InfectedAgent>()
            .FirstOrDefault(x => GodotObject.IsInstanceValid(x));
        if (game.WaveStage != "WAVE" || Runtime.Match.Wave != 2 ||
            wave2 is null || wave2.Target != player ||
            game.ActiveInfectedCount < 1 || !Runtime.Player.IsAlive)
            throw new InvalidOperationException(
                "Pass72 second live wave failed to spawn infected: " + map);

        GD.Print("TWR_PASS72_MAP_OK map=" + map +
            " grounded=true native_pickups=true wave2_infected=true" +
            " map_load_ms=" + game.MapLoadMilliseconds);

        // Controlled TEST-ONLY teardown through the original match failure
        // and results-save authority. Never mutate MatchState directly or
        // write to the user's normal profile.json file.
        Runtime.FailMatch("Pass72 test-only map scene teardown");
        if (Runtime.Match.Phase != MatchPhase.Results ||
            !Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass72 failed to save test-only match teardown: " + map);
        game.QueueFree();
        await PhysicsFrames(3);
        Runtime.ReturnToLobby();
        if (Runtime.Match.Phase != MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass72 could not return to isolated lobby: " + map);
        _verified++;
    }
}

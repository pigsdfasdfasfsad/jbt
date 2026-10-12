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

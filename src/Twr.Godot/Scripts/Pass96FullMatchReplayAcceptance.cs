using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;
using Twr.Domain.Persistence;

namespace Twr.Godot;

/// <summary>
/// Pass96 exported-Windows regression for a production Ranch GameplayRoot:
/// stage through 15 real Regular waves, save the completed profile, then
/// replay Wave1 on the SAME map and verify rewards remain available.
/// Only test clocks are accelerated; real game spawning, stage transitions,
/// match commands, domain accounting, and JSON persistence stay in place.
/// Not proof of fifteen human-played five-minute waves.
/// </summary>
public partial class Pass96FullMatchReplayAcceptance : Node
{
    public LocalSessionNode Runtime { get; set; } = null!;
    private const string Map = "Ranch";
    private const string ProfilePath =
        "user://pass96-full-match-replay-smoke-profile.json";
    private int _completionEvents;

    public override async void _Ready()
    {
        try
        {
            await VerifyFullMatchAndReplay();
            GD.Print("TWR_SMOKE_PASS96_FULL_MATCH_REPLAY_OK"+
                " map=Ranch first_run_waves=15"+
                " replay_wave1_paid_again=true"+
                " duplicate_wave_claim_blocked=true"+
                " premature_wave15_claim_blocked=true"+
                " persisted_profile_verified=true"+
                " actual_infected_physics=true"+
                " original_five_minute_survival_unverified=true");
            GetTree().Quit(0);
        }
        catch(Exception ex)
        {
            GD.PushError("TWR_SMOKE_PASS96_FULL_MATCH_REPLAY_FAILED "+
                ex.GetType().Name+" "+ex.Message+"\n"+ex.StackTrace);
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int n)
    {
        for(var i=0;i<n;i++)
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }

    private static (int credits,int xp,int level) ExpectedWave(
        int priorCredits,int priorXp,int priorLevel,int wave)
    {
        // Source-verified single-player Bonuses arithmetic, including
        // source-verified XP thresholds and level-up credit increments.
        var credits=priorCredits+(int)Math.Floor(wave*1500.0+.5);
        var xp=priorXp+(int)Math.Floor(190.0*Math.Pow(wave,.17)+.5);
        var level=priorLevel;
        while(xp>=ProgressionRules.RequiredForNextLevel(level))
        {
            xp-=ProgressionRules.RequiredForNextLevel(level);
            level++;
            credits+=ProgressionRules.CreditRewardForLevel(level);
        }
        return (credits,xp,level);
    }

    private static void AssertAccounting(
        LocalSessionNode runtime,(int credits,int xp,int level) expected,
        string stage)
    {
        if(runtime.Player is null ||
           runtime.Player.Credits!=expected.credits ||
           runtime.Player.Xp!=expected.xp ||
           runtime.Player.Level!=expected.level)
            throw new InvalidOperationException(
                "Pass96 authoritative economy mismatch "+stage+
                " actual="+runtime.Player?.Credits+"/"+runtime.Player?.Xp+
                "/"+runtime.Player?.Level+
                " expected="+expected.credits+"/"+expected.xp+
                "/"+expected.level);
    }

    private static void AssertSavedProfile(
        LocalSessionNode runtime,string stage)
    {
        var persisted=new JsonProfileStore(
            ProjectSettings.GlobalizePath(ProfilePath)).Load();
        if(runtime.Player is null ||
           persisted.Credits!=runtime.Player.Credits ||
           persisted.Xp!=runtime.Player.Xp ||
           persisted.Level!=runtime.Player.Level)
            throw new InvalidOperationException(
                "Pass96 JSON profile not atomically saved "+stage+
                " persisted="+persisted.Credits+"/"+persisted.Xp+
                "/"+persisted.Level+
                " live="+runtime.Player?.Credits+"/"+runtime.Player?.Xp+
                "/"+runtime.Player?.Level);
    }

    private async Task<GameplayRoot> CreateActualMap()
    {
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass96 must begin from offline Lobby");
        Runtime.StartMap(Map);
        var game=new GameplayRoot
        {
            Name="Pass96Production"+Map, MapName=Map, Runtime=Runtime
        };
        AddChild(game);
        var player=game.GetNodeOrNull<FirstPersonPlayer>("Player");
        if(player is null ||
           game.GetNodeOrNull<GameplayHud>("HUD") is null ||
           Runtime.Match?.Wave!=1 ||
           Runtime.Player?.IsAlive!=true ||
           game.ActivePickupCount<1)
            throw new InvalidOperationException(
                "Pass96 required real player, HUD, loot and Wave1");
        for(var i=0;i<80 && !player.IsOnFloor();i++)
            await Frames(1);
        if(!player.IsOnFloor() || game.WaveStage!="COUNTDOWN")
            throw new InvalidOperationException(
                "Pass96 player did not land on native map collision");
        return game;
    }

    private async Task VerifyFullMatchAndReplay()
    {
        if(Runtime.Session is null ||
           Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass96 requires isolated offline match profile");
        Runtime.PresentationEvent += name =>
        {
            if(name=="MatchCompletedEvent")_completionEvents++;
        };
        var game=await CreateActualMap();
        var infectedVerified=0;
        for(var wave=1;wave<=ReleaseRules.MaxWaves;wave++)
        {
            if(Runtime.Match?.Wave!=wave ||
               Runtime.Match.Phase!=MatchPhase.Wave ||
               game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass96 countdown/authoritative wave mismatch "+wave);
            game._Process(RegularWaveRules.CountdownSeconds+.25);
            if(game.WaveStage!="WAVE" || Runtime.Match.Wave!=wave)
                throw new InvalidOperationException(
                    "Pass96 production wave did not start "+wave);
            // The actual GameRoot spawner and physics CharacterBody3D
            // remain active. Do not award kills or spoof zombie counts.
            game._Process(.05);
            await Frames(2);
            if(game.ActiveInfectedCount<1)
                throw new InvalidOperationException(
                    "Pass96 no real wave infected "+wave);
            infectedVerified++;

            var current=Runtime.Player!;
            var expected=ExpectedWave(current.Credits,current.Xp,
                current.Level,wave);
            if(wave==1)
            {
                // Cannot claim future wave rewards before surviving.
                var before=(
                    current.Credits,current.Xp,current.Level);
                Runtime.AwardWaveSurvival(15,0,1);
                AssertAccounting(Runtime,before,"early Wave15 claim");
                var denied=false;
                try{Runtime.StartMap("Cabin");}
                catch(InvalidOperationException){denied=true;}
                if(!denied || Runtime.Match?.MapName!=Map ||
                   Runtime.Match.Wave!=1)
                    throw new InvalidOperationException(
                        "Pass96 mid-match restart did not reject receipt reset");
            }
            game._Process(RegularWaveRules.WaveDurationSeconds+.25);
            if(game.WaveStage!="WAVE END" ||
               Runtime.Match?.Wave!=wave ||
               game.ActiveInfectedCount!=0)
                throw new InvalidOperationException(
                    "Pass96 real WaveEnd/cleanup failed "+wave);
            AssertAccounting(Runtime,expected,"first-run wave "+wave);
            AssertSavedProfile(Runtime,"first-run wave "+wave);
            // Domain receipt remains single-use DURING this match.
            Runtime.AwardWaveSurvival(wave,0,1);
            AssertAccounting(Runtime,expected,"duplicate wave "+wave);
            GD.Print("TWR_PASS96_WAVE_OK wave="+wave+
                " map="+Map+
                " credits="+Runtime.Player!.Credits+
                " xp="+Runtime.Player.Xp+
                " level="+Runtime.Player.Level+
                " duplicate_paid=false");
            game._Process(RegularWaveRules.WaveEndSeconds+.25);
            if(wave==ReleaseRules.MaxWaves)
            {
                if(Runtime.Match?.Phase!=MatchPhase.Results ||
                   !Runtime.Match.SaveCommitted ||
                   !Runtime.Match.CompletionAwarded ||
                   game.WaveStage!="RESULTS" ||
                   _completionEvents!=1)
                    throw new InvalidOperationException(
                        "Pass96 missing genuine Wave15 result/save event"+
                        " events="+_completionEvents+
                        " phase="+Runtime.Match?.Phase);
                AssertSavedProfile(Runtime,"completed full map");
                break;
            }
            if(game.WaveStage!="INTERMISSION" ||
               Runtime.Match?.Wave!=wave+1)
                throw new InvalidOperationException(
                    "Pass96 intermission/advance failed "+wave);
            game._Process(RegularWaveRules.IntermissionSeconds+.25);
            if(game.WaveStage!="COUNTDOWN")
                throw new InvalidOperationException(
                    "Pass96 next countdown failed "+wave);
        }
        if(infectedVerified!=ReleaseRules.MaxWaves)
            throw new InvalidOperationException(
                "Pass96 not all 15 real waves spawned infected");
        var completedCredit=Runtime.Player!.Credits;
        var completedXp=Runtime.Player.Xp;
        var completedLevel=Runtime.Player.Level;
        Runtime.AdvanceWave();
        if(_completionEvents!=1 || Runtime.Player.Credits!=completedCredit ||
           Runtime.Player.Xp!=completedXp ||
           Runtime.Player.Level!=completedLevel)
            throw new InvalidOperationException(
                "Pass96 duplicated map completion/XP after Results");
        GD.Print("TWR_PASS96_FIFTEEN_WAVES_OK"+
            " map="+Map+" waves=15"+
            " completion_events=1 committed=true"+
            " persisted_credits="+completedCredit);

        game.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby)
            throw new InvalidOperationException(
                "Pass96 completed match failed return-to-lobby");

        // A second REAL GameplayRoot on the same map within the SAME
        // LocalSession must award Wave1 credits/XP again. This detects
        // the original cross-run persistent ReceiptLedger bug.
        var replay=await CreateActualMap();
        if(Runtime.Match?.CompletedObjectives.Count!=0 ||
           Runtime.Match.CompletionAwarded ||
           Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass96 new match retained prior match state");
        var expectedReplay=ExpectedWave(
            completedCredit,completedXp,completedLevel,1);
        replay._Process(RegularWaveRules.CountdownSeconds+.25);
        replay._Process(.05);
        await Frames(2);
        if(replay.ActiveInfectedCount<1)
            throw new InvalidOperationException(
                "Pass96 same-map replay has no actual infected");
        replay._Process(RegularWaveRules.WaveDurationSeconds+.25);
        if(replay.WaveStage!="WAVE END" ||
           Runtime.Match?.Wave!=1)
            throw new InvalidOperationException(
                "Pass96 replay Wave1 failed to survive");
        AssertAccounting(Runtime,expectedReplay,
            "second run Wave1 rewarded again");
        AssertSavedProfile(Runtime,"replayed Wave1");
        Runtime.AwardWaveSurvival(1,0,1);
        AssertAccounting(Runtime,expectedReplay,
            "replay Wave1 duplicate blocked");
        GD.Print("TWR_PASS96_REPLAY_WAVE1_OK"+
            " same_map=Ranch earned_again=true"+
            " duplicate_paid=false"+
            " saved=true"+
            " credits="+Runtime.Player!.Credits+
            " xp="+Runtime.Player.Xp);

        Runtime.FailMatch("Pass96 isolated replay verified");
        if(Runtime.Match?.Phase!=MatchPhase.Results ||
           !Runtime.Match.SaveCommitted)
            throw new InvalidOperationException(
                "Pass96 replay did not commit results profile");
        AssertSavedProfile(Runtime,"replay after failure");
        replay.QueueFree();
        await Frames(3);
        Runtime.ReturnToLobby();
        if(Runtime.Match?.Phase!=MatchPhase.Lobby ||
           _completionEvents!=1)
            throw new InvalidOperationException(
                "Pass96 replay failed final lobby transition");
    }
}

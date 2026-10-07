using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class WaveRewardService(ReceiptLedger receipts, EventStream events)
{
    public bool Award(PlayerState player,string mapName,int wave,int completedObjectives,int playerCount,DateTimeOffset now)
    {
        if (wave < 1 || wave > ReleaseRules.MaxWaves) throw new ArgumentOutOfRangeException(nameof(wave));
        if (completedObjectives < 0) throw new ArgumentOutOfRangeException(nameof(completedObjectives));
        if (playerCount < 1) throw new ArgumentOutOfRangeException(nameof(playerCount));

        var receipt = $"WaveSurvival:{mapName}:{wave}";
        if (!receipts.TryIssue(receipt)) return false;

        // VERIFIED from ModuleScript.Bonuses.Source.txt for Regular mode.
        var credits = (int)Math.Floor(wave * Math.Pow(playerCount, 0.055) * 1500.0 + 0.5);
        var xp = (int)Math.Floor(190.0 * Math.Pow(wave, 0.17) + 0.5);
        var objectiveCredits = completedObjectives * 200;
        var objectiveXp = completedObjectives * 400;

        player.Credits += credits + objectiveCredits;
        player.Xp += xp + objectiveXp;
        events.Publish(new WaveRewardedEvent(wave, credits, xp, objectiveCredits, objectiveXp, RegularWaveRules.DifficultyScale(wave), now));
        return true;
    }
}

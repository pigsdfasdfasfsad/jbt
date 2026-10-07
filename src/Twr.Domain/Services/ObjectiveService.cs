using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;
using Twr.Domain.Policies;

namespace Twr.Domain.Services;

public sealed class ObjectiveService(ObjectiveRewardPolicy rewards, EventStream events)
{
    public bool Complete(MatchState match, PlayerState player, string id, string family, DateTimeOffset now)
    {
        if (!match.CompletedObjectives.Add(id))
            return false;

        var (credits, xp) = rewards.Reward(family);
        player.Credits += credits;
        player.Xp += xp;

        events.Publish(new ObjectiveCompletedEvent(id, now));
        events.Publish(new ObjectiveRewardedEvent(id, family, credits, xp, player.Credits, player.Xp, now));
        return true;
    }
}

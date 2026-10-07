using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class KillRewardService(EventStream events)
{
    public void Award(PlayerState player, string infectedType, int credits, int xp, DateTimeOffset now)
    {
        if (credits < 0 || xp < 0)
            throw new ArgumentOutOfRangeException(nameof(credits));

        player.Credits += credits;
        player.Xp += xp;
        events.Publish(new KillRewardedEvent(infectedType, credits, xp, player.Credits, player.Xp, now));
    }
}

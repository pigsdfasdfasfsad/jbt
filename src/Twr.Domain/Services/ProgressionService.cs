using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class ProgressionService(EventStream events)
{
    public int Apply(PlayerState player,DateTimeOffset now)
    {
        var advanced=0;
        while(player.Xp>=ProgressionRules.RequiredForNextLevel(player.Level))
        {
            var required=ProgressionRules.RequiredForNextLevel(player.Level);
            player.Xp-=required;
            player.Level++;
            var credits=ProgressionRules.CreditRewardForLevel(player.Level);
            player.Credits+=credits;
            advanced++;
            events.Publish(new LevelAdvancedEvent(player.Level,credits,player.Credits,player.Xp,now));
        }
        return advanced;
    }
}

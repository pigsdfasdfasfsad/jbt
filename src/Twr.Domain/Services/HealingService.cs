using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class HealingService(EventStream events)
{
    public bool Apply(PlayerState player,float amount,bool full,DateTimeOffset now)
    {
        if(!player.IsAlive || player.Health>=player.MaxHealth) return false;
        var before=player.Health;
        player.Health=full?player.MaxHealth:Math.Min(player.MaxHealth,player.Health+Math.Max(0,amount));
        var healed=player.Health-before;
        if(healed<=0)return false;
        events.Publish(new PlayerHealedEvent(healed,player.Health,now));
        return true;
    }

    public bool EquipBodyArmor(PlayerState player,DateTimeOffset now)
    {
        if(player.ArmorDurability>15)return false;
        player.ArmorDurability=40;
        events.Publish(new ArmorChangedEvent(player.ArmorDurability,now));
        return true;
    }
}

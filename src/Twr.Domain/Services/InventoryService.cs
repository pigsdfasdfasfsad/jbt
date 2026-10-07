using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class InventoryService(EventStream events)
{
    public bool Grant(PlayerState player,string item,int amount,int maxCount,DateTimeOffset now)
    {
        if(amount<=0 || maxCount<=0)return false;
        var current=player.Inventory.GetValueOrDefault(item);
        var next=Math.Min(maxCount,current+amount);
        if(next==current)return false;
        player.Inventory[item]=next;
        events.Publish(new InventoryChangedEvent(item,next,now));
        return true;
    }

    public bool Consume(PlayerState player,string item,int amount,DateTimeOffset now)
    {
        if(amount<=0)return false;
        var current=player.Inventory.GetValueOrDefault(item);
        if(current<amount)return false;
        player.Inventory[item]=current-amount;
        events.Publish(new InventoryChangedEvent(item,player.Inventory[item],now));
        return true;
    }
}

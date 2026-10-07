using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class InventoryService(EventStream events)
{
    public void Grant(PlayerState player,string item,int amount,DateTimeOffset now)
    {
        if(amount<=0)return;
        player.Inventory[item]=player.Inventory.GetValueOrDefault(item)+amount;
        events.Publish(new InventoryChangedEvent(item,player.Inventory[item],now));
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

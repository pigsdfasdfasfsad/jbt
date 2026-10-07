using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class ArmoryService(EventStream events)
{
    public bool Purchase(Profile profile,PlayerState player,string weapon,int requiredLevel,int price,DateTimeOffset now)
    {
        if(profile.Unlocks.Contains(weapon))return false;
        if(requiredLevel<1 || price<=0 || player.Level<requiredLevel || player.Credits<price)return false;

        // Source documents inflated early purchases but does not provide a
        // universal recoverable price function/entry point. Do not invent one.
        player.Credits-=price;
        profile.Unlocks.Add(weapon);
        events.Publish(new PurchaseCompletedEvent(weapon,player.Credits,now));
        return true;
    }

    public bool Equip(Profile profile,string slot,string weapon,DateTimeOffset now)
    {
        if(slot is not ("Primary" or "Secondary" or "Melee"))return false;
        if(!profile.Unlocks.Contains(weapon))return false;
        profile.Loadout[slot]=weapon;
        events.Publish(new LoadoutChangedEvent(slot,weapon,now));
        return true;
    }
}

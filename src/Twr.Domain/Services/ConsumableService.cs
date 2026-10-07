using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class ConsumableService(EventStream events)
{
    public bool UseEnergyDrink(PlayerState player,bool caffeinated,DateTimeOffset now)
    {
        var duration=caffeinated ? 40.0 : 30.0;
        if(player.EnergyDrinkSeconds>=duration-0.01)return false;
        player.EnergyDrinkSeconds=duration;
        events.Publish(new StatusEffectChangedEvent("Energy Drink",duration,true,now));
        return true;
    }

    public bool UseGasMask(PlayerState player,DateTimeOffset now)
    {
        if(player.GasMaskActive)return false;
        player.GasMaskActive=true;
        events.Publish(new StatusEffectChangedEvent("Gas Mask",0,true,now));
        return true;
    }

    public void Advance(PlayerState player,double delta,DateTimeOffset now)
    {
        if(delta<=0 || player.EnergyDrinkSeconds<=0)return;
        var before=player.EnergyDrinkSeconds;
        player.EnergyDrinkSeconds=Math.Max(0,before-delta);
        if(before>0 && player.EnergyDrinkSeconds<=0)
            events.Publish(new StatusEffectChangedEvent("Energy Drink",0,false,now));
    }
}

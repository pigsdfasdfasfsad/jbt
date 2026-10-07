using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;
using Twr.Domain.Policies;

namespace Twr.Domain.Services;

public sealed class DamageService(DamagePolicy policy, EventStream events)
{
    public void Apply(PlayerState player,float amount,string source,DateTimeOffset now,bool bypassArmor=false)
    {
        var raw=policy.ClampDamage(amount);
        var healthDamage=raw;

        if(!bypassArmor && player.ArmorDurability>0)
        {
            healthDamage=raw*0.5f;
            var absorbed=raw-healthDamage;
            // APPROXIMATED: exact armor durability depletion equation is not recovered.
            player.ArmorDurability=Math.Max(0,player.ArmorDurability-absorbed);
            if(player.ArmorDurability<=0)player.ArmorKind="";
            events.Publish(new ArmorChangedEvent(player.ArmorDurability,now));
        }

        player.Health=Math.Max(0,player.Health-healthDamage);
        events.Publish(new PlayerDamagedEvent(healthDamage,player.Health,source,now));
    }
}

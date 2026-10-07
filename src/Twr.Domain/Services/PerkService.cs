using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class PerkService(EventStream events)
{
    public bool Set(Profile profile,int playerLevel,string perk,bool enabled,DateTimeOffset now)
    {
        var required=PerkRules.RequiredLevel(perk);
        if(required==int.MaxValue)return false;

        if(enabled)
        {
            if(playerLevel<required || profile.EquippedPerks.Contains(perk))return false;
            if(profile.EquippedPerks.Count>=PerkRules.DefaultSlots)return false;
            profile.EquippedPerks.Add(perk);

            if(perk=="Overwatch")
            {
                profile.Loadout["PrimaryBeforeOverwatch"]=
                    profile.Loadout.GetValueOrDefault("Primary") ?? StarterLoadoutService.SawnOff;
                profile.Unlocks.Add("Barrett M82A1");
                profile.Loadout["Primary"]="Barrett M82A1";
            }
        }
        else
        {
            if(!profile.EquippedPerks.Remove(perk))return false;

            if(perk=="Overwatch")
            {
                profile.Unlocks.Remove("Barrett M82A1");
                profile.Loadout["Primary"]=
                    profile.Loadout.GetValueOrDefault("PrimaryBeforeOverwatch") ?? StarterLoadoutService.SawnOff;
                profile.Loadout.Remove("PrimaryBeforeOverwatch");
            }
        }

        events.Publish(new PerkLoadoutChangedEvent(perk,enabled,now));
        return true;
    }
}

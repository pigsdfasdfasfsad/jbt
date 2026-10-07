using Twr.Domain.Contracts.Events;
using Twr.Domain.Core;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class AmmoService(EventStream events)
{
    public bool Spend(PlayerState player, string weapon, int amount, DateTimeOffset now)
    {
        if (amount <= 0)
            return false;

        var current = player.Ammo.GetValueOrDefault(weapon);
        if (current < amount)
            return false;

        player.Ammo[weapon] = current - amount;
        events.Publish(new AmmoChangedEvent(weapon, player.Ammo[weapon], now));
        return true;
    }

    public bool Reload(PlayerState player, string weapon, int magazineCapacity, DateTimeOffset now)
    {
        if (magazineCapacity <= 0)
            return false;

        var loaded = player.Ammo.GetValueOrDefault(weapon);
        var reserve = player.ReserveAmmo.GetValueOrDefault(weapon);
        var needed = Math.Max(0, magazineCapacity - loaded);
        var moved = Math.Min(needed, reserve);
        if (moved <= 0)
            return false;

        player.Ammo[weapon] = loaded + moved;
        player.ReserveAmmo[weapon] = reserve - moved;
        events.Publish(new AmmoChangedEvent(weapon, player.Ammo[weapon], now));
        return true;
    }
}

using Twr.Domain.Model;

namespace Twr.Domain.Services;

public sealed class StarterLoadoutService
{
    public const string Glock17 = "Glock 17";
    public const string SawnOff = "Sawn Off Shotgun";
    public const string TwoByFour = "2x4";

    public void ResetForMatch(PlayerState player)
    {
        player.Health = player.MaxHealth;
        player.ArmorDurability = 0;
        player.Ammo.Clear();
        player.ReserveAmmo.Clear();
        player.Inventory.Clear();

        player.Ammo[Glock17] = 18;
        player.ReserveAmmo[Glock17] = 136;
        player.Ammo[SawnOff] = 2;
        player.ReserveAmmo[SawnOff] = 32;
    }
}

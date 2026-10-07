using Twr.Domain.Model;

namespace Twr.Domain.Services;

/// <summary>
/// Source-backed starting loadout quantities for the locked release.
/// </summary>
public sealed class StarterLoadoutService
{
    public const string Glock17 = "Glock 17";
    public const string SawnOff = "Sawn Off Shotgun";
    public const string TwoByFour = "2x4";

    public void ResetForMatch(PlayerState player)
    {
        player.Health = player.MaxHealth;
        player.Ammo.Clear();
        player.ReserveAmmo.Clear();

        // Recovered weapon module values.
        player.Ammo[Glock17] = 18;
        player.ReserveAmmo[Glock17] = 136;
        player.Ammo[SawnOff] = 2;
        player.ReserveAmmo[SawnOff] = 32;
    }
}

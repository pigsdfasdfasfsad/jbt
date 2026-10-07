namespace Twr.Domain.Model;

public sealed class PlayerState
{
    public float MaxHealth { get; init; } = 100;
    public float Health { get; set; } = 100;
    public int Credits { get; set; } = 0;
    public int Xp { get; set; } = 0;
    public int Level { get; set; } = 1;
    public Dictionary<string, int> Ammo { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> ReserveAmmo { get; } = new(StringComparer.Ordinal);
    public bool IsAlive => Health > 0;
}

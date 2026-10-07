namespace Twr.Domain.Policies;
public sealed class InfectedSpawnPolicy {
    public bool CanSpawn(string infectedType) => !string.Equals(infectedType,"Juggernaut",StringComparison.OrdinalIgnoreCase);
}

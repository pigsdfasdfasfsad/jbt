using Twr.Domain.Policies; namespace Twr.Domain.Services;
public sealed class SpawnDirector(InfectedSpawnPolicy policy) { public IReadOnlyList<string> FilterEligible(IEnumerable<string> candidates)=>candidates.Where(policy.CanSpawn).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }

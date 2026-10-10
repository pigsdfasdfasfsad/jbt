using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// EXPLICIT F9-only accessibility fallback for the partial recovered
/// Laboratory source scene. The authored exterior infected spawn markers
/// are unchanged by default; none of these are claimed as retail positions.
/// Tries 24m first, then 14m and 10m only when an isolated source island
/// cannot provide enough physically supported distant points.
/// </summary>
public static class Pass40AdaptiveEntry
{
    public const float MinimumFallbackDistance = 10f;
    private static readonly float[] PreferredDistanceMetres = [24f,14f,10f];

    public static (Vector3 Position,float MinimumDistance)? TryFind(
        Pass25SourceNavigationRuntime navigator,World3D world,
        Vector3 originalSpawn,Vector3 playerPosition,uint variation)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(world);
        // Never replace exact source-connected original entry markers.
        // Every accepted alternative belongs to the player's graph
        // component AND passes actual exported-game floor/capsule physics.
        foreach(var distance in PreferredDistanceMetres)
        {
            var candidates=navigator.GetAssistedInfectedSpawnCandidates(
                originalSpawn,playerPosition,variation,distance);
            if(candidates.Length==0)continue;
            var position=Pass32SpawnSafety.FindSupportedPlacement(
                world,candidates,playerPosition,distance);
            if(!position.HasValue)continue;
            return (position.Value,distance);
        }
        return null;
    }
}

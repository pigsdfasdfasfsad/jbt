using System;
namespace Twr.Domain.Services;

/// <summary>
/// Source confirms immunity and distance falloff, but the exact falloff
/// equation is NOT recovered. The half-damage-at-edge factor is approximate.
/// </summary>
public static class Pass24SporeImpact
{
    public const float GasTickIntervalSeconds = .5f;
    public static float ClusterDamage(float damage, float distance,
        float radius, bool worldOccluded)
    {
        if (!float.IsFinite(damage) || !float.IsFinite(distance) ||
            !float.IsFinite(radius) || damage <= 0 || distance < 0 ||
            radius <= 0 || distance > radius || worldOccluded) return 0;
        return damage * (1f - .5f * Math.Clamp(distance / radius, 0f, 1f));
    }
    public static float GasDamage(float damage, bool mask, bool occluded, bool inCloud)
        => mask || occluded || !inCloud || !float.IsFinite(damage) || damage <= 0
            ? 0 : damage;
}

using System;

namespace Twr.Godot;

/// <summary>
/// The supplied Jack reference describes an approximately ten-second fuse
/// and decreasing damage with distance. The recovered script provides the
/// peak damage and radius; those remain catalog-driven (375, 35 studs in
/// this snapshot). The exact server falloff curve was not recovered.
/// </summary>
public static class JackDetonationRules
{
    public const double ReferenceDelaySeconds = 10.0;
    public static float ApproximateFalloff(float distance,float radius)
    {
        // Explicit reconstruction choice: linear decrease to zero at edge.
        // This is NOT asserted to be the original game's exact damage curve.
        if (!float.IsFinite(distance) || !float.IsFinite(radius) || radius<=0f)
            return 0f;
        return Math.Clamp(1f-Math.Max(0f,distance)/radius,0f,1f);
    }
}

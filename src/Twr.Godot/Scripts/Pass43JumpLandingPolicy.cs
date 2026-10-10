using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// A physics contact is NOT a completed nav jump unless the actor reaches
/// the intended source-linked landing waypoint on the opposite surface.
/// This separates launch, landing, and failure for actual owner playtests.
/// </summary>
public static class Pass43JumpLandingPolicy
{
    public const float HorizontalToleranceMetres = .70f;
    public const float VerticalToleranceMetres = .95f;
    public const double MinimumFlightSeconds = .15;

    public static bool IsSuccessful(bool isOnFloor, double elapsedSeconds,
        Vector3 actualBodyCentre, Vector3 expectedBodyCentre)
    {
        if (!isOnFloor || !double.IsFinite(elapsedSeconds) ||
            elapsedSeconds < MinimumFlightSeconds ||
            !Finite(actualBodyCentre) || !Finite(expectedBodyCentre))
            return false;
        var gap = expectedBodyCentre - actualBodyCentre;
        var horizontal = new Vector2(gap.X, gap.Z).Length();
        return horizontal <= HorizontalToleranceMetres &&
               Math.Abs(gap.Y) <= VerticalToleranceMetres;
    }

    private static bool Finite(Vector3 point) =>
        float.IsFinite(point.X) && float.IsFinite(point.Y) &&
        float.IsFinite(point.Z);
}

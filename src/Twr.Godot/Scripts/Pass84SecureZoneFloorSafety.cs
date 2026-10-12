using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Only reconstructed two-level Manor needs the extra same-story check.
/// Original owner-supplied 3D source map objective zones retain their
/// existing gameplay parameters and remain unchanged.
/// </summary>
public static class Pass84SecureZoneFloorSafety
{
    public const float SameStoryBandMetres = 1.35f;

    public static bool CanAccumulate(Vector3 playerFeet, Vector3 target)
    {
        if (!Finite(playerFeet) || !Finite(target)) return false;
        return Math.Abs(playerFeet.Y - target.Y) <= SameStoryBandMetres;
    }

    private static bool Finite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}

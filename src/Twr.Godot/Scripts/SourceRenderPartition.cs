using System;
using System.Globalization;
using Godot;

namespace Twr.Godot;

/// <summary>Rendering-only partition for non-streamed original scenes.
/// Godot culls one entire MultiMesh, not its individual instances. Bound
/// each batch spatially and by count without changing source transforms,
/// material ownership, or physics. The existing SHA-bound streamer stays intact.</summary>
public static class SourceRenderPartition
{
    public const float CellMeters = 32f;
    public const int MaximumInstances = 128;

    public static string Cell(Vector3 p)
    {
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))
            throw new ArgumentException("Nonfinite original render transform");
        return string.Create(CultureInfo.InvariantCulture,
            $"{Math.Floor(p.X/CellMeters)},{Math.Floor(p.Y/CellMeters)},{Math.Floor(p.Z/CellMeters)}");
    }
}

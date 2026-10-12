using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass82 gameplay markers for reconstructed Manor only. Their coordinates
/// are derived from known fallback ramp landings, not owner retail geometry.
/// </summary>
public static class Pass82ManorUpperSpawnPlan
{
    public static Vector3[] BuildPickupMarkers(params Pass77StairLink[] stairs)
    {
        RequireThree(stairs);
        // Player feet stand on deck at stair.UpperFeet.Y. PickupActor
        // renders a box centred 0.62m over that plane.
        return stairs.Select(s => s.UpperFeet +
            Vector3.Right*4.25f + Vector3.Up*.62f).ToArray();
    }

    public static Vector3[] BuildObjectiveMarkers(params Pass77StairLink[] stairs)
    {
        RequireThree(stairs);
        // Objective actors already implement LOS/vertical-band interaction.
        // Leave 0.35m clearance above the actual solid balcony floor.
        return stairs.Select(s => s.UpperFeet +
            Vector3.Left*4.25f + Vector3.Up*.35f).ToArray();
    }

    private static void RequireThree(Pass77StairLink[] stairs)
    {
        if (stairs.Length != 3 ||
            stairs.Any(s => s.LowerFeet.Y >= s.UpperFeet.Y ||
                            s.UpperFeet.Y < 2.7f ||
                            !float.IsFinite(s.UpperFeet.X)))
            throw new ArgumentException(
                "Pass82 requires exactly three reconstructed walkable Manor stairs.");
    }
}

namespace Twr.Godot;

/// <summary>
/// Public fallback-Manor-only Escort upper-wing rotation. The three
/// reconstructed upper floors are west, east, front (Pass82 order).
/// No owner-original objective marker or non-Manor map is rewritten.
/// </summary>
public static class Pass95ManorEscortRotation
{
    public const int UpperWingCount = 3;

    /// <summary>
    /// Maintain Pass94 Wave5 front-hall compatibility, then send successive
    /// later Escort cycles to west and east. Earlier optional Wave3 Escort
    /// can use east. All indices refer to physical Pass82 marker ordering.
    /// </summary>
    public static int UpperWingForWave(int wave)
    {
        if (wave < 3 || wave > 15)
            return -1;
        return (wave / 5 + 1) % UpperWingCount;
    }
}

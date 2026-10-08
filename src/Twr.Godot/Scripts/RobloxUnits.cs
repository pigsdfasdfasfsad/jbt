namespace Twr.Godot;

/// <summary>
/// Conversion between the authored Roblox coordinate system and Godot's
/// metre-based physics. A normal Roblox stud is 0.28 metres. Mesh positions,
/// character speeds, and authored weapon ranges must use one shared factor.
/// Gravity, jump motion and imported projectile behavior have separate tuning
/// and are NOT claimed to be fully source-verified.
/// </summary>
public static class RobloxUnits
{
    public const float MetersPerStud = 0.28f;
    public static float Distance(float studs) => studs * MetersPerStud;
    public static float Speed(float studsPerSecond) => studsPerSecond * MetersPerStud;
}

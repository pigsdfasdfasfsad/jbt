namespace Twr.Domain.Model;

/// <summary>
/// Regular-mode timing/scaling contract for the offline reconstruction.
/// Recovered values are separated from fitted reconstruction values so stronger
/// source evidence can replace fitted values without obscuring provenance.
/// </summary>
public static class RegularWaveRules
{
    // Recovered from surviving round-flow / Bonuses evidence.
    public const double WaveDurationSeconds = 300.0;
    public const double FinalWaveDifficultyScale = 1.75;

    // Countdown UI is source-backed. Intermission and WaveEnd durations remain
    // reconstruction values until exact retail timing is recovered.
    public const double CountdownSeconds = 10.0;
    public const double IntermissionSeconds = 20.0;
    public const double WaveEndSeconds = 8.0;

    // FITTED reconstruction spawner curve. No surviving retail source contains
    // these exact values. Keep centralized and replace when stronger evidence appears.
    public const double SpawnBaseIntervalSeconds = 2.2;
    public const double SpawnPerWaveFalloff = 0.94;
    public const double SpawnMinimumIntervalSeconds = 0.25;
    public const int AliveBase = 12;
    public const int AlivePerWave = 4;
    public const int AliveCap = 45;

    public static double DifficultyScale(int wave)
    {
        var clamped = Math.Clamp(wave, 1, ReleaseRules.MaxWaves);
        if (ReleaseRules.MaxWaves <= 1)
            return 1.0;

        return Math.Pow(
            FinalWaveDifficultyScale,
            (clamped - 1.0) / (ReleaseRules.MaxWaves - 1.0));
    }

    public static double InfectedWalkSpeedScale(int wave)
    {
        var scale = DifficultyScale(wave);
        return 1.0 + (scale - 1.0) * 0.5;
    }

    public static double SpawnIntervalSeconds(int wave)
    {
        var clamped = Math.Max(1, wave);
        var interval = SpawnBaseIntervalSeconds *
                       Math.Pow(SpawnPerWaveFalloff, clamped - 1);
        return Math.Max(SpawnMinimumIntervalSeconds, interval);
    }

    public static int MaxAlive(int wave)
    {
        var clamped = Math.Max(1, wave);
        return Math.Min(AliveBase + AlivePerWave * (clamped - 1), AliveCap);
    }
}

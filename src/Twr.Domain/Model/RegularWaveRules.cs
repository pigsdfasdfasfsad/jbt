namespace Twr.Domain.Model;

public static class RegularWaveRules
{
    // RECOVERED: non-Endless waves are five minutes and Bonuses ramps to 1.75.
    public const double WaveDurationSeconds = 300.0;
    public const double FinalWaveDifficultyScale = 1.75;

    // Countdown is source-backed by surviving UI. These stage lengths are
    // reconstruction values until exact retail timing is recovered.
    public const double CountdownSeconds = 10.0;
    public const double IntermissionSeconds = 20.0;
    public const double WaveEndSeconds = 8.0;

    // FITTED reconstruction spawner curve. Exact retail spawn cadence/cap is lost.
    public const double SpawnBaseIntervalSeconds = 2.2;
    public const double SpawnPerWaveFalloff = 0.94;
    public const double SpawnMinimumIntervalSeconds = 0.25;
    public const int AliveBase = 12;
    public const int AlivePerWave = 4;
    public const int AliveCap = 45;

    public static double DifficultyScale(int wave)
    {
        var clamped = Math.Clamp(wave, 1, ReleaseRules.MaxWaves);
        var waveSteps = Math.Max(1, ReleaseRules.MaxWaves - 1);
        var raw = Math.Pow(FinalWaveDifficultyScale, (clamped - 1.0) / waveSteps);
        return Math.Round(raw, 2, MidpointRounding.AwayFromZero);
    }

    public static double InfectedWalkSpeedScale(int wave)
    {
        // APPROXIMATED: the source says speed rises each wave but the exact
        // speed multiplier is not recovered. This is the documented fit.
        var scale = DifficultyScale(wave);
        return 1.0 + (scale - 1.0) * 0.5;
    }

    public static double SpawnIntervalSeconds(int wave)
    {
        var clamped = Math.Max(1, wave);
        return Math.Max(SpawnMinimumIntervalSeconds,
            SpawnBaseIntervalSeconds * Math.Pow(SpawnPerWaveFalloff, clamped - 1));
    }

    public static int MaxAlive(int wave)
    {
        var clamped = Math.Max(1, wave);
        return Math.Min(AliveBase + AlivePerWave * (clamped - 1), AliveCap);
    }
}

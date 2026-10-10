using System;
namespace Twr.Domain.Services;

/// <summary>
/// Wall-clock independent gas tick scheduler. No damage after expiry; a
/// long frame catches up at most eight pulses and discards excess backlog.
/// </summary>
public sealed class Pass24GasTickClock
{
    public const int MaxTicksPerFrame = 8;
    private const double Interval = Pass24SporeImpact.GasTickIntervalSeconds;
    private double _nextTick = Interval;
    public double Elapsed { get; private set; }
    public bool Expired { get; private set; }

    public int Advance(double deltaSeconds, double durationSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (!double.IsFinite(durationSeconds) || durationSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (Expired) return 0;
        Elapsed = Math.Min(durationSeconds, Elapsed + deltaSeconds);
        var pulses = 0;
        while (_nextTick <= Elapsed + 1e-8 && pulses < MaxTicksPerFrame)
        {
            _nextTick += Interval;
            pulses++;
        }
        if (_nextTick <= Elapsed + 1e-8)
        {
            var skipped = Math.Floor((Elapsed - _nextTick) / Interval) + 1;
            _nextTick += Math.Max(1, skipped) * Interval;
        }
        Expired = Elapsed >= durationSeconds;
        return pulses;
    }
}

using System;

namespace Twr.Godot;

/// <summary>
/// Rolling, bounded OBSERVATIONAL frame-time telemetry; not a synthetic FPS
/// target or a claim that the private Laboratory ran on a user's GPU.
/// Sample real _Process delta; ignore accelerated test stage jumps.
/// </summary>
public sealed class Pass43FrameWindow
{
    public const int Capacity = 2048;
    private readonly float[] _samples = new float[Capacity];
    private int _next;
    private int _count;

    public int SampleCount => _count;
    public int PeakLivingInfected { get; private set; }
    public double ObservedRealSeconds { get; private set; }

    public void Record(double deltaSeconds, int livingInfected)
    {
        // Deliberately ignore giant synthetic accelerated 300-second frames,
        // negative deltas and long program stalls. Do not disguise their
        // existence: only sampled real frame time is reported below.
        if (!double.IsFinite(deltaSeconds) ||
            deltaSeconds <= 0 || deltaSeconds > .5)
            return;
        _samples[_next] = (float)(deltaSeconds * 1000.0);
        _next = (_next + 1) % Capacity;
        if (_count < Capacity) _count++;
        ObservedRealSeconds += deltaSeconds;
        PeakLivingInfected = Math.Max(PeakLivingInfected, Math.Max(0,livingInfected));
    }

    public readonly record struct Summary(
        int SampledFrames, double MedianMs, double P95Ms, double P99Ms,
        double WorstMs, int PeakLivingInfected, double ObservedRealSeconds);

    public Summary Snapshot()
    {
        if (_count == 0)
            return new Summary(0,0,0,0,0,PeakLivingInfected,ObservedRealSeconds);
        var ordered = new float[_count];
        Array.Copy(_samples,ordered,_count);
        Array.Sort(ordered);
        static double Percentile(float[] sorted, double p) =>
            sorted[Math.Max(0,(int)Math.Ceiling(p * sorted.Length)-1)];
        return new Summary(_count,Percentile(ordered,.50),
            Percentile(ordered,.95),Percentile(ordered,.99),
            ordered[^1],PeakLivingInfected,ObservedRealSeconds);
    }
}

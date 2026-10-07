namespace Twr.Domain.Core;
public sealed class FixedStepClock {
    public const double StepSeconds = 1.0 / 60.0;
    private double _accumulator;
    public int Push(double deltaSeconds) { _accumulator += Math.Max(0, deltaSeconds); var n=(int)(_accumulator/StepSeconds); _accumulator-=n*StepSeconds; return n; }
}

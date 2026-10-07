namespace Twr.Domain.Contracts.Events; public sealed record WaveStartedEvent(int Wave, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

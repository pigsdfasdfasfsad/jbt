namespace Twr.Domain.Contracts.Events; public sealed record PlayerDamagedEvent(float Amount, float HealthAfter, string Source, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

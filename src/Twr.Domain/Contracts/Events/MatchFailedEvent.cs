namespace Twr.Domain.Contracts.Events;
public sealed record MatchFailedEvent(string MapName, int Wave, string Reason, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

namespace Twr.Domain.Contracts.Events; public sealed record ObjectiveCompletedEvent(string ObjectiveId, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

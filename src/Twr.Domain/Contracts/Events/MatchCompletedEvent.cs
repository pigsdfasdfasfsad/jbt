namespace Twr.Domain.Contracts.Events; public sealed record MatchCompletedEvent(string MapName, int Wave, int AwardedXp, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

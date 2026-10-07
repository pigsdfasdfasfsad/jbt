namespace Twr.Domain.Contracts.Events;

public sealed record KillRewardedEvent(
    string InfectedType,
    int Credits,
    int Xp,
    int TotalCredits,
    int TotalXp,
    DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

namespace Twr.Domain.Contracts.Events;
public sealed record ObjectiveRewardedEvent(
    string ObjectiveId,
    string Family,
    int Credits,
    int Xp,
    int TotalCredits,
    int TotalXp,
    DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

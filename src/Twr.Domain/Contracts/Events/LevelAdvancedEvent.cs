namespace Twr.Domain.Contracts.Events;
public sealed record LevelAdvancedEvent(int Level,int CreditReward,int Credits,int XpTowardNext,DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

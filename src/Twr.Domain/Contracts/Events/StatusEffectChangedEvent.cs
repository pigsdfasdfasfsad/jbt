namespace Twr.Domain.Contracts.Events;
public sealed record StatusEffectChangedEvent(string Effect,double RemainingSeconds,bool Active,DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

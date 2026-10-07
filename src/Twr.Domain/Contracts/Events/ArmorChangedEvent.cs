namespace Twr.Domain.Contracts.Events;
public sealed record ArmorChangedEvent(float Durability, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

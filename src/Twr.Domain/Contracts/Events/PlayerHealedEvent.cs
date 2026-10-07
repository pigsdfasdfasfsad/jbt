namespace Twr.Domain.Contracts.Events;
public sealed record PlayerHealedEvent(float Amount, float Health, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

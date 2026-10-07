namespace Twr.Domain.Contracts.Events;
public sealed record LoadoutChangedEvent(string Slot,string WeaponId,DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

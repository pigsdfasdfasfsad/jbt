namespace Twr.Domain.Contracts.Events;
public sealed record PerkLoadoutChangedEvent(string PerkName,bool Enabled,DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

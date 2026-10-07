namespace Twr.Domain.Contracts.Events;
public sealed record InventoryChangedEvent(string ItemId, int Count, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

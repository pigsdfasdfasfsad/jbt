namespace Twr.Domain.Contracts.Events; public sealed record PurchaseCompletedEvent(string ItemId, int CreditsAfter, DateTimeOffset At) : Twr.Domain.Contracts.IGameEvent;

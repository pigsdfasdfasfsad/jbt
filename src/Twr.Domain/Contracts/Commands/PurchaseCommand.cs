namespace Twr.Domain.Contracts.Commands; public sealed record PurchaseCommand(string ItemId, int Price) : Twr.Domain.Contracts.IGameCommand;

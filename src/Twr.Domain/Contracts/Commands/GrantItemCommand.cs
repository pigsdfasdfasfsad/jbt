namespace Twr.Domain.Contracts.Commands;
public sealed record GrantItemCommand(string ItemId, int Amount = 1) : Twr.Domain.Contracts.IGameCommand;

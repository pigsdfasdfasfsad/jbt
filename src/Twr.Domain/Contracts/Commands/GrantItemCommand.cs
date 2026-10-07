namespace Twr.Domain.Contracts.Commands;
public sealed record GrantItemCommand(string ItemId,int Amount=1,int MaxCount=int.MaxValue) : Twr.Domain.Contracts.IGameCommand;

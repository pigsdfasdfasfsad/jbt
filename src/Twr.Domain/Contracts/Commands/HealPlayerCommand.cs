namespace Twr.Domain.Contracts.Commands;
public sealed record HealPlayerCommand(float Amount, bool Full = false) : Twr.Domain.Contracts.IGameCommand;

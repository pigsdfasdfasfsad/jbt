namespace Twr.Domain.Contracts.Commands;
public sealed record FailMatchCommand(string Reason) : Twr.Domain.Contracts.IGameCommand;

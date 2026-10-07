namespace Twr.Domain.Contracts.Commands;
public sealed record CompleteObjectiveCommand(string ObjectiveId, string Family = "") : Twr.Domain.Contracts.IGameCommand;

namespace Twr.Domain.Contracts.Commands;
public sealed record AwardWaveSurvivalCommand(int Wave, int CompletedObjectives, int PlayerCount) : Twr.Domain.Contracts.IGameCommand;

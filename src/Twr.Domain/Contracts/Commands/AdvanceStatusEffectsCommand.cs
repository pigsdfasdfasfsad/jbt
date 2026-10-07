namespace Twr.Domain.Contracts.Commands;
public sealed record AdvanceStatusEffectsCommand(double DeltaSeconds) : Twr.Domain.Contracts.IGameCommand;

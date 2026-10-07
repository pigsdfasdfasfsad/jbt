namespace Twr.Domain.Contracts.Commands;

public sealed record AwardKillCommand(string InfectedType, int Credits, int Xp) : Twr.Domain.Contracts.IGameCommand;

namespace Twr.Domain.Contracts.Commands;
public sealed record SetPerkCommand(string PerkName,bool Enabled) : Twr.Domain.Contracts.IGameCommand;

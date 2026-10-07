namespace Twr.Domain.Contracts.Commands;
public sealed record SetLoadoutCommand(string Slot,string WeaponId) : Twr.Domain.Contracts.IGameCommand;

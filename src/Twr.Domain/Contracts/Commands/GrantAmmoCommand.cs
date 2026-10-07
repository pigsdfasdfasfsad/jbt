namespace Twr.Domain.Contracts.Commands;
public sealed record GrantAmmoCommand(string WeaponId, int Amount, int MaxReserve) : Twr.Domain.Contracts.IGameCommand;

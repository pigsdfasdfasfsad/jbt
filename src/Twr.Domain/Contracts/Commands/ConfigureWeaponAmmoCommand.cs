namespace Twr.Domain.Contracts.Commands;
public sealed record ConfigureWeaponAmmoCommand(string WeaponId,int Magazine,int Reserve) : Twr.Domain.Contracts.IGameCommand;

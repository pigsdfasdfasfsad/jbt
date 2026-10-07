namespace Twr.Domain.Contracts.Commands; public sealed record SpendAmmoCommand(string WeaponId, int Amount) : Twr.Domain.Contracts.IGameCommand;

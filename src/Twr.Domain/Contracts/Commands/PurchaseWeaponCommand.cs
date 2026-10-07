namespace Twr.Domain.Contracts.Commands;
public sealed record PurchaseWeaponCommand(string WeaponId,int RequiredLevel,int Price) : Twr.Domain.Contracts.IGameCommand;

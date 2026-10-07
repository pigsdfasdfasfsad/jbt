namespace Twr.Domain.Contracts.Commands;

public sealed record ReloadWeaponCommand(string WeaponId, int MagazineCapacity) : Twr.Domain.Contracts.IGameCommand;

namespace Twr.Domain.Contracts.Commands;

public sealed record OpenSkinCaseCommand(string CaseName) : Twr.Domain.Contracts.IGameCommand;
public sealed record SellOwnedSkinCommand(string SkinId) : Twr.Domain.Contracts.IGameCommand;
public sealed record ApplyWeaponSkinCommand(string WeaponName,string? SkinId) : Twr.Domain.Contracts.IGameCommand;

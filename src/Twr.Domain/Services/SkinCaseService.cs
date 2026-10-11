using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Twr.Domain.Model;

namespace Twr.Domain.Services;

/// <summary>
/// Local-only cosmetic economy. No Roblox client/server, product IDs, money
/// purchases or external random services. The original per-skin odds were
/// not present in the supplied sources, so missing skins in a selected tier
/// are sampled UNIFORMLY; this is an explicitly provisional distribution.
/// Only authoritative commands call this service; the GUI never sends prices.
/// </summary>
public sealed class SkinCaseService
{
    private readonly Func<int,int> _nextIndex;
    public SkinCaseService(Func<int,int>? nextIndex=null) =>
        _nextIndex = nextIndex ?? RandomNumberGenerator.GetInt32;

    public sealed record Result(bool Success,string Message,string? SkinId=null,int CreditDelta=0)
    {
        public static Result Fail(string reason) => new(false,reason);
    }

    public Result Open(Profile profile,PlayerState player,string caseName)
    {
        var definition=SkinCaseCatalog.FindCase(caseName);
        if(definition is null) return Result.Fail("Unknown case.");
        if(!definition.OnSale) return Result.Fail("This source case is off-sale.");
        if(player.Credits<definition.PriceCredits)
            return Result.Fail("Not enough credits.");
        var missing=definition.PurchasePool
            .Where(name=>!profile.OwnedSkins.Contains(caseName+"/"+name))
            .ToArray();
        if(missing.Length==0) return Result.Fail("All skins in this case are already owned.");

        var index=_nextIndex(missing.Length);
        if(index<0 || index>=missing.Length)
            return Result.Fail("Invalid local case sampler output.");

        var rewardId=caseName+"/"+missing[index];
        if(profile.OwnedSkins.Contains(rewardId))
            return Result.Fail("Duplicate skin rejected.");
        // The authoritative case price and reward pool come from the domain
        // catalog, NEVER client-provided prices or alleged rewards.
        player.Credits-=definition.PriceCredits;
        profile.OwnedSkins.Add(rewardId);
        return new Result(true,
            $"Unlocked {missing[index]} from {caseName} case.",
            rewardId,-definition.PriceCredits);
    }

    public Result Sell(Profile profile,PlayerState player,string? skinId)
    {
        var skin=SkinCaseCatalog.FindSkin(skinId);
        if(skin is null || !profile.OwnedSkins.Contains(skin.Id))
            return Result.Fail("Skin is not owned.");
        if(skin.Exclusive)
            return Result.Fail("Exclusive skins cannot be sold.");
        var sourceCase=SkinCaseCatalog.FindCase(skin.CaseName)!;
        var refund=sourceCase.PriceCredits/2;
        if(player.Credits>int.MaxValue-refund)
            return Result.Fail("Credit limit would overflow.");
        profile.OwnedSkins.Remove(skin.Id);
        foreach(var name in profile.EquippedWeaponSkins
            .Where(pair=>pair.Value==skin.Id).Select(pair=>pair.Key).ToArray())
            profile.EquippedWeaponSkins.Remove(name);
        player.Credits+=refund;
        return new Result(true,
            $"Sold {skin.Name} for {refund:N0} credits.",skin.Id,refund);
    }

    public Result Apply(Profile profile,string weaponName,string? skinId)
    {
        if(string.IsNullOrWhiteSpace(weaponName) ||
           (profile.Loadout.GetValueOrDefault("Primary")!=weaponName &&
            profile.Loadout.GetValueOrDefault("Secondary")!=weaponName) ||
           !profile.Unlocks.Contains(weaponName))
            return Result.Fail("Choose an owned, equipped primary or secondary weapon.");

        if(skinId is null || skinId.Length==0)
        {
            if(!profile.EquippedWeaponSkins.Remove(weaponName))
                return Result.Fail("Weapon already uses its default finish.");
            return new Result(true,$"Removed cosmetic from {weaponName}.");
        }

        var skin=SkinCaseCatalog.FindSkin(skinId);
        if(skin is null || !profile.OwnedSkins.Contains(skin.Id))
            return Result.Fail("Cannot equip an unowned skin.");
        if(profile.EquippedWeaponSkins.GetValueOrDefault(weaponName)==skin.Id)
            return Result.Fail("That skin is already applied.");
        profile.EquippedWeaponSkins[weaponName]=skin.Id;
        return new Result(true,$"Applied {skin.Name} to {weaponName}.",skin.Id);
    }
}

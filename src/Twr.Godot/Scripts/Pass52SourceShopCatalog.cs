using System;
using System.Collections.Generic;
using System.Linq;

namespace Twr.Godot;

/// <summary>
/// Pass52: source-confirmed credit-price labels for all seven original case
/// families. From user-supplied ModuleScript.Shop.Source.txt,
/// SHA256 4df17a84418b830e3a26f9246057a38b08234597344f62d2335a6a661e98e9db.
/// Original case buy/drop/reward transactions are NOT implemented. This class
/// is metadata only: reading it cannot spend credits, award skins or contact
/// Roblox commerce services.
/// </summary>
public static class Pass52SourceShopCatalog
{
    public const string SourceFileSha256 =
        "4df17a84418b830e3a26f9246057a38b08234597344f62d2335a6a661e98e9db";

    public sealed record Case(string Name, int PriceCredits);

    private static readonly Case[] SourceCases =
    [
        new("Low",6000),
        new("Mid",20000),
        new("High",50000),
        new("Neon",120000),
        new("Tactical",12000),
        new("Ethereal",1000000),
        new("Hallows",200000)
    ];

    public static IReadOnlyList<Case> All => Array.AsReadOnly(SourceCases);

    public static bool TryFind(string name, out Case? item)
    {
        item = SourceCases.FirstOrDefault(x =>
            string.Equals(x.Name, name, StringComparison.Ordinal));
        return item is not null;
    }
}

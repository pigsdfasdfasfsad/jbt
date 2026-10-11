using System;
using System.Collections.Generic;
using System.Linq;

namespace Twr.Domain.Model;

/// <summary>
/// Source-confirmed skin names grouped by ModuleScript.Skins.Source.txt.
/// Original Shop module supplies the exact credit prices. Nine High skins
/// are Christmas/exclusive rewards and never appear in ordinary High rolls.
/// Hallows was off-sale after November 1, 2025. Raw Roblox code, image bytes,
/// texture references and paid-commerce identifiers are intentionally absent.
/// </summary>
public static class SkinCaseCatalog
{
    public sealed record Case(
        string Name, int PriceCredits, bool OnSale,
        string[] PurchasePool, string[] EventOnlySkins)
    {
        public int TotalSkins => PurchasePool.Length + EventOnlySkins.Length;
    }

    public sealed record Skin(string Id, string Name, string CaseName, bool Exclusive);

    public static readonly IReadOnlyList<Case> Cases = Array.AsReadOnly(new Case[]
    {
        new("Low",6000,true,[
            "Cocoa","Storm Blue","Orange","Dark Green","Flint",
            "White","Red","Purple","Pink","Pastel Blue",
            "Dark Grey","Yellow","Sage Green","Salmon Pink","Tan",
            "Brown","Grime","Sand Red","Teal","Linen"],[]),
        new("Mid",20000,true,[
            "Brushed Metal","Bronze","Eighties","Caution","City Bus",
            "VI","Noir","Blackout","Whiteout","Sports Car","Spiffo",
            "Dark Tan","Veil","Red Polymer","Neapolitan","Coyote Brown",
            "Desert","Winter","OD Green"],[]),
        new("High",50000,true,[
            "Hope","Dark Mahogany","Steampunk","Hot Head","Virtue",
            "Jupiter","Galaxy","Diamond","Gold","Ruby","Emerald",
            "Sapphire","Amethyst","Spetsnaz Urban","Pacific Tactical",
            "Cherry Blossom","Lava","Pearl","Royalty","Art Deco"],[
            "Christmas 2018","Christmas 2019","Christmas 2020",
            "Christmas 2021","Christmas 2022","Christmas 2023",
            "Ugly Sweater","Gift Wrap","Candy Cane"]),
        new("Neon",120000,true,[
            "Grid","Lightning","Triangles","Matrix","Reality",
            "Pandora","Hawaiian","Ego","Mischief","Lucidity",
            "Past","Fireball","Ripples","Miami","Bliss"],[]),
        new("Tactical",12000,true,[
            "Desert Storm","Guerilla Warfare","Sage","Plum","Pine",
            "Space Marine","Brushed Silver","Tactical Pink","Infiltration",
            "Midnight Bronze","Tungsten","Battleship Grey",
            "Accelerator","Atlantic","Earth"],[]),
        new("Ethereal",1000000,true,[
            "Spirit","Ghost","Peak","Oil Spill","Solar Wind",
            "Stardust","Entropy","Shifting Ceiling","Database",
            "Shattered","Scanning","Nuclear Decay","Perihelion",
            "Tailoring Reality","Wading Out"],[]),
        new("Hallows",200000,false,[],[
            "Cactacombs","Jacklight","Arachnid","Hexbrew","Specter"])
    });

    private static readonly IReadOnlyDictionary<string, Case> ByCase =
        Cases.ToDictionary(c => c.Name, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, Skin> BySkin =
        Cases.SelectMany(c =>
                c.PurchasePool.Select(s => new Skin(c.Name+"/"+s,s,c.Name,false))
                .Concat(c.EventOnlySkins.Select(s =>
                    new Skin(c.Name+"/"+s,s,c.Name,true))))
            .ToDictionary(s => s.Id, StringComparer.Ordinal);

    public static IReadOnlyCollection<Skin> AllSkins { get; } = BySkin.Values.ToArray();

    public static Case? FindCase(string? name) =>
        name is not null && ByCase.TryGetValue(name,out var found) ? found : null;

    public static Skin? FindSkin(string? id) =>
        id is not null && BySkin.TryGetValue(id,out var found) ? found : null;

    public static int OwnedCount(Profile profile, Case entry) =>
        entry.PurchasePool.Count(s => profile.OwnedSkins.Contains(entry.Name+"/"+s));

    public static bool Completed(Profile profile, Case entry) =>
        entry.PurchasePool.Length > 0 &&
        OwnedCount(profile,entry) == entry.PurchasePool.Length;
}

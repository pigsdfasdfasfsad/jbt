using Twr.Domain.Model;

namespace Twr.Godot;

public static class InfectedCatalog
{
    public static readonly InfectedDefinition[] Active =
    [
        new("Bolter", 32.5, 4, 13.5, true),
        new("Civilian", 65, 8, 15, true),
        new("Sprinter", 65, 8, 18, true),
        new("Military", 113.75, 12, 17, true),
        new("Hazmat", 85, 12, 15, true),
        new("Riot", 357.5, 24, 12, true),
        new("Burster", 87.75, 12, 10, true),
        new("Bloater", 487.5, 60, 7, true)
    ];

    public static (int Credits, int Xp) BonusReward(string name) => name switch
    {
        "Bolter" => (8, 4),
        "Civilian" => (5, 3),
        "Sprinter" => (5, 3),
        "Military" => (13, 7),
        "Hazmat" => (38, 15),
        "Riot" => (25, 13),
        "Burster" => (25, 13),
        "Bloater" => (50, 25),
        _ => (0, 0)
    };

    public static (int Credits, int Xp) Reward(string name) => name switch
    {
        "Bolter" => (15, 8),
        "Civilian" => (10, 5),
        "Sprinter" => (10, 5),
        "Military" => (25, 13),
        "Hazmat" => (75, 30),
        "Riot" => (50, 25),
        "Burster" => (50, 25),
        "Bloater" => (100, 50),
        _ => (0, 0)
    };
}

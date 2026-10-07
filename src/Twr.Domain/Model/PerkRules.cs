namespace Twr.Domain.Model;

public static class PerkRules
{
    public const int DefaultSlots=3;
    public static readonly IReadOnlyDictionary<string,int> RequiredLevels =
        new Dictionary<string,int>(StringComparer.Ordinal)
        {
            ["Hardened Sight"]=10, ["Play Maker"]=10, ["Dexterous"]=10,
            ["Bruiser"]=20, ["Efficiency"]=20, ["Trigger Finger"]=20,
            ["Medic"]=30, ["Enhanced Electronics"]=30, ["Eagle Eyes"]=30,
            ["Caffeinated"]=45, ["Fortifier"]=45, ["Heavy Hitter"]=45,
            ["Adrenaline Rush"]=60, ["Pyrotechnic"]=60, ["Steady Hand"]=60,
            ["Speed Demon"]=85, ["Carpenter"]=85, ["Brisk"]=85,
            ["Guardian Angel"]=100, ["Juggernaut"]=100, ["Overwatch"]=100
        };

    public static int RequiredLevel(string perk) =>
        RequiredLevels.TryGetValue(perk,out var level) ? level : int.MaxValue;
}

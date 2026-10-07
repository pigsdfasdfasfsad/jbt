using System.Text.Json;

namespace Twr.Godot;

public sealed record RuntimeWeaponDefinition(
    string Name,
    string Slot,
    string WeaponType,
    string Type,
    float Damage,
    int Level,
    int Price,
    int Magazine,
    int Reserve,
    int AmmoPickup,
    bool AmmoPickupVerified,
    double Rpm,
    float Range,
    bool RangeVerified,
    float Spread,
    int MaxPen,
    double ReloadSeconds,
    double ActionSeconds,
    bool Bladed,
    string? ProjectileType)
{
    public bool IsMelee => WeaponType.Equals("Melee", StringComparison.OrdinalIgnoreCase);
    public bool IsShotgun => Type.Contains("Shotgun", StringComparison.OrdinalIgnoreCase);
    public bool IsAutomatic =>
        Type.Contains("Auto", StringComparison.OrdinalIgnoreCase) ||
        Type.Equals("Flamethrower", StringComparison.OrdinalIgnoreCase);
    public bool IsLauncher =>
        Type.Equals("Launcher", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(ProjectileType);
    public bool IsFlamethrower => Type.Equals("Flamethrower", StringComparison.OrdinalIgnoreCase);
}

public static class RuntimeWeaponCatalog
{
    private static IReadOnlyDictionary<string, RuntimeWeaponDefinition>? _cache;

    // VERIFIED from the Ammo wiki table where older weapon modules omit AmmoPickup.
    private static readonly IReadOnlyDictionary<string,int> VerifiedAmmoPickups =
        new Dictionary<string,int>(StringComparer.Ordinal)
        {
            ["Sawn Off Shotgun"]=8, ["Ruger 10-22"]=20, ["Ingram MAC-10"]=60,
            ["Sten Mk V"]=32, ["Mossberg 500"]=8, ["Thompson M1"]=20,
            ["Aero Survival Rifle"]=17, ["Winchester Model 70"]=10, ["KAC PDW"]=30,
            ["Benelli M4"]=10, ["PP-91 Kedr"]=40, ["UMP-45"]=25, ["SAP-6"]=12,
            ["Kriss Vector"]=50, ["M1 Garand"]=8, ["OTs-14 Groza"]=30,
            ["MP5A2"]=30, ["G36C"]=30, ["AR-57"]=50, ["MP7"]=41, ["PPSh-41"]=71,
            ["AUG"]=30, ["P90"]=50, ["SPAS-12"]=8, ["M16A1"]=20, ["M4A1"]=30,
            ["SKO Shorty"]=10, ["FN FAL"]=20, ["SCAR-H"]=20, ["M14"]=20,
            ["AK-47"]=30, ["Mosin Nagant"]=10, ["AS VAL"]=30, ["VSS"]=20,
            ["CMMG Mk47 Mutant"]=30, ["RPK"]=40, ["ASh-12"]=20, ["LWRC IC-PSD"]=60,
            ["M110 SASS"]=20, ["Flamethrower"]=50, ["MK18"]=30, ["AA-12"]=16,
            ["M60"]=100, ["SVD"]=10, ["Barrett M82A1"]=10,
            ["Winchester Model 1892"]=15, ["RPG-7"]=2, ["MG 42"]=75,
            ["Glock 17"]=17, ["LH9 MKII"]=13, ["Walther P38"]=8, ["XD-9"]=16,
            ["Serbu Super Shorty"]=4, ["M1911A1"]=7, ["Makarov"]=8, ["TEC-9"]=32,
            ["MP-443 Grach"]=17, ["PB 6P9"]=8, ["P320"]=21, ["Uzi"]=25,
            ["Taurus Judge"]=12, ["Taurus Model 66"]=12, ["CBJ-MS"]=50,
            ["CZ Scorpion EVO Micro K"]=20, ["Obrez Mosin"]=10, ["Gepard PDW"]=25,
            ["Colt Python"]=12, ["HK P30L"]=15, ["Maxim 9"]=17, ["TP9SFx"]=20,
            ["Desert Eagle"]=7, ["M320"]=3
        };

    public static RuntimeWeaponDefinition Get(string name)
    {
        _cache ??= Load();
        if (_cache.TryGetValue(name, out var definition)) return definition;
        return _cache["Glock 17"];
    }

    public static IReadOnlyList<RuntimeWeaponDefinition> All()
    {
        _cache ??= Load();
        return _cache.Values.OrderBy(x => x.Slot, StringComparer.Ordinal)
            .ThenBy(x => x.Level)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<RuntimeWeaponDefinition> BySlot(string slot) =>
        All().Where(x => x.Slot.Equals(slot, StringComparison.Ordinal)).ToArray();

    private static IReadOnlyDictionary<string, RuntimeWeaponDefinition> Load()
    {
        var catalogText = global::Godot.FileAccess.GetFileAsString("res://Content/weapons/catalog.json");
        using var catalog = JsonDocument.Parse(catalogText);
        var result = new Dictionary<string, RuntimeWeaponDefinition>(StringComparer.Ordinal);

        foreach (var item in catalog.RootElement.GetProperty("weapons").EnumerateArray())
        {
            var path = item.GetProperty("path").GetString() ?? "";
            if (path.Length == 0) continue;

            var text = global::Godot.FileAccess.GetFileAsString("res://Content/weapons/" + path);
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var stats = root.GetProperty("stats");

            var name = root.GetProperty("name").GetString() ?? "";
            var slot = String(stats, "Slot", item.GetProperty("slot").GetString() ?? "");
            var weaponType = String(stats, "WeaponType", "Gun");
            var type = String(stats, "Type", weaponType);
            var damage = Float(stats, "Damage", 0);
            var level = Int(stats, "Level", name is "Sawn Off Shotgun" or "Glock 17" or "2x4" ? 1 : 999);
            var price = Int(stats, "Price", 0);
            var magazine = Int(stats, "Mag", 0);
            var reserve = Int(stats, "Pool", 0);
            var moduleHasAmmoPickup = stats.TryGetProperty("AmmoPickup", out _);
            var wikiHasAmmoPickup = VerifiedAmmoPickups.TryGetValue(name, out var verifiedPickup);
            var ammoPickupVerified = moduleHasAmmoPickup || wikiHasAmmoPickup;
            var ammoPickup = moduleHasAmmoPickup
                ? Int(stats, "AmmoPickup", Math.Max(1, magazine))
                : wikiHasAmmoPickup ? verifiedPickup : Math.Max(1, magazine);
            var rpm = Double(stats, "RPM", 0);
            var rangeVerified = stats.TryGetProperty("Distance", out _) || stats.TryGetProperty("Range", out _);
            var range = Float(stats, "Distance", Float(stats, "Range", weaponType == "Melee" ? 4.5f : 1000f));
            var spread = Float(stats, "Spread", 0);
            var maxPen = Int(stats, "MaxPen", 0);
            var bladed = Bool(stats, "Bladed", false);
            var projectileType = OptionalString(stats, "ProjectileType");

            var reloadSeconds = Duration(root, "Reload", 0);
            var actionSeconds = weaponType == "Melee"
                ? Duration(root, "Swing", 0.7)
                : Duration(root, "Fire", rpm > 0 ? 60.0 / rpm : 0.25);

            if (name.Length == 0 || slot.Length == 0) continue;
            result[name] = new RuntimeWeaponDefinition(
                name, slot, weaponType, type, damage, level, price,
                magazine, reserve, ammoPickup, ammoPickupVerified, rpm,
                range, rangeVerified, spread, maxPen, reloadSeconds,
                actionSeconds, bladed, projectileType);
        }

        return result;
    }

    private static double Duration(JsonElement root, string name, double fallback)
    {
        if (!root.TryGetProperty("animation_durations", out var durations)) return fallback;
        if (!durations.TryGetProperty(name, out var entry)) return fallback;
        return Double(entry, "sum_segment_seconds", fallback);
    }

    private static string String(JsonElement e, string name, string fallback) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static string? OptionalString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int Int(JsonElement e, string name, int fallback)
    {
        if (!e.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number) return fallback;
        return value.TryGetInt32(out var number) ? number : (int)Math.Round(value.GetDouble());
    }

    private static float Float(JsonElement e, string name, float fallback)
    {
        if (!e.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number) return fallback;
        return value.GetSingle();
    }

    private static double Double(JsonElement e, string name, double fallback)
    {
        if (!e.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number) return fallback;
        return value.GetDouble();
    }

    private static bool Bool(JsonElement e, string name, bool fallback)
    {
        if (!e.TryGetProperty(name, out var value) ||
            value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return fallback;
        return value.GetBoolean();
    }
}

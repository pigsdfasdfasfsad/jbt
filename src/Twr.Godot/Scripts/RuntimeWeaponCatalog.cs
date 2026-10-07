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
            var ammoPickupVerified = stats.TryGetProperty("AmmoPickup", out _);
            var ammoPickup = Int(stats, "AmmoPickup", Math.Max(1, magazine));
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

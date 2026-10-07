using System.Text.Json;

namespace Twr.Godot;

public sealed record RuntimeMapDefinition(
    string Name,
    string Location,
    string Skybox,
    IReadOnlyList<string> Objectives);

public static class MapCatalogRuntime
{
    private static IReadOnlyDictionary<string, RuntimeMapDefinition>? _cache;

    public static RuntimeMapDefinition Get(string mapName)
    {
        _cache ??= LoadAll();
        if (_cache.TryGetValue(mapName, out var definition)) return definition;
        throw new ArgumentException("Unknown release map: " + mapName);
    }

    public static IReadOnlyList<RuntimeMapDefinition> All()
    {
        _cache ??= LoadAll();
        return _cache.Values.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyDictionary<string, RuntimeMapDefinition> LoadAll()
    {
        var json = global::Godot.FileAccess.GetFileAsString("res://Content/maps/maps.json");
        using var document = JsonDocument.Parse(json);
        var result = new Dictionary<string, RuntimeMapDefinition>(StringComparer.Ordinal);

        foreach (var entry in document.RootElement.GetProperty("maps").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString() ?? "";
            var location = entry.GetProperty("location").GetString() ?? "";
            var skybox = entry.GetProperty("skybox").GetString() ?? "";
            var objectives = entry.GetProperty("objectives").EnumerateArray()
                .Select(x => x.GetString() ?? "")
                .Where(x => x.Length > 0)
                .ToArray();
            if (name.Length > 0)
                result[name] = new RuntimeMapDefinition(name, location, skybox, objectives);
        }

        return result;
    }
}

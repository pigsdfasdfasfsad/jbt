using System.Text.Json;

namespace Twr.Godot;

public sealed record RuntimePerkDefinition(string Name,string Category,int Level,string Description);

public static class RuntimePerkCatalog
{
    private static IReadOnlyList<RuntimePerkDefinition>? _cache;

    public static IReadOnlyList<RuntimePerkDefinition> All()
    {
        if(_cache is not null)return _cache;
        var json=global::Godot.FileAccess.GetFileAsString("res://Content/perks/perks.json");
        using var document=JsonDocument.Parse(json);
        _cache=document.RootElement.GetProperty("perks").EnumerateArray()
            .Select(x=>new RuntimePerkDefinition(
                x.GetProperty("name").GetString() ?? "",
                x.GetProperty("category").GetString() ?? "",
                x.GetProperty("Level").GetInt32(),
                x.GetProperty("Desc").GetString() ?? ""))
            .OrderBy(x=>x.Level)
            .ThenBy(x=>x.Category,StringComparer.Ordinal)
            .ToArray();
        return _cache;
    }
}

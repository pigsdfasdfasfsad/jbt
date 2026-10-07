using System.Text.Json;

namespace Twr.Godot;

public sealed record RuntimeFortificationDefinition(
    string Name,
    int Count,
    int Swings,
    float? Damage,
    float? Radius,
    int? MaxPen);

public static class FortificationCatalogRuntime
{
    public static IReadOnlyList<RuntimeFortificationDefinition> Load()
    {
        var json = global::Godot.FileAccess.GetFileAsString("res://Content/fortifications/fortifications.json");
        using var document = JsonDocument.Parse(json);
        var result = new List<RuntimeFortificationDefinition>();

        foreach (var entry in document.RootElement.GetProperty("fortifications").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString() ?? "";
            var count = entry.GetProperty("count").GetInt32();
            var swings = entry.GetProperty("swings").GetInt32();
            float? damage = entry.TryGetProperty("damage", out var d) ? d.GetSingle() : null;
            float? radius = entry.TryGetProperty("radius", out var r) ? r.GetSingle() : null;
            int? maxPen = entry.TryGetProperty("maxPen", out var p) ? p.GetInt32() : null;

            if (!string.IsNullOrWhiteSpace(name))
                result.Add(new RuntimeFortificationDefinition(name, count, swings, damage, radius, maxPen));
        }

        return result;
    }
}

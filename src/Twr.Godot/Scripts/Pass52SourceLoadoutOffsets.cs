using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass52: read the ORIGINAL author-supplied CFrame.new LoadoutOffset from
/// normalized inert weapon JSON; never execute or evaluate source Luau.
/// The source module also positions equipped models at Lobby.LoadoutPoints
/// [slot].CFrame * Stats.LoadoutOffset. Unknown expressions stay unmodified.
/// </summary>
public static class Pass52SourceLoadoutOffsets
{
    private static Dictionary<string, Transform3D>? _byWeapon;
    private static readonly Transform3D Identity = Transform3D.Identity;
    private const float Stud = RobloxUnits.MetersPerStud;

    public static int SourceOffsetCount
    {
        get { EnsureLoaded(); return _byWeapon!.Count; }
    }

    public static bool TryGet(string? name, out Transform3D offset)
    {
        EnsureLoaded();
        if (name is not null && _byWeapon!.TryGetValue(name, out offset))
            return true;
        offset = Identity;
        return false;
    }

    private static void EnsureLoaded()
    {
        if (_byWeapon is not null) return;
        var found = new Dictionary<string, Transform3D>(StringComparer.Ordinal);
        try
        {
            using var catalog = JsonDocument.Parse(
                global::Godot.FileAccess.GetFileAsString(
                    "res://Content/weapons/catalog.json"));
            foreach (var item in catalog.RootElement
                .GetProperty("weapons").EnumerateArray())
            {
                var path = item.GetProperty("path").GetString() ?? "";
                if (path.Length < 5 || path.Length > 140 ||
                    !path.EndsWith(".json", StringComparison.Ordinal) ||
                    path.Contains("..", StringComparison.Ordinal) ||
                    path.StartsWith('/') || path.Contains('\\'))
                    continue;
                using var record = JsonDocument.Parse(
                    global::Godot.FileAccess.GetFileAsString(
                        "res://Content/weapons/" + path));
                var root = record.RootElement;
                var name = root.GetProperty("name").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(name) || name.Length > 128 ||
                    !root.TryGetProperty("stats", out var stats) ||
                    !stats.TryGetProperty("LoadoutOffset", out var field) ||
                    !field.TryGetProperty("expression", out var expression))
                    continue;
                if (TryParse(expression.GetString(), out var frame))
                    found[name] = frame;
            }
        }
        catch (Exception ex)
        {
            // Missing/corrupt normalized content never blocks menu navigation.
            GD.PushWarning("TWR_PASS52_LOADOUT_OFFSETS_FALLBACK " + ex.Message);
        }
        _byWeapon = found;
        GD.Print($"TWR_PASS52_SOURCE_OFFSETS_READY count={_byWeapon.Count}");
    }

    /// <summary>
    /// Strict CFrame.new(x,y,z,r00,r01,r02,r10,r11,r12,r20,r21,r22)
    /// numeric literal parser, with the two RPG-7 source half-turns.
    /// This is not a Luau interpreter.
    /// </summary>
    public static bool TryParse(string? expression, out Transform3D frame)
    {
        frame = Identity;
        if (expression is null || expression.Length > 384)
            return false;
        // The two original RPG launchers append exactly this Y half turn.
        // No arbitrary Luau expression, callback or method can be evaluated.
        const string HalfTurn = " * CFrame.Angles(0, math.pi, 0)";
        var halfTurn = expression.EndsWith(HalfTurn,StringComparison.Ordinal);
        var literal = halfTurn ? expression[..^HalfTurn.Length] : expression;
        if (!literal.StartsWith("CFrame.new(",StringComparison.Ordinal) ||
            !literal.EndsWith(')'))
            return false;
        var values = literal.AsSpan(11,literal.Length - 12)
            .ToString().Split(',', StringSplitOptions.TrimEntries);
        if (values.Length != 12) return false;
        var n = new float[12];
        for (var i=0; i<n.Length; i++)
        {
            if (!float.TryParse(values[i], NumberStyles.Float,
                CultureInfo.InvariantCulture, out n[i]) ||
                !float.IsFinite(n[i]))
                return false;
            if (i < 3 && Math.Abs(n[i]) > 20f) return false;
            if (i >= 3 && Math.Abs(n[i]) > 1.1f) return false;
        }
        // Reflection Z maps original Roblox stud CFrames to -Z Godot meters.
        var basis = new Basis(
            new Vector3(n[3], n[6], -n[9]),
            new Vector3(n[4], n[7], -n[10]),
            new Vector3(-n[5], -n[8], n[11]));
        if (!float.IsFinite(basis.Determinant()) ||
            Math.Abs(basis.Determinant()) < .3f)
            return false;
        if (halfTurn)
            basis *= new Basis(Vector3.Up, Mathf.Pi);
        frame = new Transform3D(basis,
            new Vector3(n[0], n[1], -n[2]) * Stud);
        return true;
    }
}

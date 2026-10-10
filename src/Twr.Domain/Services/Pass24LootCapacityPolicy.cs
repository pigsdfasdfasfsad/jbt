using System;
using System.Collections.Generic;

namespace Twr.Domain.Services;

/// <summary>
/// Admission policy for the legacy dictionary-backed 16-slot inventory.
/// A zero-count item never occupies a slot. Source-equivalent stack limits
/// remain authoritative; the presentation layer cannot grant items.
/// </summary>
public static class Pass24LootCapacityPolicy
{
    public const int DefaultSlots = 16;

    public static int OccupiedSlots(IReadOnlyDictionary<string,int> inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var used = 0;
        foreach (var pair in inventory)
        {
            if (pair.Value < 0) throw new ArgumentException("Negative inventory quantity");
            if (pair.Value > 0) used++;
        }
        return used;
    }

    public static bool CanGrant(IReadOnlyDictionary<string,int> inventory,
        string itemId, int amount, int maxCount, int capacity = DefaultSlots)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || maxCount <= 0 ||
            capacity < 1 || capacity > 4096) return false;
        var current = inventory.TryGetValue(itemId, out var count) ? count : 0;
        if (current < 0 || current >= maxCount ||
            (long)current + amount > int.MaxValue) return false;
        return current > 0 || OccupiedSlots(inventory) < capacity;
    }
}

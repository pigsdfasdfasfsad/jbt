using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass109: a small, original-inspired Repair emblem generated entirely from
/// local Godot vector geometry. Its circular cream field, dark double rim,
/// diagonal open-end wrench and ring-ended handle reference the owner-provided
/// RepairObjective.png. This is NOT the original Roblox image binary.
/// </summary>
public partial class Pass109RepairObjectiveBadge : Control
{
    public const float DesignSize = 42f;
    private static readonly Color Paper = new(.88f, .86f, .83f);
    private static readonly Color Ink = new(.19f, .14f, .145f);

    public override void _Ready()
    {
        Name = "Pass109RepairObjectiveBadge";
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
    }

    public override void _Draw()
    {
        var center = new Vector2(21f, 21f);
        // Source-style contrasting concentric boundary and flat pale field.
        DrawCircle(center, 20f, Paper);
        DrawArc(center, 18.45f, 0, Mathf.Tau, 56,
            Ink, 2.0f, true);

        // Diagonal wrench silhouette directed from upper-left to lower-right.
        // Construct the full jaw with thick vector strokes; the opening
        // remains genuinely open, not an overlaid bitmap/magic text glyph.
        DrawLine(new Vector2(15.0f, 15.9f),
            new Vector2(29.0f, 30f), Ink, 5.2f, true);
        DrawCircle(new Vector2(30f, 31f), 3.3f, Ink);
        DrawCircle(new Vector2(30f, 31f), 1.45f, Paper);
        var jaw = new Vector2(13.6f, 13.2f);
        DrawArc(jaw, 6.0f, -.28f, 2.55f, 25,
            Ink, 4.4f, true);
        DrawLine(new Vector2(9.1f, 8.1f),
            new Vector2(15.2f, 11.7f), Ink, 3.0f, true);
        DrawLine(new Vector2(8.7f, 15.5f),
            new Vector2(11.0f, 18.5f), Ink, 3.0f, true);
    }
}

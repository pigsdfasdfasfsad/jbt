using Godot;

namespace Twr.Godot;

/// <summary>
/// Adds owner-supplied map-reference cards without replacing the original
/// functional map buttons. Absent artwork leaves the old menu untouched.
/// </summary>
public static class Pass27MapCardDecoration
{
    public static void Apply(Button button, string mapName)
    {
        var texture = Pass27SourceArtCatalog.MapCard(mapName);
        if (texture is null) return;
        button.Alignment = HorizontalAlignment.Right;
        button.AddChild(new TextureRect
        {
            Name = "SourceMapCard",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            OffsetLeft = 7,
            OffsetTop = 3,
            OffsetRight = 211,
            OffsetBottom = 51,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }
}

using Godot;

namespace Twr.Godot;

/// <summary>
/// Uses owner-held offline recovered weapon silhouette references in the
/// Armory/Loadout. The button, purchase eligibility, credit checks and loadout
/// state remain controlled by the existing runtime, never by this decoration.
/// A missing icon leaves the text-only armory fully usable.
/// </summary>
public static class Pass46ArmoryArtDecoration
{
    public static bool Apply(Button button, string weaponName)
    {
        var icon = Pass27SourceArtCatalog.WeaponIcon(weaponName);
        if (icon is null) return false;

        button.CustomMinimumSize = new Vector2(1080, 59);
        // Align the existing actionable text opposite the art to avoid
        // overlaying a weapon silhouette over price, slot and lock status.
        button.Alignment = HorizontalAlignment.Right;
        button.TooltipText = weaponName;
        button.AddChild(new TextureRect
        {
            Name = "SourceWeaponSilhouette",
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            OffsetLeft = 9,
            OffsetTop = 5,
            OffsetRight = 163,
            OffsetBottom = 54,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        return true;
    }
}

using Godot;
using Twr.Domain.Model;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class GameplayHud : CanvasLayer
{
    private Label _top = null!;
    private Label _health = null!;
    private Label _ammo = null!;
    private Label _credits = null!;
    private Label _banner = null!;
    private Label _objective = null!;
    private Label _utility = null!;

    public override void _Ready()
    {
        _top = MakeLabel(24, 20, 760, 42, 24);
        _health = MakeLabel(24, 650, 360, 38, 22);
        _ammo = MakeLabel(860, 650, 390, 38, 22);
        _credits = MakeLabel(24, 610, 500, 34, 18);
        _banner = MakeLabel(250, 90, 780, 70, 32);
        _banner.HorizontalAlignment = HorizontalAlignment.Center;
        _objective = MakeLabel(250, 150, 780, 42, 20);
        _objective.HorizontalAlignment = HorizontalAlignment.Center;
        _utility = MakeLabel(250, 195, 780, 36, 17);
        _utility.HorizontalAlignment = HorizontalAlignment.Center;

        var crosshair = MakeLabel(620, 342, 40, 40, 26);
        crosshair.Text = "+";
        crosshair.HorizontalAlignment = HorizontalAlignment.Center;
    }

    private Label MakeLabel(float left, float top, float width, float height, int size)
    {
        var label = new Label
        {
            OffsetLeft = left,
            OffsetTop = top,
            OffsetRight = left + width,
            OffsetBottom = top + height
        };
        label.AddThemeFontSizeOverride("font_size", size);
        AddChild(label);
        return label;
    }

    public void UpdateState(PlayerState player, MatchState match, string stage, double seconds, string weaponName)
    {
        _top.Text = $"REGULAR  |  {match.MapName.ToUpperInvariant()}  |  WAVE {match.Wave}/15  |  {stage}  {FormatTime(seconds)}";
        _health.Text = $"HEALTH  {Math.Ceiling(player.Health):0} / {player.MaxHealth:0}   ARMOR {Math.Ceiling(player.ArmorDurability):0}";

        if (weaponName == StarterLoadoutService.TwoByFour)
        {
            _ammo.Text = "2x4   MELEE";
        }
        else
        {
            var loaded = player.Ammo.GetValueOrDefault(weaponName);
            var reserve = player.ReserveAmmo.GetValueOrDefault(weaponName);
            _ammo.Text = $"{weaponName.ToUpperInvariant()}   {loaded} / {reserve}";
        }

        _credits.Text = $"CREDITS ${player.Credits:N0}    XP {player.Xp:N0}    [1] SAWN OFF  [2] GLOCK  [3] 2x4";
    }

    public void SetBanner(string text) => _banner.Text = text;
    public void SetObjective(string text) => _objective.Text = text;
    public void SetUtility(string text) => _utility.Text = text;

    private static string FormatTime(double seconds)
    {
        var total = Math.Max(0, (int)Math.Ceiling(seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}

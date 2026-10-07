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

    public override void _Ready()
    {
        _top = MakeLabel(24, 20, 760, 42, 24);
        _health = MakeLabel(24, 650, 360, 38, 22);
        _ammo = MakeLabel(940, 650, 310, 38, 22);
        _credits = MakeLabel(24, 610, 400, 34, 18);
        _banner = MakeLabel(250, 90, 780, 70, 32);
        _banner.HorizontalAlignment = HorizontalAlignment.Center;

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

    public void UpdateState(PlayerState player, MatchState match, string stage, double seconds)
    {
        _top.Text = $"REGULAR  |  {match.MapName.ToUpperInvariant()}  |  WAVE {match.Wave}/15  |  {stage}  {FormatTime(seconds)}";
        _health.Text = $"HEALTH  {Math.Ceiling(player.Health):0} / {player.MaxHealth:0}";
        var loaded = player.Ammo.GetValueOrDefault(StarterLoadoutService.Glock17);
        var reserve = player.ReserveAmmo.GetValueOrDefault(StarterLoadoutService.Glock17);
        _ammo.Text = $"GLOCK 17   {loaded} / {reserve}";
        _credits.Text = $"CREDITS ${player.Credits:N0}    XP {player.Xp:N0}";
    }

    public void SetBanner(string text) => _banner.Text = text;

    private static string FormatTime(double seconds)
    {
        var total = Math.Max(0, (int)Math.Ceiling(seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}

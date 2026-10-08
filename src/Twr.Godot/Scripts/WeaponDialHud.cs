using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Approximate circular weapon/ammunition HUD guided by the owner-supplied
/// WeaponHUD.png source reference. Drawn entirely with Godot primitives so
/// no network fonts, external textures, or original private art are required.
/// </summary>
public partial class WeaponDialHud : Control
{
    private Label _magazine = null!;
    private Label _reserve = null!;
    private Label _weapon = null!;
    private Label _category = null!;
    private float _ratio = 1f;
    private bool _melee;
    private bool _throwable;
    private string _display = "";
    private int _lastMag = -1;
    private int _lastReserve = -1;

    private static readonly Color Cream = new(0.89f, 0.88f, 0.80f);
    private static readonly Color Green = new(0.35f, 0.67f, 0.35f);
    private static readonly Color Ring = new(0.34f, 0.37f, 0.37f, 0.81f);

    public override void _Ready()
    {
        Name = "OriginalStyleAmmoDial";
        AnchorLeft = 1;
        AnchorRight = 1;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetLeft = -290;
        OffsetRight = -18;
        OffsetTop = -293;
        OffsetBottom = -18;
        MouseFilter = MouseFilterEnum.Ignore;

        _magazine = TextAt("00", 39, 86, 103, 47, 43);
        _reserve = TextAt("000", 42, 135, 118, 49, 38);
        _weapon = TextAt("GLOCK 17", 22, 244, 242, 28, 15);
        _weapon.HorizontalAlignment = HorizontalAlignment.Center;
        _category = TextAt("SIDEARM", 21, 219, 245, 25, 13);
        _category.HorizontalAlignment = HorizontalAlignment.Center;
    }

    public void Display(
        string name, int loaded, int reserve, int magazineCapacity,
        string category, bool melee, bool throwable)
    {
        var nextRatio = melee ? 1f :
            Math.Clamp(loaded / (float)Math.Max(1, magazineCapacity), 0f, 1f);
        var changed = Math.Abs(_ratio - nextRatio) > 0.0001f ||
            _melee != melee || _throwable != throwable || _display != category ||
            _lastMag != loaded || _lastReserve != reserve;
        _ratio = nextRatio;
        _melee = melee;
        _throwable = throwable;
        _display = category;
        _lastMag = loaded;
        _lastReserve = reserve;

        _magazine.Text = melee ? "--" : loaded.ToString("00");
        _reserve.Text = melee ? "MELEE" : throwable ? "READY" : reserve.ToString("000");
        _category.Text = category.ToUpperInvariant();
        _weapon.Text = name.ToUpperInvariant();
        if (changed) QueueRedraw();
    }

    public override void _Draw()
    {
        var center = new Vector2(134f, 133f);
        DrawCircle(center, 119f, new Color(0.035f, 0.042f, 0.048f, 0.68f));
        DrawArc(center, 119f, -Mathf.Pi * 0.83f, Mathf.Pi * 1.17f,
            96, Ring, 5.5f, true);
        DrawArc(center, 108f, -Mathf.Pi * 0.76f, Mathf.Pi * 1.06f,
            96, new Color(0.17f, 0.20f, 0.19f, 0.97f), 8.4f, true);

        // Eight green segments indicate the magazine's fractional capacity,
        // matching the segmented circular visual language of the source.
        for (var segment = 0; segment < 8; segment++)
        {
            var a = -Mathf.Pi * .73f + segment * .257f;
            var filled = _melee || _ratio * 8f > segment + .02f;
            DrawArc(center, 108f, a, a + .208f,
                12, filled ? Green : new Color(.19f,.24f,.19f,.55f),
                8f, true);
        }
        DrawArc(center, 90f, Mathf.Pi * .74f, Mathf.Pi * 1.27f, 25,
            Cream, 2.5f, true);
        DrawLine(new Vector2(35,204),new Vector2(225,204),
            new Color(.48f,.51f,.48f,.42f),1.0f,true);

        // A built-in stylized weapon silhouette gives a recognizable
        // reference-scale weapon readout until real weapon icons are imported.
        var silhouette = new Color(.87f, .88f, .81f, .91f);
        if (_throwable)
        {
            DrawRect(new Rect2(177,104,22,33),silhouette);
            DrawRect(new Rect2(183,97,9,8),silhouette);
            DrawLine(new Vector2(185,96),new Vector2(197,96),silhouette,3f);
        }
        else if (_melee)
        {
            DrawLine(new Vector2(177,102),new Vector2(213,158),silhouette,8f,true);
            DrawRect(new Rect2(173,98,24,10),silhouette);
        }
        else
        {
            DrawRect(new Rect2(171,107,65,10),silhouette);
            DrawRect(new Rect2(181,117,28,8),silhouette);
            DrawLine(new Vector2(207,122),new Vector2(221,146),silhouette,9f,true);
            DrawRect(new Rect2(230,107,11,4),silhouette);
            DrawRect(new Rect2(174,100,19,5),silhouette);
        }
    }

    private Label TextAt(
        string value,float x,float y,float width,float height,int font)
    {
        var label = new Label
        {
            Text = value,
            OffsetLeft = x,
            OffsetTop = y,
            OffsetRight = x + width,
            OffsetBottom = y + height,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size",font);
        label.AddThemeColorOverride("font_color",Cream);
        AddChild(label);
        return label;
    }
}

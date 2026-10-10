using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Four by four read-only UI for the legacy authoritative dictionary inventory.
/// Unlike a native indexed inventory, it cannot rearrange or split stacks.
/// Press I to toggle. Never treats world pickups as collected.
/// </summary>
public partial class Pass29InventoryPanel : CanvasLayer
{
    public LocalSessionNode? Runtime { get; set; }
    private Control _panel = null!;
    private Label _heading = null!;
    private readonly Label[] _slots = new Label[16];
    private double _refresh;
    private bool _open;

    public override void _Ready()
    {
        _panel = new Control {
            Name = "InventoryGrid16", AnchorLeft = .5f, AnchorRight = .5f,
            AnchorTop = .5f, AnchorBottom = .5f,
            OffsetLeft = -245, OffsetRight = 245,
            OffsetTop = -230, OffsetBottom = 230,
            Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_panel);
        _panel.AddChild(new ColorRect {
            OffsetRight = 490, OffsetBottom = 460,
            Color = new Color(.025f,.034f,.045f,.94f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        _heading = MakeLabel(_panel, 18, 12, 453, 38, 22);
        for (var i = 0; i < 16; i++)
        {
            var x = 18 + i % 4 * 117;
            var y = 61 + i / 4 * 81;
            var tile = new ColorRect {
                OffsetLeft = x, OffsetRight = x + 108,
                OffsetTop = y, OffsetBottom = y + 73,
                Color = new Color(.10f,.14f,.18f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _panel.AddChild(tile);
            _slots[i] = MakeLabel(tile, 6, 6, 95, 63, 13);
        }
        var footer = MakeLabel(_panel, 18, 397, 450, 46, 13);
        footer.Text = "I / Esc: close  |  Read-only item stacks  |  16 slots";
    }

    private static Label MakeLabel(Node parent, float x, float y,
        float w, float h, int font)
    {
        var result = new Label {
            OffsetLeft = x, OffsetRight = x + w,
            OffsetTop = y, OffsetBottom = y + h,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        result.AddThemeFontSizeOverride("font_size", font);
        result.AddThemeColorOverride("font_color", new Color(.88f,.92f,.92f));
        parent.AddChild(result);
        return result;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
            return;
        if (key.Keycode != Key.I && !(key.Keycode == Key.Escape && _open))
            return;
        if (key.Keycode == Key.I) _open = !_open;
        else _open = false;
        _panel.Visible = _open;
        _refresh = 0;
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!_open) return;
        _refresh -= delta;
        if (_refresh > 0) return;
        _refresh = .16;
        var inventory = Runtime?.Player?.Inventory;
        var items = inventory is null
            ? Array.Empty<KeyValuePair<string,int>>()
            : inventory.Where(x => x.Value > 0)
                .OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
        _heading.Text = "INVENTORY    " + items.Length + " / 16" +
            (items.Length >= 16 ? "   FULL" : "");
        for (var i = 0; i < 16; i++)
            _slots[i].Text = i < items.Length
                ? $"{i+1:00}   {items[i].Key}\nx{items[i].Value}"
                : $"{i+1:00}    EMPTY";
    }
}

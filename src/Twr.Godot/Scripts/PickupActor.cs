using Godot;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class PickupActor : Node3D
{
    public string PickupType { get; set; } = "Bandages";
    public FirstPersonPlayer? Player { get; set; }
    public LocalSessionNode? Runtime { get; set; }
    public int GrantCount { get; set; } = 1;
    public Action<PickupActor>? Collected { get; set; }

    private bool _interactWasDown;

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(0.55f, 0.35f, 0.55f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = PickupColor(PickupType),
                    Roughness = 0.8f
                }
            }
        });

        AddChild(new Label3D
        {
            Text = $"{PickupType}\n[E]",
            Position = new Vector3(0, 0.8f, 0),
            FontSize = 32,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        });
    }

    public override void _Process(double delta)
    {
        if (Player is null || Runtime?.Player is null)
            return;

        var down = Input.IsKeyPressed(Key.E);
        var pressed = down && !_interactWasDown;
        _interactWasDown = down;

        if (!pressed || Player.GlobalPosition.DistanceTo(GlobalPosition) > 2.5f)
            return;

        if (!TryCollect())
            return;

        Collected?.Invoke(this);
        QueueFree();
    }

    private bool TryCollect()
    {
        if (Player is null || Runtime?.Player is null)
            return false;

        var state = Runtime.Player;
        switch (PickupType)
        {
            case "Bandages":
            {
                var before = state.Health;
                Runtime.HealPlayer(20);
                return state.Health > before;
            }
            case "Medkit":
            {
                var before = state.Health;
                Runtime.HealPlayer(0, true);
                return state.Health > before;
            }
            case "Body Armor":
            {
                var before = state.ArmorDurability;
                Runtime.EquipBodyArmor();
                return state.ArmorDurability > before;
            }
            case "Ammo":
            {
                var weapon = Player.EquippedWeaponName;
                if (weapon == StarterLoadoutService.TwoByFour)
                    return false;

                var before = state.ReserveAmmo.GetValueOrDefault(weapon);
                if (weapon == StarterLoadoutService.SawnOff)
                    Runtime.GrantAmmo(weapon, 8, 32);
                else
                    Runtime.GrantAmmo(weapon, 18, 136); // APPROXIMATED: no recovered Glock AmmoPickup field.
                return state.ReserveAmmo.GetValueOrDefault(weapon) > before;
            }
            default:
                Runtime.GrantItem(PickupType, GrantCount);
                return true;
        }
    }

    private static Color PickupColor(string type) => type switch
    {
        "Bandages" => new Color(0.85f, 0.82f, 0.72f),
        "Medkit" => new Color(0.72f, 0.12f, 0.12f),
        "Body Armor" => new Color(0.12f, 0.35f, 0.78f),
        "Ammo" => new Color(0.68f, 0.56f, 0.18f),
        _ => new Color(0.42f, 0.42f, 0.42f)
    };
}

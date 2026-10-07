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
                Runtime.HealPlayer(Runtime.HasPerk("Medic") ? 26 : 20);
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
            case "Energy Drink":
                return Runtime.ActivateEnergyDrink();
            case "Gas Mask":
                return Runtime.ActivateGasMask();
            case "Ammo":
            {
                // VERIFIED: one Ammo pickup supplies both primary and
                // secondary independently, capped at each weapon's reserve.
                var changed = false;
                foreach (var weapon in new[] { Player.PrimaryWeaponName, Player.SecondaryWeaponName })
                {
                    var spec = RuntimeWeaponCatalog.Get(weapon);
                    if (spec.IsMelee || spec.Reserve <= 0) continue;

                    var before = state.ReserveAmmo.GetValueOrDefault(weapon);
                    Runtime.GrantAmmo(weapon, spec.AmmoPickup, spec.Reserve);
                    changed |= state.ReserveAmmo.GetValueOrDefault(weapon) > before;
                }
                return changed;
            }
            default:
            {
                var maxCount=PickupType switch
                {
                    "Clap Bomb" => Runtime.HasPerk("Play Maker") ? 2 : 1,
                    "Barbed Wire" => Runtime.HasPerk("Fortifier") ? 4 : 2,
                    "Jack" => 1,
                    "50 Cal" => 1,
                    "Frag" or "Molotov" or "Nerve Gas" => 1,
                    _ => int.MaxValue
                };
                return Runtime.GrantItem(PickupType,GrantCount,maxCount);
            }
        }
    }

    private static Color PickupColor(string type) => type switch
    {
        "Bandages" => new Color(0.85f, 0.82f, 0.72f),
        "Medkit" => new Color(0.72f, 0.12f, 0.12f),
        "Body Armor" => new Color(0.12f, 0.35f, 0.78f),
        "Ammo" => new Color(0.68f, 0.56f, 0.18f),
        "Energy Drink" => new Color(0.20f,0.62f,0.86f),
        "Gas Mask" => new Color(0.16f,0.18f,0.16f),
        "Frag" => new Color(0.18f,0.25f,0.15f),
        "Molotov" => new Color(0.82f,0.31f,0.05f),
        "Nerve Gas" => new Color(0.09f,0.16f,0.10f),
        _ => new Color(0.42f, 0.42f, 0.42f)
    };
}

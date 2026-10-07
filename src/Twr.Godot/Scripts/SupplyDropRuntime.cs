using Godot;

namespace Twr.Godot;

public partial class SupplyDropRuntime : Node3D
{
    public float GroundY { get; set; }
    public Action<Vector3>? Landed { get; set; }

    private bool _landed;

    // APPROXIMATED: the reference explicitly states pallet landing time has no fixed value.
    private const float DescentSpeed = 3.0f;

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(2.4f, 0.45f, 2.4f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.29f, 0.23f, 0.12f),
                    Roughness = 0.95f
                }
            }
        });

        AddChild(new Label3D
        {
            Text = "SUPPLY PALLET",
            Position = new Vector3(0, 1.0f, 0),
            FontSize = 38,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        });
    }

    public override void _Process(double delta)
    {
        if (_landed)
            return;

        var position = GlobalPosition;
        position.Y -= DescentSpeed * (float)delta;
        if (position.Y <= GroundY)
        {
            position.Y = GroundY;
            _landed = true;
            GlobalPosition = position;
            Landed?.Invoke(position);
            QueueFree();
            return;
        }

        GlobalPosition = position;
    }
}

using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Native exported-Windows physics fixture; fabricated floor and obstacle
/// only. It does not contain or pretend to validate the private source map.
/// </summary>
public partial class Pass119OriginalSourceSpawnAcceptance : Node3D
{
    public override async void _Ready()
    {
        try
        {
            await Run();
            GD.Print("TWR_SMOKE_PASS119_SOURCE_SPAWN_OK" +
                " exact_original_xz=true unsupported_rejected=true" +
                " wall_embedded_rejected=true near_player_rejected=true" +
                " original_private_geometry_unverified=true");
            GetTree().CallDeferred("quit", 0);
        }
        catch (Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS119_SOURCE_SPAWN_FAILED " +
                e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
            GetTree().CallDeferred("quit", 1);
        }
    }

    private void AddCollider(string name, Vector3 centre, Vector3 size)
    {
        var body = new StaticBody3D
        {
            Name = name,
            Position = centre,
            CollisionLayer = 1u,
            CollisionMask = 0u
        };
        AddChild(body);
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = size }
        });
    }

    private async System.Threading.Tasks.Task Frames(int count)
    {
        for(var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async System.Threading.Tasks.Task Run()
    {
        // Distant marker is outside 8x8 floor; centre marker is blocked.
        AddCollider("Pass119SyntheticFloor",
            new Vector3(0, -.25f, 0), new Vector3(8, .5f, 8));
        AddCollider("Pass119SyntheticWall",
            new Vector3(0, .9f, 0), new Vector3(1.4f, 1.8f, 1.4f));
        var player = new CharacterBody3D
        {
            Name = "Pass119SyntheticPlayer",
            Position = new Vector3(20, .8f, 20),
            CollisionLayer = 2u,
            CollisionMask = 1u
        };
        AddChild(player);
        player.AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = .30f, Height = 1.6f }
        });
        await Frames(4);

        var markers = new[]
        {
            new Vector3(12,0,0), // no floor
            new Vector3(0,0,0), // inside blocking wall
            new Vector3(2.5f,0,0) // supported original marker
        };
        var result = Pass119OriginalSourceSpawnSafety.TryFind(
            GetWorld3D(), markers, markers[0] + Vector3.Up * .8f,
            new Vector3(20,0,20), player.GetRid(), out var alternate);
        if (!result.HasValue || !alternate ||
            Math.Abs(result.Value.X - 2.5f) > .001f ||
            Math.Abs(result.Value.Z) > .001f ||
            Math.Abs(result.Value.Y - .90f) > .12f)
            throw new InvalidOperationException(
                "No physically supported alternate original marker");

        var unchanged = Pass119OriginalSourceSpawnSafety.TryFind(
            GetWorld3D(), markers, markers[2] + Vector3.Up * .8f,
            new Vector3(20,0,20), player.GetRid(), out var moved);
        if (!unchanged.HasValue || moved ||
            unchanged.Value.DistanceTo(result.Value) > .001f)
            throw new InvalidOperationException(
                "Already supported source position was moved");

        var invented = Pass119OriginalSourceSpawnSafety.TryFind(
            GetWorld3D(), markers, new Vector3(50,.8f,50),
            new Vector3(20,0,20), player.GetRid(), out _);
        if (invented.HasValue)
            throw new InvalidOperationException(
                "Fabricated marker erroneously accepted");

        var nearPlayer = Pass119OriginalSourceSpawnSafety.TryFind(
            GetWorld3D(), markers, markers[2] + Vector3.Up * .8f,
            new Vector3(2.5f,0,1), player.GetRid(), out _);
        if (nearPlayer.HasValue)
            throw new InvalidOperationException(
                "Near-player spawn incorrectly passed");

        var noSupport = Pass119OriginalSourceSpawnSafety.TryFind(
            GetWorld3D(), new[] { markers[0] },
            markers[0] + Vector3.Up * .8f,
            new Vector3(20,0,20), player.GetRid(), out _);
        if (noSupport.HasValue)
            throw new InvalidOperationException(
                "Unsupported only-source spawn incorrectly passed");

        GD.Print("TWR_PASS119_SOURCE_SPAWN_PHYSICS_OK" +
            " chosen=" + result.Value +
            " original_preferred=" + unchanged.Value);
    }
}

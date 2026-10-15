using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Exported-Windows native fixture: synthetic collision objects ONLY.
/// Original private map pack and source meshes stay on the owner's machine.
/// </summary>
public partial class Pass121OriginalPlayerSpawnAcceptance : Node3D
{
    public override async void _Ready()
    {
        try
        {
            await VerifyOriginalOnlyPlayerSpawnPhysics();
            GD.Print("TWR_SMOKE_PASS121_SOURCE_PLAYER_OK" +
                " original_xz_only=true blocked_marker_rejected=true" +
                " unsupported_floor_rejected=true capsule_clearance=true" +
                " player_rid_excluded=true source_private_map_unverified=true");
            GetTree().CallDeferred("quit", 0);
        }
        catch (Exception e)
        {
            GD.PushError("TWR_SMOKE_PASS121_SOURCE_PLAYER_FAILED " +
                e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
            GetTree().CallDeferred("quit", 1);
        }
    }

    private StaticBody3D AddSolid(string name, Vector3 position, Vector3 size)
    {
        var body = new StaticBody3D
        {
            Name = name, Position = position,
            CollisionLayer = 1u, CollisionMask = 0u
        };
        AddChild(body);
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = size }
        });
        return body;
    }

    private async System.Threading.Tasks.Task Frames(int n)
    {
        for (var i = 0; i < n; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private async System.Threading.Tasks.Task VerifyOriginalOnlyPlayerSpawnPhysics()
    {
        AddSolid("Pass121SyntheticFloor",
            new Vector3(0, -.25f, 0), new Vector3(8, .5f, 8));
        AddSolid("Pass121BlockingSolid",
            new Vector3(0, .9f, 0), new Vector3(1.5f, 1.8f, 1.5f));
        await Frames(4);

        // Simulates three already-recovered ORIGINAL markers, not generated
        // relocation coordinates. Only last is genuinely safe.
        var markers = new[]
        {
            new Vector3(12, 0, 0), // unsupported terrain
            Vector3.Zero,        // wall collision overlaps capsule
            new Vector3(2.5f, 0, 0) // source X/Z with real floor
        };
        var chosen = Pass121OriginalPlayerSpawnSafety.TryFind(
            GetWorld3D(), markers,
            markers[0] + Vector3.Up * .5f, default,
            out var alternate, out var rejected);
        if (!chosen.HasValue || !alternate || rejected < 2 ||
            Math.Abs(chosen.Value.X - markers[2].X) > .001f ||
            Math.Abs(chosen.Value.Z - markers[2].Z) > .001f ||
            Math.Abs(chosen.Value.Y - .08f) > .03f)
            throw new InvalidOperationException(
                "Could not select only the original collision-safe spawn");

        // The authored preferred source origin must remain unchanged in X/Z.
        var preferred = Pass121OriginalPlayerSpawnSafety.TryFind(
            GetWorld3D(), markers, markers[2] + Vector3.Up * .5f, default,
            out var changed, out var preferredRejections);
        if (!preferred.HasValue || changed || preferredRejections != 0 ||
            preferred.Value.DistanceTo(chosen.Value) > .001f)
            throw new InvalidOperationException(
                "Source preferred player marker was unnecessarily relocated");

        var fabricated = Pass121OriginalPlayerSpawnSafety.TryFind(
            GetWorld3D(), markers, new Vector3(47, .5f, 47), default,
            out _, out _);
        if (fabricated.HasValue)
            throw new InvalidOperationException(
                "Unrecoverable/fabricated player marker was admitted");

        var noFloor = Pass121OriginalPlayerSpawnSafety.TryFind(
            GetWorld3D(), new[] { markers[0] },
            markers[0] + Vector3.Up * .5f, default, out _, out _);
        if (noFloor.HasValue)
            throw new InvalidOperationException(
                "Missing original collision unexpectedly counted as floor");

        // A real CharacterBody standing at its correct source-derived
        // origin must not block its OWN subsequent shape clearance query.
        var player = new CharacterBody3D
        {
            Name = "Pass121ProductionSizedCharacter",
            Position = chosen.Value,
            CollisionLayer = 1u, CollisionMask = 1u
        };
        AddChild(player);
        player.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, .9f, 0),
            Shape = new CapsuleShape3D
            {
                Radius = .29f, Height = 1.80f
            }
        });
        await Frames(3);
        var excludedSelf = Pass121OriginalPlayerSpawnSafety.TryFind(
            GetWorld3D(), markers,
            markers[2] + Vector3.Up * .5f, player.GetRid(),
            out var selfMoved, out _);
        if (!excludedSelf.HasValue || selfMoved)
            throw new InvalidOperationException(
                "A source player blocked its own spawn floor clearance");

        GD.Print("TWR_PASS121_SOURCE_PLAYER_PHYSICS_OK" +
            " marker=" + markers[2] + " origin=" + chosen.Value +
            " rejected=" + rejected + " native_player_rid_excluded=true");
        player.QueueFree();
        await Frames(3);
    }
}

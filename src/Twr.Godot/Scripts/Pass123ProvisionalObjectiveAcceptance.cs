using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass123 exported-Windows native physics acceptance, synthetic geometry
/// only. This fixture exercises the actual production Pass122 grounding
/// and Pass89 bidirectional capsule corridor policies.
/// </summary>
public partial class Pass123ProvisionalObjectiveAcceptance : Node3D
{
    private int _physicsFrames;
    private StaticBody3D _temporaryWall = null!;
    private CharacterBody3D _player = null!;
    private Vector3 _firstLocation;

    public override void _Ready()
    {
        Box("Pass123SyntheticFloor", new Vector3(0f,-.25f,0f),
            new Vector3(24f,.5f,24f));
        _temporaryWall = Box("Pass123RouteBlockingWall",
            new Vector3(0f,1.5f,0f),new Vector3(.75f,3f,4f));
        _player = new CharacterBody3D
        {
            Name = "Pass123RealSizedPlayer",
            Position = new Vector3(-3f,.08f,0f),
            CollisionLayer=1u,CollisionMask=1u
        };
        AddChild(_player);
        _player.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0f,.9f,0f),
            Shape = new CapsuleShape3D
            {
                Radius=.29f,Height=1.8f
            }
        });
    }

    private StaticBody3D Box(string name, Vector3 position, Vector3 size)
    {
        var body=new StaticBody3D
        {
            Name=name,Position=position,
            CollisionLayer=1u,CollisionMask=0u
        };
        AddChild(body);
        body.AddChild(new CollisionShape3D
        {
            Shape=new BoxShape3D { Size=size }
        });
        return body;
    }

    private static Vector3[] SourceNeutralAnchors() =>
    [
        new Vector3(3f,.08f,0f),    // floor exists, but WALL disconnects
        new Vector3(-3f,.08f,5f),   // clear source-neutral walking lane
        new Vector3(40f,.08f,0f)    // missing floor
    ];

    public override void _PhysicsProcess(double delta)
    {
        _physicsFrames++;
        if (_physicsFrames != 4 && _physicsFrames != 8) return;

        try
        {
            var anchors=SourceNeutralAnchors();
            var playerFeet=_player.GlobalPosition;
            if (_physicsFrames == 4)
            {
                var chosen=Pass123ProvisionalObjectiveRouting.TryFind(
                    GetWorld3D(),anchors,0,playerFeet,_player.GetRid(),
                    Array.Empty<Vector3>(),
                    out var alternate,out var rejected,out var duplicates);
                if (!chosen.HasValue || !alternate || rejected < 1 ||
                    duplicates != 0 ||
                    Math.Abs(chosen.Value.X+3f)>.01f ||
                    Math.Abs(chosen.Value.Z-5f)>.01f)
                    throw new InvalidOperationException(
                        "A wall-disconnected provisional objective was accepted");

                _firstLocation=chosen.Value;
                var repeat=Pass123ProvisionalObjectiveRouting.TryFind(
                    GetWorld3D(),anchors,0,playerFeet,_player.GetRid(),
                    new[] { _firstLocation },
                    out _,out _,out var blockedDuplicates);
                if (repeat.HasValue || blockedDuplicates < 1)
                    throw new InvalidOperationException(
                        "Duplicate objective near occupied provisional marker");
                var unsupported=Pass123ProvisionalObjectiveRouting.TryFind(
                    GetWorld3D(),new[]{anchors[2]},0,
                    playerFeet,_player.GetRid(),
                    Array.Empty<Vector3>(),out _,out _,out _);
                if (unsupported.HasValue)
                    throw new InvalidOperationException(
                        "Missing physical world floor passed provisional planning");

                _temporaryWall.QueueFree();
            }
            else
            {
                var afterRemoval=Pass123ProvisionalObjectiveRouting.TryFind(
                    GetWorld3D(),anchors,0,playerFeet,_player.GetRid(),
                    Array.Empty<Vector3>(),
                    out var alternate,out _,out _);
                if (!afterRemoval.HasValue || alternate ||
                    Math.Abs(afterRemoval.Value.X-3f)>.01f ||
                    Math.Abs(afterRemoval.Value.Z)>.01f)
                    throw new InvalidOperationException(
                        "Opening the physical corridor did not restore the preferred marker");

                GD.Print("TWR_PASS123_PROVISIONAL_CORRIDOR_PHYSICS_OK" +
                    " wall_blocked=true alternate_reachable=true" +
                    " duplicates_rejected=true no_floor_rejected=true" +
                    " dynamic_corridor_reopened=true");
                GD.Print("TWR_SMOKE_PASS123_PROVISIONAL_OBJECTIVES_OK" +
                    " exported_godot_physics=true owner_source_scene_unverified=true" +
                    " full_retail_objective_routes_unverified=true");
                SetPhysicsProcess(false);
                GetTree().CallDeferred("quit",0);
            }
        }
        catch(Exception error)
        {
            GD.PushError("TWR_SMOKE_PASS123_PROVISIONAL_OBJECTIVES_FAILED " +
                error.GetType().Name + " " + error.Message + "\n" +
                error.StackTrace);
            SetPhysicsProcess(false);
            GetTree().CallDeferred("quit",1);
        }
    }
}

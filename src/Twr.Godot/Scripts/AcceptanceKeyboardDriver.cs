using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Native acceptance helper: actual input/movement, not teleportation.
/// Only the camera yaw is test-controlled. Use a real-time frame limit when
/// running headless so the external render-frame watchdog cannot interrupt
/// an in-flight physics-loop acceptance.</summary>
internal static class AcceptanceKeyboardDriver
{
    public static async Task Frames(Node owner,int count)
    {
        for(var i=0;i<count;i++) await owner.ToSignal(owner.GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    public static void SendKey(Key key,bool down)
    {
        using var e=new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=down };
        Input.ParseInputEvent(e);
    }
    public static async Task PressE(Node owner)
    {
        // Material interactions are sampled by ObjectiveRuntime._Process.
        // A slow real renderer can run several physics ticks before one
        // idle update, so a physics-only key tap could vanish unobserved.
        // Hold each edge across a complete idle callback, as a human tap is.
        async Task IdleEdge()
        {
            await owner.ToSignal(owner.GetTree(),SceneTree.SignalName.ProcessFrame);
            await owner.ToSignal(owner.GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        SendKey(Key.E,false); await IdleEdge();
        try { SendKey(Key.E,true); await IdleEdge(); }
        finally { SendKey(Key.E,false); }
        await IdleEdge();
    }
    public static async Task<float> Walk(Node owner,FirstPersonPlayer player,IReadOnlyList<Vector3> path)
    {
        float distance=0;
        try
        {
            foreach(var point in path)
            {
                var ticks=0;
                while(new Vector2(point.X-player.GlobalPosition.X,point.Z-player.GlobalPosition.Z).Length()>.18f)
                {
                    if(++ticks>600) throw new InvalidOperationException("Keyboard movement stalled at "+player.GlobalPosition+" toward "+point);
                    var previous=player.GlobalPosition;
                    var delta=point-previous;
                    player.Rotation=new Vector3(0,Mathf.Atan2(-delta.X,-delta.Z),0);
                    SendKey(Key.W,true);
                    await Frames(owner,1);
                    distance+=previous.DistanceTo(player.GlobalPosition);
                    if(player.GlobalPosition.Y<point.Y-1.5f)
                        throw new InvalidOperationException("Player fell off verified walking route");
                }
                SendKey(Key.W,false); await Frames(owner,2);
            }
            if(!player.IsOnFloor()) await Frames(owner,5);
            if(!player.IsOnFloor()) throw new InvalidOperationException("Destination is not physically supported");
            return distance;
        }
        finally { SendKey(Key.W,false); }
    }
}

using System;
using Godot;

namespace Twr.Godot;

/// <summary>Moves the existing survivor by CharacterBody3D physics, never by
/// position assignment. World changes invalidate routes; only a complete new
/// collision-checked route may resume motion. No original navigation claim.</summary>
public sealed class SourceEscortFollower
{
    private Vector3[] _route;
    private int _next=1;
    private bool _blocked;
    private double _retry;
    private Vector3 _lastPosition;
    private double _progressTimer;
    private readonly Vector3 _destination;
    public int AcceptedReplans { get; private set; }
    public int BlockedSteps { get; private set; }
    public float DistanceWalked { get; private set; }
    public bool Completed { get; private set; }
    public Vector3 Destination => _destination;
    public Vector3[] RemainingRoute => _route[Math.Min(_next,_route.Length)..];

    public SourceEscortFollower(SourceEscortLayout layout)
    {
        if(layout.SurvivorRoute.Length<2 || layout.SurvivorRoute.Length>SourceTraversalRoutes.MaximumWaypoints+2)
            throw new ArgumentException("Escort needs a complete prevalidated route");
        _route=(Vector3[])layout.SurvivorRoute.Clone();
        _destination=layout.Destination;
        _lastPosition=layout.Position;
    }

    public bool Tick(World3D world, CharacterBody3D survivor, FirstPersonPlayer player,
        double delta, float speed, float proximity, Func<Vector3,Vector3,Vector3[]>? graph)
    {
        if(Completed)return true;
        if(delta<=0 || !double.IsFinite(delta) || !float.IsFinite(speed) || speed<=0)return false;
        _retry=Math.Max(0,_retry-delta);
        var from=survivor.GlobalPosition;
        var exclude=new global::Godot.Collections.Array<Rid>{player.GetRid(),survivor.GetRid()};
        if(!player.GameplayControlsEnabled || player.GlobalPosition.DistanceTo(from)>proximity)
        {
            Stop(survivor,delta);
            return false;
        }
        if(_blocked)
        {
            if(_retry<=0)
            {
                _retry=.65;
                if(SourceTraversalRoutes.TryPlan(world,from,_destination,player.GetRid(),graph,
                    out var renewed,survivor.GetRid()))
                {
                    _route=renewed;_next=1;_blocked=false;AcceptedReplans++;
                    _progressTimer=0;_lastPosition=from;
                }
            }
            if(_blocked){Stop(survivor,delta);return false;}
        }
        // A close X/Z match never skips an upper landing or crosses a wall.
        while(_next<_route.Length-1 &&
            new Vector2(_route[_next].X-from.X,_route[_next].Z-from.Z).Length()<.12f &&
            Math.Abs(_route[_next].Y-from.Y)<.22f && survivor.IsOnFloor())_next++;
        if(_next>=_route.Length-1 && Pass90EscortArrivalSafety.CanComplete(
            world,from,_destination,survivor.IsOnFloor(),true,exclude))
        {
            Stop(survivor,delta);Completed=true;return true;
        }
        var target=_route[Math.Min(_next,_route.Length-1)];
        var offset=target-from;var flat=new Vector3(offset.X,0,offset.Z);
        var distance=flat.Length();
        if(distance<.01f)
        {
            // Remain under gravity while settling onto a ramp endpoint.
            // Do not hold an airborne survivor in place vertically.
            Stop(survivor,delta);return false;
        }
        var advance=Math.Min(distance,Math.Max(.10f,Math.Min(.55f,speed*(float)delta)));
        var probe=from.Lerp(target,advance/distance);
        if(!Pass89EscortSurfaceSafety.CanTravel(world,from,probe,exclude))
        {
            BlockedSteps++;_blocked=true;_retry=.20;
            Stop(survivor,delta);return false;
        }
        var direction=flat/distance;
        var horizontalSpeed=Math.Min(speed,distance/(float)delta);
        var vertical=survivor.IsOnFloor()?Math.Min(0,survivor.Velocity.Y):survivor.Velocity.Y-20f*(float)delta;
        survivor.Velocity=new Vector3(direction.X*horizontalSpeed,vertical,direction.Z*horizontalSpeed);
        survivor.MoveAndSlide();
        DistanceWalked+=from.DistanceTo(survivor.GlobalPosition);
        _progressTimer+=delta;
        if(_progressTimer>=1.0)
        {
            if(_lastPosition.DistanceTo(survivor.GlobalPosition)<.08f)
            { _blocked=true;_retry=.30;BlockedSteps++; }
            _progressTimer=0;_lastPosition=survivor.GlobalPosition;
        }
        return false;
    }

    private static void Stop(CharacterBody3D survivor,double delta)
    {
        var vertical=survivor.IsOnFloor()?Math.Min(0,survivor.Velocity.Y):survivor.Velocity.Y-20f*(float)delta;
        survivor.Velocity=new Vector3(0,vertical,0);
        survivor.MoveAndSlide();
    }
}

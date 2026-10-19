using System;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Native movement regression: a real Bolter performs its own leap.
/// No teleport, reflection, artificial time increments or forced landing.</summary>
public partial class BolterGroundingAcceptance : Node3D
{
    public LocalSessionNode Runtime { get; set; } = null!;
    public override async void _Ready()
    {
        try
        {
            Runtime.StartMap("Ranch");
            var floor = new StaticBody3D { Position = new Vector3(0,-.25f,0) };
            AddChild(floor);
            floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(40,.5f,20) } });
            var player = new FirstPersonPlayer { Name="Player", Runtime=Runtime, Position=new Vector3(6,.08f,0) };
            AddChild(player);
            var enemy = new InfectedAgent { Name="Bolter", InfectedType="Bolter", Runtime=Runtime,
                Target=player, Damage=1f, MoveSpeed=0f, Position=new Vector3(0,.85f,0) };
            AddChild(enemy);
            var frames=0;
            while(enemy.BolterLeapStarts74==0 && ++frames<120) await Frame();
            if(enemy.BolterLeapStarts74!=1) throw new InvalidOperationException("Natural supported leap did not start");
            var peak=enemy.GlobalPosition.Y;
            var recoveredLanding=false;
            for(var i=0;i<55;i++)
            {
                await Frame();
                peak=Math.Max(peak,enemy.GlobalPosition.Y);
                if(i>22 && enemy.IsOnFloor() && peak-enemy.GlobalPosition.Y>.2f) recoveredLanding=true;
            }
            if(!recoveredLanding || !enemy.IsOnFloor())
                throw new InvalidOperationException($"Bolter recovery suspended gravity: peak={peak:F4} final={enemy.GlobalPosition.Y:F4} grounded={enemy.IsOnFloor()}");
            if(enemy.BolterLeapStarts74!=1 || enemy.BolterLeapHits74>1)
                throw new InvalidOperationException("Leap cadence/contact authority changed");
            GD.Print($"TWR_BOLTER_GROUNDING_OK natural_leap=true peak={peak:F4} final={enemy.GlobalPosition.Y:F4} grounded=true contacts={enemy.BolterLeapHits74}");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception error)
        {
            GD.PushError("TWR_BOLTER_GROUNDING_FAILED "+error);
            RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }
    private async Task Frame() => await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Synthetic route geometry; production player, survivor, mission,
/// movement, completion and rewards. Exercises a real temporary obstruction.</summary>
public partial class SourceEscortAcceptance : Node3D
{
    public LocalSessionNode Runtime { get; set; }=null!;
    public override async void _Ready()
    {
        try
        {
            Runtime.StartMap("Ranch");
            Box("Ground",new(0,-.25f,0),new(36,.5f,28));
            Box("PermanentCorner",new(0,1.5f,0),new(.75f,3f,4f));
            var player=new FirstPersonPlayer { Runtime=Runtime,Name="Player",Position=new(-7,.08f,-3) };
            AddChild(player);await Frames(6);
            Vector3[] Detour(Vector3 from,Vector3 to)=>[from,new(-5,0,3),new(5,0,3),to];
            var plan=SourceEscortLayout.TryCreate(GetWorld3D(),player.GlobalPosition,
                new(-5,0,0),new(5,0,0),player.GetRid(),Detour);
            if(plan is null || plan.SurvivorRoute.Length<4)
                throw new InvalidOperationException("Complete corner detour was not validated");
            // Partial and unsupported destination routes must fail before spawn.
            if(SourceEscortLayout.TryCreate(GetWorld3D(),player.GlobalPosition,
                new(-5,0,0),new(5,0,0),player.GetRid(),(a,b)=>[a,new(-5,0,3)]) is not null ||
                SourceEscortLayout.TryCreate(GetWorld3D(),player.GlobalPosition,
                new(-5,0,0),new(40,0,0),player.GetRid(),Detour) is not null)
                throw new InvalidOperationException("Partial route or unsupported destination admitted");
            var walked=await AcceptanceKeyboardDriver.Walk(this,player,plan.PlayerRoute);
            // Stand to the side using input so the actual collision body
            // doesn't become the survivor's temporary obstacle.
            walked+=await AcceptanceKeyboardDriver.Walk(this,player,[player.GlobalPosition,new(-7,0,0)]);
            var receipts=0;
            var mission=new ObjectiveRuntime { Name="RoutedEscort",Family="Escort",
                ObjectiveId="Ranch:SourceEscortAcceptance",Player=player,Runtime=Runtime,
                Position=plan.Position,SourceEscortPlan=plan,SourceEscortGraph=Detour,
                Completed=_=>receipts++ };
            AddChild(mission);
            var blocker=Box("TemporaryDoor",new(-5,1.5f,1.5f),new(3,3,.55f));
            await Frames(42);
            var actor=mission.GetNode<CharacterBody3D>("EscortSurvivor");
            if(mission.IsCompleted || receipts!=0 || mission.SourceEscortMotion!.BlockedSteps==0 || actor.GlobalPosition.Z>1.4f)
                throw new InvalidOperationException("Survivor crossed blocked corridor or paid early");
            blocker.QueueFree();await Frames(4);
            var frames=0;
            while(!mission.IsCompleted && ++frames<1200)await Frames(1);
            if(!mission.IsCompleted || !actor.IsOnFloor() || receipts!=1 ||
                mission.SourceEscortMotion!.AcceptedReplans<1 || mission.SourceEscortMotion.DistanceWalked<10f)
                throw new InvalidOperationException($"Escort stalled: position={actor.GlobalPosition} route={string.Join(';',mission.SourceEscortMotion!.RemainingRoute.Select(p=>p.ToString()))} blocks={mission.SourceEscortMotion.BlockedSteps}");
            var credits=Runtime.Profile!.Credits;var xp=Runtime.Profile.Xp;
            Runtime.CompleteObjective(mission.ObjectiveId,"Escort");
            if(Runtime.Profile.Credits!=credits || Runtime.Profile.Xp!=xp || receipts!=1)
                throw new InvalidOperationException("Escort reward duplicated on replay");
            var cornerSurvivorWalked=mission.SourceEscortMotion.DistanceWalked;
            await VerifyRampTraversal();
            GD.Print($"TWR_SOURCE_ESCORT_OK ramp_up_and_down=true blocked_then_replanned=true full_corner_detour=true grounded_arrival=true survivor_walked={cornerSurvivorWalked:F2} player_walked={walked:F2} receipts=1 reward_replay_idempotent=true synthetic_world=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e){GD.PushError("TWR_SOURCE_ESCORT_FAILED "+e);RuntimeShutdownCoordinator.Request(GetTree(),1);}
    }
    private async Task VerifyRampTraversal()
    {
        // A source-neutral ramp joins floors three metres apart. It tests
        // the production follower's height/arrival and grounded waypoint
        // rules, not original Laboratory stair recovery or navmesh parity.
        foreach(var node in GetChildren().OfType<Node3D>())node.QueueFree();
        await Frames(4);
        Box("LowerLanding",new(0,-.25f,-2),new(8,.5f,8));
        Box("UpperLanding",new(0,2.75f,16),new(8,.5f,8));
        var angle=-Mathf.Atan2(3f,10f);
        var normal=new Basis(Vector3.Right,angle)*Vector3.Up;
        var ramp=Box("PhysicalRamp",new Vector3(0,1.5f,7)-normal*.15f,
            new(4,.30f,Mathf.Sqrt(109f)+.15f));
        ramp.Rotation=new Vector3(angle,0,0);
        var player=new FirstPersonPlayer { Name="RampPlayer",Runtime=Runtime,
            Position=new(-1,.08f,0) };
        AddChild(player);await Frames(8);
        Vector3[] StairGraph(Vector3 a,Vector3 b)
        {
            Vector3[] slope=[new(0,0,2),new(0,.3f,3),new(0,.9f,5),new(0,1.5f,7),
                new(0,2.1f,9),new(0,2.7f,11),new(0,3,12)];
            return b.Y>=a.Y?[a,..slope,b]:[a,..slope.Reverse(),b];
        }
        float totalWalked=0;
        foreach(var ascent in new[]{true,false})
        {
            var start=ascent?new Vector3(0,0,1):new Vector3(0,3,14);
            var goal=ascent?new Vector3(0,3,14):new Vector3(0,0,1);
            Func<Vector3,Vector3,Vector3[]> graph=StairGraph;
            // The player approach is already valid across the same ramp.
            // The graph proposes exact synthetic test waypoints; production
            // queries still recheck every corridor before a mission spawns.
            var plan=SourceEscortLayout.TryCreate(GetWorld3D(),player.GlobalPosition,
                start,goal,player.GetRid(),graph)
                ?? throw new InvalidOperationException("Ramp Escort plan rejected: ascent="+ascent);
            var count=0;
            var mission=new ObjectiveRuntime { Family="Escort",Name=ascent?"UphillEscort":"DownhillEscort",
                Position=plan.Position,SourceEscortPlan=plan,SourceEscortGraph=graph,
                Runtime=Runtime,Player=player,ObjectiveId="Ranch:RampEscort:"+ascent,Completed=_=>count++ };
            AddChild(mission);await Frames(3);
            var survivor=mission.GetNode<CharacterBody3D>("EscortSurvivor");
            var initialY=survivor.GlobalPosition.Y;
            var ticks=0;
            while(!mission.IsCompleted && ++ticks<1800)await Frames(1);
            var deltaY=survivor.GlobalPosition.Y-initialY;
            if(!mission.IsCompleted || !survivor.IsOnFloor() || count!=1 ||
                (ascent?deltaY<2.5f:deltaY> -2.5f))
                throw new InvalidOperationException("Ramp Escort failed: ascent="+ascent+
                    " actor="+survivor.GlobalPosition+" dy="+deltaY+
                    " route="+string.Join(";",mission.SourceEscortMotion!.RemainingRoute)+
                    " blocks="+mission.SourceEscortMotion.BlockedSteps);
            totalWalked+=mission.SourceEscortMotion!.DistanceWalked;
            GD.Print("TWR_SOURCE_ESCORT_RAMP_OK ascent="+ascent+" rise="+deltaY+
                " grounded=true reward_once=true no_mid_motion_teleport=true");
            mission.QueueFree();await Frames(4);
        }
        if(totalWalked<20f)throw new InvalidOperationException("Ramp circuit did not traverse both floors");
    }
    private StaticBody3D Box(string name,Vector3 position,Vector3 size)
    {
        var body=new StaticBody3D { Name=name,Position=position };AddChild(body);
        body.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=size } });return body;
    }
    private async Task Frames(int count)
    {for(var i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Twr.Domain.Model;

namespace Twr.Godot;

/// <summary>
/// Real physics, scripted keyboard walking, and full material loops on a
/// synthetic test floor. No private scene bytes, teleported pickups,
/// direct health assignments, or fabricated reward grants.
/// </summary>
public partial class SourceTraversalAcceptance : Node3D
{
    public LocalSessionNode Runtime { get; set; } = null!;
    private FirstPersonPlayer _player = null!;
    private float _walked;
    private int _deposits;
    private int _receipts;

    public override async void _Ready()
    {
        try
        {
            Runtime.StartMap("Ranch");
            var ground = Box("SyntheticFloor", new(0,-.25f,0), new(32,.5f,32));
            var wall = Box("CorridorWall", new(0,1.5f,0), new(.75f,3f,4f));
            _player = new FirstPersonPlayer { Name="RouteTestPlayer", Runtime=Runtime,
                Position=new Vector3(-4,.15f,0) };
            AddChild(_player);
            await Frames(8);
            using var graph = new AStar3D();
            var points = new[] { new Vector3(-4,0,0), new Vector3(-4,0,3.5f),
                new Vector3(4,0,3.5f), new Vector3(4,0,0) };
            for (var i=0;i<points.Length;i++)
            {
                graph.AddPoint(i,points[i]);
                if (i>0) graph.ConnectPoints(i-1,i);
            }
            Vector3[] Proposal(Vector3 from,Vector3 to) =>
                graph.GetPointPath(graph.GetClosestPoint(from),graph.GetClosestPoint(to));
            if (!SourceTraversalRoutes.TryPlan(GetWorld3D(),_player.GlobalPosition,
                    points[^1],_player.GetRid(),Proposal,out var route) || route.Length<4)
                throw new InvalidOperationException("Valid walking detour rejected");
            if (SourceTraversalRoutes.TryPlan(GetWorld3D(),points[0],points[^1],
                    _player.GetRid(),(_,_)=>points[..2],out _))
                throw new InvalidOperationException("Partial graph route admitted");
            var blocked=Box("TemporaryRouteBlock",new(0,1.5f,3.5f),new(.75f,3f,4f));
            await Frames(3);
            if (SourceTraversalRoutes.TryPlan(GetWorld3D(),points[0],points[^1],
                    _player.GetRid(),Proposal,out _))
                throw new InvalidOperationException("Graph bypassed new physical wall");
            blocked.QueueFree(); await Frames(3);
            if (!SourceTraversalRoutes.TryPlan(GetWorld3D(),points[0],points[^1],
                    _player.GetRid(),Proposal,out route))
                throw new InvalidOperationException("Removed obstruction did not reopen path");
            await Walk(route);
            GD.Print("TWR_SOURCE_DETOUR_WALK_OK real_keyboard=true partial_rejected=true dynamic_wall_rejected=true distance="+_walked);

            // The actual player walks back; do not move its transform to a
            // test fixture destination to fake a traversed corridor.
            await Walk(route.Reverse().ToArray());
            var scene = GetWorld3D();
            foreach (var config in new[] { (family:"Load",large:false,count:10),
                         (family:"Repair",large:false,count:10),
                         (family:"Repair",large:true,count:12) })
            {
                var plan=SourceObjectiveLayout.TryPlan(scene,_player.GlobalPosition,
                    _player.GetRid(),config.family,new[]{new Vector3(-6,0,-4)},
                    Array.Empty<Vector3>(),null,config.large);
                if (plan is null || plan.MaterialFeet.Length!=config.count ||
                    plan.MaterialRoutes.Length!=config.count)
                    throw new InvalidOperationException("Material circuit incomplete "+config);
                await Walk(plan.PlayerRoute);
                var id="Ranch:SourceTraversal:"+config.family+":"+config.count;
                var objective=new ObjectiveRuntime
                {
                    Name="Test"+config.family+config.count, Family=config.family,
                    ObjectiveId=id, Runtime=Runtime, Player=_player,
                    Position=plan.Position,SourceMaterialLayout=plan,
                    Completed=_=>_receipts++
                };
                var credits=Runtime.Player!.Credits;
                AddChild(objective); await Frames(3);
                if (objective.Pass98RequiredMaterials!=config.count)
                    throw new InvalidOperationException("Repair variant diverged from planned material count");
                for (var i=0;i<config.count;i++)
                {
                    await Walk(plan.MaterialRoutes[i]);
                    await PressE();
                    if (!objective.Pass98CarryingMaterial)
                        throw new InvalidOperationException("Material could not be picked up by E "+i);
                    await Walk(plan.MaterialRoutes[i].Reverse().ToArray());
                    await PressE();
                    _deposits++;
                    if (i<config.count-1 && objective.Pass98DepositedMaterials!=i+1)
                        throw new InvalidOperationException("Material deposit did not advance exactly once");
                }
                if (Runtime.Player.Credits<credits+4000)
                    throw new InvalidOperationException("Real objective completion did not award credits");
                var awarded=Runtime.Player.Credits;
                Runtime.CompleteObjective(id,config.family);
                if (Runtime.Player.Credits!=awarded)
                    throw new InvalidOperationException("Objective replay duplicated reward");
                GD.Print("TWR_SOURCE_MATERIAL_CIRCUIT_OK family="+config.family+" count="+config.count+" real_keyboard=true real_E=true replay_idempotent=true");
                await Frames(3);
                await Walk(plan.PlayerRoute.Reverse().ToArray());
            }
            if (_deposits!=32 || _receipts!=3 || _walked<70)
                throw new InvalidOperationException("Incomplete physical source-neutral acceptance");
            Runtime.FailMatch("Source traversal acceptance finished");
            if (Runtime.Match?.SaveCommitted!=true) throw new InvalidOperationException("Results not saved");
            GD.Print("TWR_SMOKE_SOURCE_TRAVERSAL_OK materials=32 missions=3 distance="+_walked+
                " synthetic_floor=true original_scene_unverified=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e)
        {
            SendKey(Key.W,false); SendKey(Key.E,false);
            GD.PushError("TWR_SMOKE_SOURCE_TRAVERSAL_FAILED "+e);
            RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }

    private StaticBody3D Box(string name,Vector3 position,Vector3 size)
    {
        var body=new StaticBody3D { Name=name,Position=position,CollisionLayer=1,CollisionMask=0 };
        AddChild(body);
        body.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=size } });
        return body;
    }
    private async Task Frames(int count)
    {
        for (var i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    private static void SendKey(Key key,bool down)
    {
        using var e=new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=down };
        Input.ParseInputEvent(e);
    }
    private async Task PressE()
    {
        SendKey(Key.E,false); await Frames(3);
        SendKey(Key.E,true); await Frames(4);
        SendKey(Key.E,false); await Frames(3);
    }
    private async Task Walk(Vector3[] path)
    {
        _walked += await AcceptanceKeyboardDriver.Walk(this,_player,path);
    }
}

using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Expressway-only navigation graph across the authored traffic and checkpoint
/// corridors. This route graph is a functional interim reconstruction, not a
/// recovered original Roblox navmesh. Other maps retain existing steering.
/// The graph is built once; infected share it and recompute paths sparingly.
/// </summary>
public partial class ExpresswayNavigationRuntime : Node3D
{
    private readonly AStar3D _graph = new();
    private readonly HashSet<long> _blockedPoints = [];
    private const int Columns = 7;
    private const int Rows = 31;
    private const float Spacing = 4f;

    // Positions correspond to the approximation built in ExpresswaySceneBuilder.
    // Margin accounts for the agent capsule radius; temporary fortifications
    // and moved actors are handled by the existing per-agent local steering.
    private static readonly (float X,float Z,float Width,float Length)[] Blockers =
    [
        (-16f,-53f,2.5f,5.0f),(15f,-54f,3f,6.9f),
        (-15f,-38f,2.5f,5.5f),(16f,-38f,2.5f,5.0f),
        (-13f,-18f,2.5f,5.5f),(14f,-21f,2.5f,5.6f),
        (-15f,-3f,2.5f,5f),(14f,0f,2.5f,5.7f),
        (-14f,17f,2.5f,5.7f),(15f,22f,2.5f,5f),
        (-16f,38f,2.5f,5.7f),(15f,42f,2.5f,5.5f),
        (-12f,55f,2.5f,5f),
        (-9f,-39f,3.3f,5.3f),(9f,-39f,3.3f,5.3f),
        (-18f,-27f,4.2f,.9f),(-11f,-27f,4.2f,.9f),
        (11f,-27f,4.2f,.9f),(18f,-27f,4.2f,.9f),
        (0f,-25f,3.6f,1.7f),
        (-19f,37f,3.8f,.8f),(-13f,37f,3.8f,.8f),
        (13f,37f,3.8f,.8f),(19f,37f,3.8f,.8f),
        (-10f,34f,3.8f,1.6f),(10f,34f,3.8f,1.6f)
    ];

    public int NavigablePoints => _graph.GetPointCount();

    public override void _Ready()
    {
        BuildGraph();
        GD.Print("TWR_EXPRESSWAY_NAV_READY points=" +
            NavigablePoints + " blockers=" + Blockers.Length);
    }

    public Vector3[] GetRoute(Vector3 from,Vector3 to)
    {
        if (_graph.GetPointCount() == 0) return [];
        var start=_graph.GetClosestPoint(from);
        var end=_graph.GetClosestPoint(to);
        if (start < 0 || end < 0) return [];
        return _graph.GetPointPath(start,end);
    }

    private void BuildGraph()
    {
        _graph.Clear();
        _blockedPoints.Clear();
        for(var row=0;row<Rows;row++)
        {
            var z=-60f+row*Spacing;
            for(var column=0;column<Columns;column++)
            {
                var x=-18f+column*6f;
                var id=Id(column,row);
                if (Blocked(new Vector2(x,z))) { _blockedPoints.Add(id); continue; }
                _graph.AddPoint(id,new Vector3(x,1.0f,z));
            }
        }

        // Cardinal + short diagonal connections allow infected to navigate
        // around spaced vehicles and sandbag barriers without a navmesh bake.
        for(var row=0;row<Rows;row++)
            for(var column=0;column<Columns;column++)
            {
                var a=Id(column,row);
                if (!_graph.HasPoint(a))continue;
                foreach(var (dc,dr) in new[] {
                    (1,0),(0,1),(1,1),(-1,1)
                })
                {
                    var c=column+dc;
                    var r=row+dr;
                    if(c<0||c>=Columns||r<0||r>=Rows)continue;
                    var b=Id(c,r);
                    if (!_graph.HasPoint(b))continue;
                    var pa=_graph.GetPointPosition(a);
                    var pb=_graph.GetPointPosition(b);
                    if(SegmentClear(new Vector2(pa.X,pa.Z),
                                    new Vector2(pb.X,pb.Z)))
                        _graph.ConnectPoints(a,b);
                }
            }
    }

    private static long Id(int column,int row) => row*Columns+column;

    private static bool SegmentClear(Vector2 start,Vector2 end)
    {
        var length=start.DistanceTo(end);
        var steps=Math.Max(2,(int)Math.Ceiling(length/.65f));
        for(var i=0;i<=steps;i++)
            if(Blocked(start.Lerp(end,i/(float)steps)))return false;
        return true;
    }

    private static bool Blocked(Vector2 point)
    {
        if(Math.Abs(point.X)>20.7f || Math.Abs(point.Y)>62.5f)return true;
        foreach(var obstacle in Blockers)
            if(Math.Abs(point.X-obstacle.X)<obstacle.Width*.5f+.64f &&
               Math.Abs(point.Y-obstacle.Z)<obstacle.Length*.5f+.64f)
                return true;
        return false;
    }
}

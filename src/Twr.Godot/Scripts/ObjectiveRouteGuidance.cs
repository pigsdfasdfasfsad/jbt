using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Twr.Godot;

/// <summary>Optional offline route assistance. H is unused by existing controls;
/// F4 remains the original floorplan overlay. Collision-query planning is on
/// physics ticks; visualization is applied on idle frames. Off by default.</summary>
public partial class ObjectiveRouteGuidance : Node3D
{
    public FirstPersonPlayer Player { get; set; } = null!;
    public LocalSessionNode Runtime { get; set; } = null!;
    public Func<Vector3,Vector3,Vector3[]>? GraphRoute { get; set; }
    public bool GuideEnabled { get; private set; }
    public int VisibleSegments { get; private set; }
    public string DisplayText => _text.Text;
    private MultiMeshInstance3D _dots = null!;
    private Label _text = null!;
    private double _refresh;
    private Vector3[] _planned=[];
    private string _caption="";
    private bool _dirty=true;
    private ObjectiveRuntime? _tracked;
    private const int MaximumDots=96;

    public override void _Ready()
    {
        _dots=new MultiMeshInstance3D
        {
            Name="OptionalRouteDots",
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
            Multimesh=new MultiMesh
            {
                TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,
                Mesh=new BoxMesh
                {
                    Size=new Vector3(.11f,.016f,.11f),
                    Material=new StandardMaterial3D
                    {
                        AlbedoColor=new Color(.95f,.76f,.20f),
                        ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded
                    }
                }
            }
        };
        AddChild(_dots);
        var canvas=new CanvasLayer { Name="OptionalRouteGuideHud",Layer=5 };
        AddChild(canvas);
        _text=new Label { Name="RouteGuideStatus",Position=new Vector2(22,118),
            MouseFilter=Control.MouseFilterEnum.Ignore,AutowrapMode=TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize=new Vector2(420,0),Size=new Vector2(420,70) };
        _text.AddThemeFontSizeOverride("font_size",17);
        _text.AddThemeColorOverride("font_shadow_color",new Color(0,0,0,.9f));
        _text.AddThemeConstantOverride("shadow_offset_x",1);
        _text.AddThemeConstantOverride("shadow_offset_y",1);
        canvas.AddChild(_text);
    }

    private bool PlayerReady => GodotObject.IsInstanceValid(Player) &&
        Player.IsInsideTree() && Player.GameplayControlsEnabled && Runtime?.Player?.IsAlive==true;

    public override void _UnhandledInput(InputEvent input)
    {
        if(input is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode!=Key.H || !PlayerReady)return;
        GuideEnabled=!GuideEnabled;_refresh=0;_dirty=true;
        if(!GuideEnabled){_planned=[];_tracked=null;}
        GetViewport().SetInputAsHandled();
    }

    public override void _PhysicsProcess(double delta)
    {
        if(!GuideEnabled || !PlayerReady)
        {
            if(_planned.Length>0){_planned=[];_tracked=null;_dirty=true;}
            return;
        }
        _refresh-=delta;
        if(_refresh>0)return;
        _refresh=.50;
        var candidates=GetTree().GetNodesInGroup("twr-objective")
            .OfType<ObjectiveRuntime>().Where(x=>GodotObject.IsInstanceValid(x) && !x.IsQueuedForDeletion())
            .SelectMany(x=>x.RouteGuideTargets()).ToArray();
        // The carried material belongs to one mission. Never redirect its
        // player to another depot just because that depot is nearer.
        var carrying=candidates.Any(x=>x.Carrying);
        _planned=[];_tracked=null;_caption="NO REACHABLE OBJECTIVE ROUTE";
        foreach(var target in candidates.Where(x=>!carrying || x.Carrying)
            .OrderBy(x=>Player.GlobalPosition.DistanceSquaredTo(x.Position)).Take(12))
        {
            if(!SourceTraversalRoutes.TryPlan(GetWorld3D(),Player.GlobalPosition,target.Position,
                Player.GetRid(),GraphRoute,out var route,target.IgnoreBody))continue;
            _planned=route;_tracked=target.Owner;
            var length=0f;for(var i=1;i<route.Length;i++)length+=route[i-1].DistanceTo(route[i]);
            _caption=target.Instruction+"\n"+$"{length:0.0} m walking route | H: hide guide";
            break;
        }
        if(candidates.Length==0)_caption="NO ACTIVE OBJECTIVE | H: hide guide";
        _dirty=true;
    }

    public override void _Process(double delta)
    {
        if(!PlayerReady)
        {
            _dots.Visible=false;_text.Visible=false;VisibleSegments=0;return;
        }
        _text.Visible=true;
        if(_tracked is not null && (!GodotObject.IsInstanceValid(_tracked) ||
            _tracked.IsQueuedForDeletion() || _tracked.IsCompleted))
        { _tracked=null;_planned=[];_refresh=0;_dirty=true; }
        if(!_dirty)return;
        _dirty=false;
        _text.Text=GuideEnabled?_caption:"H: optional objective route guide";
        var points=new List<Vector3>();
        if(GuideEnabled)
            for(var i=1;i<_planned.Length && points.Count<MaximumDots;i++)
            {
                var a=_planned[i-1];var b=_planned[i];var length=a.DistanceTo(b);
                var steps=Math.Max(1,(int)Math.Ceiling(length/.65f));
                for(var n=0;n<steps && points.Count<MaximumDots;n++)
                {
                    var p=a.Lerp(b,n/(float)steps)+Vector3.Up*.055f;
                    if(p.DistanceSquaredTo(Player.GlobalPosition)>.8f*.8f)points.Add(p);
                }
            }
        _dots.Multimesh!.InstanceCount=points.Count;
        for(var i=0;i<points.Count;i++)
            _dots.Multimesh.SetInstanceTransform(i,new Transform3D(Basis.Identity,ToLocal(points[i])));
        _dots.Visible=GuideEnabled && points.Count>0;VisibleSegments=points.Count;
    }
}

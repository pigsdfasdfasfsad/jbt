using System;
using Godot;

namespace Twr.Godot;

/// <summary>Viewmodel-only camera-relative fitting near static geometry.
/// Uniform scaling about the camera preserves projected proportions; it does
/// not change camera pose/FOV, aim rays, recoil, collision, or firing origin.
/// The render near plane tracks the fit to avoid slicing the reduced model.
/// This is an explicit presentation adaptation, not recovered source animation.</summary>
public sealed class HeldModelClearance : IDisposable
{
    private readonly ProjectileSweep _probe = new(.025f);
    private const uint WorldMask = 1u | Pass36SourceFragmentsRuntime.SourceServerCollisionLayer;
    private Transform3D? _base;
    private Transform3D _applied;
    private Camera3D? _camera;
    private float _nominalNear;
    public float FitScale { get; private set; } = 1f;

    public void Restore(WeaponViewModelRuntime model)
    {
        if(_base is { } original && model.Transform.IsEqualApprox(_applied))
            model.Transform=original;
        // Equipment input may already have replaced the root pose. Never
        // subtract yesterday's correction from a newly selected weapon.
        _base=null;
        model.ClearanceBasePose=null;
        if(_camera is not null && GodotObject.IsInstanceValid(_camera))_camera.Near=_nominalNear;
    }

    public void Apply(WeaponViewModelRuntime model,Camera3D camera,World3D world,FirstPersonPlayer player,double delta)
    {
        if(!ReferenceEquals(_camera,camera)) {_camera=camera;_nominalNear=camera.Near;}
        if(!model.Visible || !TryCameraBounds(model,out var bounds)) {FitScale=1f;return;}
        var front=bounds.Position.Z;
        if(front>=-.08f){FitScale=1f;return;}
        var min=bounds.Position;var max=bounds.End;var center=bounds.GetCenter();
        Span<Vector3> samples=stackalloc Vector3[5]
        {
            new(center.X,center.Y,front),new(min.X,min.Y,front),new(min.X,max.Y,front),
            new(max.X,min.Y,front),new(max.X,max.Y,front)
        };
        var target=1f;
        foreach(var sample in samples)
        {
            var from=camera.GlobalPosition;var to=camera.ToGlobal(sample);
            var hit=_probe.Move(world,from,to-from,WorldMask,player);
            if(!hit.Hit)continue;
            var fraction=!hit.Valid || hit.InitialOverlap ? .025f :
                Math.Clamp(from.DistanceTo(hit.Centre)/from.DistanceTo(to)-.015f,.025f,1f);
            target=Math.Min(target,fraction);
        }
        // Contract immediately to prevent a moving player piercing a wall;
        // release smoothly when walking away. Do not accumulate transforms.
        FitScale=target<FitScale ? target : Mathf.Lerp(FitScale,target,1f-MathF.Exp(-18f*(float)delta));
        if(FitScale>.999f)FitScale=1f;
        var original=model.Transform;
        _base=original;
        model.ClearanceBasePose=original;
        // Preserve the same normalized clipping envelope as the ordinary
        // model. Physics and camera pose/FOV are not affected.
        camera.Near=Math.Max(.001f,_nominalNear*FitScale);
        _applied=new Transform3D(original.Basis.Scaled(Vector3.One*FitScale),original.Origin*FitScale);
        model.Transform=_applied;
    }

    public static bool TryCameraBounds(WeaponViewModelRuntime model,out Aabb bounds)
    {
        var low=Vector3.One*float.PositiveInfinity;var high=-low;var count=0;
        Visit(model,model.Transform,ref low,ref high,ref count);
        bounds=count>0 ? new Aabb(low,high-low) : default;
        return count>0 && low.IsFinite() && high.IsFinite();
    }
    private static void Visit(Node node,Transform3D toCamera,ref Vector3 low,ref Vector3 high,ref int count)
    {
        foreach(var child in node.GetChildren())
        {
            if(child is not Node3D part || !part.Visible)continue;
            var transform=toCamera*part.Transform;
            if(part is MeshInstance3D mesh && mesh.Mesh is not null)
            {
                var box=Pass45FallbackProxyStreamer.WorldBounds(transform,mesh.Mesh.GetAabb());
                low=new(Math.Min(low.X,box.Position.X),Math.Min(low.Y,box.Position.Y),Math.Min(low.Z,box.Position.Z));
                high=new(Math.Max(high.X,box.End.X),Math.Max(high.Y,box.End.Y),Math.Max(high.Z,box.End.Z));
                count++;
            }
            Visit(part,transform,ref low,ref high,ref count);
        }
    }
    public void Dispose()=>_probe.Dispose();
}

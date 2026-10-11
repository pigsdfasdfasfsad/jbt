using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass 45: no replacement for source meshes or collision. This renderer
/// manages spatially grouped original-positioned fallback MeshPart/CSG proxies
/// that were previously batched across an entire map and always visible.
/// GPU FPS must be measured on the owner's actual Windows device.
/// </summary>
public partial class Pass45FallbackProxyStreamer : Node3D
{
    // Larger than Pass28's 96-stud native tile to avoid exploding the
    // draw-call count for the 15,678 remaining source visual proxy records.
    public const int TileSizeStuds = 256;
    private const float DrawRadiusMetres = 145f;
    private const float HideRadiusMetres = 166f;

    private readonly record struct Batch(
        MultiMeshInstance3D Node, Vector2 Minimum, Vector2 Maximum,
        int Instances);
    private readonly List<Batch> _batches = [];
    private Node3D? _player;
    private Vector2 _lastPlayer = new(float.NaN, float.NaN);
    private double _refresh;

    public bool CullEnabled { get; private set; } = true;
    public int BatchCount => _batches.Count;
    public int SourceProxyInstanceCount { get; private set; }
    public int VisibleBatchCount { get; private set; }
    public int VisibleInstanceCount { get; private set; }

    public static Aabb WorldBounds(Transform3D transform, Aabb local)
    {
        // Mesh.GetAabb() preserves the original imported mesh's LOCAL vertex
        // envelope when that mesh is present; proxies use its known unit bound.
        // Never compute bounds from a tile centre. Large source mountains,
        // rotated beams and special-mesh offsets must stay visible in range.
        var center = transform * (local.Position + local.Size * .5f);
        var half = local.Size * .5f;
        var x = transform.Basis.X;
        var y = transform.Basis.Y;
        var z = transform.Basis.Z;
        var extension = new Vector3(
            Math.Abs(x.X) * half.X + Math.Abs(y.X) * half.Y + Math.Abs(z.X) * half.Z,
            Math.Abs(x.Y) * half.X + Math.Abs(y.Y) * half.Y + Math.Abs(z.Y) * half.Z,
            Math.Abs(x.Z) * half.X + Math.Abs(y.Z) * half.Y + Math.Abs(z.Z) * half.Z);
        return new Aabb(center - extension, extension * 2f);
    }

    public void Register(MultiMeshInstance3D mesh, Aabb sourceBounds,
        int sourceInstances)
    {
        if (sourceInstances <= 0 || sourceInstances > 20000 ||
            _batches.Count >= 8000 ||
            !Finite(sourceBounds.Position) || !Finite(sourceBounds.Size) ||
            sourceBounds.Size.X < 0 || sourceBounds.Size.Y < 0 ||
            sourceBounds.Size.Z < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceBounds),
                "Pass45 proxy spatial render budget or bounds invalid");
        AddChild(mesh);
        var min = new Vector2(sourceBounds.Position.X, sourceBounds.Position.Z);
        var end = sourceBounds.End;
        var max = new Vector2(end.X, end.Z);
        _batches.Add(new Batch(mesh,min,max,sourceInstances));
        SourceProxyInstanceCount += sourceInstances;
        VisibleBatchCount++;
        VisibleInstanceCount += sourceInstances;
    }

    public void Track(Node3D player)
    {
        _player=player;
        _lastPlayer = new Vector2(float.NaN,float.NaN);
        UpdateVisibility(force:true);
    }

    public void SetCullEnabled(bool enabled)
    {
        if (CullEnabled == enabled) return;
        CullEnabled=enabled;
        UpdateVisibility(force:true);
    }

    public void ToggleCull() => SetCullEnabled(!CullEnabled);

    public override void _Process(double delta)
    {
        if (_player is null || !GodotObject.IsInstanceValid(_player)) return;
        _refresh-=delta;
        if (_refresh>0) return;
        _refresh=.32;
        UpdateVisibility(force:false);
    }

    private void UpdateVisibility(bool force)
    {
        if (_player is null || !GodotObject.IsInstanceValid(_player)) return;
        var p = _player.GlobalPosition;
        var centre = new Vector2(p.X,p.Z);
        if (!force && _lastPlayer.DistanceSquaredTo(centre)<25f) return;
        _lastPlayer=centre;
        var shown=0;
        var drawn=0;
        foreach(var group in _batches)
        {
            // Match Pass28's hysteresis, but use this Batch's complete
            // actual-transformed source extent, not its 256-stud tile origin.
            var visible = !CullEnabled ||
                Pass28PrimitiveStreamer.CanSeeBounds(
                    centre,group.Minimum,group.Maximum,
                    group.Node.Visible ? HideRadiusMetres : DrawRadiusMetres);
            group.Node.Visible=visible;
            if(!visible)continue;
            shown++;
            drawn+=group.Instances;
        }
        VisibleBatchCount=shown;
        VisibleInstanceCount=drawn;
    }

    private static bool Finite(Vector3 point) =>
        float.IsFinite(point.X) && float.IsFinite(point.Y) &&
        float.IsFinite(point.Z);
}

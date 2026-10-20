using System;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Continuous sphere sweep for the visible projectile, including initial
/// overlap (CastMotion alone explicitly ignores already-overlapping bodies).
/// One shape/query pair is owned by each projectile, not allocated per tick.
/// Call only on the physics thread. This is reconstructed physical behavior,
/// not a claim to have recovered Roblox's unpublished projectile algorithm.
/// </summary>
public sealed class ProjectileSweep : IDisposable
{
    public const float Skin = .004f;
    private readonly SphereShape3D _shape;
    private readonly PhysicsShapeQueryParameters3D _query;
    private bool _disposed;

    public readonly record struct Result(bool Valid, bool Hit, bool InitialOverlap,
        Vector3 Centre, Vector3 Normal);

    public ProjectileSweep(float radius)
    {
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        _shape = new SphereShape3D { Radius = radius };
        _query = new PhysicsShapeQueryParameters3D
        {
            Shape = _shape, Margin = 0f, CollideWithBodies = true,
            CollideWithAreas = false
        };
    }

    public Result Move(World3D world, Vector3 start, Vector3 motion, uint mask,
        FirstPersonPlayer? source = null)
    {
        if (_disposed || !Finite(start) || !Finite(motion) || mask == 0)
            return new Result(false, true, true, start, Vector3.Zero);
        _query.CollisionMask = mask;
        // A freed player must never be dereferenced by a surviving projectile.
        _query.Exclude = source is not null && GodotObject.IsInstanceValid(source)
            && source.IsInsideTree()
            ? new global::Godot.Collections.Array<Rid> { source.GetRid() }
            : new global::Godot.Collections.Array<Rid>();
        _query.Transform = new Transform3D(Basis.Identity, start);
        _query.Motion = Vector3.Zero;
        var space = world.DirectSpaceState;
        if (space.IntersectShape(_query, 1).Count != 0)
            return new Result(true, true, true, start, Vector3.Zero);
        var length = motion.Length();
        if (length < .000001f)
            return new Result(true, false, false, start, Vector3.Zero);

        _query.Motion = motion;
        var fractions = space.CastMotion(_query);
        if (fractions.Length < 2 || !float.IsFinite(fractions[0]) || !float.IsFinite(fractions[1]))
            return new Result(false, true, true, start, Vector3.Zero);
        var safe = Math.Clamp(fractions[0], 0f, 1f);
        var unsafeFraction = Math.Clamp(fractions[1], safe, 1f);
        if (safe >= 1f)
            return new Result(true, false, false, start + motion, Vector3.Zero);

        // Read the normal at first overlap; GetRestInfo ignores Motion.
        // Move the returned CENTRE to the safe side, never to a surface
        // point that embeds the rest of the sphere inside a wall or floor.
        _query.Motion = Vector3.Zero;
        _query.Transform = new Transform3D(Basis.Identity,
            start + motion * Math.Min(1f, unsafeFraction + Skin / length));
        var info = space.GetRestInfo(_query);
        var normal = info.Count > 0 ? info["normal"].AsVector3() : -motion / length;
        if (!Finite(normal) || normal.LengthSquared() < .0001f) normal = -motion / length;
        return new Result(true, true, false,
            start + motion * Math.Max(0f, safe - Skin / length), normal.Normalized());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _query.Dispose();
        _shape.Dispose();
    }

    private static bool Finite(Vector3 p) =>
        float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
}

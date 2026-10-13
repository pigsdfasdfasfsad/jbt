using System;
using System.Collections.Generic;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Pass111: short-lived, non-interactive visual corpse using the rendered
/// meshes of the REAL infected actor at the moment of death. This is an
/// approximate single-pivot fall, NOT recovered Roblox skeletal ragdoll.
/// No collision object, combat authority, reward, loot or persistence.
/// </summary>
public partial class Pass111FallenInfected : Node3D
{
    public const int MaxLiveBodies = 16;
    public const int MaxSnapshotMeshes = 64;
    public const float FallSeconds = .62f;
    public const float LifetimeSeconds = 5.5f;

    public int SourceMeshCount { get; private set; }
    public bool SnapshotWasTruncated { get; private set; }
    public bool SourceScenePrepared { get; private set; }
    public bool SourceBlueprintPresent { get; private set; }
    public bool Headshot { get; private set; }
    public float AgeSeconds { get; private set; }
    public float FallProgress => Mathf.Clamp(AgeSeconds / FallSeconds, 0f, 1f);
    public bool HasPhysicsOrScripts => false; // only passive Godot render nodes
    // Pass112: Source R6/fallback limb pivots are retained as read-only
    // visual articulation groups; zero means unsupported original rig.
    public int ArticulatedLimbCount112 => _limbs112.Count;
    public int MeshesOnArticulatedLimbs112 { get; private set; }
    public float MaximumAppliedLimbDelta112 { get; private set; }
    public int SnapshotMeshNodeCount => CountSnapshotMeshes(this);

    private sealed record LimbPose(Node3D Pivot, Vector3 OriginalRotation);
    private readonly Dictionary<string,LimbPose> _limbs112 =
        new(StringComparer.Ordinal);
    private Transform3D _originalPose;
    private bool _initialized;

    /// <summary>
    /// Source mesh instances may come from a locally installed source rig
    /// or the source-name-guided fallback humanoid. We keep only passive
    /// mesh/material references, not the source scripts, animation players,
    /// physics bodies, collision shapes or navigation.
    /// </summary>
    public static Pass111FallenInfected? Snapshot(
        Node3D world, InfectedAgent infected, bool headshot)
    {
        if (!GodotObject.IsInstanceValid(world) ||
            !GodotObject.IsInstanceValid(infected) ||
            infected.IsQueuedForDeletion())
            return null;

        var visual = infected.GetNodeOrNull<InfectedVisualAssembler>(
            "InfectedVisual");
        if (visual is null || !GodotObject.IsInstanceValid(visual))
            return null;

        var corpse = new Pass111FallenInfected
        {
            Name = "Pass111FallenInfected",
            Headshot = headshot,
            SourceScenePrepared = visual.UsingOriginalScene,
            SourceBlueprintPresent = visual.UsingSourceBlueprint
        };
        world.AddChild(corpse);
        corpse.GlobalTransform = infected.GlobalTransform;
        corpse._originalPose = corpse.GlobalTransform;

        // Flatten the visibly posed hierarchy into camera-independent,
        // world-local MeshInstance3D leaves. Never duplicate source scripts.
        var meshes = new List<MeshInstance3D>();
        GatherVisibleMeshes(visual, meshes);
        foreach (var mesh in meshes)
        {
            if (corpse.SourceMeshCount >= MaxSnapshotMeshes)
            {
                corpse.SnapshotWasTruncated = true;
                break;
            }
            if (mesh.Mesh is null ||
                !GodotObject.IsInstanceValid(mesh.Mesh))
                continue;

            // Pass112 groups only recognized *existing* shoulder/hip
            // pivots. A source-rig with unknown anatomy remains a truthful
            // original death-frame flat snapshot, not a guessed skeleton.
            Node3D snapshotParent = corpse;
            var parentInverse = corpse._originalPose.AffineInverse();
            if (Pass112R6CorpseArticulation.TryFindAnchor(
                    mesh,visual,out var region,out var sourcePivot) &&
                sourcePivot is not null)
            {
                if (!corpse._limbs112.TryGetValue(region,out var limb))
                {
                    var pivot=new Node3D { Name = "Pass112" + region + "Pivot" };
                    corpse.AddChild(pivot);
                    pivot.Transform=corpse._originalPose.AffineInverse() *
                        sourcePivot.GlobalTransform;
                    limb=new LimbPose(pivot,pivot.Rotation);
                    corpse._limbs112.Add(region,limb);
                }
                snapshotParent=limb.Pivot;
                parentInverse=limb.Pivot.GlobalTransform.AffineInverse();
                corpse.MeshesOnArticulatedLimbs112++;
            }

            var staticMesh = new MeshInstance3D
            {
                Name = "Pass111Mesh" + corpse.SourceMeshCount,
                Mesh = mesh.Mesh,
                Transform = parentInverse * mesh.GlobalTransform,
                MaterialOverride = mesh.MaterialOverride,
                MaterialOverlay = mesh.MaterialOverlay,
                CastShadow = mesh.CastShadow
            };
            // Preserve any installed original per-surface materials without
            // copying or writing the owner's private asset resource bytes.
            for (var surface = 0;
                surface < mesh.Mesh.GetSurfaceCount(); surface++)
            {
                var material = mesh.GetSurfaceOverrideMaterial(surface);
                if (material is not null)
                    staticMesh.SetSurfaceOverrideMaterial(surface, material);
            }
            snapshotParent.AddChild(staticMesh);
            corpse.SourceMeshCount++;
        }

        if (corpse.SourceMeshCount == 0)
        {
            corpse.QueueFree();
            return null;
        }

        corpse._initialized = true;
        return corpse;
    }

    private static void GatherVisibleMeshes(
        Node node, List<MeshInstance3D> output)
    {
        // Traverse all source rig pivots, but only retain visible renderers.
        // This also prevents an imported source's hidden LOD alternatives
        // from appearing simultaneously in the corpse.
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D mesh &&
                mesh.Mesh is not null && mesh.IsVisibleInTree())
                output.Add(mesh);
            GatherVisibleMeshes(child, output);
        }
    }

    private static int CountSnapshotMeshes(Node root)
    {
        var count=0;
        foreach(var child in root.GetChildren())
        {
            if(child is MeshInstance3D)count++;
            count+=CountSnapshotMeshes(child);
        }
        return count;
    }

    public override void _Process(double delta)
    {
        if (!_initialized) return;
        AgeSeconds += (float)Math.Max(0.0, delta);

        // The original reference footage shows infected remaining briefly
        // on the ground. This low-cost tumble is a presentation stand-in,
        // not a bone-based ragdoll or physical knockback simulation.
        var t = FallProgress;
        var ease = t * t * (3f - 2f * t);
        var pitch = (Headshot ? -.49f : .49f) * Mathf.Pi * ease;
        var basis = _originalPose.Basis *
            new Basis(Vector3.Right, pitch);
        GlobalTransform = new Transform3D(
            basis,
            _originalPose.Origin + Vector3.Down * (.16f * ease));

        foreach(var pair in _limbs112)
        {
            var deltaPose=Pass112R6CorpseArticulation.Delta(
                pair.Key,ease,Headshot);
            pair.Value.Pivot.Rotation=
                pair.Value.OriginalRotation + deltaPose;
            MaximumAppliedLimbDelta112=Math.Max(
                MaximumAppliedLimbDelta112,deltaPose.Length());
        }

        if (AgeSeconds > LifetimeSeconds - .70f)
        {
            var fade = Mathf.Clamp(
                (LifetimeSeconds - AgeSeconds) / .70f, 0f, 1f);
            Scale = Vector3.One * Mathf.Lerp(.85f, .25f, 1f - fade);
        }

        if (AgeSeconds >= LifetimeSeconds)
            QueueFree();
    }
}

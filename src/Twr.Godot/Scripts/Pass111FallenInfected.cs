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
    public bool HasPhysicsOrScripts => false; // only snapshots of MeshInstance3D

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

            var staticMesh = new MeshInstance3D
            {
                Name = "Pass111Mesh" + corpse.SourceMeshCount,
                Mesh = mesh.Mesh,
                Transform = corpse._originalPose.AffineInverse() *
                    mesh.GlobalTransform,
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
            corpse.AddChild(staticMesh);
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

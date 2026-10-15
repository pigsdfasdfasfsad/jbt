using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Native scene-construction and geometry-budget checks. This is not a
/// rendered similarity test or validation of original copyrighted art.
/// </summary>
public static class RuntimePickupVisualAcceptance
{
    public static async Task Verify(GameplayRoot game)
    {
        var actors = new List<PickupActor>();
        var totalParts = 0;
        try
        {
            foreach (var type in ReconstructedPickupVisuals.SupportedTypes)
            {
                var actor = new PickupActor
                {
                    Name = "PickupVisualAcceptance_" + actors.Count,
                    PickupType = type,
                    Position = new Vector3(100 + actors.Count * 1.5f, 5, 100)
                };
                game.AddChild(actor);
                actors.Add(actor);
                var model = actor.GetNodeOrNull<Node3D>("ReconstructedPickupVisual")
                    ?? throw new InvalidOperationException("Production pickup did not use reconstructed visual: " + type);
                if (model.GetMeta("OriginalGeometryRecovered").AsBool() ||
                    model.GetMeta("OriginalTexturePixels").AsBool())
                    throw new InvalidOperationException("Procedural pickup incorrectly claimed original source art");
                var meshes = model.GetChildren().OfType<MeshInstance3D>().ToArray();
                if (meshes.Length < 4 || meshes.Length > ReconstructedPickupVisuals.MaximumMeshParts ||
                    meshes.Length != model.GetChildCount())
                    throw new InvalidOperationException("Unexpected scripted/collision child or mesh budget: " + type);
                foreach (var mesh in meshes)
                    if (mesh.Mesh is null || mesh.MaterialOverride is not StandardMaterial3D)
                        throw new InvalidOperationException("Incomplete pickup mesh/material: " + type);
                var bounds = ReconstructedPickupVisuals.Measure(model);
                var envelope = ReconstructedPickupVisuals.PickupEnvelope;
                if (bounds.Size.X <= 0 || bounds.Size.Y <= 0 || bounds.Size.Z <= 0 ||
                    bounds.Size.X > envelope.X + .001f || bounds.Size.Y > envelope.Y + .001f ||
                    bounds.Size.Z > envelope.Z + .001f ||
                    (bounds.Position + bounds.Size * .5f).Length() > .001f)
                    throw new InvalidOperationException("Pickup silhouette exceeded original grounded footprint: " + type);
                if (!actor.IsInGroup("twr-pickup") ||
                    !actor.GetChildren().OfType<Label3D>().Any(label => label.Text.Contains("[E]")))
                    throw new InvalidOperationException("Pickup interaction affordance was lost: " + type);
                totalParts += meshes.Length;
            }
            await game.ToSignal(game.GetTree(), SceneTree.SignalName.PhysicsFrame);
            if (actors.Count != 13 || totalParts < 70)
                throw new InvalidOperationException("Incomplete distinct pickup catalog");
            GD.Print("TWR_RUNTIME_PICKUP_VISUALS_OK" +
                " types=" + actors.Count + " mesh_parts=" + totalParts +
                " production_pickup_actors=true original_envelope_preserved=true" +
                " no_new_colliders=true original_mesh_parity_unverified=true");
        }
        finally
        {
            foreach (var actor in actors)
                if (GodotObject.IsInstanceValid(actor)) actor.QueueFree();
        }
        await game.ToSignal(game.GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
}

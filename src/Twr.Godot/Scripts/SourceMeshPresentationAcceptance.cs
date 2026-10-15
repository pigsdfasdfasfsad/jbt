using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Tests the real map importer using disposable, synthetic v2 source records.
/// No owner map coordinates, images, meshes, or private source ZIPs are used.
/// Checks actual MultiMesh geometry/instance transforms and collision shapes.
/// </summary>
public partial class SourceMeshPresentationAcceptance : Node3D
{
    public override async void _Ready()
    {
        try
        {
            await Verify();
            GD.Print("TWR_SOURCE_MESH_PRESENTATION_OK" +
                " actual_importer=true builtin_relative_scale=true" +
                " shape_batches_distinct=true cylinder_axis_x=true" +
                " parent_collision_unchanged=true filemesh_fallback_preserved=true" +
                " private_map_visual_parity_unverified=true");
            RuntimeShutdownCoordinator.Request(GetTree(), 0);
        }
        catch (Exception e)
        {
            GD.PushError("TWR_SOURCE_MESH_PRESENTATION_FAILED " + e);
            RuntimeShutdownCoordinator.Request(GetTree(), 1);
        }
    }

    private static Dictionary<string, object> Part(int index, float[] size,
        string special = "", float[]? scale = null, string shape = "1")
    {
        var row = new Dictionary<string, object>
        {
            ["kind"] = "geometry", ["class"] = "Part",
            ["name"] = "SyntheticVisual" + index,
            ["s"] = size, ["t"] = new float[] { index * 20, 5, 0 },
            ["r"] = new float[] { 1,0,0, 0,1,0, 0,0,1 },
            ["rgb"] = new float[] { 180,180,180 }, ["mat"] = "256",
            ["opacity"] = 1f, ["shape"] = shape,
            ["collidable"] = true, ["shadow"] = true
        };
        if (special.Length > 0) row["specialMeshType"] = special;
        if (scale is not null) row["specialMeshScale"] = scale;
        return row;
    }

    private async Task Verify()
    {
        var folder = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "Content", "Maps");
        var fixture = Path.Combine(folder, "Cabin.scene.jsonl.gz");
        if (File.Exists(fixture))
            throw new InvalidOperationException("Refusing to overwrite installed source map");
        Directory.CreateDirectory(folder);
        var rows = new[]
        {
            Part(0, [12,8,2]),
            Part(1, [12,8,2], "6", [.5f,2,.25f]),
            Part(2, [6,4,2], "3", [1,2,3]),
            Part(3, [8,6,4], "2", [.5f,1,1]),
            Part(4, [10,6,4], "4", [.5f,.5f,1]),
            Part(5, [10,4,4], shape:"2"),
            Part(6, [.2f,.2f,.2f], "5", [20,3,20]),
            Part(7, [4,4,4], "6", [1,1,1]),
            Part(8, [5,4,3]),
            Part(9, [20,10,1], "6", [1,1,.5f])
        };
        Vector3[] expectedStudSizes =
        [ new(12,8,2), new(6,16,.5f), new(6,8,6), new(4,6,4),
          new(5,3,3), new(10,4,4), new(20,3,20), new(4,4,4),
          new(5,4,3), new(20,10,.5f) ];
        Node3D? stage = null;
        try
        {
            using (var file = new FileStream(fixture, FileMode.CreateNew, FileAccess.Write))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzip))
            {
                writer.WriteLine(JsonSerializer.Serialize(new
                {
                    format = "twr-source-map-v2", map = "Cabin",
                    scale = RobloxUnits.MetersPerStud, synthetic = true,
                    counts = new
                    {
                        geometry = 10, collision = 1, lights = 0,
                        infected_spawns = 1, player_spawns = 1,
                        item_markers = 1, fortification_markers = 0
                    }
                }));
                foreach (var row in rows) writer.WriteLine(JsonSerializer.Serialize(row));
                var floor = Part(20, [400,1,40]);
                floor["kind"] = "collision";
                floor["t"] = new float[] { 0,-1,0 };
                writer.WriteLine(JsonSerializer.Serialize(floor));
                foreach (var side in new[] { "player", "infected" })
                    writer.WriteLine(JsonSerializer.Serialize(new
                    {
                        kind = "spawn", side, name = "SyntheticSpawn",
                        t = new float[] { 0,0,0 }
                    }));
                writer.WriteLine(JsonSerializer.Serialize(new
                {
                    kind = "pickup_spawn", group = "Item", t = new float[] { 0,0,3 }
                }));
            }
            if (!LaboratorySourceLoader.TryBuild(this, "Cabin", out var layout))
                throw new InvalidOperationException("Synthetic map failed the actual importer");
            stage = GetNode<Node3D>("RecoveredCabin");
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            var found = new bool[rows.Length];
            var colliderChecks = 0;
            foreach (var instance in stage.GetChildren().OfType<MultiMeshInstance3D>())
            {
                var multi = instance.Multimesh;
                for (var i = 0; i < multi.InstanceCount; i++)
                {
                    var transform = multi.GetInstanceTransform(i);
                    var index = (int)Math.Round(transform.Origin.X / (20 * RobloxUnits.MetersPerStud));
                    if (index < 0 || index >= rows.Length || found[index])
                        throw new InvalidOperationException("Lost or duplicate source visual instance");
                    found[index] = true;
                    var scale = new Vector3(transform.Basis.X.Length(),
                        transform.Basis.Y.Length(), transform.Basis.Z.Length());
                    var expected = expectedStudSizes[index] * RobloxUnits.MetersPerStud;
                    if (scale.DistanceTo(expected) > .003f)
                        throw new InvalidOperationException($"Wrong visual dimensions at {index}: {scale} expected {expected}");
                    if (index == 2 && multi.Mesh is not SphereMesh)
                        throw new InvalidOperationException("Sphere SpecialMesh merged with block appearance batch");
                    if (index is 4 or 5) VerifyCylinderX(multi.Mesh);
                    if (index == 3 && multi.Mesh is not ArrayMesh)
                        throw new InvalidOperationException("Wedge SpecialMesh was replaced with a box");
                }
            }
            if (found.Any(x => !x)) throw new InvalidOperationException("Source visual omitted");
            foreach (var collision in stage.GetNode<StaticBody3D>("LaboratoryCollision")
                         .GetChildren().OfType<CollisionShape3D>())
            {
                var index = (int)Math.Round(collision.Position.X / (20 * RobloxUnits.MetersPerStud));
                if (index is < 1 or > 4) continue;
                if (collision.Shape is not BoxShape3D box)
                    throw new InvalidOperationException("Visual SpecialMesh changed parent collision kind");
                var sourceSize = (float[])rows[index]["s"];
                var expected = new Vector3(sourceSize[0], sourceSize[1], sourceSize[2]) * RobloxUnits.MetersPerStud;
                if (box.Size.DistanceTo(expected) > .003f)
                    throw new InvalidOperationException("SpecialMesh incorrectly changed parent collision size");
                colliderChecks++;
            }
            if (colliderChecks != 4) throw new InvalidOperationException("Missing source collision checks");
            GD.Print("TWR_SOURCE_MESH_IMPORT_DETAIL visuals=10 unchanged_specialmesh_colliders=4");
        }
        finally
        {
            // Only the file created by this test is removed. An existing
            // owner file was rejected above before entering this block.
            if (File.Exists(fixture)) File.Delete(fixture);
            if (stage is not null && GodotObject.IsInstanceValid(stage)) stage.QueueFree();
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void VerifyCylinderX(Mesh mesh)
    {
        var faces = mesh.GetFaces();
        var xCaps = 0;
        var yCaps = 0;
        for (var i = 0; i + 2 < faces.Length; i += 3)
        {
            var a = faces[i]; var b = faces[i+1]; var c = faces[i+2];
            // Flat cap triangles all have one constant local coordinate.
            if (Math.Abs(Math.Abs(a.X)-.5f) < .0001f &&
                Math.Abs(a.X-b.X) < .0001f && Math.Abs(a.X-c.X) < .0001f) xCaps++;
            if (Math.Abs(Math.Abs(a.Y)-.5f) < .0001f &&
                Math.Abs(a.Y-b.Y) < .0001f && Math.Abs(a.Y-c.Y) < .0001f) yCaps++;
        }
        if (xCaps < 32 || yCaps != 0)
            throw new InvalidOperationException($"Cylinder end caps are not X-aligned: X={xCaps}, Y={yCaps}");
    }
}

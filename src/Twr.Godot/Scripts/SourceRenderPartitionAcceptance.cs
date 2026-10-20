using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Actual-importer, real-renderer check, with synthetic source rows only.</summary>
public static class SourceRenderPartitionAcceptance
{
    public static async Task Verify(Node3D host)
    {
        if (DisplayServer.GetName()=="headless")
            throw new InvalidOperationException("MultiMesh readback requires real rendering");
        var dir=Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!,"Content","Maps");
        var file=Path.Combine(dir,"Ranch.scene.jsonl.gz");
        if (File.Exists(file)) throw new InvalidOperationException("Will not overwrite owner's Ranch scene");
        Directory.CreateDirectory(dir);
        Node3D? stage=null;
        var expected=new List<Vector3>();
        try
        {
            using(var stream=new FileStream(file,FileMode.CreateNew,System.IO.FileAccess.Write))
            using(var zipped=new GZipStream(stream,CompressionLevel.Optimal))
            using(var writer=new StreamWriter(zipped))
            {
                writer.WriteLine(JsonSerializer.Serialize(new {
                    format="twr-source-map-v2",map="Ranch",scale=RobloxUnits.MetersPerStud,synthetic=true,
                    counts=new {geometry=500,collision=1,lights=0,infected_spawns=1,player_spawns=1,item_markers=1,fortification_markers=0}
                }));
                for(var i=0;i<500;i++)
                {
                    var point=i<300 ? new Vector3(4+i*.025f,10,10) :
                        i<400 ? new Vector3(512+(i-300)*.025f,150,-300) :
                        new Vector3(-300+(i-400)*.025f,-100,400);
                    expected.Add(new Vector3(point.X,point.Y,-point.Z)*RobloxUnits.MetersPerStud);
                    writer.WriteLine(JsonSerializer.Serialize(new {
                        kind="geometry",@class="Part",name="SyntheticBatch"+i,
                        s=new[]{1f,1f,1f},t=new[]{point.X,point.Y,point.Z},
                        r=new[]{1,0,0,0,1,0,0,0,1},rgb=new[]{150,180,210},mat="256",
                        opacity=1,shape="1",collidable=false,shadow=true
                    }));
                }
                writer.WriteLine(JsonSerializer.Serialize(new {kind="collision",@class="Part",name="FixtureFloor",
                    s=new[]{20,1,20},t=new[]{0,-1,0},r=new[]{1,0,0,0,1,0,0,0,1},shape="1"}));
                foreach(var side in new[]{"player","infected"})
                    writer.WriteLine(JsonSerializer.Serialize(new {kind="spawn",side,name="FixtureSpawn",t=new[]{0,0,0}}));
                writer.WriteLine(JsonSerializer.Serialize(new {kind="pickup_spawn",group="Item",t=new[]{0,0,3}}));
            }
            if (!LaboratorySourceLoader.TryBuild(host,"Ranch",out _))
                throw new InvalidOperationException("Actual source loader refused partition fixture");
            stage=host.GetNode<Node3D>("RecoveredRanch");
            await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);
            var batches=stage.GetChildren().OfType<MultiMeshInstance3D>().ToArray();
            if(batches.Length<5 || batches.Length>8) throw new InvalidOperationException("Not bounded into expected cells/pages: "+batches.Length);
            var found=new HashSet<int>();
            var material=batches[0].MaterialOverride.GetRid();
            var mesh=batches[0].Multimesh.Mesh.GetRid();
            foreach(var node in batches)
            {
                var multi=node.Multimesh;
                if(multi.InstanceCount>SourceRenderPartition.MaximumInstances ||
                    node.MaterialOverride.GetRid()!=material || multi.Mesh.GetRid()!=mesh)
                    throw new InvalidOperationException("Lost batch cap or duplicated shared mesh/material resources");
                var cell="";
                for(var i=0;i<multi.InstanceCount;i++)
                {
                    var transform=multi.GetInstanceTransform(i);
                    var index=expected.FindIndex(v=>v.DistanceTo(transform.Origin)<.0001f);
                    if(index<0 || !found.Add(index)) throw new InvalidOperationException("Source transform lost, changed or duplicated");
                    var key=SourceRenderPartition.Cell(transform.Origin);
                    if(cell.Length>0 && cell!=key) throw new InvalidOperationException("Distant cell merged into one MultiMesh");
                    cell=key;
                    var extent=Vector3.One*RobloxUnits.MetersPerStud*.5f;
                    if(!multi.CustomAabb.Grow(.001f).HasPoint(transform.Origin+extent) ||
                       !multi.CustomAabb.Grow(.001f).HasPoint(transform.Origin-extent))
                        throw new InvalidOperationException("Spatial culling box clips actual source mesh extents");
                }
            }
            if(found.Count!=500) throw new InvalidOperationException("Original visual instances omitted");
            var colliders=stage.GetNode<StaticBody3D>("LaboratoryCollision").GetChildren().OfType<CollisionShape3D>().Count();
            if(colliders!=1) throw new InvalidOperationException("Rendering partition changed source physics");
            GD.Print("TWR_SOURCE_RENDER_PARTITION_OK instances=500 batches="+batches.Length+
                " max_per_batch=128 shared_mesh=true shared_material=true unchanged_colliders=1 source_transforms_preserved=true");
        }
        finally
        {
            if(File.Exists(file)) File.Delete(file);
            if(stage is not null && GodotObject.IsInstanceValid(stage)) stage.QueueFree();
        }
        await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);
    }
}

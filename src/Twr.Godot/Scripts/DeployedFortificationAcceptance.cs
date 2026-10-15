using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

public static class DeployedFortificationAcceptance
{
    public static async Task Verify(GameplayRoot game,FirstPersonPlayer player,LocalSessionNode runtime)
    {
        var definitions=FortificationCatalogRuntime.Load();
        var checkedTypes=0;
        foreach (var type in new[]{"Barbed Wire","Clap Bomb","Jack","50 Cal"})
        {
            var definition=definitions.First(x=>x.Name==type);
            var actor=new FortificationActor
            {
                Name="EquipmentVisualCheck"+checkedTypes, Definition=definition,
                Player=player,Runtime=runtime,Position=new Vector3(40+checkedTypes*4,.1f,20)
            };
            game.AddChild(actor);
            await game.ToSignal(game.GetTree(),SceneTree.SignalName.PhysicsFrame);
            var visual=actor.DeployedVisual;
            if (!visual.IsInsideTree() || CountColliders(visual)>0 ||
                visual.GetMeta("OriginalGeometryRecovered").AsBool())
                throw new InvalidOperationException("Deployed visual changed collision or claimed original topology");
            if(type=="Barbed Wire" && visual.WireSegments!=160)
                throw new InvalidOperationException("Coiled wire instance geometry is missing");
            if(type=="50 Cal")
            {
                var direction=new Vector3(.5f,.1f,-1f).Normalized();
                visual.Aim(direction);
                var pivot=visual.GetNode<Node3D>("GunAimPivot");
                if((-pivot.GlobalBasis.Z).DistanceTo(direction)>.001f)
                    throw new InvalidOperationException("Visible barrel does not follow world aiming direction");
                visual.PresentShot();
                if(!visual.ShotFlashVisible) throw new InvalidOperationException("Missing shot feedback");
                visual._Process(.2);
                if(visual.ShotFlashVisible) throw new InvalidOperationException("Muzzle flash did not expire");
            }
            else if(type!="Barbed Wire" && visual.GetChildCount()!=1)
                throw new InvalidOperationException("Reference-guided deployment model missing");
            actor.QueueFree();
            checkedTypes++;
        }
        await game.ToSignal(game.GetTree(),SceneTree.SignalName.PhysicsFrame);
        GD.Print("TWR_DEPLOYED_FORTIFICATION_VISUALS_OK types="+checkedTypes+
            " instanced_wire_segments=160 aim_tracks=true shot_feedback_expires=true"+
            " no_new_colliders=true original_mesh_parity_unverified=true");
    }
    private static int CountColliders(Node root)
    {
        var count=root is CollisionObject3D or CollisionShape3D ? 1 : 0;
        foreach(var child in root.GetChildren()) count+=CountColliders(child);
        return count;
    }
}

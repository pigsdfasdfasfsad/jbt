using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Real Jack actor timer and damage, with isolated synthetic targets.</summary>
public static class TimedJackAcceptance
{
    public static async Task Verify(GameplayRoot game,FirstPersonPlayer player,LocalSessionNode runtime)
    {
        var holder=new Node3D { Name="IsolatedJackPhysicsTargets" };
        game.AddChild(holder);
        var origin=new Vector3(90,55,0);
        InfectedAgent Target(string name,Vector3 position)
        {
            var actor=new InfectedAgent { Name=name,Health=1000,Position=position,Runtime=runtime };
            holder.AddChild(actor);
            return actor;
        }
        var near=Target("NearTarget",origin+new Vector3(1.5f,.8f,0));
        var far=Target("FarTarget",origin+new Vector3(6f,.8f,0));
        var blocked=Target("OccludedTarget",origin+new Vector3(0,.8f,3f));
        var tanker=new DamageObjectiveTarget
        { Name="JackDamageObjectiveTarget", Position=origin+new Vector3(-4,0,-3) };
        holder.AddChild(tanker);
        var wall=new StaticBody3D
        {
            Position=origin+new Vector3(0,1,1.5f),CollisionLayer=1u,CollisionMask=0u
        };
        holder.AddChild(wall);
        wall.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=new Vector3(5,2.4f,.2f) } });
        var jack=new FortificationActor
        {
            Name="ActualTimedJack",Definition=FortificationCatalogRuntime.Load().First(x=>x.Name=="Jack"),
            Player=player,Runtime=runtime,Position=origin
        };
        holder.AddChild(jack);
        jack.SetProcess(false);
        var oldControls=player.GameplayControlsEnabled;
        player.SetGameplayControlsEnabled(true);
        try
        {
            await game.ToSignal(game.GetTree(),SceneTree.SignalName.PhysicsFrame);
            await game.ToSignal(game.GetTree(),SceneTree.SignalName.PhysicsFrame);
            jack._Process(9.99);
            if(near.Health!=1000 || far.Health!=1000 || blocked.Health!=1000 || tanker.Health!=2500 || jack.IsQueuedForDeletion())
                throw new InvalidOperationException("Jack incorrectly used proximity instead of its fuse");
            jack._Process(.02);
            var nearDamage=1000-near.Health;
            var farDamage=1000-far.Health;
            if(!jack.IsQueuedForDeletion() || nearDamage<=farDamage || farDamage<=0 || blocked.Health!=1000 || tanker.Health>=2500)
                throw new InvalidOperationException("Timed Jack did not apply bounded occluded distance falloff");
            var tankerAfterFirst=tanker.Health;
            jack._Process(100);
            if(tanker.Health!=tankerAfterFirst)
                throw new InvalidOperationException("Jack damaged the objective twice");
            if(Math.Abs((1000-near.Health)-nearDamage)>.001f)
                throw new InvalidOperationException("Jack applied its detonation twice");
            GD.Print("TWR_TIMED_JACK_OK fuse_seconds=10 timer_not_proximity=true"+
                " near_damage="+nearDamage+" far_damage="+farDamage+
                " wall_blocked=true detonation_once=true actual_tanker_damage="+(2500-tanker.Health)+
                " source_peak_damage="+jack.Definition.Damage+
                " exact_falloff_curve_unverified=true elapsed_time_test_accelerated=true");
        }
        finally
        {
            player.SetGameplayControlsEnabled(oldControls);
            holder.QueueFree();
        }
        await game.ToSignal(game.GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
}

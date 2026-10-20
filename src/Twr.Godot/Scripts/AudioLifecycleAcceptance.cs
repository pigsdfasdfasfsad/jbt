using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Twr.Godot;

/// <summary>Native playback lifecycle tests, using synthesized offline sounds.
/// No test writes, original recordings or audio-network connections.</summary>
public partial class AudioLifecycleAcceptance : Node
{
    public override async void _Ready()
    {
        try
        {
            for (var cycle=0;cycle<6;cycle++)
            {
                var audio=new OfflineAudioRuntime(); AddChild(audio);
                for (var i=0;i<24;i++) audio.Play("ambient");
                await Frames(2);
                var voices=audio.GetChildren().OfType<AudioStreamPlayer>().ToArray();
                if (voices.Length!=20 || voices.Any(v=>v.Stream is null))
                    throw new InvalidOperationException("Concurrent voice bound changed");
                // Real detachment, not an imitation of _ExitTree: a detached
                // scene can outlive its node and must not retain audio handles.
                RemoveChild(audio);
                if (voices.Any(v=>GodotObject.IsInstanceValid(v) && (v.Playing || v.Stream is not null)))
                    throw new InvalidOperationException("Detached audio retained playback or stream references");
                audio.Play("gunshot");
                if (audio.GetChildCount()>voices.Length)
                    throw new InvalidOperationException("Retired scene admitted late playback");
                audio.Free(); await Frames(3);
            }
            var normal=new OfflineAudioRuntime(); AddChild(normal);
            normal.Play("pickup"); await Frames(42);
            if (normal.GetChildren().OfType<AudioStreamPlayer>().Any())
                throw new InvalidOperationException("Completed effect did not retire its voice");
            normal.Play("ambient"); await Frames(2);
            var stopped=normal.GetChildren().OfType<AudioStreamPlayer>().Single();
            stopped.Stop(); await Frames(3);
            if (normal.GetChildren().OfType<AudioStreamPlayer>().Any())
                throw new InvalidOperationException("Stopped effect still consumes the voice budget");
            normal.QueueFree(); await Frames(3);
            GD.Print("TWR_AUDIO_LIFECYCLE_OK scenes=6 voices=120 bounded=true natural_finish=true stopped_voice_reclaimed=true detach_releases_streams=true offline_synthesis=true");
            RuntimeShutdownCoordinator.Request(GetTree(),0);
        }
        catch(Exception e)
        {
            GD.PushError("TWR_AUDIO_LIFECYCLE_FAILED "+e);
            RuntimeShutdownCoordinator.Request(GetTree(),1);
        }
    }
    private async Task Frames(int count)
    { for(var i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
}

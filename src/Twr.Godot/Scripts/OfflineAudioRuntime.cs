using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Offline, locally installed PCM WAV effects with synthesized fallback.
/// This is an original substitute audio layer, NOT a recovered Roblox
/// soundtrack. Original audio IDs alone are not playable audio content.
/// </summary>
public partial class OfflineAudioRuntime : Node
{
    private const int Rate = 22050;
    private const int MaxVoices = 20;
    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "gunshot", "shotgun", "launcher", "melee", "reload",
        "wave", "wave-clear", "infected", "pickup", "hurt", "ambient"
    };
    private readonly Dictionary<string, AudioStreamWav> _streams = new(StringComparer.Ordinal);
    private readonly List<AudioStreamPlayer> _voices = [];

    public AudioStreamWav StreamFor(string kind)
    {
        if (!Events.Contains(kind)) throw new ArgumentException("Unknown sound event",nameof(kind));
        if (_streams.TryGetValue(kind,out var cached)) return cached;
        var privateWav = PrivateOverride(kind);
        var selected = privateWav ?? Synthesize(kind);
        _streams[kind] = selected;
        return selected;
    }

    public void Play(string kind)
    {
        if (_voices.Count >= MaxVoices) return;
        var voice = new AudioStreamPlayer
        {
            Name = "Sfx_" + kind,
            Stream = StreamFor(kind),
            VolumeDb = kind switch
            {
                "ambient" => -23f,
                "gunshot" or "shotgun" or "launcher" => -10f,
                "hurt" => -13f,
                _ => -15f
            }
        };
        _voices.Add(voice);
        AddChild(voice);
        voice.Finished += () =>
        {
            _voices.Remove(voice);
            voice.QueueFree();
        };
        voice.Play();
    }

    private static AudioStreamWav? PrivateOverride(string kind)
    {
        var executable = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(executable,"Content","Audio",kind+".wav"),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","Audio",kind+".wav"),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot",
                "Content","Audio",kind+".wav")
        };
        foreach (var file in candidates)
        {
            if (!File.Exists(file)) continue;
            try
            {
                var info = new FileInfo(file);
                if (info.Length > 12*1024*1024 || info.Length < 44) continue;
                var wav = ReadPcmWav(file);
                if (wav is not null) return wav;
                GD.PushWarning("TWR_AUDIO_UNSUPPORTED_PCM_FORMAT name=" + kind);
            }
            catch (Exception error)
            {
                GD.PushWarning("TWR_AUDIO_INVALID_PRIVATE_WAV name=" + kind +
                    " reason=" + error.Message);
            }
        }
        return null;
    }

    // Supports standard mono/stereo 16-bit PCM WAV; other proprietary audio
    // codecs require authorized offline conversion before private packaging.
    // Source-positioned soundscapes reuse the same bounded offline PCM parser.
    // A blank Roblox SoundId never becomes an HTTP request or arbitrary URI.
    public static AudioStreamWav? TryLoadPrivatePcm16Wav(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var length = new FileInfo(path).Length;
            if (length is < 44 or > 12*1024*1024) return null;
            return ReadPcmWav(path);
        }
        catch (Exception error)
        {
            GD.PushWarning("TWR_SOURCE_WAV_INVALID: " + error.Message);
            return null;
        }
    }

    private static AudioStreamWav? ReadPcmWav(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (Encoding.ASCII.GetString(bytes,0,4) != "RIFF" ||
            Encoding.ASCII.GetString(bytes,8,4) != "WAVE")
            return null;
        var format = 0;
        var channels = 0;
        var rate = 0;
        var bits = 0;
        byte[]? pcm = null;
        var pos = 12;
        while (pos + 8 <= bytes.Length)
        {
            var name = Encoding.ASCII.GetString(bytes,pos,4);
            var length = BitConverter.ToUInt32(bytes,pos+4);
            pos += 8;
            if (length > bytes.Length-pos) return null;
            if (name == "fmt " && length >= 16)
            {
                format = BitConverter.ToUInt16(bytes,pos);
                channels = BitConverter.ToUInt16(bytes,pos+2);
                rate = (int)BitConverter.ToUInt32(bytes,pos+4);
                bits = BitConverter.ToUInt16(bytes,pos+14);
            }
            if (name == "data" && length > 0)
            {
                pcm = new byte[(int)length];
                Buffer.BlockCopy(bytes,pos,pcm,0,(int)length);
            }
            pos += (int)length + ((int)length & 1);
        }
        if (format != 1 || channels is < 1 or > 2 || bits != 16 ||
            rate is < 8000 or > 96000 || pcm is null ||
            pcm.Length % (channels * 2) != 0)
            return null;
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = rate,
            Stereo = channels == 2,
            Data = pcm
        };
    }

    private static AudioStreamWav Synthesize(string kind)
    {
        // Fallback contains original synthesized impulses/tone (no copyrighted
        // samples), with deterministic pseudo-random grit for guns/infected.
        var duration = kind switch
        {
            "gunshot" => .20f,
            "shotgun" => .27f,
            "launcher" => .43f,
            "melee" => .13f,
            "reload" => .24f,
            "wave" or "wave-clear" => .63f,
            "infected" => .42f,
            "pickup" => .21f,
            "hurt" => .31f,
            _ => 2.5f
        };
        var sampleCount = Math.Max(1,(int)(duration * Rate));
        var bytes = new byte[sampleCount * 2];
        uint seed = 2166136261;
        foreach (var c in kind) seed = (seed ^ c) * 16777619;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)Rate;
            var progress = i / (float)sampleCount;
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            var noise = ((seed & 65535) / 32767.5f) - 1f;
            var envelope = Mathf.Pow(Mathf.Max(0,1f-progress),kind=="ambient" ? 1f : 2.1f);
            var value = kind switch
            {
                "gunshot" => noise*.73f + Mathf.Sin(t*2300f)*.20f,
                "shotgun" => noise*.90f + Mathf.Sin(t*950f)*.10f,
                "launcher" => noise*.52f + Mathf.Sin(t*260f)*.43f,
                "melee" => noise*.28f + Mathf.Sin(t*950f)*.52f,
                "reload" => Mathf.Sin(t*1400f)*.31f + noise*.22f,
                "wave" => Mathf.Sin(t*(400f+340f*progress))*.53f,
                "wave-clear" => Mathf.Sin(t*(690f-110f*progress))*.46f,
                "infected" => Mathf.Sin(t*(170f-60f*progress)+noise*.45f)*.6f,
                "pickup" => Mathf.Sin(t*(1200f+240f*progress))*.47f,
                "hurt" => noise*.48f + Mathf.Sin(t*170f)*.24f,
                _ => noise*.12f + Mathf.Sin(t*63f)*.05f
            };
            var sample = (short)(Math.Clamp(value*envelope*.57f,-1f,1f)*32767f);
            bytes[i*2] = (byte)(sample & 255);
            bytes[i*2+1] = (byte)((sample >> 8) & 255);
        }
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = Rate,
            Stereo = false,
            Data = bytes
        };
    }
}

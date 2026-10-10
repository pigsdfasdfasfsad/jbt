using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;

namespace Twr.Godot;

/// <summary>
/// Source-positioned CMaps soundscape from the owner's TestPlace.rbxlx.
/// SoundId is EMPTY for all 206 release-map source emitters. Never requests
/// Roblox audio or asserts WAV authenticity. F2 previews local offline WAV
/// substitutes. On approximated blockouts only global ambience is eligible:
/// original positioned emitters cannot be trusted to coincide with blockouts.
/// </summary>
public partial class Pass38SourceSoundscapeRuntime : Node3D
{
    private const string OriginalSourceSha =
        "272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2";
    private const int MaxDocumentBytes = 196608;
    private const int MaxEmitters = 256;
    private const int MaxLocalVoices = 12;
    private const int MaxEnvironmentVoices = 2;
    private const float MetresPerStud = RobloxUnits.MetersPerStud;
    private static readonly Dictionary<string,(string Sha,int Total,int Local,int Global)> Known =
        new(StringComparer.Ordinal)
    {
        ["Ranch"]=("478731b0c1b1c4fbb51be305487c5b88064bd464f888f7c5ee396992fb69de39",39,38,1),
        ["Mill"]=("d9c224af4727ed4cdcb3192852bc9c99d5c9e2f42ed09eeaec2f346d58a748dc",34,33,1),
        ["Bypass"]=("898b27f3619fd5f5296d16460ffe9f27faa53adffb0d01c5af102c71eb7dca8e",80,79,1),
        ["Cabin"]=("00bb0468cd73ccce6755e350846e970b3c066140bd6a47318a655864a68f18ae",1,0,1),
        ["Cargo"]=("d7dfa7a7d949d71529175a470f677bc8bdd4a7d7f47c479c8ef4596a7c53027e",30,30,0),
        ["District"]=("dde3d230781fcb716c6a8fac2b55f305edf6262fc95e4159f5db905849e5a3fb",8,7,1),
        ["Expressway"]=("413912cea893d95a670a63548027ed8eb6a644704c44ab37662ec15d03710b5d",4,3,1),
        ["Prison"]=("a7e75aff712cf66615695d68e11539dcddb3a391e109fb75e07cf80d421e0fbc",7,6,1),
        ["Laboratory"]=("19f4d05a2af98f8c20ab2e00b36ba261455783442df3354ec90f099e1f645f1d",0,0,0),
        ["Manor"]=("6496a7fbeda581e2afa4963a12eb5bd95d165e5554669349a87b1ee369f1869a",3,1,2)
    };

    private sealed record Cue(
        bool Local, Vector3 Position, float Range, AudioStreamPlayer? Ambient,
        AudioStreamPlayer3D? Positioned);

    private readonly List<Cue> _cues = new();
    private FirstPersonPlayer? _listener;
    private double _refresh;
    public bool Enabled { get; private set; }
    public bool HasExactMapGeometry { get; private set; }
    public int SourceEmitterCount { get; private set; }
    public int SourcePositionedCount { get; private set; }
    public int SourceEnvironmentCount { get; private set; }
    public int InstalledWavEmitterCount => _cues.Count;
    public int ActiveVoiceCount => _cues.Count(x =>
        x.Local ? x.Positioned?.Playing == true : x.Ambient?.Playing == true);

    public static Pass38SourceSoundscapeRuntime? TryBuild(
        Node3D owner,string mapName,bool exactSourceMapGeometry)
    {
        if (!Known.TryGetValue(mapName,out var expected)) return null;
        var exe=Path.GetDirectoryName(OS.GetExecutablePath()) ??
            Directory.GetCurrentDirectory();
        var name=mapName+".soundscape38.json";
        var path=new[]
        {
            Path.Combine(exe,"Content","SourceSoundscapes",name),
            Path.Combine(Directory.GetCurrentDirectory(),"Content","SourceSoundscapes",name),
            Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","SourceSoundscapes",name)
        }.FirstOrDefault(File.Exists);
        if(path is null)return null;

        Pass38SourceSoundscapeRuntime? node=null;
        try
        {
            var bytes=File.ReadAllBytes(path);
            if(bytes.Length<250 || bytes.Length>MaxDocumentBytes)
                throw new InvalidDataException("Original soundscape pack size invalid");
            var synthetic=OS.GetCmdlineUserArgs().Contains("--smoke-pass38",StringComparer.Ordinal)
                && mapName=="Manor";
            var hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if(!synthetic && hash!=expected.Sha)
                throw new InvalidDataException("Original soundscape SHA-256 mismatch");
            using var parsed=JsonDocument.Parse(bytes);
            var root=parsed.RootElement;
            if(Text(root,"format")!="twr-source-soundscape38-v1" ||
                Text(root,"map")!=mapName ||
                Text(root,"owner_rbxlx_sha256") !=
                    (synthetic ? new string('0',64) : OriginalSourceSha) ||
                Text(root,"source_path")!=$"ReplicatedStorage/CMaps/{mapName}" ||
                root.GetProperty("source_ids_present").GetBoolean() ||
                root.GetProperty("audio_binary_bytes_present").GetBoolean() ||
                !root.GetProperty("playback_is_approximate_with_external_wav").GetBoolean())
                throw new InvalidDataException("Original soundscape provenance mismatch");

            var rows=root.GetProperty("emitters");
            var total=root.GetProperty("emitter_count").GetInt32();
            var local=root.GetProperty("local_count").GetInt32();
            var global=root.GetProperty("environment_count").GetInt32();
            if(rows.ValueKind!=JsonValueKind.Array ||
                total<0 || total>MaxEmitters || local<0 || global<0 ||
                rows.GetArrayLength()!=total || local+global!=total ||
                (!synthetic && (total!=expected.Total ||
                    local!=expected.Local || global!=expected.Global)))
                throw new InvalidDataException("Original emitter count mismatch");

            node=new Pass38SourceSoundscapeRuntime
            {
                Name="Pass38SourceSoundscape",HasExactMapGeometry=exactSourceMapGeometry,
                SourceEmitterCount=total,SourcePositionedCount=local,
                SourceEnvironmentCount=global
            };
            var loaded=new Dictionary<string,AudioStreamWav?>(StringComparer.Ordinal);
            var observedLocal=0;
            var observedGlobal=0;
            foreach(var row in rows.EnumerateArray())
            {
                var scope=Text(row,"scope");
                if(scope is not ("local" or "environment"))
                    throw new InvalidDataException("Unknown sound source scope");
                var isLocal=scope=="local";
                if(isLocal)observedLocal++;else observedGlobal++;
                var cue=Text(row,"cue");
                if(cue.Length is <1 or >80 || cue.Any(c=>
                    !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-')))
                    throw new InvalidDataException("Unsafe source WAV cue name");
                if(Text(row,"source_name").Length>128 ||
                    Text(row,"source_path").Length>512 ||
                    row.GetProperty("source_id_available").GetBoolean() ||
                    !row.GetProperty("looped").GetBoolean())
                    throw new InvalidDataException("Source sound identity invalid");
                var position=row.GetProperty("position_studs");
                if(isLocal != (position.ValueKind==JsonValueKind.Array))
                    throw new InvalidDataException("Invalid positional sound coordinates");
                var xyz=Vector3.Zero;
                if(isLocal)
                {
                    if(position.GetArrayLength()!=3)
                        throw new InvalidDataException("Source sound CFrame truncated");
                    xyz=new Vector3(Bounded(position[0],-100000,100000),
                        Bounded(position[1],-100000,100000),
                        -Bounded(position[2],-100000,100000))*MetresPerStud;
                }
                var min=Scalar(row,"rolloff_min_studs",0,20000);
                var max=Scalar(row,"rolloff_max_studs",0,20000);
                if(max<min || row.GetProperty("rolloff_mode").GetInt32() is <0 or >3)
                    throw new InvalidDataException("Source sound rolloff bounds invalid");
                if(Scalar(row,"original_volume",0,10)!=0)
                    throw new InvalidDataException("Unexpected source Sound.Volume metadata");
                var target=Scalar(row,"script_target_volume",0,10);
                var speed=Scalar(row,"playback_speed",.01f,8f);
                // The source stored Volume=0 but has a SetVolume script target.
                // Approximate attenuation/relative loudness; do not assert retail mix parity.
                var gain=Math.Clamp(target,.001f,2f);
                var db=Math.Clamp(20f*MathF.Log10(gain)-10f,-70f,-3f);
                if(!loaded.TryGetValue(cue,out var stream))
                {
                    var candidates=new[]
                    {
                        Path.Combine(exe,"Content","Audio","MapSounds",cue+".wav"),
                        Path.Combine(Directory.GetCurrentDirectory(),"Content","Audio","MapSounds",cue+".wav"),
                        Path.Combine(Directory.GetCurrentDirectory(),"src","Twr.Godot","Content","Audio","MapSounds",cue+".wav")
                    };
                    stream=candidates.Select(OfflineAudioRuntime.TryLoadPrivatePcm16Wav)
                        .FirstOrDefault(audio=>audio is not null);
                    loaded[cue]=stream;
                }
                if(stream is null)continue; // Missing source audio remains absent, no forged samples.
                if(isLocal)
                {
                    var player=new AudioStreamPlayer3D
                    {
                        Name="SourceLocalSound"+node._cues.Count,
                        Position=xyz,Stream=stream,VolumeDb=db,PitchScale=speed,
                        MaxDistance=Math.Max(1f,max*MetresPerStud),
                        UnitSize=Math.Max(.2f,min*MetresPerStud)
                    };
                    node.AddChild(player);
                    node._cues.Add(new Cue(true,xyz,
                        Math.Max(1f,max*MetresPerStud),null,player));
                }
                else
                {
                    var player=new AudioStreamPlayer
                    {
                        Name="SourceEnvironmentSound"+node._cues.Count,
                        Stream=stream,VolumeDb=db,PitchScale=speed
                    };
                    node.AddChild(player);
                    node._cues.Add(new Cue(false,Vector3.Zero,0f,player,null));
                }
            }
            if(observedLocal!=local || observedGlobal!=global)
                throw new InvalidDataException("Source emitter scope tally mismatch");
            owner.AddChild(node);
            node.SetEnabled(false);
            GD.Print($"TWR_PASS38_SOURCE_SOUNDS_READY map={mapName} source={total} "+
                $"local={local} environment={global} installed_wav_emitters={node._cues.Count} "+
                $"source_map_aligned={exactSourceMapGeometry} default=OFF audio_original=false");
            return node;
        }
        catch(Exception e)
        {
            GD.PushWarning("TWR_PASS38_SOURCE_SOUNDS_REJECTED map="+mapName+": "+e.Message);
            if(node is not null && !node.IsInsideTree())node.Free();
            return null;
        }
    }

    public void Track(FirstPersonPlayer listener)
    {
        _listener=listener;
        _refresh=0;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled=enabled;
        _refresh=0;
        Refresh();
    }

    public override void _Process(double delta)
    {
        if(!Enabled)return;
        _refresh-=delta;
        if(_refresh>0)return;
        _refresh=.5;
        Refresh();
    }

    private void Refresh()
    {
        if(!Enabled)
        {
            foreach(var source in _cues)
            {
                if(source.Local)source.Positioned?.Stop();
                else source.Ambient?.Stop();
            }
            return;
        }
        var permitted = new HashSet<Cue>();
        foreach(var ambient in _cues.Where(c=>!c.Local).Take(MaxEnvironmentVoices))
            permitted.Add(ambient);
        if(HasExactMapGeometry && _listener is not null)
        {
            var position=_listener.GlobalPosition;
            foreach(var candidate in _cues.Where(c=>c.Local &&
                c.Position.DistanceSquaredTo(position)<=c.Range*c.Range)
                .OrderBy(c=>c.Position.DistanceSquaredTo(position))
                .Take(MaxLocalVoices))
                permitted.Add(candidate);
        }
        foreach(var candidate in _cues)
        {
            var active=permitted.Contains(candidate);
            if(candidate.Local)
            {
                if(candidate.Positioned is null)continue;
                if(active && !candidate.Positioned.Playing)candidate.Positioned.Play();
                else if(!active && candidate.Positioned.Playing)candidate.Positioned.Stop();
            }
            else if(candidate.Ambient is not null)
            {
                if(active && !candidate.Ambient.Playing)candidate.Ambient.Play();
                else if(!active && candidate.Ambient.Playing)candidate.Ambient.Stop();
            }
        }
    }

    private static string Text(JsonElement row,string key) =>
        row.TryGetProperty(key,out var value) &&
        value.ValueKind==JsonValueKind.String ? value.GetString() ?? "" : "";

    private static float Bounded(JsonElement value,float lower,float upper)
    {
        if(value.ValueKind!=JsonValueKind.Number || !value.TryGetSingle(out var number) ||
            !float.IsFinite(number) || number<lower || number>upper)
            throw new InvalidDataException("Source sound field out of bounds");
        return number;
    }

    private static float Scalar(JsonElement row,string key,float lower,float upper) =>
        Bounded(row.GetProperty(key),lower,upper);
}

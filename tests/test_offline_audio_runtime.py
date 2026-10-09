"""The offline game should not be silent when external SoundIds are unavailable."""
from conftest import ROOT

S=ROOT/"src/Twr.Godot/Scripts"

def test_offline_synth_fallback_and_owner_wav_override_exist():
    audio=(S/"OfflineAudioRuntime.cs").read_text()
    assert 'class OfflineAudioRuntime : Node' in audio
    assert 'Content","Audio",kind+".wav"' in audio
    assert 'AudioStreamWav.FormatEnum.Format16Bits' in audio
    assert 'private static AudioStreamWav Synthesize(string kind)' in audio
    assert 'private static AudioStreamWav? ReadPcmWav(string path)' in audio
    assert 'if (!Events.Contains(kind))' in audio
    assert 'MaxVoices = 20' in audio
    assert 'HttpClient' not in audio

def test_fire_reload_damage_waves_and_infected_deaths_play_sounds():
    player=(S/"FirstPersonPlayer.cs").read_text()
    game=(S/"GameplayRoot.cs").read_text()
    assert 'public OfflineAudioRuntime? Audio' in player
    assert 'Audio?.Play("reload")' in player
    assert 'Audio?.Play("melee")' in player
    assert 'spec.IsShotgun ? "shotgun" : "gunshot"' in player
    assert 'Audio = _audio' in game
    for event in ('"wave"','"wave-clear"','"infected"','"hurt"','"ambient"'):
        assert f'_audio.Play({event})' in game

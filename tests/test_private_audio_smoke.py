"""Exported executable audio test proves local WAV overrides are read."""
import subprocess
import sys
import wave
from conftest import ROOT

def test_wav_fixture_valid_pcm16(tmp_path):
    subprocess.run([sys.executable,
        str(ROOT/"tools/assets/make_private_audio_smoke_fixture.py"),
        "--output-dir",str(tmp_path)],check=True,capture_output=True)
    with wave.open(str(tmp_path/"gunshot.wav"),"rb") as src:
        assert src.getnchannels()==1
        assert src.getsampwidth()==2
        assert src.getframerate()==22050
        assert src.getnframes()==2205

def test_exported_windows_binary_runs_real_audio_override_smoke():
    runner=(ROOT/"build/Windows/build.ps1").read_text()
    bootstrap=(ROOT/"src/Twr.Godot/Scripts/Bootstrap.cs").read_text()
    for token in ("--smoke-private-audio",
                  "TWR_SMOKE_PRIVATE_AUDIO_OK samples=2205"):
        assert token in runner
        assert token in bootstrap
    assert "Remove-Item $privateAudioFixture -Force" in runner

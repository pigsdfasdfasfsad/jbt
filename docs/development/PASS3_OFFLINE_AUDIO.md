# PASS 3 — Complete audible offline feedback foundation

The old preview had essentially no sound. A new Godot AudioStreamWav runtime
provides generated, original substitute cues for gunshots, shotguns,
launchers, melee, reloads, infected deaths, damage, wave start/clear and
ambient background noise. These are NOT original Roblox sound recordings.

For authorized original sounds, privately install standard 16-bit
PCM mono or stereo WAV files beside the exported game:

    ThoseWhoRemainOffline.exe
    Content/Audio/gunshot.wav
    Content/Audio/shotgun.wav
    Content/Audio/launcher.wav
    Content/Audio/melee.wav
    Content/Audio/reload.wav
    Content/Audio/wave.wav
    Content/Audio/wave-clear.wav
    Content/Audio/infected.wav
    Content/Audio/pickup.wav
    Content/Audio/hurt.wav
    Content/Audio/ambient.wav

If a WAV is missing/invalid the game uses the generated cue automatically.
Converting files to PCM16 is an offline operation. No network calls or Roblox
audio-ID resolutions run inside the game. The 190 private SoundIds are
separately indexed in the owner's audio-inventory CSV and do not themselves
provide legal access to original sound bytes.

The generated sounds are intended to replace silence until genuine
original-style sound engineering and audio rights checks are complete.

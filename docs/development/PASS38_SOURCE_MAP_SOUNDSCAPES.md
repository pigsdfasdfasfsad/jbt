# Pass 38 — Source-Recovered Offline Map Soundscapes

**Source:** owner's `TestPlace/TestPlace.rbxlx` in private `TestPlace.zip`.
Original SHA-256:
`272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

## Evidence versus approximation

The original `ReplicatedStorage/CMaps/<map>/Local Sounds` and
`Environment Sounds` record **206 source emitters** for the ten release maps:
197 positioned `Part` emitters and 9 non-positional environments.

| Map | Local positions | Environment cues |
| --- | ---: | ---: |
| Ranch | 38 | 1 |
| Mill | 33 | 1 |
| Bypass | 79 | 1 |
| Cabin | 0 | 1 |
| Cargo | 30 | 0 |
| District | 7 | 1 |
| Expressway | 3 | 1 |
| Prison | 6 | 1 |
| Laboratory | 0 | 0 |
| Manor | 1 | 2 |

**Critical source limitation:** All 206 original `SoundId` fields are blank
and all 206 serialized `Volume` fields are zero. The authored child
`NumberValue SetVolume` settings provide target volumes; the audio
recordings themselves are **not present**. Source `Playing` values cannot
prove the original scripted runtime activation sequence. No source URL,
user-account bypass, or network audio fetch is used.

`tools/audio/recover_source_soundscape38.py` deterministically extracts the
positions, cue identities, intended volumes, loops, initial playing flags,
falloff radii and playback rates to SHA-256-pinned JSON files.
The release-map original data remains **private** and is not stored in
public Git history.

`tools/audio/generate_substitute_map_sounds38.py` optionally creates 32
*explicitly synthetic* ambient WAV loops. These clips are NOT original
TWR sound effects, voice acting, soundtrack, or authentic recordings.
They can be replaced with appropriately licensed, legitimately sourced WAVs.

## Install

Unzip the owner's private Pass 38 pack **next to** the Windows executable.
Keep the original directory structure:

- `Content/SourceSoundscapes/<Map>.soundscape38.json`
- `Content/Audio/MapSounds/<cue>.wav`

Only install owner-held packs; do not publish original records as a public
Actions artifact. If any source JSON is absent or fails its pinned SHA,
normal game audio remains available.

During a running match press **F2** to toggle the soundscape preview.
**F10** displays how many source emitters and WAV-equipped voices loaded;
**F11** exports those counts to the diagnostics JSON.

Source emission is **OFF by default**. Global environment previews are
available over fallback maps. Positional source sounds are enabled only
when the map has a `Recovered<Map>` original scene, because the fallback
blockout locations have not been surveyed against the original Roblox
coordinate frame. Positioned playback uses Godot metres = studs * 0.28,
a bounded set of up to 12 nearest local voices and two environment voices,
and approximate inverse distance rolloff. The real original audio mix and
script-triggered transitions are not claimed.

## Validation

The synthetic tests generate ten invented CMaps records with deterministic
SHA checks, a sample fixture for the actual exported Godot Windows game,
and **generated test WAVs**. The Windows --smoke-pass38 exercise asserts that
F2 plays the environment sample, leaves the unaligned 3D emitter silent,
and stops everything on second toggle. CI cleans its fixture files and checks
that no original JSON or test WAVs enter the public executable artifact.

Remaining gaps: original sound binaries and scripting/mix parity, most original
custom map meshes, supported entrances and spawn pathing, and full 15-wave
player-controlled verification.

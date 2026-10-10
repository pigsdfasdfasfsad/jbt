# TWR Offline

Durable development repository for the standalone offline reconstruction of **Those Who Remain**.

## Locked target

- Windows standalone game using Godot 4 + C#/.NET.
- No Roblox, Roblox Studio, Roblox account, Roblox server, or Internet connection at runtime.
- Local-authoritative single-player simulation with command/event boundaries suitable for later LAN/co-op work.
- Older-style **Regular-only, 15-wave** release profile. Wave 15 completes the map; there is no wave 16.
- Hardcore, Classic, Endless, enemy Juggernauts, Juggernaut waves, and ordinary-hit movement stun are disabled.
- Completion reward is one-time and is persisted before results/lobby transition.
- Release maps: Ranch, Mill, Bypass, Cabin, Cargo, District, Expressway, Prison, Laboratory, Manor.

## Recovery status

This branch contains the latest recoverable development state assembled from surviving engineering artifacts and immutable source evidence. The previous full working tree did not survive byte-for-byte. Files restored from surviving artifacts, files deterministically regenerated from recovered normalized evidence, and newly reconstructed implementation files are distinguished in `recovery/PROVENANCE.json` and `docs/RECOVERY_STATUS.md`.

The historical engineering report records **82/82 tests**, **78 C# files**, **21 acceptance gates**, ten converted source maps, and **14,363 normalized RemoteSpy calls**. Those are historical checkpoint facts, not automatically current claims. Run `python tools/validation/run_all.py` for current repository truth.

## Evidence policy

Original uploaded evidence remains immutable and outside ordinary public Git history. This repository stores hashes, manifests, normalized data, generated reconstruction outputs, tests, source code, and documentation. Raw `.rbxlx`/`.rbxl`, raw evidence ZIPs, raw recordings, and release binaries are intentionally excluded.

## Validation

```text
python -m pip install -e .[test]
python tools/validation/run_all.py
```

## Windows output

The pinned workflow targets:

`build/output/ThoseWhoRemainOffline.exe`

A successful executable build is only claimed after the Windows workflow actually produces that file.

## Windows executable distribution

The Windows CI workflow validates, exports, and smoke-tests the game in its
temporary runner workspace. A successful Pass 38 branch build uploads a
**public-safe, no-private-assets** Windows ZIP as a temporary GitHub Actions
artifact; this is not a complete original-content game release. The owner-only
source soundscapes, terrain, models, and sound recordings remain out of Git and
are distributed in a separate owner-held content pack. Executables are not
published as GitHub Releases.

Pass 38 adds 206 source-indexed map sound emitters across the ten release maps
(197 positioned, 9 environmental). Their original SoundId fields are blank in
the available TestPlace source, so the optional F2 audio preview requires
locally installed substitute/authorized PCM16 WAVs. F2 is OFF by default,
and source 3D positions require the real recovered map rather than approximate
blockouts. See docs/development/PASS38_SOURCE_MAP_SOUNDSCAPES.md.

## Laboratory source-pack runtime verification

Laboratory can load an external, owner-held `Content/Maps/Laboratory.scene.jsonl.gz`
beside the Windows executable. Original Roblox geometry bytes remain out of Git.

The Windows workflow now generates a **synthetic**, explicitly marked Laboratory
scene with 30,000 dummy render instances, 1,000 server-wall colliders plus 16,600 source-collidable render objects, 801 dummy lights,
15 infected spawn markers, and 8 player spawn markers. It loads that pack through
the **exported Windows executable** and requires both `TWR_LAB_SOURCE_LOADED`
and `TWR_SMOKE_LAB_SOURCE_OK` in the process log. The fixture is deleted at
the end of the smoke step and is never distributed. This validates loader
execution and pack discovery, **not** fidelity to the original Laboratory.

## Laboratory light streaming

The recovered Laboratory pack includes source light positions, colors, angles,
and ranges. Instead of fixing the 128 nearest lights at the initial spawn,
the scene now reselects up to 128 nearest emitters when the player moves.
Other source lights remain in the pack and are eligible for activation.
Roblox SurfaceLight is approximated with a directed Godot spotlight until
an exact surface-light shader exists. Per-map skybox textures remain unavailable.

## Prepared offline Laboratory textures

The original Laboratory scene references 59 different per-MeshPart texture
asset IDs. Mesh textures can be included **before** the Godot export at
`src/Twr.Godot/Content/Assets/Textures/<numeric asset id>.png` and are
loaded locally through `res://Content/Assets/Textures/`. If a texture is not
present, the source-color proxy remains. The same approach supports prepared
unit-normalized Godot mesh resources in `Content/Assets/Meshes/` before export.

**No original mesh or texture bytes are currently committed to public Git.**
Source IDs are not texture images, and this loader does not fetch resources
over the network.

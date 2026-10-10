# TWR Offline

Durable development repository for the standalone offline reconstruction of **Those Who Remain**.

## Latest verified branch: Pass 40 (October 10, 2026)

[Pass 40 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass40-grounded-lab-navigation) ·
[Windows compile and native tests](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38085199252) (423 Python tests passed, exported Windows game verified with synthetic original-scene fixtures)

- Adds an owner-private SHA-bound **14,726-node / 43,920-edge** grounded navigation graph from **18,130** original-positioned Laboratory collision bounds. This is an **approximation**, not the missing Roblox navigation mesh
- Source floor geometry remains physically incomplete: 133 graph components, all 8 original player markers anchored, only 3 of 15 original infected markers near any waypoint and **no exact infected source spawn connected to the player's initial region**
- **F9** is OFF by default and optionally uses physics-checked connected entry positions at 24m, then 14m or 10m if no distant candidate exists. This is an explicit accessibility approximation, not original retail spawn behavior
- **F4** opens original Lab floor plans; **N** switches to three owner-only, checksum-verified floor navigation connectivity diagrams; **Left/Right** changes floor height band
- F10/F11 records graph integrity and adaptive F9 placements. Source navigators and diagnostic PNGs remain owner-private and are removed from public GitHub Actions artifacts
- Windows smoke in the exported Godot executable tests real floor-capsule collision checks, opt-in F9 wave spawn, and F4/N switching using **entirely synthetic** source data. A human-controlled 15-wave original map playtest is still outstanding
- Owner-side downloadable ZIP combines the verified Windows executable with Pass 39 Laboratory scene and collision data, nine smaller source map fragments, Pass 37 lighting, Pass 38 original-positioned sound references and generated replacement audio, and the Pass 40 graph

Source/provenance details: [Pass 40 reconstruction notes](docs/development/PASS40_GROUNDED_LABORATORY_NAVIGATION.md).

## Previous verified branch: Pass 39 (October 10, 2026)

[Source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass39-original-laboratory) ·
[Windows verification](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38082626002)

- Full loaded owner-source **Laboratory** scene from TestPlace: **34,268** authored geometry records, 836 lights, eight player and fifteen infected spawn markers, 174 pickup/fortification anchors
- Owner-private SHA-bound **Laboratory.col26.gz** cache: **18,130** original-size collision shapes in **180** spatial tiles, of which 4,501 use openly labeled approximate bounds
- Three offline 2D floor plans rendered from Laboratory original source coordinates, accessed in game with **F4** and **Left/Right**; maps are read-only, SHA-bound to private original scene pack
- The other nine release maps retain playable **approximate blockouts** while owner-private, source-verified CMaps wall/prop fragments and F4 bird's-eye source footprints are available separately
- Original MeshPart/UnionOperation triangle binaries, textures, navigation parity, animations and original audio still missing. This is **not** a complete 1:1 game
- The GitHub Actions job runs Python contracts, compiles Godot/.NET, exports Windows, exercises an actual exported native `--smoke-pass39` scene with synthetic test geometry/PNGs, and removes all synthetic fixtures before uploading the public-safe executable. Real owner-private full-scene playthroughs remain unverified

The **private owner Windows ZIP** adds ten-map source outlines, original Laboratory geometry and 180-tile collision cache to the executable, plus Pass 37 lighting and Pass 38 synthetic substitute soundscapes. This private content is never committed to GitHub or added to a public CI artifact. See [Pass 39 development notes](docs/development/PASS39_ORIGINAL_LABORATORY.md).

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

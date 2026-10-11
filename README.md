# TWR Offline

Durable development repository for the standalone offline reconstruction of **Those Who Remain**.

## Pass 45 source development — Laboratory fallback proxy spatial culling

[Pass 45 source code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass45-lab-proxy-visibility) ·
[Original source geometry audit and technical notes](docs/development/PASS45_SOURCE_PROXY_STREAMING.md)

- This pass adds distance culling to **15,678 original-positioned visible MeshPart/UnionOperation/SpecialMesh fallback proxy records** that Pass44's ordinary native-Part pack does not contain.
- Uses a 256-source-stud spatial tile grid and shares material/mesh resources across tiles, rather than keeping geographically distant proxies in world-wide appearance batches. The original loaded Laboratory source produces **669 proxy batches**, with estimated **455 initially visible** (145m radius) at source spawn #1.
- F1 switches *only the proxy culling* ON/OFF for same-match performance comparisons, default **ON**. F10/F11 reports active source proxies and real p50/p95/p99 frame times; no physics, enemy spawns or source navigation are changed.
- Original native 18,213 ordinary source Parts, collision, maps, fallback meshes and Godot offline runtime remain present. If the original native28 pack is absent the existing global source batching remains supported.
- No original custom MeshPart/CSG triangle data is reconstructed; improved FPS is **not claimed** until player-recorded F1 ON/OFF performance reports confirm it. CI uses synthetic source geometry only, with no private original assets in GitHub artifacts.
- Outstanding completion gates remain the other nine missing full original map scenes, authentic custom meshes/terrain/recordings/animations, full enemy traversal and an actual human-controlled 15-wave match.

## Previous verified source branch: Pass 44 (October 10, 2026)

[Development code](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass44-native-lab-streaming) · [Successful Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38097138396) · [Technical notes](docs/development/PASS44_NATIVE_LAB_RENDER_STREAMING.md)

- **Missing content fixed:** Earlier owner Windows ZIPs lacked the SHA-bound `Content/Geometry/Laboratory.native28.gz` native source renderer pack. The Pass 44 owner package includes it.
- Recovered **18,213 source-positioned, size/rotation/material-preserving native Part and Wedge instances**, grouped into **787 spatial draw batches across 47 tiles** with bounded distance culling.
- Exact source spawn #1 geometry audit: 510/787 batches within 145m; 277 off-range batches and 6,228 instances are eligible for hiding. This is **static geometry culling**, not measured GPU FPS.
- The source loader now preserves all 337 `SpecialMesh` legacy visual fallbacks (instead of inadvertently hiding type=6) and retains the original collision cache.
- F10/F11 reports source-stream instance/batch/visibility counts. **443 Python tests and the exported Windows native smoke passed**, including near/far culling, source collision and special-mesh preservation.
- Owner source files and derived private Part pack **remain outside GitHub Actions artifacts**. The game is playable offline but still lacks nine full original map scenes, original custom mesh triangle data, terrain and retail media parity. A real 15-wave private-source FPS playthrough remains unverified.

## Previous verified source branch: Pass 43 (October 10, 2026)

[Pass 43 development branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass43-physical-jump-verification) ·
[Successful Windows native acceptance](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38094133273) ·
[Pass 43 engineering and playtest notes](docs/development/PASS43_PHYSICAL_TRAVERSAL_ACCEPTANCE.md)

- Fix the former **false-positive infected jump landing** check. Source-linked jumps now only count as successful when Godot's physical infected body makes grounded contact **near the actual destination waypoint**, not by hitting any floor
- Count separate launches, confirmed landings, failed jumps and active jumps; **preserve totals across enemy deaths and wave cleanup** for meaningful match QA
- Add bounded **2,048-frame rolling p50/p95/p99 frametime** collection, peak infected count and F10 display; F11 now writes timestamped JSON archives as well as the backwards-compatible latest snapshot
- Introduce a native Windows **two-platform physics-gap acceptance test**: synthetic solid floors are separated by a real 4.2-stud pit; a real Godot infected is required to **physically cross and land** without teleporting
- Add an actual **blocking wall** on the same test jump. Ground contact or timeout on the takeoff side must register **failure**, not a landing
- Native CI also exercises real Godot actor cleanup and **15 accelerated waves**. **439 Python regression tests** and exported Windows traversal tests passed. This is not a full original-map human-controlled match
- Preserve the owner-private Pass 39 Laboratory geometry, Pass 40/41/42 navigation packs, original-positioned wall/prop fragments, and ten-map lighting and sound metadata in the owner-only Windows ZIP. Public Actions artifacts remain private-source-free
- Still incomplete: all nine remaining full original map scenes, missing custom MeshPart/CSG triangle bytes, original terrain, source animations/audio and human-performed 15-wave traversal and frame-time benchmarking

## Previous source-development branch: Pass 42 (October 10, 2026)

[Pass 42 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass42-jump-navigation) ·
[Pass 42 source and limitations](docs/development/PASS42_LABORATORY_JUMP_NAVIGATION.md) ·
[Windows CI and native test](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38093325817)

- Recover original **Pathfind** settings: `AgentCanJump=true`, `AgentCanClimb=false`, `WaypointSpacing=4` studs. The previous all-walking graph could not represent these actions
- Create a **SHA-bound TWRNAV42** graph with **9 source-supported jump actions**. Preserve all **14,726** prior waypoints, all **43,937** walk connections, and add only 9 typed jumps
- Decrease Laboratory's graph disconnects from **117 to 108**; the player-start area's potentially route-connected waypoints expand from **291 to 1,681**. These edges are approximate and still governed by actual Godot collisions
- Infected in Laboratory can follow bounded ballistic jump actions. Existing source-based **F9 entry assistance remains OFF by default**, and earlier nav41/nav31 packs remain available as fallbacks
- The owner-private three-floor **F4 -> N** map displays cyan jump links, gold native bridge repairs, connected green regions, and disconnected purple regions. F10/F11 reports loaded jump actions and observed actor jumps
- Windows Actions compiles and exports the **actual Windows executable**, runs all Python checks, and tests a **fabricated** source-bound jump route plus accelerated real Godot actor/round transitions through **15 waves**; it excludes every original and synthetic private navigation file from the public artifact
- **Not complete:** an accelerated 15-wave runtime smoke is not a full player-controlled match. Original terrain voxels, custom mesh triangles, nine fully reconstructed maps, retail animations/recordings and full graphical/physics traversal QA are missing

## Previous source-development branch: Pass 41 (October 10, 2026)

[Pass 41 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass41-source-native-bridges) ·
[Lab bridge repair details](docs/development/PASS41_SOURCE_NATIVE_BRIDGE_REPAIR.md)

- **Laboratory** now has a SHA-pinned TWRNAV41 v3 graph preserving the original reconstructed 14,726 source-sampled nodes and 43,920 previous graph edges; only **17 new short native-Part-floor-validated bridges** were added, with nine collision/floor checks per bridge, for **43,937 total edges**
- Existing **133 disconnected** map graph regions become **117**; the 291-node original player-start region still has no verified path to the Lab interior. This is **not** original Roblox navigation mesh parity
- **F4 / N** displays three new owner-private source connectivity diagrams; 17 new bridges appear in gold. F4 switches the map view, Left/Right selects floors. F10/F11 records the number of loaded bridges and remaining disconnected regions
- Existing optional **F9** accessible infected-spawn fallback remains OFF by default, retains collision checks and minimum 24/14/10m candidate distances. It does not claim to reproduce the original horde spawn behavior
- New public-surface validation: deterministic conversion tests, unsupported floor/wall/proxy rejection, SHA checks, and actual exported Windows synthetic-fixture navigation/F4/F9 smoke. Original owner source packs remain **private** and out of GitHub Actions artifacts
- **Still incomplete:** Nine fully reconstructed map scenes, Laboratory terrain/custom mesh binaries and player-to-interior routes, full source-faithful enemy navigation and animations, original audio, visual pixel-match and a player-controlled 15-wave Windows performance pass

## Previous verified branch: Pass 40 (October 10, 2026)

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

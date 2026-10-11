# CURRENT VERIFIED DEVELOPMENT CHECKPOINT — PASS 52

**Date:** October 10, 2026 (US Central). **Branch:** `twr-pass52-source-loadout-and-shop`.

**Successful Windows Actions run:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38105034356

**485 Python tests passed**, exported Windows C# Godot build **0 errors / 1 old Pass36 compiler warning**, baseline **10-map × 15-wave domain smoke** passed, Pass52 native `TWR_SMOKE_PASS52_LOBBY_SHOP_OK` passed and public/private asset audit passed. Original loaded lobby cameras now interpolate over three seconds with the recovered 50° FOV, all **91** bundled normalized per-weapon `LoadoutOffset` CFrames parsed (including source RPG Y half-turn), user-facing loadout preview tilt is limited to ±25°, and the original Shop camera and seven credit-price case tiers are available in read-only mode. Case purchases, skins, owner mesh binaries, real 15-wave playthroughs and a privately distributed EXE remain unresolved. See [Pass52 evidence and limitations](PASS52_SOURCE_LOBBY_MOTION_AND_CASES.md).

---

## Latest verified update: Pass 51 (October 10, 2026, US Central)

Development branch: `twr-pass51-original-loadout-preview`. Successful native Windows CI: https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38104204978. **477 Python tests passed**, Godot/.NET Windows executable compiled with **0 build errors**, baseline 10-map/150-wave smoke passed, new Pass51 showroom native smoke returned `TWR_SMOKE_PASS51_LOADOUT_OK`, and owner/private/synthetic file exclusion passed. The exact five source loadout CFrames are used for a non-authoritative weapon showroom and the 91-weapon menu. Source mesh ownership and rendering remain explicitly distinguished from approximate presentation. This is tested on fabricated CI source packs; no original-asset 3D side-by-side comparison, human-driven 15-wave match or private distribution EXE was produced in this pass. [Detailed Pass51 record](PASS51_ORIGINAL_LOADOUT_PREVIEW.md).

# PASS 4 — Verified source terrain and improved infected/weapon visuals (October 2026)

**Source branch:** twr-offline-dev. **Latest pass 4 successful Windows
integration build:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37875134264
(358 Python tests passed, zero C# compiler warnings, native Windows Godot
export, synthetic terrain/water/collision smoke, six source-weapon assemblies,
six source-zombie assemblies, ballistic effects, and all ten map source
loader smoke tests, and the audio classification tests).

## Newly implemented

- Original owner-held Terrain.SmoothGrid v1 bytes for all ten named release
  map snapshots are successfully decoded with a bounded native RLE decoder.
  4-stud source voxel positions, original 23-slot MaterialColors palettes
  and water-vs-solid classification are preserved.
- tools/terrain/convert_smoothgrid.py produces deterministic compressed
  .terrainmesh.gz greedy-surface files. Merging original voxel faces reduces
  the content enough for chunked offline rendering/collision.
- RecoveredTerrainRuntime integrates solid voxel meshes as original-colored
  chunk objects, creates static ConcavePolygonShape3D collision geometry
  for solids, and draws non-collidable water separately.
- The optional source file path is Content/Terrain/<Map>.terrainmesh.gz,
  physically located beside the private exported Windows EXE. Fallback maps
  continue to launch if a source terrain file is absent.
- Source firearm part geometries now align the barrel direction toward the
  Godot first-person camera and derive muzzle-flash coordinates from the
  source model's barrel position instead of fixed category guesswork.
  Magazines and bolts/slides move independently during basic firing/reload
  animations.
- Visible short-lived bullet tracers and solid-vs-infected impact flashes
  are generated offline. They are approximations of the real game.
- Infected track sustained movement failure and try alternate detour angles;
  recovered zombie assembly presentation now includes lightweight hit flinch
  and type-specific movement gait. This is NOT complete cross-floor pathfinding.
- The 190 owner-held original SoundIds have an optional heuristic
  classification utility. No restricted audio was downloaded.

## Private source delivery

TWR-Pass4-Original-Terrain-Private-Pack-v5.zip contains all ten original
place scene packs, converted original terrain surface files, source weapons,
source infected variants, and private audio reference indexes. The
Build-Private-Windows.ps1 wrapper can locally compile the matching current
GitHub source with Godot/.NET and package the Content folder to a Windows
ZIP outside GitHub.

The source chunk converter uses NumPy at offline conversion time only;
the exported game does not depend on Python, Roblox, internet or Godot
editor to read the already converted terrain files.

## Integrity and limitations

- Ten real private terrain files passed binary-face and archive integrity
  checks. The exported Windows Godot smoke verified a generated SYNTHETIC
  terrain file: one chunk, solid collision, separate water material.
  No actual owner-held ten-terrain graphical Windows test has been run.
- Terrain representation uses rectangular exposed voxel faces (greedy mesh),
  not Roblox's original surface-nets smoothing and interpolation.
- MeshPart and UnionOperation render binaries, original sound/animation
  binaries, skyboxes, UI texture artwork and some character variants remain
  incomplete.
- A domain-only 15-wave simulation does not equal ten complete human-
  controlled playable 15-wave matches.
- Original objectives, zombie multi-floor navigation, rendering performance,
  real collision clearances and graphical fidelity still need testing.
- No newly compiled EXE with this pass has been delivered privately in chat.

---

# LATEST VERIFIED THREE-PASS IMPLEMENTATION UPDATE — OCTOBER 2026

**New owner-requested iteration series on twr-offline-dev.** The historical
checkpoint below remains for provenance; this section records what was
actually implemented and tested in the newest three passes.

**Latest fully verified Windows build:** Actions run
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37871159241
(346 Python tests passed, 0 compiler warnings, native Godot Windows export
passed; all ten synthetic original-source map loaders, offline OBJ+PNG and
PCM16 WAV file loading, original-source zombies/weapons and Expressway
navigation smoke tests passed). The new muzzle-flash regression and private
packaging script PowerShell parser check also passed.

## Pass 1 — Map rendering and privately installed art

- OfflineAssetResolver loads valid authorized unit-normalized .obj meshes
  and .png files directly from Content/Assets beside exported Windows EXE,
  with caps and resource caches; original asset IDs are never downloaded
  at runtime.
- Existing Godot res:// mesh resources remain supported at build time.
- Loader is wired into all ten source maps, recovered infected variants and
  original weapon part assemblies; verified by a genuine exported Windows
  smoke using a tiny synthetic external OBJ and a valid external PNG.
- RobloxMaterialSurface adds non-original deterministic detail to source
  material colors for wood, concrete, brick, grass and related surfaces.
  Source mesh binaries are still missing for many geometry records.

## Pass 2 — Zombies, combat and interior clearance

- All 44 available source-derived active infected R6 variants lacked a
  complete ordinary Head part. Missing heads are now reconstructed at source
  R6 scale without inventing cloud mesh IDs, and accessory handles without
  their original mesh are no longer drawn as large solid blocks.
- Melee attack now requires floor-aware vertical proximity and an unobstructed
  physics ray, preventing through-floor and through-wall ghost hits; Bloater
  spore attacks also check for direct world obstruction.
- Player and infected collision radii were decreased toward Roblox R6
  world scale. Source infected spawns were recentered to the new capsule
  height to better support narrow rooms and doorways.
- A complete multi-floor navigation mesh and original animation tracks have
  NOT been verified. This is an improvement, not end-to-end combat fidelity.

## Pass 3 — Audible offline presentation, firing effects, release packaging

- OfflineAudioRuntime plays synthesized, original replacement sound cues
  for gunfire, reload, melee, infected deaths, player damage and wave
  transitions, or uses owner-provided valid PCM16 mono/stereo .wav files
  beside the exported EXE in Content/Audio.
- A brief emissive first-person muzzle flash was added for firearms.
- build/Windows/package_private.ps1 now accepts -PrivateSourceZip and checks
  each of the ten original source-map headers, refuses synthetic fixtures,
  bundles recovered enemy/tool source blueprints and terrain data, optionally
  copies authorized mesh/texture/sound files, and writes a manifest with hashes
  and explicit warnings. Private ZIP destination must remain outside GitHub.
- CI checks the Windows packaging script's PowerShell syntax without
  uploading or installing user-owned original art in public Actions.
- Owner-held chat download TWR-Three-Pass-Private-Source-Pack-v4.zip includes
  the actual ten-map recovery data and a private Windows build wrapper. It is
  NOT an EXE; the latest code must be built with Godot/.NET on Windows.

## What remains objectively incomplete

1. Original mesh/union/terrain visuals, textures, skyboxes, animation clips
   and many original audio binaries must still be recovered or recreated.
2. Original map-scale collision and AI navigation must be interactively tested
   on all ten authentic source packs, not just synthetic smoke fixtures.
3. Real objective placements, pickup accessibility, all weapon behaviors,
   lighting and performance must be visually calibrated in actual gameplay.
4. Full human-controlled 15-wave sessions, benchmarked performance,
   automated crash recovery and signed/private Windows executable release
   are not complete.
5. The old 72 MB preview ZIP is NOT upgraded by this source code commit.
   GitHub intentionally hosts no private executable artifact.

---

# CURRENT SOURCE RECOVERY UPDATE — 2026-10-08

**New complete original-place evidence supersedes the older source-blocker
sections below.** New owner-supplied `twr places(1).zip` includes the loaded
Workspace/Map scene for every one of the ten release maps. Original full
source-positioned scenes have been privately extracted and linked in the
conversation alongside 100 original weapon assembly hierarchies, source R6
infected variants, original Lighting metadata and 190 sound IDs.

**Canonical current update:**
[docs/development/SOURCE_PLACE_RECOVERY_2026-10-08.md](SOURCE_PLACE_RECOVERY_2026-10-08.md)

Old assertions below that the nine maps have *only* incomplete CMaps
fragments were accurate before the new source ZIP was provided but are now
historical. Real original mesh/CSG assets and terrain voxel conversion remain
unresolved; complete Windows graphical playtests are still pending.

---

# TWR Offline — Current Implementation Status
Updated: 2026-10-08. Canonical checkpoint for continuing the existing Godot/C# game.

## Target release and privacy
- Windows 11 fully offline single-player in Godot 4 C#/.NET.
- Ten source release maps, Regular mode, exactly 15 waves, no wave 16.
- GitHub source branch: pigsdfasdfasfsad/jbt, twr-offline-dev.
- No executable archive, original Roblox place bytes, or restricted asset
  binaries are published to GitHub Releases or Actions artifacts.

## Most recent independently verified Windows build
- Latest independently verified code commit: 5b9b16d58ee724529c3fe20772f83f344bf8507c.
- Successful Windows Actions run:
  https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37840538113
- **315 Python tests passed**, all validators passed, and the Godot
  C# Windows export succeeded with **zero compiler warnings**.
- Expressway scene smoke constructed 140 bridge objects, 14 traffic
  assemblies, 61 screening-zone props and 12 street-lamp assemblies.
- Expressway navigation smoke verified **204 reachable AStar3D waypoint
  nodes** and a connected **29-point route** along the bridge. This is
  path-construction evidence, not evidence that every enemy navigates reliably
  in actual human-controlled wave gameplay.
- Exported executable smoke markers:
  TWR_SMOKE_COMPLETE_OK maps=10 waves=150;
  TWR_SMOKE_WEAPON_VISUALS_OK categories=6 throwables=1;
  TWR_SMOKE_INFECTED_VISUALS_OK types=8;
  TWR_SMOKE_FIDELITY_METADATA_OK;
  TWR_SMOKE_LAB_SOURCE_OK infected_spawns=15 item_markers=127 fortification_markers=47.
- Synthetic Laboratory CI fixture: 30,000 geometry items, 17,600 collision
  shapes, 801 source lights, original-count spawn groups. Asset caches correctly
  report 3 synthetic mesh IDs and 2 synthetic texture IDs unresolved.
- **Critical distinction**: 150 simulated wave state transitions are not a
  human playthrough, and a synthetic scene is NOT an original map visual test.

## Implementation passes added after initial Laboratory reconstruction
1. 6744312a / 75ad58fd: source wedge geometry and convex wedge colliders,
   corrected prepared-mesh material overrides, source-part material handling,
   and rebuilt tests. Previous failures were repaired in later green commits.
2. 75ad58fd: infected capsules are centered 0.9m above exact source foot
   markers, retaining original horizontal coordinates.
3. 462fdd16 / a634a46d: separate source pools of 127 ItemPickupBox coordinates
   and 47 FortificationPickupBox coordinates; runtime samples unique marker
   positions instead of placing forts beside approximate item anchors.
4. 5ae5c3dd: reproducible, source-hash-validated private marker extractor and
   synthetic tests. The owner-held private pack v2 was generated locally and
   verified. Its raw scene records are NOT in Git.
5. d941ed06: SpecialMesh.Scale now affects visuals independently from the tiny
   physical carrier Part.Size (important for remote Laboratory mountains).
6. 99ce37ea / de7bc11b: category-specific animated, multi-part first-person
   weapon fallbacks replace the single rectangular gun, with a prepared original
   weapon scene resource hook. All 6 representative types pass Windows smoke.
7. ff263c68: Roblox authored speed and weapon Distance/Range values are
   converted from studs to Godot metres using 0.28m/stud.
8. c5409705 / 46cb7b2a: F12 owner-local screenshot + camera-transform JSON
   metadata capture for reference comparisons. CI validates metadata only.
9. 2469d1bb: source grenade/fire/gas/spore area radii now use consistent
   stud-to-metre conversion; projectile speeds and unproven falloff remain
   explicitly approximate.
10. e7f1b2ec: private asset installation audit; verifies owner-provided .res/.png
    headers and records original source IDs missing without fetching assets.
11. b57ac978: source-mesh and prepared-texture lookup caches reduce repeated
    resource checking, and logs now count loaded/unresolved IDs.

## Owner-held source artifact packages — private conversation downloads
- TWR-Laboratory-Source-Pack-v2.zip:
  Content/Maps/Laboratory.scene.jsonl.gz with 34,268 geometry records,
  1,517 server-wall records, 836 lights, 15 infected spawn locations,
  8 player spawn locations, and NEW original 127/47 pickup spawn markers.
  Standalone original model/texture/UnionOperation binaries NOT included.
- TWR-Infected-Assembly-Evidence.zip: 520 inert instance/attachment/mesh
  references from infected asset hierarchies, no original mesh binaries.
- TWR-All-Maps-Partial-Source-Evidence.zip: partial stored source fragments and
  collision data for ten maps. NOT ten complete source-rendered maps.
- TWR-Map-Visual-Reference-Set.zip: private gameplay reference images.
- Original source archives currently accessible:
  TestPlace.zip, scripts.zip, other scripts and information.zip,
  images.zip, image-docs.zip, videos of game.zip.

## Critical source availability
- TestPlace.zip newer snapshot has a complete loaded Laboratory source scene
  in Workspace/Map plus separate stored CMaps fragments for ten maps.
- The separate previously referenced twr places.zip is NOT currently available
  in this Project's visible files, Library search, or working container.
  A complete original export for the remaining nine maps is NOT recovered.
- A separate SouthernMansion_Forensic_v2.rbxlx was found in Library, but it
  does not identify itself as original TWR Manor. Do not silently promote it
  to authentic source map data.
- 490 distinct Laboratory original MeshId references and 59 distinct
  MeshPart TextureID references are present in the manifest. Asset references
  are NOT binary meshes/textures. Decal/Texture face dependencies add more.
- At least 3,013 Laboratory UnionOperation render binaries remain unrecovered.
  Original CSG mesh data cannot be inferred from its part bounds alone.

## Per-map acceptance status

| Map | Available evidence | Integrated runtime | Source models/materials | Actual visual/playtest |
| --- | --- | --- | --- | --- |
| Laboratory | Loaded source geometry, 174 pickups, spawns, 836 lights | Optional owner-held private scene pack; fallback blockout | Missing many original mesh/CSG/texture files | NOT VERIFIED |
| Ranch | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Mill | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Bypass | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Cabin | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Cargo | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| District | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Expressway | Partial stored source; 6 original screenshots | Reference-guided elevated bridge/checkpoint, vehicles, procedural material/sky and 204-point interim AI routing | Original source mesh/CSG/texture binaries NOT COMPLETE | Windows headless scene/nav smoke PASS; graphical playtest NOT VERIFIED |
| Prison | Partial stored fragment | Approximate blockout | NOT COMPLETE | NOT VERIFIED |
| Manor | Partial stored fragment, 1,602 unresolved unions | Approximate blockout | NOT COMPLETE | NOT VERIFIED |

## Other incomplete first-release systems
- Infected: 8 distinct procedural animated humanoid proxies, but still no
  exact original meshes, animations, multi-floor navigation or ragdolls.
- Weapons: 91 mechanical catalog entries; category-model fallbacks exist, but
  original weapon rigs, animations, sound, muzzle effects, and visual parity
  are NOT complete.
- Objectives/fortifications: functional baseline, but exact original per-map
  objective positions and fort emplacement zones unavailable.
- UI/audio/VFX: functional menu/HUD shell, but original-style visuals, full
  audio and animations, sky/weather/post-process not complete.
- Real gameplay: no full interactive 15-wave human-controlled original
  Laboratory playthrough, nor any interactive testing of the other nine maps.
- No up-to-date compiled Windows executable has been delivered in this chat.
  CI intentionally discards its executable rather than making it downloadable.

## Next implementation passes / acceptance gates
1. Privately install the real owner-held Laboratory Source Pack v2 and execute
   a graphical Windows build: verify loaded-source marker counts, geometry,
   movement/scale, floor and ramp collisions, 127/47 pickup pools and
   navigation under actual gameplay (not synthetic smoke).
2. Use F12 to capture position/FOV-matched Godot screenshots; compare original
   Laboratory image references room by room. Repair geometry, materials, missing
   props, light behavior, reflections, shadow budget and atmospheric effects.
3. Resolve or legally recreate actual mesh/texture/CSG/sound/animation assets.
   Use tools/assets/private_asset_inventory.py and original source manifest to
   track dependencies. Do not claim art fidelity from asset ID strings.
4. Obtain full authorized original exports of the other nine maps if possible;
   otherwise reconstruct missing geometry with explicit visual references,
   tracked deviations and practical gameplay tests. Never treat CMaps fragments
   as the full original decorated maps.
5. Source-guide infected assembly and AI navigation across all map floors,
   original weapon handling and appearance, objectives, pickups, fortifications,
   UI, sounds, weather and progression calibration.
6. Play entire Regular 15-wave sessions on each of ten maps, verify FPS/memory,
   save/restart, objective accessibility, offline startup, and crash recovery.
7. Privately package the final Windows executable with complete authorized
   local data via build/Windows/package_private.ps1. Do not publish it on GitHub.

## Useful developer files
- src/Twr.Godot/Scripts/LaboratorySourceLoader.cs
- src/Twr.Godot/Scripts/LaboratoryLightStreamer.cs
- src/Twr.Godot/Scripts/RobloxPrimitiveGeometry.cs
- src/Twr.Godot/Scripts/InfectedVisualAssembler.cs
- src/Twr.Godot/Scripts/WeaponViewModelRuntime.cs
- src/Twr.Godot/Scripts/FidelityCaptureRuntime.cs
- src/Twr.Godot/Scripts/RobloxUnits.cs
- tools/maps/augment_laboratory_pickups.py
- tools/assets/private_asset_inventory.py
- docs/development/LABORATORY_PICKUP_MARKERS.md
- docs/development/LOCAL_FIDELITY_CAPTURES.md
- docs/development/ROBLOX_WORLD_UNITS.md
- build/Windows/build.ps1 and build/Windows/package_private.ps1

Complete means the playable game and graphics meet acceptance criteria, not
merely that C# compiles or automated wave state progression passes.

## Visual reconstruction pass: Expressway and HUD (2026-10-08)

- Source-guided Expressway raised bridge and checkpoint implemented through
  ExpresswaySceneBuilder (commit 4b3b2a8).
- The exported Windows scene smoke created 140 bridge nodes, 14 traffic nodes,
  61 checkpoint objects and 12 street lamp assemblies.
- The procedural scene is an **approximation** using original TWR Expressway
  screenshots, not an import of the complete original map assets.
- The HUD now includes anchored survival/wave panels, a segmented armor bar,
  a health bar, centered sight and a circular weapon/ammo gauge inspired by the
  owner-provided WeaponHUD.png. The weapon silhouette is temporary vector art.
- Real interactive camera screenshot comparison is still required. Automated
  headless runtime construction cannot certify the appearance or playability.

## Expressway first full visual implementation pass — latest (2026-10-08)

**Original screenshot references used:** images.zip entries
Expressway.png, ExpresswayPreview.png, ExpresswayPreview2.png,
ExpresswayIcon.png, Card-expressway.png and Vote-expressway.png.

**Actual code and CI evidence:**

1. 1c9bff43 / 4b3b2a8b: Created 925-line ExpresswaySceneBuilder
   and replaced the visible Expressway box arena with an elevated freeway
   structure, multi-lane asphalt, bridge beams/pylons, concrete parapets,
   skyline, abandoned traffic, two military Humvees, screening tents,
   barricades, fencing, stop signs, sandbags, traffic cones, crates,
   checkpoint signage, stationary helicopter and street lamps.
2. 65bbd6b4 / 1a16c776 / 4cc5bec9: Original-inspired circular
   ammunition HUD and responsive health/armor/wave panels. The first
   attempt left two obsolete source-string assertions; the next Windows
   run passed after verifying real behavior remained intact.
3. 4d32b719 / 68f529a3 / 92f67ee2 / 5b9b16d5: Dedicated shared AStar3D
   static-lane route network for infected on Expressway, with blockers for
   vehicles, Humvees, sandbags and screening walls. Two C# binding
   mismatches involving AStar3D.GetPointCount were fixed. The final
   compiled Windows scene smoke passed with 204 nodes and a 29-point path.
4. 663b288d: Deterministic locally generated surface-grain textures for
   Expressway asphalt and concrete to avoid the earlier entirely flat gray
   surfaces. No network or proprietary mesh/texture data is used.

**Gaps that prevent calling this map "complete":**

- Every road dimension and vehicle placement remains reconstructed from
  reference pictures, not original instance-level source coordinates.
- Original Expressway vehicle meshes, signs, checkpoint textures, cloud sky,
  CSG, vegetation, original spawn markers, sound and animations are missing.
- Real rendering, camera-matched fidelity comparisons, Godot frame time,
  player collision, AI movement under live waves and checkpoint traversal
  have not been interactively tested.
- The shared AStar grid approximates clearance around static obstacles but
  does not dynamically rebuild when fortifications are deployed.
- Existing original-looking model preview art is not proof of original assets.
- No latest privately downloadable Windows executable has been produced.
  CI exports executables only transiently and does not publish binary artifacts.

**Next concrete implementation passes:**

1. Graphical Windows playtest of Expressway on actual hardware with screenshot
   captures at several camera bearings using F12; repair noticeable geometry,
   lighting, vehicle, barricade placement and HUD layout discrepancies.
2. Traverse the bridge and complete multiple actual waves; catch AI pathing
   stalls, static collision blocking, line-of-sight problems, draw-call/frame
   time spikes, and unreachable pickup/objective anchors.
3. Obtain authorized original Expressway map scene/mesh/texture files if
   available; replace approximate prop meshes and material references.
4. Reuse the Expressway visual/AI engineering methods to reconstruct one
   further map to a demonstrable standard before spreading more placeholders.
5. Integrate source-based infected rig/animation, weapon/audio and effect
   packages as additional asset evidence becomes available.


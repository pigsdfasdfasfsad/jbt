# TWR Offline — Development Continuity Checkpoint (2026-10-08)

## Authority and scope
- Repository: pigsdfasdfasfsad/jbt, branch twr-offline-dev.
- Target: standalone Windows Godot/C# offline single-player, old Regular mode,
  ten release maps, 15 waves, no wave 16.
- Do not put compiled Windows executables, raw RBXLX or private evidence onto GitHub.
- Do not claim original Roblox fidelity based only on successful CI.

## Verified latest build
- GitHub Actions run: https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37757015158
- Conclusion: SUCCESS.
- 273 Python tests passed; all static/domain acceptance validators passed.
- Godot/.NET Release export succeeded with zero compiler warnings.
- Simulated domain completion: 10 maps / 150 cumulative waves.
- **New:** exported Windows executable actually loaded a generated synthetic
  Laboratory source-pack fixture: 30,000 geometry instances, 1,000 colliders,
  801 lights, 15 infected spawns, 8 player spawns.
- Required markers: TWR_LAB_SOURCE_LOADED and TWR_SMOKE_LAB_SOURCE_OK.
- Important: these are *synthetic* data and do not verify original Laboratory
  rendering, real geometry collision, AI navigation, or real manual playability.

## Changes completed during this session
1. 3c255942 — Add synthetic offline Laboratory source-pack integration smoke,
   plus spotlight scene-tree initialization fix.
2. 6cbc0d76 — Preserve original Laboratory infected spawn marker coordinates
   without arbitrary ±2.5m horizontal jitter.
3. fedebfd6 — Add reproducible static source-map coverage scanner, tests, and
   docs/maps/SOURCE_COVERAGE.md for the two Roblox place snapshots.
4. 6ae224d4 — Introduce LaboratoryLightStreamer, reselecting up to 128
   source-positioned lights around the moving player.
5. 89287e36 — Correct an outdated Python smoke-test assertion; CI passed.
6. 516e3923 — Add build/Windows/package_private.ps1, an owner-side script
   that checks the private Laboratory pack header and produces a ZIP outside Git.
   The packaging script has *not yet been executed in a real Windows user setup*.

## Original source evidence now available in chat (not GitHub)
- Owner supplied: TestPlace.zip, scripts.zip, other scripts and information.zip,
  images.zip, image-docs.zip, and gameplay recording archives.
- Prior private file: TWR-Laboratory-Source-Pack.zip.
- New private file: TWR-All-Maps-Partial-Source-Evidence.zip.
  Contains ten compressed stored-map fragment datasets, ten overhead footprint
  PNG diagnostics, map coverage, and local regeneration scripts.
  **NOT complete maps**; these files should not replace functional game maps.

## Source findings
- Both TestPlace snapshots have the same loaded Workspace/Map containing
  34,268 visible/renderable Laboratory instances.
- Only the newer snapshot includes ReplicatedStorage/CMaps for the ten maps.
- Loaded Laboratory map private pack reports 34,268 geometry records,
  1,517 server-wall collision shapes, 836 source light records,
  15 infected spawn markers, and 8 player spawn markers.
- Laboratory lights consist of 812 SpotLight, 21 SurfaceLight, 3 PointLight.
  SurfaceLight is approximated in Godot by a directed spotlight.
- Manor's stored Map folder contains 1,602 UnionOperation records whose true
  original CSG mesh binaries are not included.
- Separate CMaps maps are *stored supplemental map fragments*, not proof of
  ten complete original render scenes.
- The Lighting ModuleScript bodies sampled from the provided place are API
  error placeholders, not recovered per-map lighting algorithms. Some serialized
  sky/post-processing effect properties do survive.
- Asset identifiers are *references*, not the mesh/texture/audio binaries.
  Original assets can only be recovered where they are actually available and
  authorized to use.

## Current known functional and fidelity limitations
- Laboratory source pack was not privately run on Windows in this session.
  Synthetic pack only was used for exported executable loading validation.
- Original 12,587 Laboratory MeshPart instances and 3,013 UnionOperations still
  cannot render true source shapes without corresponding imported mesh data.
- Laboratory collision currently restores server walls and limited collidable
  floor/stair/ramp Parts. Full collision, multi-floor paths and navigation are
  not interactively verified.
- Original Laboratory objective and loot positions remain provisional.
- Lighting environment, skybox, source SurfaceLight emission profile and
  post-processing are not yet exact, even though light transforms were recovered.
- Other nine maps remain blockouts in the playable Godot build.
- Infected still use capsule placeholder meshes, approximate AI, and no
  complete original animations/rigs.
- Weapons still use placeholder visuals and incomplete original audio.
- UI, sound, atmosphere, fortifications, wave balancing and all-map playtesting
  are not at source-fidelity acceptance.
- No complete updated executable has been transferred into this ChatGPT
  conversation: Windows CI intentionally does not publish binary artifacts.
  Do not link an old executable and label it as the newest build.

## Required next implementation sequence
1. Re-run all current validation tests and check current GitHub head.
2. Use the **real** owner-held Laboratory.scene.jsonl.gz in a Windows build;
   confirm TWR_LAB_SOURCE_LOADED with geometry=34268, collisions, lights and
   original spawn markers. Measure actual load time, FPS, and memory.
3. Test a complete human-driven 15-wave Laboratory playthrough; record failures.
4. Recover/convert owner-authorized mesh, texture, skybox, audio, and CSG assets;
   update an explicit asset dependency manifest. Never invent recovered bytes.
5. Fix the Laboratory geometry collision, original floor/stairs, real spawn
   navigation and objective placement; compare screenshots against footage.
6. Implement source-guided infected assembly and animation; then weapons,
   UI, audio and gameplay tuning.
7. Repeat verified map conversion for the remaining nine, using the stored
   fragments only for the parts they actually support.
8. Build and verify private Windows distribution outside Git; only present a
   ChatGPT download if its exact sandbox file was really built here.

## Completion gates
- Build and smoke success is necessary but insufficient.
- Every map requires source-proven geometry/material/lighting inspection,
  traversability, infected navigation, original spawn evidence, interaction
  verification, visual comparisons and real gameplay testing.
- The first release is complete only after a full playable 15-wave session on
  every map, authentic player/infected/weapon presentation, working interface
  and offline sound, stable saves, acceptable performance, and no major blockers.
- Record deviations explicitly; never substitute a fabricated completion %.

## Useful references
- docs/maps/SOURCE_COVERAGE.md — map-by-map recovered evidence.
- docs/development/PRIVATE_WINDOWS_PACKAGING.md — owner-only ZIP packaging.
- src/Twr.Godot/Scripts/LaboratorySourceLoader.cs
- src/Twr.Godot/Scripts/LaboratoryLightStreamer.cs
- build/Windows/build.ps1
- tools/maps/audit_map_sources.py

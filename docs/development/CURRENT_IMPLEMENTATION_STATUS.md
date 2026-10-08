# TWR Offline — Current Implementation Status

Updated: 2026-10-08. This is the canonical handoff for future implementation passes.
Do not interpret a successful CI result as completed game fidelity.

## Scope and release contract

- Repo/branch: pigsdfasdfasfsad/jbt / twr-offline-dev.
- Godot 4 C#/.NET; fully offline Windows single-player.
- Older-style Regular mode only; ten maps and exactly 15 waves.
- Do not publish executable archives or private Roblox asset bytes on GitHub.

## Latest verified CI

- Last implementation/ref-index commit: 337dc4da960d1f70c3ecb41c095671dc182b348e
- Successful Actions run: https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37821020531
- 282 Python tests pass; all static/evidence/domain validators pass.
- Windows Godot/.NET executable export succeeded; 0 compiler warnings.
- Domain-only simulation completed 10 maps x 15 waves = 150 state advances.
- Exported game instantiated all 8 infected visual assembly types.
- Exported game verified Hazmat fire, Bloater fire, and Riot melee modifiers.
- Exported game loaded a **synthetic** Laboratory scene with 30,000
  render records, 16,600 source-collidable render parts, 1,000 server walls,
  801 lights, 15 infected spawn markers, and 8 player spawn markers.
- This is not an actual real-source-pack graphical playtest.

## Current major implementation work

1. 2a10110c — source-collidable Laboratory geometry, including invisible
   collision barriers, shared BoxShape resources, proxy material updates,
   reduced draw batches when actual mesh resources are absent.
2. 9b375f1a / 85c9745a — prepared original infected scenes supported at
   Content/Assets/Infected/<Type>.tscn; the default visible capsule has been
   replaced by animated humanoid proxies for the 8 Regular-mode types;
   exported runtime smoke covers all eight types.
3. 2ab00f46 / b1e5838d — verified Infected Info manipulators now apply:
   Hazmat and Burster smoke immunity, Bolter half smoke intensity, Bloater
   triple fire damage, Riot half melee damage. Molotov correctly hurts Hazmat.
   Old assertions expecting the bug were updated.
4. cc83d299 — prepared, local MeshPart textures can be loaded at
   Content/Assets/Textures/<numeric asset ID>.png before export.
5. 0cdca996 — 16,600 source-collision shape stress test, verified in CI.
6. 337dc4da — public-safe ten-map screenshot indexer and source coverage.
   The images themselves remain in the owner-held original ZIP.
7. Previous work: source-positioned Laboratory scene import, exact recovered
   infected spawn markers, moving-light streaming with 128-active emitter
   budget, all-map fragment evidence, and private owner-side Windows packaging.

## Private extracted evidence available in the original chat (NOT Git)

- TWR-Laboratory-Source-Pack.zip: 34,268 source-positioned renderable records,
  1,517 source server-wall records, 836 original light records,
  15 infected spawns, 8 player spawns, references to original textures/meshes.
- TWR-All-Maps-Partial-Source-Evidence.zip: static fragments and server-wall
  evidence for ten stored maps. They are NOT ten complete map scenes.
- TWR-Infected-Assembly-Evidence.zip: 520 source instances from
  ReplicatedStorage/Assets/AI (Body, AttachmentBody, and Infected branches).
  These are source hierarchies, NOT decoded original model binaries.
- TWR-Map-Visual-Reference-Set.zip: 65 original Laboratory and Manor
  screenshot references, including contact sheets.
- Source archives: TestPlace.zip, scripts.zip, images.zip,
  image-docs.zip, other scripts and information.zip, videos of game.zip.
- The provided 7 videos mostly show an isolated test arena and example lobby,
  rather than full original map traversal. Reference screenshots cover
  areas of the game, but are not automatic camera-aligned render comparisons.

## Per-map fidelity acceptance matrix

Statuses are **implementation evidence**, not completion percentage.

| Map | Available original scene data | In normal playable build | Correct original meshes/textures | Exact spawns/navigation | Reference-verified visuals |
| --- | --- | --- | --- | --- | --- |
| Laboratory | Loaded source scene available in private ZIP (34,268 records) | Optional external source loader; fallback blockout | NO | Spawn positions recovered; real navigation NOT verified | NO |
| Ranch | Partial stored fragments | Blockout only | NO | NO | NO |
| Mill | Partial stored fragments | Blockout only | NO | NO | NO |
| Bypass | Partial stored fragments | Blockout only | NO | NO | NO |
| Cabin | Partial stored fragments | Blockout only | NO | NO | NO |
| Cargo | Partial stored fragments | Blockout only | NO | NO | NO |
| District | Partial stored fragments | Blockout only | NO | NO | NO |
| Expressway | Partial stored fragments | Blockout only | NO | NO | NO |
| Prison | Partial stored fragments | Blockout only | NO | NO | NO |
| Manor | Partial stored fragments (1,602 unresolved unions) | Blockout only | NO | NO | NO |

## Infected, weapons and game presentation

- Infected: eight types have temporary animated humanoid visuals.
  Correct original meshes, character assembly variations, animation assets,
  ragdolls and accurate pathfinding remain INCOMPLETE.
- Weapons: mechanical catalog exists; original first-person/world models,
  real animations and sound library remain INCOMPLETE.
- Game loop: Regular 15-wave domain state passes headless smoke; real wave
  pacing, enemy distributions, 10 map objectives and full match traversal
  are NOT manually verified.
- UI: existing menu, armory and HUD functional shell; full visual parity
  with original UI remains INCOMPLETE.
- Audio/VFX: full original audio and visual effects are not integrated.

## Critical remaining blockers and implementation order

1. **Obtain authorized mesh/texture/CSG bytes**: the supplied XML place has
   Roblox asset references but does not contain original MeshPart/Union
   render binaries. Its embedded SharedStrings table contains small metadata
   values, not complete CSG render geometry.
2. Get remaining complete source map place exports if available. Do not
   fabricate them from partial CMaps folder data. Owner may need to provide
   prior twr places.zip and missing runtime captures.
3. Test actual owner-held Laboratory.scene.jsonl.gz with a real graphical
   Windows installation, observe map, collisions, walk paths, load time,
   frames and RAM. The stress test proves loader execution, not playability.
4. Convert and install allowed original models/materials, CSG, meshes,
   textures; compare Lab from camera-matched original screenshot references.
5. Complete per-map scene imports for the other nine maps and required
   original player/infected/objective spawn locations.
6. Replace temporary infected assembly with source-guided rigs and animations;
   implement navigation suitable for multi-floor authored maps.
7. Finish weapon models/animations/sounds, gameplay calibration, equipment,
   objective logic, UI, lighting, ambience, VFX, and performance tests.
8. Carry out **real human-controlled 15-wave sessions** on all ten maps,
   save/restart testing, and offline Windows execution checks.
9. Produce final Windows ZIP privately outside Git. CI intentionally does
   not upload a GitHub Actions executable artifact. Do not label the old ZIP
   as the newest compiled build.

## CI and handoff instructions

- Inspect latest branch before editing. Preserve existing engine architecture.
- Run python tools/validation/run_all.py where available, plus Windows CI.
- Scripts: build/Windows/build.ps1 and build/Windows/package_private.ps1.
- Continuity sources: docs/RECOVERY_STATUS.md,
  docs/development/CONTINUITY_CHECKPOINT_2026-10-08.md,
  docs/maps/SOURCE_COVERAGE.md,
  docs/development/MAP_VISUAL_REFERENCE_COVERAGE.md.
- Mark every item VERIFIED, TEST PASS, SOURCE-ONLY, APPROXIMATED,
  UNAVAILABLE, or BLOCKED; never invent completion scores.
- Commit after each meaningful implementation and repair pass.

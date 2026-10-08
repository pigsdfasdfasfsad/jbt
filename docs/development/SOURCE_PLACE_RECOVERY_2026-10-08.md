# Complete original-place recovery and remaining Windows parity work

Updated 2026-10-08. This document supersedes the earlier conclusion that
complete source scenes existed only for Laboratory. New owner-held evidence
was supplied after that initial recovery.

## New private original source: twr places(1).zip

The supplied ZIP contains 27 Roblox XML place snapshots. Ten named gameplay
snapshots contain the **loaded full Workspace/Map scenes**, not merely
ReplicatedStorage/CMaps stored fragments. The source packs remain private and
must not be committed to public Git or bundled into a public CI artifact.

The 10 extracted source-map packs use twr-source-map-v2. They preserve
real original Part/MeshPart/Union/Wedge positions, orientation, dimensions,
RGBA color/material references, CanCollide flags, light emitters, source
player/infected spawns, item and fortification markers and original place
Lighting service ambient/fog settings. The original default CanCollide=true
must apply whenever a Part lacks an explicit override.

| Loaded source map | Geometry | Solid render parts | Server-wall shapes | Lights | Infected spawns | Item/Fort markers |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Laboratory | 34,267 | 16,613 | 1,528 | 836 | 15 | 127 / 47 |
| Manor | 21,445 | 12,748 | 785 | 189 | 8 | 63 / 27 |
| Prison | 18,558 | 12,811 | 1,234 | 215 | 28 | 81 / 50 |
| District | 18,701 | 13,084 | 346 | 50 | 19 | 53 / 32 |
| Expressway | 14,268 | 7,014 | 274 | 136 | 3 | 33 / 17 |
| Mill | 8,915 | 4,636 | 1,055 | 133 | 7 | 63 / 24 |
| Bypass | 9,391 | 6,251 | 768 | 332 | 19 | 74 / 31 |
| Ranch | 6,564 | 4,901 | 1,266 | 128 | 9 | 56 / 23 |
| Cargo | 6,931 | 6,055 | 321 | 334 | 12 | 31 / 17 |
| Cabin | 4,915 | 2,441 | 871 | 47 | 16 | 19 / 13 |

All ten snapshots include eight player spawn markers. Original terrain
SmoothGrid and MaterialColors binary data were preserved separately.
**Preserved original voxel data is NOT decoded playable/renderable terrain.**

These counts are source evidence, not verified loaded-geometry frame counts
or graphical acceptance-test results. Nonuniform CSG bounds do not substitute
for source render meshes. Direct visual comparisons remain unperformed.

## Original zombies and weapon models discovered

- Original instanced Workspace/Entities/Infected R6 zombie model transforms,
  colors, Motor6D joint names, mesh and apparel references recovered.
- Six types with one or more live source variants: Civilian, Sprinter,
  Military, Hazmat, Burster and Bolter. Source snapshots yield 44 distinct
  zombie variants. Complete active Riot/Bloater original variants not found;
  data-driven temporary fallback remains in use.
- Original ReplicatedStorage/Models/Tools contains 100 separate original
  weapon, throwable and equipment model assemblies, 945 source render
  parts and 871 unique mesh references. Their transforms/colors now take
  precedence over procedural first-person weapon models if the private
  Content/Weapons/SourceWeaponModels.json.gz is installed.
- Original mesh binaries/clothing pixels/animations are often represented
  by cloud asset IDs, not embedded local bytes. A rendered box proxy does
  not achieve original artistic fidelity.

## Audio and downloads

27 source snapshots reference 190 unique Roblox SoundIds over 10,015
Sound instance occurrences. The private source-delivery pack includes
AudioInventory/audio_asset_ids.csv and a conservative authorized-public-audio
fetcher, plus per-map source paths and asset links.

SoundIds do not contain original audio bytes or redistribution rights.
The original place XML embeds no complete WAV/MP3/OGG binaries. In this
execution environment public Roblox asset delivery was not accessible.
Do not bypass owner-specific audio access controls.

## Source code integration and test results

- The generic map import overload of LaboratorySourceLoader handles
  twr-source-map-v2 scene packs for all ten maps, prefers owner-held scene
  files by map name, validates counts/scale, and falls back to the existing
  map builder if the pack is absent.
- The loader accepts author-authored per-map ambient/outdoor/fog metadata,
  and streams up to 128 recovered source lights near the player.
- InfectedSourceModelRuntime reads owner-held private R6 assembly packs and
  renders the recovered body components from their original transforms and
  colors, subject to missing original mesh resource fallbacks.
- OriginalWeaponSourceRuntime reads owner-held original 100-tool assembly
  packs and connects to WeaponViewModelRuntime before procedural fallback.
- Generated **synthetic, non-Roblox** Windows smoke fixtures verify importing
  all ten maps, representative original infected model assembly and weapon
  assembly. Private Roblox asset data is never uploaded to GitHub.

Latest independently verified reference:
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/37856896072
(322 passing automated tests, zero compiler warnings, all ten synthetic
source-map smoke plus six original R6 assembly smoke).
Later weapon and lighting commits must each pass CI independently before
their runtime success is claimed.

## Private delivery and installation

The conversation contains:
TWR-Original-Ten-Maps-Zombies-Weapons-Private-Pack-v3.zip

Its source-derived data files are installed relative to the CURRENT
compiled Windows executable (not in public GitHub):
- Content/Maps/<Map>.scene.jsonl.gz
- Content/Enemies/InfectedSourceVariants.json.gz
- Content/Weapons/SourceWeaponModels.json.gz
- Content/Terrain/<Map>.SmoothGrid.bin (not yet interpreted)
- AudioInventory/* is an external owner-only reference

Its Build-Private-Windows.ps1 can clone the code, run local Windows build
and stage the private Content folder. It needs real dotnet + Godot Windows
build tools and internet while building, but not once built.

The current sandbox has no dotnet, Godot, Wine or real Windows graphical
runtime. The GitHub Windows runner does not publish binaries/artifacts at
the owner's request. Therefore the latest executable has NOT been
privately delivered in this chat, and actual gameplay/visual fidelity tests
are still outstanding.

## Outstanding development and acceptance

1. Validate a real owner-held ten-map pack **inside a graphical Windows build**,
   test source camera/physics scale, floor collisions, map traversal and
   performance for all 10 maps. Synthetic fixture tests do not cover this.
2. Resolve cloud-hosted original MeshPart, UnionOperation/CSG, decals,
   textures and clothing binaries through authorized means, or independently
   recreate geometry with proven visually matched reference views.
3. Decode/reconstruct Terrain.SmoothGrid voxel geometry and materials for
   each map and generate Godot terrain surface/collision assets.
4. Derive/map original objective placements and repair objective/reward/
   pickup interactions for all 15 waves in real player-controlled matches.
5. Integrate original infected animation/rig variations and accurate global
   multi-floor pathfinding on all real original scenes.
6. Complete weapon rig motion, recoil/ADS fidelity and audio; integrate
   authorized real audio or original-style substitutes where unavailable.
7. Finish source-faithful UI, environment shading, skyboxes, particles,
   hit reactions, achievements, saving and 15-wave performance.
8. Reproduce original side-by-side screenshots via F12 while playing
   on a graphical Windows desktop, fix mismatch and iterate.
9. Produce a complete privately downloadable Windows ZIP in ChatGPT if a
   suitable Windows development environment becomes available; no GitHub
   binary uploads.

Success is a real tested playable game, not test counts or raw instance totals.

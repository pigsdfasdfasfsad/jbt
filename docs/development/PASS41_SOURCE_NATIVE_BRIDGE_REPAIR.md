# Pass 41 — Laboratory Native-Part Bridge Validation and Offline QA

**Build branch:** `twr-pass41-source-native-bridges`  
**Based on:** Verified Pass 40 owner-private Laboratory source scene.

## Changes actually made

Pass 40's source-grounded navigation has 14,726 sampled nodes and 43,920
short edges, spread across **133 disconnected components**. The original
player-spawn room lies in a 291-node island. No actual original infected
spawn is currently on a component sharing that player's source route.

Pass 41 introduces a deterministic owner-private **TWRNAV41 v3** graph
that preserves the full Pass 40 vertex array and all 43,920 original
reconstructed edges, then adds **17 additional links** validated against
native **Roblox Part floor surfaces**, not missing MeshPart/CSG custom
triangles. This reduces the number of graph regions to **117** without
inventing doors, ramps, teleporters or long-distance paths.

| Source-bound repair metric | Value |
| --- | ---: |
| Original waypoints preserved | 14,726 / 14,726 |
| Original reconstructed edges preserved | 43,920 / 43,920 |
| Examined nearby cross-component pairs | 2,987 |
| Passed initial source clearance / floor support | 293 |
| Passed strict native-Part-only floor + body clearance | 291 |
| Distinct safe source component pairs | 17 |
| Exported added edges | 17 |
| New graph edges | 43,937 |
| Components before / after | 133 / **117** |
| Original player-start component size after repair | **291** |

The stricter exporter checks **nine points along each potential bridge**
using the source Native Part floor only, tests body-height obstruction
against 18,130 original-positioned collision bounds and limits each added
link to 8 source studs maximum (2.24m) with at most 2.2-stud vertical
difference. It keeps one shortest safe link per original component pair.

**This does not make Laboratory fully navigable.** The original source
snapshot has missing custom mesh binary triangles, absent original terrain
and missing door/fence/stair logic. The original player spawn remains
isolated from the large Laboratory interior. When F9 is ON, the existing
24/14/10-metre **physics-validated** accessibility fallback from Pass 40
continues to handle some disconnected infected spawns.

## Source and provenance

Original private scene compressed SHA-256:
`35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74`.

Base Pass 40 navigation SHA-256:
`b021b905a0a87619997a0e2da08a4c7b2db18ecd2a5250f93dfd84d08cb26bdc`.

**New owner-private** `Content/Navigation/Laboratory.nav41.gz`:
`12ac9bae602cbdea3ae6a6789289d1068f1c8efd5d603de34d6186698c375dd0`.

The Windows game prefers this SHA-verified graph, then falls back to
Pass 40 `Laboratory.nav31.gz`, then the older Pass 25 graph or existing
local steering if the private newer file is missing or rejected.
TWRNAV41 is accepted **only** with a matching exact graph SHA and source
scene SHA (except the clearly fabricated `--smoke-pass41` integration
test). No Roblox runtime, online fetch or Studio is required.

Export script: `tools/validation/recover_source_bridges41.py` with
`--scene ... --base ... --out ...`. This uses NumPy, Shapely and the
existing Pass 40 source-surface extraction utility. The real owner source
files and converted graph are **not** checked into public Git history.

## Layer-by-layer F4 navigation diagnostics

Three owner-only PNG files reflect the **new** 117-component connectivity
and identify the 17 new short links in **gold**, accessible player-start
nodes in **green**, and disconnected components in **purple**:

- `Content/MapPlans/Laboratory.navgraph41-0.png`
- `Content/MapPlans/Laboratory.navgraph41-1.png`
- `Content/MapPlans/Laboratory.navgraph41-2.png`

The runtime pins both the exact navigation SHA and every PNG SHA. If these
are absent/stale it falls back to Pass 40 diagrams, then original Pass 39
source-bounds plans. **F4** toggles the plan, **N** switches between source
bounds and the graph, and **Left/Right** changes the selected floor band.
F10/F11 telemetry reports bridge count, remaining disconnected regions,
and whether Pass 41 repair diagrams are installed.

## Automated tests

The public Windows GitHub Actions job runs all Python repository contracts
and exports an actual Godot/.NET Windows executable. A **fabricated**
Laboratory source scene, a 19-node TWRNAV41 v3 with a 7-stud test corridor
and three synthetic PNGs verify that the compiled runtime:

- accepts a source-bound v3 graph, refuses unsupported formats and
  interprets the injected repaired shortcut as a direct A* route;
- retains offline F4/N floor switching and the optional F9 spawn fallback;
- never enables F9 by default;
- removes every private/synthetic `nav41.gz`, map scene, collision sidecar
  and navigation PNG before creating a public-safe Windows Actions artifact.

The Python suite verifies deterministic source-bound graph export and
rejects unsupported floors, surrogate MeshPart geometry, collision walls,
long-range shortcuts, and a modified source scene.

**Limitations:** The synthetic CI test cannot verify graphical fidelity,
FPS, actual missing CSG triangle collisions, original multi-floor
movement, 15-wave source-map traversal or user-controlled gameplay.
Run a player-controlled session on Windows and capture F10/F11 telemetry
and F12 frames for future map repairs.

## Installation

Keep the Pass 40 full owner-side Windows ZIP folder. Install this pass's
owner-private `Content/Navigation/Laboratory.nav41.gz` and
`Content/MapPlans/Laboratory.navgraph41-*.png` over the existing
directory without deleting the Pass 39 Laboratory scene/collision pack,
Pass 40 nav31 fallback, or Pass 37/38 lighting and sound content.

In Laboratory, F4 -> N displays actual new bridge diagnostics; the rest
of the maps remain the existing approximate/full-fragment state. The
project is **not** yet a 1:1 reproduction of the original TWR game.

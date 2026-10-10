# Pass 40 — Reconstructed Source-Bound Laboratory Navigation and F9 Accessibility

**Base:** Pass 39 full loaded owner-side Laboratory scene, original-place coordinate system; no Roblox service or Studio requirement at runtime.

## Offline source-derived navigation

`tools/validation/recover_source_navigation40.py` consumes only the verified compressed `Content/Maps/Laboratory.scene.jsonl.gz` with SHA-256
`35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74`.
The converter samples original-positioned and sized horizontal walkable bounding faces at 3.5-stud spacing, rejects placements crossing modeled collision volumes, tests support and body clearance along short graph edges, and exports the existing `TWRNAV31` v2 binary graph. It never executes Luau, requests cloud meshes, or creates magical long-range links.

Real original-source output: `Content/Navigation/Laboratory.nav31.gz`, SHA-256
`b021b905a0a87619997a0e2da08a4c7b2db18ecd2a5250f93dfd84d08cb26bdc`.

| Quantified laboratory recovery | Actual result |
| --- | ---: |
| Authored collision bounding volumes examined | 18,130 |
| Potential horizontal walkable source surfaces | 1,519 |
| Candidate walkable cells | 23,951 |
| Accepted grounded waypoint cells | **14,726** |
| Accepted short source-bounded edges | **43,920** |
| Connected graph components | **133** |
| Largest graph component | 2,732 nodes |
| Player start component | 291 nodes |
| Original player spawns near graph | **8 / 8** |
| Original infected spawns near graph | **3 / 15** |
| Original infected spawn components sharing a route with player | **0 / 15** |

**Important:** This is a conservative **approximation derived from original geometry bounding shapes**, NOT the lost original Roblox navmesh or proven walkable routes. Source MeshPart/UnionOperation triangles, original exterior terrain, dynamic door state and complete stair/fence behavior remain missing. The player starting island is graph-disconnected from much of the Lab. Its component has no candidates at least 24m from the central player start. Do not report this as complete navigation or as verified 15-wave playability.

The private nav pack uses the already deployed `Pass25SourceNavigationRuntime` loader. The runtime verifies the SHA-256 of the entire installed compressed map scene embedded in the `TWRNAV31` header. The new `IsPass40GroundedGraph` flag additionally pins the exact verified graph bytes. Missing/mismatched packs fail closed to local steering; they do not break map startup.

## Optional and bounded F9 accessibility

**F9 remains OFF by default.** Without F9 the source exact infected spawn markers and their positions are unchanged.

When a spawned infected comes from an isolated exterior/upper-floor source marker, `Pass40AdaptiveEntry` attempts replacement in the player's **existing graph-connected component**. Placement preference:

1. A physically supported waypoint at least **24 metres** from the player, preserving the Pass 32 minimum wherever possible.
2. If no supported placement exists, try **14 metres**.
3. As a last resort, try **10 metres**.

Each candidate must pass **the existing physical floor ray and capsule-clearance query** in `Pass32SpawnSafety` before assignment; candidates nearer than 10m are always rejected, invalid minimum-distance arguments are rejected, and no original source-connected spawn is relocated merely because F9 is enabled. This experimental approach trades exact source entry fidelity for possible gameplay accessibility in the incomplete map. Results may vary with doors, player position and missing original collision.

F10/F11 show source nav graph status and how often a closer-than-24m placement was required. Assisted rescue remains bounded by existing limits of two rescues per infected.

## In-game F4 visual diagnostics

Three private PNG overlays (`Content/MapPlans/Laboratory.navgraph40-{0,1,2}.png`) visualize the graph on the three Pass39 original-source floor plans. Green identifies the component accessible from the original source player start, purple identifies disconnected components. Distances around player spawn 1 (10m, 14m, 24m) illustrate why the F9 near fallback is needed. These overlays do not prove that every colored edge is walkable with actual original dynamic models.

In Laboratory during gameplay: **F4** opens the source floor plan, **Left/Right** switches levels, **N** switches between original source bounds and the new connectivity diagram, and **F4** closes it. The F4 graph view activates only when all 3 PNGs and the exact Pass40 nav pack SHA match; otherwise the original Pass39 floorplans remain accessible.

## Testing and distribution

The public repository contains only code, synthetic fixtures and provenance documentation. The Python test creates deterministic **invented** geometry, verifies graph framing/counts/source SHA, and compares repeated exports. The Windows workflow compiles the real Godot/.NET game then injects a fabricated Laboratory scene, a 19-node nav31 graph, and synthetic floor/map PNGs. The exported Windows `--smoke-pass40` test checks the in-game F4-N view, the F9 opt-in and an actual Regular-mode infected spawn using physics-supported 14m fallback. All synthetic fixtures are removed and private data exclusions verified before public artifact upload.

**The native smoke is not an actual human-played original Laboratory 15-wave match.** Real-world FPS, zombie traversal over stairwells, exact source collision, exit doors, and actual owner private assets must still be tested graphically.

For owner-only installation, place these new files alongside the Pass39 executable:

```text
Content/Navigation/Laboratory.nav31.gz
Content/Navigation/LABORATORY_NAVIGATION_MANIFEST40.json
Content/MapPlans/Laboratory.navgraph40-0.png
Content/MapPlans/Laboratory.navgraph40-1.png
Content/MapPlans/Laboratory.navgraph40-2.png
```

The previous private Pass39 Laboratory scene/collision/geometry packs and earlier Pass37/38 lighting/sounds remain necessary for the fullest available owner-side reconstruction. **The other nine maps still lack fully recovered loaded geometry.**

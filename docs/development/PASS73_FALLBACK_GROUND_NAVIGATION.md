# Pass 73 — Physically sampled fallback ground navigation and native interaction acceptance

**Branch:** `twr-pass73-fullmap-ground-navigation`
**Parent verified build:** [Pass72 Windows CI, ten actual map scene and Wave 2 tests](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38201737219).

## Implemented game changes

1. `Pass73GroundNavigationRuntime.cs` incrementally samples ordinary Godot floor colliders at 3m grid spacing, capping the entire grid to 2,800 cells and checking walkable floor normals, standing-capsule overlap, short floor-height changes and three adjacent collision rays for graph connections. Graph sampling/connect work is time-sliced across physics frames to avoid map-entry stutters.
2. Runtime attaches this fallback ONLY when the selected map is in approximate/fallback reconstruction mode and has no source navigation graph, excluding Expressway (which already has its own route graph) and any original-world Laboratory load. Source/owner pathfinding evidence remains authoritative when available.
3. Infected retain their previous physics and local raycast steering. When direct pursuit is occluded, a ground-route may detour around buildings. Anchor-distance and vertical-height checks keep second floors and unreachable islands from falsely connecting; routes are shared in a bounded 192-path cache.
4. F10 diagnostic display exposes graph node/edge counts and readiness; no map content, collision authority, retail paths or source meshes are modified.
5. First-person Space presses use a short 0.14-second buffer, so an airborne press does not queue an unintended jump long after landing. Pickup and objective progression ignore dead players, preventing postmortem grants.
6. New exported Windows native smoke enters all ten gameplay scenes and checks graph readiness on the nine approximate maps plus unchanged Expressway navigation. A clearly labelled synthetic Ranch wall tests real keyboard walking/sprinting/jumping, an actual infected character body navigating around collision, real Medkit and Radio rewards through the command bus, and blocking dead-player pickups/objectives.

## Fidelity limits and acceptance

- These are physics-derived paths in the existing **approximate fallback blockouts**, not the recovered Roblox navmesh.
- Grid sampling intentionally represents the primary ground floor only. Multi-floor interiors, source stair traversal, special infected movement, moving fortifications, exhaustive objective families and all 15 actual human-played waves remain follow-up work.
- No owner original maps, textures, voice/sound archives or private profiles are committed. The native smoke uses isolated `user://pass73-ground-navigation-smoke-profile.json`.
- **Verified:** [exported Windows CI run 38202837479](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38202837479) completed successfully: 601 Python regressions, Windows Godot/.NET build (zero compiler errors), full per-map smoke markers, physical keyboard/pursuit/reward acceptance and private-asset exclusion.

## Observed exported Windows fallback route graph metrics

All nine new graph measurements are on **public reconstructed fallback geometry**, not owner-original map packs.

| Map | Route graph | Nodes | Connected edges |
| --- | --- | ---: | ---: |
| Ranch | New sampled ground graph | 799 | 2,718 |
| Mill | New sampled ground graph | 954 | 3,312 |
| Bypass | New sampled ground graph | 1,075 | 3,712 |
| Cabin | New sampled ground graph | 595 | 2,090 |
| Cargo | New sampled ground graph | 787 | 2,413 |
| District | New sampled ground graph | 904 | 3,029 |
| Expressway | Existing roadway graph | 204 | Not asserted by this pass |
| Prison | New sampled ground graph | 938 | 3,076 |
| Laboratory | New fallback graph; original owner source absent in CI | 1,074 | 3,579 |
| Manor | New sampled ground graph | 607 | 1,855 |

### Ranch native movement, AI and interaction evidence

The exported Windows run reported `TWR_PASS73_RANCH_INTERACTION_OK` after measuring 1.35m walking and 1.82m sprinting with virtual W / Shift+S keyboard events. It verified jumping and landing, a real live infected character-body detour of 7.75m around a synthetic 12m-wide solid wall, and an 8.90m reduction in distance to the player. Real Medkit interaction raised domain health, Radio secure completion awarded domain rewards, and further E-key pickup/objective input after death made no domain changes.

The existing `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` domain-only smoke still passed but is **not** a genuine human-controlled 15-wave survival test.

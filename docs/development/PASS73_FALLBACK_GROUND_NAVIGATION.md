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
- CI will require all ten per-map markers plus `TWR_PASS73_RANCH_INTERACTION_OK` and final `TWR_SMOKE_PASS73_FULLMAP_GAMEPLAY_OK maps=10`, before marking this pass verified.

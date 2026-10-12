# Pass 77 — Reconstructed Manor Stair Geometry and Collision-Audited Uphill Navigation

**Branch:** `twr-pass77-manor-stair-traversal`  
**Parent:** [Pass76 successfully exported Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38208475688).

## Source/geometry limitations

The uploaded TestPlace place snapshots and connected project recovery artifacts do not substantiate fully recovered original retail Manor stair meshes and dimensions in this source repository. The original game is not imported into the public build. The three stair flights here are **approximate fallback geometry**, not exact source triangles/UVs. Their previous eight disconnected high vertical step colliders and unsupported centre-deck gaps made ordinary first-person traversal unreliable.

## Implemented

1. **Physically walkable fallback Manor stair flights.** The existing three `AddUpperFloor` calls for WestWing, EastWing and FrontHall retain eight visible stair treads; these are now noncolliding meshes. Each has one continuous inclined Godot `StaticBody3D` ramp whose rise/run determines a slope verified to be under 35°. The ramp is generated as a rotated solid, not a visual-only decal. A central 5.6m-wide upper landing overlaps both sides of the split upper deck, making exit possible without jumping the missing centre platform.
2. **Bounded, clearly labelled stair markers.** `Pass77StairLink` records actual reconstructed bottom and top surface positions. `RuntimeMapLayout.ApproximateStairs77` remains empty for all other maps and when a private source scene is used; original Laboratory scene, source navigation, owner original collision SHA rules and Expressway road path remain unchanged.
3. **Audited upward infected pursuit.** Only on the reconstructed Manor, an infected below a player on a physically connected upper floor can use a cautious uphill route. Every stair link must pass seven vertical normal/height collision samples and three swept corridor rays, plus a physically bounded ground-graph approach and upstairs corridor verification. The resulting 12-segment slope route retains feet-level waypoints for the existing physics-based `InfectedAgent`; there is no warping, collision bypass, fabricated jump edge, or replacement of recovery data. Stair descent and unconnected upper wings remain intentionally unimplemented.
4. **Exported Windows acceptance.** The new `--smoke-pass77` uses an isolated `user://pass77-manor-stair-smoke-profile.json` and creates a real Manor `GameplayRoot`, testing all three reconstructed ramp/landing colliders, actual W-key first-person ascent along FrontHall, an audited lower-to-upper AI waypoint route and a real infected CharacterBody climbing without custom teleportation. All other source-map content remains excluded from public CI.

**Native exported Windows verification pending.** Python contracts, C# export, player climb, infected route and private-asset exclusion must all pass before this can be called verified.

# Pass 77 — Reconstructed Manor Stair Geometry and Collision-Audited Uphill Navigation

**Branch:** `twr-pass77-manor-stair-traversal`  
**Parent:** [Pass76 successfully exported Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38208475688).

## Source/geometry limitations

The uploaded TestPlace place snapshots and connected project recovery artifacts do not substantiate fully recovered original retail Manor stair meshes and dimensions in this source repository. The original game is not imported into the public build. The three stair flights here are **approximate fallback geometry**, not exact source triangles/UVs. Their previous eight disconnected high vertical step colliders and unsupported centre-deck gaps made ordinary first-person traversal unreliable.

## Implemented

1. **Physically walkable fallback Manor stair flights.** The existing three `AddUpperFloor` calls for WestWing, EastWing and FrontHall retain eight visible stair treads; these are now noncolliding meshes. Each has one continuous inclined Godot `StaticBody3D` ramp whose rise/run determines a slope verified to be under 35°. The ramp is generated as a rotated solid, not a visual-only decal. A central 5.6m-wide upper landing overlaps both sides of the split upper deck, making exit possible without jumping the missing centre platform.
2. **Continuous widened upper connector after real native failures.** The exported Windows player first stopped at Y=2.48m against a raised flat landing lip; reducing the lip moved the collision to Y=2.84m, still below the 2.96m deck. The connector is now an actual **5.6-metre-wide angled collision bridge** matching the ramp's exact slope and overlapping both side balconies at the same floor height. The centre no longer contains a separate horizontal collision riser. In the third native run the **player actually reached the upper landing**, rising 2.68m to Y=2.91m, but the infected stalled 0.30m below the landing. Subsequent collision widening/recessing preserved the same infected stall at Z=29.49m, identifying a navigation timing issue rather than a residual collider seam: the original 1.58m ground-route waypoint radius prematurely skipped the final uphill waypoint. On Manor uphill routes only, infected must now approach uphill waypoints to within 0.35m horizontal and 0.15m vertical before progressing sideways to the balcony. Pass77 therefore widened the primary ramp to 5.6m, directly reaching both upper side decks, and recessed the secondary centre bridge by 0.16m to remove an internal overlapping-collider lip. Native infected upper-landing acceptance remains pending.
3. **Bounded, clearly labelled stair markers.** `Pass77StairLink` records actual reconstructed bottom and top surface positions. `RuntimeMapLayout.ApproximateStairs77` remains empty for all other maps and when a private source scene is used; original Laboratory scene, source navigation, owner original collision SHA rules and Expressway road path remain unchanged.
3. **Audited upward infected pursuit.** Only on the reconstructed Manor, an infected below a player on a physically connected upper floor can use a cautious uphill route. Every stair link must pass seven vertical normal/height collision samples and three swept corridor rays, plus a physically bounded ground-graph approach and upstairs corridor verification. The resulting 12-segment slope route retains feet-level waypoints for the existing physics-based `InfectedAgent`; there is no warping, collision bypass, fabricated jump edge, or replacement of recovery data. Stair descent and unconnected upper wings remain intentionally unimplemented.
4. **Exported Windows acceptance.** The new `--smoke-pass77` uses an isolated `user://pass77-manor-stair-smoke-profile.json` and creates a real Manor `GameplayRoot`, testing all three reconstructed ramp/landing colliders, actual W-key first-person ascent along FrontHall, an audited lower-to-upper AI waypoint route and a real infected CharacterBody climbing without custom teleportation. All other source-map content remains excluded from public CI.

## Final exported Windows verification

**Successful:** [Windows CI run 38212138620](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38212138620), testing gameplay commit `61c8d24ad49ef83f116ea0b9b6b82d5af079a24a`.

- **627 Python regressions passed**, with one inherited XML parsing FutureWarning.
- Godot/.NET Windows standalone export finished with **0 compiler errors**.
- Real production Manor Front Hall W-key player ascent: `TWR_PASS77_REAL_PLAYER_STAIRS_OK rise=2.68 upper_landing_reached=true`.
- Production `InfectedAgent` moved through actual CharacterBody collision and reached the same upper landing: `TWR_PASS77_REAL_INFECTED_STAIRS_OK actual_rise=2.62 upper_landing_reached=true source_jump_attempts=0`.
- Final `TWR_SMOKE_PASS77_MANOR_STAIRS_OK` and owner-private assets exclusion passed.
- Existing `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` is **domain simulation**, not 150 physically survived waves.

The prior native failures were substantive: flat-landings blocked the player, the short second connector made a contact seam, and the flat-ground waypoint acceptance condition let an infected skip the last uphill segment before reaching the deck. The last defect was fixed by requiring 0.35m planar / 0.15m vertical arrival on ascending fallback Manor waypoints.

**Fidelity boundary:** The original Manor stair meshes/transforms are not recovered or authenticated in this public source branch. The native traversal proves only the reconstructed **Front Hall** route. Other two flights, downward traversal, different infected types, human-played full 15 waves, source-authentic visuals and a privately assembled downloadable .exe remain unfinished.

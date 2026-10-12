# Pass 89 — Real grounded Escort objective navigation

Starting from verified Pass88, the reconstructed fallback-map Escort
survivor previously walked a direct 24-metre vector from its own feet
toward a local -Z waypoint. A 3D wall, pillar or missing physical floor
could leave the optional objective impossible to complete or leave its
survivor jammed against geometry.

Pass89 extends the existing collision-derived Pass73 graph with a
read-only, actor-foot-origin escorted route. Unlike infected GetRoute,
this method returns a *positive supported destination route* for a
genuinely clear direct corridor and [] for no verified route. All
computed path legs are rechecked against the live static collider world,
and the survivor validates the next 55cm physics step before running
its normal CharacterBody3D MoveAndSlide. Neither actors nor goal are
teleported through walls.

At activation the objective selects its original -24Z marker when it
can actually be reached. If the expected point fails the collision
audit, it tests eight bounded 12–24m local alternatives; this changes
only a public-safe, approximate fallback-map objective position and is
explicitly not a recovered Roblox retail destination. A route blockage
makes the NPC stop and retry, never walk through a solid obstruction.

Original-source scene packs, non-Pass73 Expressway graph behavior and
upper-floor Manor Escort remain on the previous behavior until
source-accurate navigation can be reconstructed. This pass does not
pretend to recover the original Escort animation rig or full campaign.

Acceptance: structural regression tests plus exported Windows
Pass89 physically obstructed Ranch NPC traversal, with repeat Pass78,
Pass83 and Pass88 native regressions and the owner-private exclusion
audit required to pass before claiming verification.

## Verified Windows acceptance

**Successful GitHub Actions run:** [38237636678](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38237636678) at commit `17fd9fb830db378dab4474ee7f44813e89233d29`.

- **666 Python regression tests passed**, one inherited source-parser FutureWarning.
- Godot 4.7.2 / .NET 8 export of the Windows executable completed with **0 compiler errors**, one inherited Pass36 CS0108 warning.
- Pass78 twelve real player/infected Manor stair traversals passed; Pass83 three upper wing physical pursuits/pickups passed; Pass88 ten-map live scene + Wave1/Wave2 acceptance and Manor Wave3 Radio floor checks passed.
- Pass89 physically injected a 14-metre-wide solid Ranch divider BEFORE Pass73 graph sampling. The directly obstructed path was rejected, and a supported 10-waypoint detour was returned.
- A second live obstruction was introduced AFTER graph construction. The formerly valid path was correctly rejected by the live collision audit.
- A real `ObjectiveRuntime` Escort spawned its ordinary `CharacterBody3D` survivor. The NPC advanced **8.66m laterally** and **17.25m in Z**, following the supported detour on solid floor with no survivor teleport and no bypassed reward authority. The successful marker is `TWR_SMOKE_PASS89_ESCORT_PHYSICAL_NAVIGATION_OK`.
- The owner-private original source pack exclusion check passed. Executable artifact upload **remains disabled** by the repository's pre-existing no-public-artifacts policy.

### Still incomplete

The Windows check tests physical avoidance of one injected wall on a reconstructed Ranch map. It does not prove that every Escort mission reaches the final goal, uses authentic Roblox retail destination markers or animations, or works across all maps and upper floors. Those remain future reconstruction tasks, alongside original map geometry and 15-wave real-time playthrough validation.

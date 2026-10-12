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

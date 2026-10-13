# Pass102: Complete map spawn to mission ingress (fallback collision)

**Base:** Windows-validated Pass101 branch (run 38330923050, 738 Python tests).

## Purpose

Pass101 physically walked the regular first-person player from normal spawn to a Load depot in Ranch, Mill, Bypass, District and Prison. It still teleported the player to the depot as isolated *test setup* for Cabin, Cargo, Laboratory, Expressway and the upstairs Manor objective. This left five spawn-to-mission routes unproven even though ten-map material pickup/deposit loops were physically exercised.

Pass102 removes all scripted player-position assignments from the ten-map `--smoke-pass96` acceptance harness. The actual player is created by production `GameplayRoot` at `_mapLayout.PlayerSpawn`. Normal keyboard W events, rotation (equivalent to steering the view), player collision, gravity and `MoveAndSlide` control all movement.

For Cabin, Cargo, Laboratory and Expressway, ingress uses a bounded route planned by `Pass73GroundNavigationRuntime.GetVerifiedEscortRoute89` (Expressway uses its separate Pass92 escort-safe graph, leaving the infected road graph unchanged). If a direct full-capsule physical path is open, it is preferred. Every routed waypoint is rechecked against live collision and physically traversed. No actor or static collider is repositioned.

Manor is a separate 3D route: the player must walk from actual ground-floor spawn to the real reconstructed WestWing ramp foot, walk *up the same collision slope* checked by earlier Pass77/78, then step across the upper landing and reach the first Load objective on its original fallback upper-floor location. Floor height is checked explicitly before crediting arrival. The other two staircases remain in the existing native physics regressions.

All ten maps then walk to a material, press E to pick it up, walk back and press E to deposit. Twenty production-objective Load/Repair item interactions and the earlier full material loop tests are retained.

## Acceptance criteria

`TWR_PASS102_MAP_SPAWN_INGRESS_OK` must appear once for each of Cabin, Cargo, Laboratory, Expressway and Manor. `TWR_SMOKE_PASS102_ALL_SPAWN_OBJECTIVE_INGRESS_OK newly_verified_maps=5 total_verified_maps=10` must appear after every objective round trip. Windows CI fails for any missing marker, blocked route, incorrect floor, missing objective interaction, player death, physics discontinuity or nonzero exit code.

CI also reruns Pass99 carrying arbitration, Pass100 20 material loops, Pass101 five original spawn walks, Pass78 twice on the real three Manor stairs, Pass95 escort, Pass97 fifteen-wave replay, Pass98 E-key material lifecycle, and the privacy exclusion check.

## Scope

All ten environments in the public Windows CI build are approximate *fallback blockouts*. Passage through them is not proof of original Roblox retail coordinates or 1:1 visual/animation/audio fidelity. A headless test controlling the real player's input is not a full human-controlled survival session.

**Status:** Implementation and native test gates committed. A successful Windows export/native smoke run on the latest code SHA is required before this pass can be called verified. The CI workflow does not release an executable.

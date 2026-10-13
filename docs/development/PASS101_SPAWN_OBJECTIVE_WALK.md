# Pass101 — Actual player-spawn to objective entrance routes

**Starting point:** Pass100 physics and two-way material traversal was accepted in exported Windows ([run 38328932663](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38328932663)). Pass100 started a test player directly at the Load depot on each map and did not establish that players spawning into a map could actually reach that mission.

## Acceptance upgrade

The same full production Godot/.NET `GameplayRoot`, `FirstPersonPlayer` movement code and `ObjectiveRuntime` are used in the existing `--smoke-pass96` exported-Windows acceptance. The test now drives the actual W key from the **unmodified player spawn**, where Godot places the player in `GameplayRoot._Ready`, toward the fully grounded first Load objective depot on Ranch, Mill, Bypass, District and Prison. No player position or velocity is assigned on these five ingress routes.

The test rotates facing like a mouse operator, then allows `FirstPersonPlayer._PhysicsProcess` to handle each normal W input and `MoveAndSlide` collision. It checks that the player remains on a floor, moves meaningful physical distance, stops within 1.15m of the destination, and has no >0.55m single-frame discontinuity. It then physically walks to the first material, presses E to pick up, walks back to the depot, and presses E to deposit.

Each successful map emits `TWR_PASS101_MAP_SPAWN_INGRESS_OK`, with measured distance and initial/collision-supported final position. All five must pass for `TWR_SMOKE_PASS101_SPAWN_OBJECTIVE_INGRESS_OK maps=5`.

Manor's existing **upper-floor** placement, plus Cabin, Cargo, Laboratory and Expressway, still use the deliberate Pass100 depot-entry setup for Load. The full Pass100 material round-trip test remains active for all ten maps and both objective families, while Pass98 still completes four entire objectives on Ranch and Manor via genuine E presses and test-player-controlled material traversal.

## CI guardrails

The Pass101 branch runs the new five-map ingress gate **in addition to** Pass100 20 material round trips, Pass98 entire Ranch/Manor material lifecycle, Pass99 one-carry arbitration, Pass97 fifteen-wave replay, and earlier native physics/escort regressions. The private-map exclusion check is enabled. No private owner art, files or executable is uploaded by this branch's public workflow.

## Remaining acceptance

A result from this pass is *not* a proof that all ten maps permit spawn-to-objective travel: Cabin, Cargo, Laboratory, Expressway and elevated Manor still need character-controlled ingress. This also is not a human-operated 15-wave survival session, original placement recovery, 1:1 visual fidelity certification or a distributable game.

## Verified native acceptance

[GitHub Actions run 38330029719](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38330029719) successfully compiled/exported the actual Windows Godot/.NET game and passed the **Pass96 material preflight containing all five new Pass101 spawn ingress checks**, plus Pass100 20 physical material round trips on ten maps, Pass97 fifteen-wave replay/persistence and Pass98 full material lifecycle. The separate Pass95 Manor Escort and Pass73 all-map physical movement regression gates also passed during the same run. The later full CI pipeline's final conclusion must still be checked before describing the branch as globally green. The public CI workflow does not release its executable artifact.

These pass results establish actual headless player movement and authoritative E-key interactions on the stated fallback routes. They do not establish a complete human-operated match or original retail visual geometry.


## Expanded regression: physical Front Hall infected descent

The first comprehensive native [Pass101 CI run 38330029719](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38330029719) **passed** the five-map spawn ingress, ten-map Load/Repair material walking, full Ranch/Manor E-key objective completions, fifteen-wave replay, Pass95 Manor Escort and Pass73 all-map movement. It then failed the newly re-enabled older Pass78 three-flight stair regression: the same real infected climbed every flight, descended West and East, but the Front Hall infected reached about `(X=1.94, Y=1.16, Z=34.36)` and did not reach a verified grounded lower-floor arrival.

This revealed that the generic 2.2m flat-world obstacle-lookahead can turn a descending infected off the lower stair axis when it detects nearby shell walls beyond the immediate waypoint. Pass101 now checks a **short 0.4m physical downhill step** against the actual ramp slope and full-capsule Godot swept collision/floor support in `Pass73GroundNavigationRuntime.SafeManorDescentStep101`. Only when that safety test accepts the next step does `InfectedAgent` follow the current route direction instead of flat-world avoidance. All fallback/source distinctions, collision authority, and no-teleport restrictions remain intact.

An early Windows preflight runs the real Pass78 exported executable on all three stairs before the longer objective/wave suite. **Run 38330923050 passed that native exported-game preflight on the corrected code SHA `13fc4220d535a71fd4f3150ea052d3e1b8590996`**, including all three actual infected stair descents. The full branch regression suite runs afterward; only a fully green pipeline can certify every gate.

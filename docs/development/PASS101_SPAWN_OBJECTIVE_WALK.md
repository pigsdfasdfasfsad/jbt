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

**Status:** implementation committed and Windows CI requested; native gate is authoritative, not contract source text.

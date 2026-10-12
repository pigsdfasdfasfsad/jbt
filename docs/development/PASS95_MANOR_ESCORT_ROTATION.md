# Pass95 — Physically verified Manor upper-wing Escort rotation

**Base:** [Pass94 verified Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38304284899), including real front-hall staircase descent, domain reward idempotence and cumulative nine-map Escort physics regression.

## Feature

Previously fallback Manor upper-floor Escort objectives reused the same wave-indexed marker, so guaranteed later Escort waves (5, 10, 15) all selected the front hall. Pass95 extends only these **approximate reconstructed Manor** objectives with a deterministic three-wing rotation. Wave5 remains front hall (already native-verified); Wave10 uses west upstairs; Wave15 uses east upstairs. Optional Wave3/8/13 Escorts can also rotate through upper wings. The owner-original map, all other nine maps and original recovered source markers are unchanged.

No new virtual collision shortcuts are introduced. Existing Pass77 ramps, Pass80 upper crossings, Pass82 player-aligned upper markers, Pass89 actual .42m survivor capsule, Pass90 supported-ground completion and Pass94 short-leg stair probes are reused. Real production Wave10 and Wave15 Escort survivor bodies should physically complete their objectives using the real Godot colliders, and their normal domain callbacks must grant 4,000 credits/3,500 XP exactly once.

## Acceptance and gating

The new exported Windows `--smoke-pass95` run advances normal Manor match stages through Wave15, accelerating only test-time clocks (not player health, NPC position, collision or credits). It observes real WestWing and EastWing survivors reaching physically supported objectives, plus exactly-once credit/XP for each. On a source-absent fallback map, the EastWing approximate 24m target may be inaccessible; Pass90 can accept another collision-checked **upper-floor** destination instead. Actual stair descent is required only when the chosen, grounded destination is downstairs. The test must not insist that every legitimate upper-floor rescue go down a staircase. Both scenarios run with real CharacterBody3D movement and abort on movement stalls. Existing Pass78/83/88/89/90/91/92/94 native regressions and owner-private asset exclusion remain required.

**Status:** implementation submitted; success requires a green native Windows run. This pass does not claim recovered original retail Manor mesh positions, animation fidelity or manually verified 15-wave survival.

## First native execution

Windows run [38305396390](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38305396390) compiled and passed source tests. Its real **Wave10 WestWing** NPC finished physically (22.52m total displacement; 193 supported movement steps) with 4,000 credits/3,500 XP and no replay payout. Wave15 **EastWing** also completed its production objective with supported grounding, but the acceptance script incorrectly required a downstairs descent even though the existing Pass90 fallback chose a reachable same-floor upper goal. A follow-up asserts the selected destination is on a valid floor, and requires continuous physical descent only when that destination is on the ground story. Original owner map positions remain untouched.

This does **not** establish a 1:1 recovered retail evacuation point; the saved original geometry needed for exact floor/objective destinations remains outstanding.

### Harness lifecycle correction

The follow-up run [38305777398](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38305777398) again observed full WestWing completion, but `goalY=NaN` for the already-completed objective. The production objective automatically freed its marker immediately after completion, making a post-completion node read invalid even though the NPC's physical completion was accepted. Pass95 now records the physically grounded destination height **while** the NPC is moving and only evaluates upper-vs-lower goal behavior against that earlier evidence. The NPC, objective, map geometry and domain payout are unmodified by this test fix. Native re-verification remains required.

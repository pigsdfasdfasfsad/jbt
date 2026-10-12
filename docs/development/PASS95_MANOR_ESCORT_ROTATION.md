# Pass95 — Physically verified Manor upper-wing Escort rotation

**Base:** [Pass94 verified Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38304284899), including real front-hall staircase descent, domain reward idempotence and cumulative nine-map Escort physics regression.

## Feature

Previously fallback Manor upper-floor Escort objectives reused the same wave-indexed marker, so guaranteed later Escort waves (5, 10, 15) all selected the front hall. Pass95 extends only these **approximate reconstructed Manor** objectives with a deterministic three-wing rotation. Wave5 remains front hall (already native-verified); Wave10 uses west upstairs; Wave15 uses east upstairs. Optional Wave3/8/13 Escorts can also rotate through upper wings. The owner-original map, all other nine maps and original recovered source markers are unchanged.

No new virtual collision shortcuts are introduced. Existing Pass77 ramps, Pass80 upper crossings, Pass82 player-aligned upper markers, Pass89 actual .42m survivor capsule, Pass90 supported-ground completion and Pass94 short-leg stair probes are reused. Real production Wave10 and Wave15 Escort survivor bodies should physically complete their objectives using the real Godot colliders, and their normal domain callbacks must grant 4,000 credits/3,500 XP exactly once.

## Acceptance and gating

The new exported Windows `--smoke-pass95` run advances normal Manor match stages through Wave15, accelerating only test-time clocks (not player health, NPC position, collision or credits). It observes a real WestWing survivor reaching its supported objective, an EastWing survivor descending a supported staircase and completing, and exactly-once credit/XP for each. Both scenarios run with real CharacterBody3D movement and abort on movement stalls. Existing Pass78/83/88/89/90/91/92/94 native regressions and owner-private asset exclusion remain required.

**Status:** implementation submitted; success requires a green native Windows run. This pass does not claim recovered original retail Manor mesh positions, animation fidelity or manually verified 15-wave survival.

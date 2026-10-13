# Pass98 — Complete real-E material objectives and shared interaction safety

**Baseline:** [Pass97 verified Windows export](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38325416220). Ten maps have source-absent fallback collision-grounded material placements; the actual gameplay loop had only exercised the first Ranch pickup and deposit in native acceptance.

## Gameplay improvement

A held E key previously had a shared per-player claim budget for **ordinary world pickups only**. Load and Repair objectives listened for E independently. With two objectives overlapping in a single wave, each could collect/deposit a material from the **same E press**; a material interaction and a regular world pickup could also both execute. The nearest physically occluded prop could block selection of another visible reachable prop.

Pass98 integrates the production `ObjectiveRuntime` Load/Repair interaction with the existing `Pass76InteractionSafety` per-player E press budget:

- One successful action (pickup **or** deposit) claims the same E press budget as ordinary world loot. The player physics actor clears that claim **only after E is released**. Other objectives and pickups cannot also use the held press.
- Material candidates are filtered against real player reach, world-collision occlusion, valid Godot instance state and removal status **before** the nearest candidate is selected.
- A Load/Repair actor carries at most one physical item, updates true remaining/deposited counters, and grants its reward only after every required item has been individually brought to the deposit.
- Objective-family rewards, 10-item Load and randomized 10/12-item Repair variants, source-owner scene geometry, infected movement and 15-wave economy were not reauthored.

## Native Windows acceptance

The separate `--smoke-pass98` executable entrypoint uses a clean isolated profile and instantiates **actual** Ranch and Manor `GameplayRoot` scenes. Wave1 production Load and Wave2 production Repair are started through normal domain/stage transitions, rather than manufacturing completion commands. The test operator positions only the actual physics-controlled **player** at each already-validated grounded material and deposit point, then sends real E key-down and key-up events.

A separate isolated Ranch setup first spawns **two truly overlapping Load objectives** on identical validated floor points, then holds E across multiple frames. Exactly one actor is permitted to collect one item, and the following E press at the depot must deposit into only that actor; a second objective must not silently pick up or deposit in the same press. Natural loot is removed only from this overlap-test fixture to avoid randomized unrelated input recipients.

Every material in each production objective must be picked up individually, carried and deposited, with no duplicate E action when held. The objective must issue exactly one callback and its real domain receipt must grant the precise source-backed 4,000 credits and 3,500 XP, including proper level-up credit bonuses. Repeating the same completion command may **not** pay again. The accepted match is saved and returns to lobby.

This test deliberately accelerates stage clocks and teleports the test **player** between otherwise solid, line-of-sight-proven surfaces; it does not pretend a human walked all those paths. The NPC, objective, material meshes, colliders and authoritative player credits/XP are never teleported or altered to fake success.

The Pass98 CI gate also preserves Pass95 upper-Manor Escorts; both Pass96 physics/objective material checks; Pass97 full fifteen-wave material/persistence integration; Pass78/83/88/89/90/91/92/94 native regressions; and exclusion of owner-private map packs or public downloadable executables.

**Status:** implementation committed; Pass98 requires green exported Windows native validation before completion is claimed.

## Remaining work

Original retail map/asset coordinate fidelity, non-placeholder item models, human-controlled 15-wave survival, complete objective collision walks, original animation/audio and a distributable owner-approved Windows package are not verified by this pass.

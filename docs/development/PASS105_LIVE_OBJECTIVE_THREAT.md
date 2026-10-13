# Pass 105 — real infected pursuit and melee damage while carrying a material

**Baseline:** Pass104 two full wave objectives physically completed on Ranch and Manor, final native Windows CI [38338396152](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38338396152), 748 Python tests and successful Windows Godot/.NET export.

## Target

Earlier Pass103 and Pass104 acceptance correctly exercised all 44 physical material pickups/deposits and 336.4m of normal W-key movement, but **froze the main wave timer and enemy spawning** while the test player carried materials. This can conceal bugs where a real enemy attack interrupts E-key pickup/deposit or where wave-hostile interactions interfere with carrying state.

Pass105 adds a separate, bounded, **real hostile Wave1 objective** preflight in the exported Windows executable. The previous full physical Load and Repair regressions remain unchanged and run independently on this branch.

## Actual acceptance scenario

1. Use the clean isolated `pass105-live-threat-material-smoke-profile.json`; launch the production **Ranch** map at its natural player spawn.
2. Begin the normal Wave1 countdown and Load objective. Physically walk from player spawn to the real Load depot and to the first grounded material using ordinary first-person W input, production collision, gravity and verified supported fallback routes.
3. Press E to pick up one material and assert the production carry state.
4. Resume the live `GameplayRoot._Process` wave loop. Assert the **production native wave-spawner creates a real `InfectedAgent`** targeting the real player, and remains active during the ensuing test.
5. For a repeatable, short combat exposure, **the test repositions only this freshly production-spawned enemy** to an unobstructed, physically supported nearby ground lane, configures an ordinary Civilian melee attack at limited nonzero damage and speed, and lets its real Godot `CharacterBody3D` physics, LOS and `Runtime.DamagePlayer` path run. It does **not** create a fake infected actor or directly set player health. The original enemy appearance, random spawn location and long-distance pathfinding are *not* being certified by this control.
6. Require measurable physical infected approach (at least 0.65m closing), real resulting melee health damage, continued living player and retained carried material, all while the ordinary Wave1 loop is still enabled.
7. Physically walk back to the real depot **without disabling enemy AI or the running wave**, press E and verify one actual deposited material, no lost carry state, no premature objective completion and no early credits/XP award.
8. Finish the intentionally incomplete match through its normal failure and persistence path, restore the local lobby, and emit evidence markers.

A successful exported Windows run must include both `TWR_PASS105_RANCH_UNDER_FIRE_DEPOSIT_OK` and `TWR_SMOKE_PASS105_LIVE_OBJECTIVE_OK`. Damage must be positive and the test must exit zero. Earlier Pass103/104 native runs and all ten-map material, escort and private-asset regressions remain enabled.

## Acceptance limitations and next work

This pass adds a **single material** exposed to bounded live attack on Ranch; it **does not** claim a complete real-time ten-item objective completed through uninterrupted enemy pressure. The sampled enemy's near-item lane and Civilian stats are test-controlled; production random spawn coordinates, long-range pursuit and balancing remain separate. It does not prove a human player surviving waves or original-map geometry fidelity. A later pass should extend live combat to longer natural-spawn encounters and entire objectives without invulnerability, relocation, teleportation or combat freezing.

**Status:** implementation committed, awaiting exported Windows native CI result on the final branch SHA.

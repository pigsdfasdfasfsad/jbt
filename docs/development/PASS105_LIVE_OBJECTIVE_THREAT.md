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
5. For a repeatable, short combat exposure, **the test repositions only this freshly production-spawned enemy** to an unobstructed, physically supported nearby ground lane, configures an ordinary Civilian melee attack at limited nonzero damage and speed, and lets its real Godot `CharacterBody3D` physics, LOS and `Runtime.DamagePlayer` path run. The chosen lane prefers the material's outward direction from its deposit, so the test enemy starts behind the actual return route. It does **not** create a fake infected actor or directly set player health. The original enemy appearance, random spawn location and long-distance pathfinding are *not* being certified by this control.
6. Require measurable physical infected approach (at least 0.65m closing), real resulting melee health damage, continued living player and retained carried material, all while the ordinary Wave1 loop is still enabled.
7. Physically walk back to the real depot **without disabling enemy AI or the running wave**, press E and verify one actual deposited material, no lost carry state, no premature objective completion and no early credits/XP award.
8. Finish the intentionally incomplete match through its normal failure and persistence path, restore the local lobby, and emit evidence markers.

The accepted walk route is still inspected against **all static collision and floor support**, but its *planning-only* Pass89 clearance probe omits moving infected body RIDs: these are not permanent walls. The actual FirstPersonPlayer `MoveAndSlide` still collides with those infected during every W-key step. This is not collision disabling, route teleportation or enemy-freezing.

A successful exported Windows run must include both `TWR_PASS105_RANCH_UNDER_FIRE_DEPOSIT_OK` and `TWR_SMOKE_PASS105_LIVE_OBJECTIVE_OK`. Damage must be positive and the test must exit zero. Earlier Pass103/104 native runs and all ten-map material, escort and private-asset regressions remain enabled.

## Acceptance limitations and next work

This pass adds a **single material** exposed to bounded live attack on Ranch; it **does not** claim a complete real-time ten-item objective completed through uninterrupted enemy pressure. The sampled enemy's near-item lane and Civilian stats are test-controlled; production random spawn coordinates, long-range pursuit and balancing remain separate. It does not prove a human player surviving waves or original-map geometry fidelity. A later pass should extend live combat to longer natural-spawn encounters and entire objectives without invulnerability, relocation, teleportation or combat freezing.

**Status:** the original native Pass105 encounter passed in two exported Windows preflights, [run 38347029430](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38347029430) and [run 38347111514](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38347111514). These runs also passed independent prior Pass103/104 mission preflights. The subsequently committed dynamic-enemy route-planning improvement was fully verified on compiled executable source SHA `ad556c20b6a5579b120e03277050162efa0ad0d7` in [run 38347592407](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38347592407). **All 751 Python contract tests passed (one inherited warning), native Windows Godot/.NET export succeeded, and every enabled Pass101–105 physics/objective plus older 15-wave replay, infected navigation, Escort, stair and owner-private-asset exclusion gate passed.** Its own native Pass105 hostile-interaction acceptance also passed.

## Measured native evidence from a completed run

[Windows Actions run 38347111514](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38347111514) completed **successfully** across the full CI pipeline (751 passed Python contract tests, one inherited warning, native Godot/.NET export, all earlier gameplay regressions and private-asset exclusion).

Its bounded Pass105 physical gameplay reported:

| Native measurement | Value |
| --- | ---: |
| Real player W-key walking from Ranch spawn to deposit | 15.5m |
| Production-spawned infected physically closed | 1.3m |
| Authoritative melee health damage while carrying | 4.0 |
| Real Load material pickup and deposit | 1 each |
| Player position teleports | 0 |
| Premature objective payout | none |
| Interrupted match result | persisted |

The printed native marker was `TWR_PASS105_RANCH_UNDER_FIRE_DEPOSIT_OK` followed by `TWR_SMOKE_PASS105_LIVE_OBJECTIVE_OK`. The final revised-code [full successful Windows CI run 38347592407](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38347592407) independently reproduced **15.5m player travel, 1.3m infected pursuit, 4.0 real melee damage, one E-key pickup/deposit, zero player teleports and successful persistence**. This final run finished its complete test suite, including the private-source exclusion audit, with no failures.

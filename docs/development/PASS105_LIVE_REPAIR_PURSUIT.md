# Pass105 — production infected pursuing a carrying player during Wave2 Repair

**Base:** [Pass104 Windows-native green pipeline](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38338396152): 748 Python tests, 10 map traversal regression, real physics Wave1 Load and Wave2 Repair on Ranch/Manor, 44 material deposits and 336.4 metres, 15-wave simulation/replay and private-source exclusion.

**Status:** Native Windows and full CI **verified**. Implementation SHA `ed71073eec7340f650a1e267101f5fd815a788a0` passed [all enabled CI gates in run 38340291660](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38340291660). A second independent build [38340312355](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38340312355) also **completed the entire Windows CI pipeline successfully**, including 750 Python tests and the final source/privacy checks.

## New integration boundary

Earlier tests proved three different things separately:
- InfectedAI can spawn from the actual normal wave spawner and pursue/attack a player through Godot physics (Pass70/71/88 and escort/pathfinding regressions).
- The player can physically carry and deposit every production material without teleportation (Pass103 and Pass104).
- Pass104 accelerates wave transition and freezes new enemy spawning entirely during the full two-wave material loop. It therefore did **not** establish overlap between a living infected and material-carry gameplay.

Pass105 adds `--smoke-pass105` as an **independent** Windows-native acceptance that reuses all Pass104 real W/E-key walking and domain reward rules. On Ranch and Manor, at the exact moment the player picks up the final Wave2 Repair material:

1. Call the regular `GameplayRoot._Process(.05)` once while the *real* Wave2 stage is active. This invokes the **production spawner** and collision-approved fallback infected entry validation. Test only a single selected infected in its genuine sampled spawn position.
2. Freeze only the stage clock/next timed spawns, leaving the enemy's unmodified `InfectedAgent._PhysicsProcess`, gravity, pathing, damage and attack logic, and the real player/collider/objective logic active.
3. While the player is holding the last item, allow ninety actual physics frames for the infected to chase the player. Require a real grounded, living enemy whose collision body moves at least 0.30 metres, and confirm the player remains alive.
4. Walk back to the real Repair depot by the existing full-capsule-approved physical W-key route, deposit with normal E-key input, and require that the same enemy **remains alive, grounded and actively in the scene when the production Repair objective completes**. The baseline Pass104 objective receipts, exactly-once rewards, persistence and earlier-wave completion ID must still pass.
5. Repeat on the other map. Emit `TWR_PASS105_MAP_REPAIR_PURSUIT_OK` for each and `TWR_SMOKE_PASS105_LIVE_REPAIR_PURSUIT_OK` as the final summary.

The test is deliberately strict: it never writes `player.GlobalPosition`, `player.Velocity`, enemy positions, speeds, damages, health, or reward balances. The live infected is spawned by the normal production wave spawner, and is not a fabricated no-damage dummy.

## What it does *not* prove

The enemy starts from its normal, often distant, collision-approved map spawn. Pass105 does **not** require it to close to melee range, damage the player, or be shot and killed. New timed spawns and the round clock remain paused during the long acceptance loop, so this is **not** a full human-survived two-wave match under continuous enemy pressure, and not evidence of 1:1 retail map geometry/AI behavior.

A subsequent pass should verify actual camera-fired defense, ammunition, taking damage during an objective, and recoverable carry/deposit flow without god mode, eventually enabling a bounded live-wave combat-and-material loop.

The native smoke executable is built on the Actions runner; public compiled artifacts and private owner-original scene data remain disabled in the repository workflow.

## Verified exported Windows run and measured live pursuit

The first fully green Actions run [38340291660](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38340291660) exported the normal offline executable, then passed **748 Python source/contract tests (one inherited warning)**, all Pass103/104/105 native acceptance steps, ten-map material ingress and 15-wave replay, real stair/infected/escort tests, and the owner-original asset exclusion audit.

Actual Pass105 native output:

| Public fallback map | Wave2 actor still alive at real Repair E-deposit | Horizontal enemy displacement |
| --- | --- | ---: |
| Ranch | Yes, grounded | 8.28 metres |
| Manor | Yes, grounded | 8.93 metres |
| **Total** | **Both completed** | **17.21 metres** |

`TWR_SMOKE_PASS105_LIVE_REPAIR_PURSUIT_OK maps=Ranch,Manor full_wave2_repairs=2 materials_deposited=22 live_pursuit_metres=17.21 real_wave2_spawn=true physics_enemy_alive_at_deposit=true real_W_E_keys=true player_teleports=0 no_damage_immunity=true timed_extra_spawns_paused=true player_combat_kill_not_tested=true full_combat_survival_unverified=true`

### Production item counts can vary

Earlier Pass104 production tests generated 12 Repair items on **each** map (24 total) and 44 total Load+Repair deposits. The Pass105 run generated 22 Repair items in total (42 Load+Repair deposits). These are both genuine native successful production outcomes; **neither 10 nor 12 is safe to hardcode as the required Repair item count on every map or every execution**. The strengthened acceptance uses each actor's exact `Pass96MaterialFeet` and `Pass98RequiredMaterials` and validates every real pickup/deposit/reward. Check actual generated count rather than changing the game to fit the test.

No assertion is made that the pursuer reached melee distance, damaged the player, or was shot. The new verified boundary is **real spawned infected physics movement concurrent with carrying and E-depositing a production objective** on both maps, preserving prior full objectives, accounting, map geometry and persistent offline results.

## Second independent complete run

The latest full code commit `ed71073eec7340f650a1e267101f5fd815a788a0` also passed all 32 Windows pipeline steps in [run 38340312355](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38340312355), with **750 Python tests passed (one inherited warning)**. Native pursuit distances varied with the production spawn/random type selection:

| Run | Ranch horizontal infected movement | Manor horizontal infected movement | Total |
| --- | ---: | ---: | ---: |
| 38340291660 | 8.28 m | 8.93 m | 17.21 m |
| **38340312355** | **9.26 m** | **8.07 m** | **17.33 m** |

Both runs spawned a real collision-approved Wave2 infected on each map, proved movement via real Godot physics, and successfully deposited the final Repair material with the living pursuer in the scene. Exactly-once accounting, persistence, the original full material loops, upper-floor navigation, escort, 15-wave simulation/replay and owner-private-asset exclusions remained green. The random spawn variation demonstrates why the acceptance verifies **actual physical pursuit** rather than matching a fixed travel distance.

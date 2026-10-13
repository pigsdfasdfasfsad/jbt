# Pass105 — production infected pursuing a carrying player during Wave2 Repair

**Base:** [Pass104 Windows-native green pipeline](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38338396152): 748 Python tests, 10 map traversal regression, real physics Wave1 Load and Wave2 Repair on Ranch/Manor, 44 material deposits and 336.4 metres, 15-wave simulation/replay and private-source exclusion.

**Status:** committed as a new optional headless native smoke mode; must pass exported Windows CI before the Pass105 gameplay claim is considered verified.

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

# Pass103 — full production Wave1 Load completed by physical walking

## Baseline and target

Pass102 [Windows CI 38334803204](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38334803204) passed all ten reconstructed fallback map spawn-to-Load-depot routes, twenty full physics Load/Repair first-item round trips, fifteen-wave simulation/persistence replay, and prior combat/escort/stair physics and private-source exclusion gates. It did not fully complete every item of a production objective through walking: Pass98's four full objectives covered all ten-or-more materials but the acceptance player was repositioned directly to each pickup/depot.

Pass103 adds **independent** exported-Windows acceptance `--smoke-pass103`: production Wave1 Load missions on **Ranch and Manor**, with the normal playable map, normal player spawn, collision, gravity, regular Load objective actor, authoritative domain rewards, and repeated genuine W/E inputs.

## Procedure

1. Create a clean isolated offline profile and enter Ranch's real fallback game scene at its normal player spawn.
2. Run the normal Wave1 countdown by advancing the stage clock once; freeze the subsequent **wave clock and enemy spawning** for the narrow interaction acceptance, while leaving normal actor physics, item interaction and collision active.
3. Walk physically from original player spawn to the real, wave-created Load depot, using direct full-capsule-tested travel or the verified fallback ground route when needed. Only player heading and ordinary W-key input are controlled.
4. For **every** generated material, physically walk to that item's grounded world location; press/release E to carry it; walk back to depot and press/release E to insert it. Assert pickup, remaining and deposited inventory transitions after each E. No player, pickup, depot, actor, physics floor or wall position is overwritten.
5. Assert completion occurs **only after the final material**. Confirm one completion callback, authoritative saved completion ID, 4,000-credit / 3,500-XP mission reward with exact level-up calculation, and a duplicate receipt awards nothing.
6. Fail the isolated match, check the local result was saved, restore lobby, and repeat for Manor.

Successful native output must contain one `TWR_PASS103_MAP_FULL_LOAD_OK` for each Ranch/Manor and a `TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_OK` summary showing two completed production Load missions, at least twenty physical material round trips, real W/E input, 0 teleports and exactly-once completion receipts.

## CI and privacy

The Pass103 branch retains the prior Pass102 ten-map physical ingress marker gate, full Pass100/101/102/98 objective regressions, three-stair Manor infected descent, escort tests, wave replay/persistence, and final non-public-owner-asset exclusion. The Windows pipeline never uploads its `.exe` or private owner-provided Roblox scene packs.

## Limits

This is a **headless mission-interaction acceptance**. The wave countdown is accelerated and enemy-wave spawning is frozen during the material operation, so it does **not** prove surviving a real-time wave while completing every item. Only **Load on Wave1** is being added; earlier full, teleport-controlled Pass98 still covers Load/Repair Wave1/2 on Ranch and Manor, and Pass102 still covers one physically carried item per family on each of ten maps. These public blockouts are not certified 1:1 original art, source collision, sound or animation.

## Final native Windows verification

**Pass103 is fully Windows-CI verified.** The final code SHA `439c8c46ffae48756802cd88b7381681cc923293` passed all enabled checks in [GitHub Actions run 38335724427](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38335724427). The previous build/run [38335687532](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38335687532) also passed every native regression. The final source revision recorded **746 passing Python contract tests (one inherited warning)**, a successful Godot/.NET Windows export, and the complete Pass78/83/88/89/90/91/92/94/95/96/97/98/99/100/101/102/103 gameplay and private-asset-exclusion gates.

Actual output from the final Windows game:

| Map | Production Wave | Materials physically carried and deposited | Total physical walking |
| --- | --- | ---: | ---: |
| Ranch | 1, Load | 10 of 10 | 72.5 m |
| Manor | 1, Load | 10 of 10 | 90.5 m |
| **Total** | **Two complete Wave1 Load objectives** | **20 of 20** | **163.0 m** |

Native summary: `TWR_SMOKE_PASS103_FULL_PHYSICAL_LOAD_OK maps=Ranch,Manor completed_production_wave1_loads=2 material_pickup_deposits=20 walked_metres=163.0 actual_W_E_keys=true player_teleports=0 objective_completion_receipts_once=true`.

The headless test intentionally disables time-based enemy spawning after beginning Wave1. These results verify mission interactions and reward accounting under physical movement, **not** genuine real-time survival performance or exact retail art/scene fidelity. This run compiles the Windows executable but intentionally does **not** publish a downloadable public binary.


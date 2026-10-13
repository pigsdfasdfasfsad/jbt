# Pass104 — Full physical production Wave2 Repair

**Base:** Pass103 full-physical Wave1 Load (746 Python tests and successfully exported Windows native CI run 38335724427).  
**Status:** implementation and automated Windows acceptance gate committed; the latest exported native run must pass before the feature is certified.

## Goal

Pass103 proved that the actual production Wave1 Load objective could be completed by ordinary first-person input, including ten grounded material pickups and deposits on Ranch and Manor (20 items, 163.0 metres combined, zero teleports). It did not prove complete **Wave2 Repair** via regular movement. Pass98 exercised every material in both families, but its full-objective harness directly repositioned the test player beside each pickup and deposit; Pass100 physically carried one material for each family on all ten maps.

Pass104 adds a separate exported Windows `--smoke-pass104` mode **without replacing Pass103**. It reuses Pass103's known collision-safe normal player movement and E-key presses, but continues after successful production Wave1 Load into Wave2 and finishes **every** Repair material without assigning actor positions or editing domain awards.

## Native production sequence

1. Load a clean isolated `pass104-physical-repair-lifecycle-smoke-profile.json` and enter the actual Ranch game from its normal spawn.
2. Start the ordinary production countdown, identify the genuine Wave1 Load actor and physically complete all ten Load-material pickup/deposit loops, checking every E-key state change and exactly-once first-wave reward.
3. Advance the **normal GameplayRoot stage machine** through `Wave -> WaveEnd -> Intermission -> Countdown -> Wave2`, exercising the production EndWave teardown, wave-survival payout and domain wave progression. The Wave1 completion receipt must remain present.
4. Stop automatic stage-time and enemy-spawning updates during the isolated Wave2 mission acceptance. Leave the real player movement, gravity, collision, actual Repair actor, E-key handling and authoritative domain commands unchanged.
5. Use the real Wave2 Repair depot and **every** generated grounded material location (the observed Wave2 Repair production catalog requires twelve rather than Wave1 Load's ten). Walk from the current player position to the Repair depot, then physically walk to **each** material, press and release E to carry, walk back and press/release E to deposit. Accept only full-capsule-supported routes; the test must fail instead of teleporting or ignoring blocked geometry.
6. Verify that the production `Repair` actor completed only after the last deposit, that its match-scoped completion ID and the Wave1 `Load` ID are retained, and that its reward is paid once, including the correct level-up accounting and duplicate receipt no-op.
7. Persist the match result via the production session, restore the lobby, then repeat the same continuous two-wave test on fallback Manor.

The executable must emit `TWR_PASS104_MAP_FULL_REPAIR_OK` separately for Ranch and Manor, and `TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK maps=Ranch,Manor completed_wave1_loads=2 completed_wave2_repairs=2 wave2_material_deposits=<actual sum of all Repair items>` with a zero process exit code.

## Protected regressions

The new Windows preflight additionally preserves the separate original Pass103 full-Load native preflight, Pass102 ten-map first-depot ingress, Pass100 all-map first-material walking, Pass99 material ownership, Pass98 full Load/Repair E-key lifecycle, Pass96 objective floor and 15-wave simulation/persistence, Manor stair/infected/escort checks, and the audit that private original Roblox assets are not in public CI exports.

## Explicit limitations

The standard Wave1 and Wave2 stage transitions run; the **duration is accelerated** in the test rather than playing a real-time match. Enemy waves are not fought while collecting the materials. The coordinates and physics are of public fallback reconstructions, not verified original retail map geometry. No claims of a fully human-played, continuously survived two-wave match, final 1:1 art/audio/animation fidelity, or publicly downloadable game are supported by this test.

## Native discovery: production Repair uses 12 items

The first exported Windows attempts [38337855475](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38337855475) and [38337817965](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38337817965) **completed the actual two-wave in-game test**, but the *PowerShell CI acceptance expression* incorrectly pinned `material_items=10` and `wave2_material_deposits=20`. The authoritative Godot production objective actually generated **12 Repair items per map**, while Load correctly generated 10. The native game carried and deposited every item:

| Map | Wave2 Repair items | Physical Wave2 Repair walking |
| --- | ---: | ---: |
| Ranch | 12 pickups + 12 deposits | 97.0 m |
| Manor | 12 pickups + 12 deposits | 76.4 m |
| **Total** | **24 fully deposited** | **173.4 m** |

The two preceding full Wave1 Load missions also walked 163.0m and deposited all 20 Load materials, making **336.4m total movement and 44 fully deposited production materials** across both maps and waves. Both Repair completion callbacks and duplicate-reward protection passed in the actual Windows executable, and both results were committed in the isolated local session.

The acceptance workflow has been corrected to parse each actual `material_items`, `pickups` and `deposits` value; each must equal the generated count (at least ten). Its summary's total Repair deposits must equal the sum verified on both maps. This strengthens coverage and avoids assuming Load and Repair have identical item requirements.

The first runs were **CI failures due to their test matcher**, not native gameplay failures. A new fully green Windows CI result on the corrected matcher is still required for the pass's final acceptance.

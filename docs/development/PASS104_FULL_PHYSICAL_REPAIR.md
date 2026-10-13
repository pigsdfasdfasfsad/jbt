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
5. Use the real Wave2 Repair depot and the actual ten grounded material locations. Walk from the current player position to the Repair depot, then physically walk to **each** material, press and release E to carry, walk back and press/release E to deposit. Accept only full-capsule-supported routes; the test must fail instead of teleporting or ignoring blocked geometry.
6. Verify that the production `Repair` actor completed only after the last deposit, that its match-scoped completion ID and the Wave1 `Load` ID are retained, and that its reward is paid once, including the correct level-up accounting and duplicate receipt no-op.
7. Persist the match result via the production session, restore the lobby, then repeat the same continuous two-wave test on fallback Manor.

The executable must emit `TWR_PASS104_MAP_FULL_REPAIR_OK` separately for Ranch and Manor, and `TWR_SMOKE_PASS104_FULL_PHYSICAL_REPAIR_OK maps=Ranch,Manor completed_wave1_loads=2 completed_wave2_repairs=2 wave2_material_deposits=20` with a zero process exit code.

## Protected regressions

The new Windows preflight additionally preserves the separate original Pass103 full-Load native preflight, Pass102 ten-map first-depot ingress, Pass100 all-map first-material walking, Pass99 material ownership, Pass98 full Load/Repair E-key lifecycle, Pass96 objective floor and 15-wave simulation/persistence, Manor stair/infected/escort checks, and the audit that private original Roblox assets are not in public CI exports.

## Explicit limitations

The standard Wave1 and Wave2 stage transitions run; the **duration is accelerated** in the test rather than playing a real-time match. Enemy waves are not fought while collecting the materials. The coordinates and physics are of public fallback reconstructions, not verified original retail map geometry. No claims of a fully human-played, continuously survived two-wave match, final 1:1 art/audio/animation fidelity, or publicly downloadable game are supported by this test.

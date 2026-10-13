# Pass 99 — One carried material per player across simultaneous objectives

**Base:** Pass98 Windows-verified branch `twr-pass98-material-objective-completion`.

## Production change

The Pass98 key-press claim prevented collecting two materials during a *single* E press, but did not prevent an additional E press from collecting another objective's material while the player still carried the first. A two-objective wave could therefore place two items in an invisible, player-wide inventory although each mission's private `_carrying` flag allowed only one.

Pass99 introduces an explicit player-keyed carrying owner in `Pass76InteractionSafety`. Each pickup claims that slot before mutating the item list. An E press on a second objective cannot collect material until the first is deposited. Depositing releases the slot; objective removal, scene teardown and player removal clear stale claims. Weak references and queued-deletion checks guard expired Godot actors. Existing single-key-press loot arbitration, physical range and wall-occlusion checks remain unchanged.

## New checks

`tests/test_pass99_material_carry.py` protects the release and claim paths and requires the exported-native acceptance marker.

The already-real-input `--smoke-pass98` test now presses E **again** while standing at overlapping material placements after one objective has been collected. It requires both objective item totals and the single carrier state to remain unchanged; the successful run prints `TWR_PASS99_GLOBAL_MATERIAL_CARRY_OK`. This is a regression test of actual production actors under exported Windows, not a substitute for a human playthrough. Windows CI on the Pass99 branch runs that expanded acceptance alongside the existing previous tests and private-asset exclusion gate.

## Evidence and limits

Implementation and native acceptance gates are committed. Do not consider Pass99 verified until the new branch's full GitHub Windows workflow succeeds and emits the Pass99 marker. No owner-private game files are uploaded. This change does not repair missing retail meshes, maps, animation/audio, full 15-wave human movement playtesting or packaging.

# Pass 111 — Falling infected bodies without changing combat authority

**Parent:** [Pass110 verified Windows run 38356914570](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38356914570) — complete offline Godot/.NET build and full native regression pipeline, including all previous pickup, objective, gun ADS, infected and ten-map gates.

## Player-visible improvement

Before this pass, a lethal hit called `InfectedAgent.QueueFree()` immediately after rewarding a kill. The entire infected mesh vanished without a visible falling body. The owner's `videos of game.zip`, especially `infected walking and being shot.mp4`, depicts infected bodies falling and remaining on the arena floor for a short time. The video is a visual reference, **not** an exact original ragdoll system specification.

Pass111 adds `Pass111FallenInfected`: on the **same real lethal hit**, `GameplayRoot.OnInfectedDied` captures the currently visible mesh instances from the actual live `InfectedVisualAssembler` *before* the infected node is freed. It flattens their instantaneous world poses into a new collection of passive meshes (reusing the already-loaded mesh resources, material overrides and original/local source colors), tilts them down across ~0.62 seconds and leaves a brief floor-level silhouette for a total of **5.5 seconds**.

Unlike detaching/reusing the entire rig, only `MeshInstance3D` leaves survive. Scripts, collision shapes, damage/collision objects, AI, sound emitters, animation controllers, weapon hitboxes, navigation links, reward authority and inventory are NOT copied. If the owner has locally installed a recovered infected source rig, its visible static meshes/materials are reused in memory. Otherwise the existing procedural humanoid proxies are captured. **No private mesh or texture bytes are written, committed or uploaded.**

This is **not a skeletal bone-by-bone ragdoll**, not source-verifiable original character animation and not an expensive real physics corpse. The fall is an explicitly approximate single-pivot first-person effect guided by the owner's clip.

## Performance/lifecycle safeguards

- At most **16** corpse objects active simultaneously, with a maximum of **64 visible mesh elements each**. The oldest visual is retired when the cap is reached, and every corpse self-retires after 5.5 seconds. Long-distance kills farther than 70 metres do not add corpse visuals.
- Corpses are *only* children of the `GameplayRoot` and never enter the live `_infected` combat list or its `infected` group. They have no colliders and cannot block bullets, player walking, zombies, pickups, paths or objective reach checks. A late kill produces the same XP/credit bonuses, Burster spore side-effects and exactly-once logic as before.
- The existing wave-end `ClearInfected()` also clears any remaining corpse visuals; no old bodies leak to results or the next map. Snapshot counts are exposed as read-only telemetry for acceptance diagnostics.
- The original source scene has priority as *input* when installed; the corpse renderer does not replace or change the original live rig in any state.

## Windows gameplay verification

`--smoke-pass111` runs the established **Pass70 real exported-Windows gameplay** scenario: the actual first-person Glock fires through the ordinary weapon handler and physics ray, kills the Wave1 infected, spends exactly one cartridge, awards credits/XP once, and permits a real R-key reload. New acceptance asserts exactly one non-colliding corpse with 8+ passive source mesh parts appears from that real death, finishes tipping over during the player's reload, does not count as an enemy, and disappears on the genuine Wave1->Wave2 cleanup. The old Pass70 checks for weapons, wave awards, ammo reserves and round progression are left intact.

The corresponding CI step requires both `TWR_SMOKE_PASS111_INFECTED_CORPSE_OK` and the older `TWR_SMOKE_PASS70_LIVE_COMBAT_OK` emitted from the *same* exported Windows run. The Pass111 step reuses Pass70's existing **synthetic original-style Glock model/aim hierarchy fixture** in the temporary CI build and removes that fixture in a PowerShell `finally` cleanup. This preserves the old source-assembly assertions without uploading or depending on private owner weapons. The dedicated Pass111 acceptance executes **immediately after Windows export**, before the longer objective/map regressions, so failures are detected promptly. `tests/test_pass111_fallen_infected.py` covers bounds/material preservation and authority separation. All Pass103-110 and earlier stair, navigation, escort, infected, source-leak and persistence gates remain enabled for the new branch.

**Verification status:** source and native acceptance must pass Windows CI before a Pass111 native-green claim. Headless tests do not certify the original recorded ragdoll's bone shapes or graphical animation timing; manual F12 gameplay screenshot/video side-by-side remains required.

## Files

- `src/Twr.Godot/Scripts/Pass111FallenInfected.cs`: render-only corpse mesh snapshot and animated removal.
- `src/Twr.Godot/Scripts/GameplayRoot.cs`: bound live queue; snapshot invoked from actual death callback; clear on map/wave transitions.
- `src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs`: real-Glock native corpse integration check.
- `src/Twr.Godot/Scripts/Bootstrap.cs`: `--smoke-pass111` entrypoint.
- `tests/test_pass111_fallen_infected.py`: regression coverage.
- `.github/workflows/windows-build.yml`: dedicated Windows pass111 gate.

Compiled owner-private executables and original Roblox source assets are **not** released publicly from GitHub; public Actions artifact upload remains disabled.

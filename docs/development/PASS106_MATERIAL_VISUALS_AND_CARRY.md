# Pass 106 — Distinct objective item silhouettes and first-person carried props

**Baseline:** Pass105 native Windows run [38347592407](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38347592407), successful on the last tested gameplay commit. This branch builds on the existing Godot/.NET game; it is not a rewrite.

## Visible gameplay change

Previously, every material in the real Load and Repair objectives was the **same gold cuboid**, even though recovered source objective labels distinguish generators, toolboxes, propane tanks, water jugs, can packages, spark plugs, vehicle wheels and jerry cans.

`ObjectiveMaterialVisual106` now creates recognizably different **render-only** 3D shapes and colors based on those actual recovered labels. World materials, gameplay item lists and the held view all use the same model builder. New geometry consists only of Godot `MeshInstance3D` primitive components; it adds **no colliders, teleports, game authority, world mesh changes, external assets or required network traffic**.

When the player collects one material with a valid real E-key interaction, `ObjectiveRuntime` parents a reduced-size model to the actual first-person `CaptureCamera`. It is a carried material display independent of the current weapon viewmodel. The original world item is consumed, the one-item player carry slot is acquired and the status text updates through unchanged production interaction rules. The camera prop is hidden and queued for deletion on deposit, objective teardown or player death. Invalid/contended E presses cannot manufacture a carried model because the new model is created inside the existing successful authoritative pickup branch.

All visuals are **approximated from recovered names**. They are **not** evidence of authentic Roblox meshes, materials, handling animations, exact dimensions or physical grip placement.

## Acceptance

The existing exported native Windows Pass103 full Load mission and Pass104 Wave2 Repair mission test **every** physical pickup/deposit loop on Ranch and Manor with ordinary W/E input and the original game interaction and reward path. Pass106 extends the loops to verify, after each E pickup, that the real player camera has a visible, mesh-populated model whose recovered item label matches the held item. After each E deposit the camera model must disappear, including the final item that causes immediate objective teardown. A summary marker is printed:

`TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK families=Load,Repair maps=Ranch,Manor visual_pickups=<number> cleared_deposits=<number>`

In the prior verified production catalog, Load has ten items per map, and Repair generated twelve per map: **44** full pickup/display/deposit/disappearance cycles across two maps and two waves. CI asserts at least forty and exact pickup/deposit count equality rather than pinning a possibly variable Repair count.

The independent Pass105 native hostile-interaction acceptance also checks that the material model remains attached and unchanged while a real, production-spawned infected physically approaches and deals authoritative melee health damage, and that real E deposit removes the model. It must print `TWR_PASS106_UNDER_FIRE_CARRY_VISUAL_OK`.

The unchanged Pass102 ten-map spawn-route tests, Pass99 shared carrying-state test, Pass98 real E-key objective lifecycle, earlier weapon/physics/escort/stair checks and the private-asset exclusion gate remain enabled in CI for this new branch.

## Limits and remaining work

This pass builds better proxy models, not original mesh recovery. It does not implement item-specific hand IK or carry animations, and headless Godot mesh-instance tests cannot confirm aesthetic quality, self-clipping or whether the chosen camera placement occludes a particular weapon. Human-controlled graphical screenshots and weapon/carry layout review are required. Native CI compiles a Windows executable but deliberately does **not** publish public executable artifacts.

## Final verified Windows acceptance

**Pass106 is fully verified in GitHub Actions [run 38348938021](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38348938021).** The exact gameplay/source/test commit `a8dbf3015968b136ac193f20a2fd6df55c20ae0a` built the Godot/.NET Windows executable, completed **757 Python tests** (one pre-existing warning) and passed all native regression and final private-asset exclusion gates. The immediately preceding gameplay/CI commit also had a separate successful full Windows run [38348912008](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38348912008).

In the final exported Windows test, the production Repair variant randomly selected ten items per map rather than the twelve from the earlier Pass104 run. The actual acceptance reported:
- `TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK families=Load maps=Ranch,Manor visual_pickups=20 cleared_deposits=20`.
- `TWR_SMOKE_PASS106_FULL_CARRY_VISUALS_OK families=Load,Repair maps=Ranch,Manor visual_pickups=40 cleared_deposits=40`. All forty generated materials in this run were physically carried, shown on the camera and then correctly removed from the carried view.
- `TWR_PASS106_UNDER_FIRE_CARRY_VISUAL_OK family=Load carried_item=Generator visual_mesh_parts=5 survived_real_melee_damage=true disappeared_after_deposit=true`.

The successful production-spawned Wave1 hostile fixture separately recorded 4.0 points of player damage and 1.3m actual zombie closing distance while the carried material remained intact. The test controls the hostile enemy's starting lane and does not certify full natural-spawn pursuit or a human-operated full wave.

The private-owner source exclusion gate passed. **No public downloadable `.exe` was released.** CI compiles a Windows game for test execution but disables binary artifact upload.

## Next recommended pass

**Pass107:** visually render the actual first-person carry poses and representative world item models on a graphical Windows client, check the overlays against each weapon's aim view/ADS and UI/crosshair, and refine clipping, scale, lighting and source-accurate shapes using reference screenshots. Existing headless tests certify node ownership and interaction lifecycle but do not certify what appears on screen.


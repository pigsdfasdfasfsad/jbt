# Pass 110 — Physically deposited parts now rebuild a visible Repair truck

**Parent checkpoint:** [Pass109 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass109-reference-generator-repair-icon), with real Load/Repair and reference-guided generator/HUD acceptance. Pass109's broader CI status must be checked independently.

## Primary player-visible change

Previously, every Repair objective on the public-safe reconstructed maps used the same bright green **DepositTarget** cube, with no discernible Silverado/Tundra at the service bay. Material pickup and deposit worked but repairing a vehicle did not change the world.

Pass110 adds **`Pass110RepairVehicleVisual`**, a small standalone C#/Godot truck model beside the **unchanged** deposit marker, using approximately 37 primitive meshes. The approximate parked pickup has a chassis, weathered body, cabin windows, roof, pickup bed, doors/mirrors, hood, grille, head/brake lights, bumpers and tires. It is rendered only in *source-absent fallback map scenes*, never stacked onto recovered owner-source geometry. It has no collision shape, rigid/character/static physics body, combat health, navigation authority or additional interaction target.

There are **two approximate visual variants** keyed to actual recovered source repair quotas, not invented additional gameplay rules:

| Source repair quota | Game visual variant | Deposits needed |
|---|---|---|
| Small Silverado | Shorter nominal pickup | 4 spark plugs + 2 wheels + 4 jerry cans = 10 |
| Large Tundra | 15% larger nominal pickup | 4 spark plugs + 2 wheels + 6 jerry cans = 12 |

**Visual progression is bound to real E-key deposits, not timers, proximity or test injection.** Both front tires begin mounted; the pair of missing rear tires becomes visible as Wheel A/B are deposited. The four exposed engine-bay coils appear one at a time on spark plug deposits. Fuel cans progressively fill a small service gauge. When every required part is actually deposited, a small completion lamp lights up before the existing objective-completion callback removes the actor.

The current 3D positions, colors, silhouettes, installed-part sequence, 15% scaling and vehicle placement are **APPROXIMATIONS**. The owner-supplied references and surviving original game scripts establish the item/vehicle names and quotas, **not the underlying original Roblox meshes and exact part animations**. Do not call the model an asset rip or a 1:1 original Silverado/Tundra.

## Authority preservation

- `ObjectiveRuntime.BuildRepair` adds the new model **only** when `UseFallbackMaterialGrounding96` is enabled, after all existing material and ground-placement logic. The existing grounded source-neutral depot, `DepositTarget`, original W/E reach tests and `Pass76InteractionSafety` one-item carry budget remain unchanged.
- `ObjectiveRuntime.TickFillInteract` calls `PresentSuccessfulDeposit(_heldMaterialName106)` solely after its existing authoritative E/depot reach condition has succeeded. Visual code cannot complete missions, change `_deposited`, award credits, grant inventory or move the player.
- `Pass110RepairVehicleVisual` tracks successful material **names** in an ordinal `HashSet`; unknown or duplicate names cannot install twice. The model's state is gameplay-following, not gameplay-governing.
- Owner-original map/prop geometry is not published or modified. No binary source textures/images are committed.

## Native Windows regression

`--smoke-pass110` reuses the physically walked production Wave1 Load and Wave2 Repair missions on **Ranch** and **Manor**, with the real first-person player and genuine W/E input. It also runs the earlier four RMB aiming/stow checks, approximately 160+ camera-frustum cases, Pass109 world/held Generator and live Repair wrench badge checks, and existing exactly-once reward/persistence validations.

The new native acceptance checks:

1. Each real Wave2 Repair objective owns one `Pass110RepairVehicle` with ~37 render-only mesh parts, the variant matching the objective's source-recognized `RepairVariant` and required material count. No added collision objects or owner-original-geometry claims.
2. Initial vehicle state has no installed spark-plug visuals, no fuel, two missing rear wheels and no completion lamp.
3. **After every real E deposit** (not after pickup), the test reads the actual carried material name collected by E and increments only that source-named category. Each visual install count must match the real plug, wheel and fuel deposition totals, **regardless of pickup order or overlapping ground markers**. Rear-wheel visibility and completion lamp are checked as well.
4. On the final deposit the real `ObjectiveRuntime.Completed` callback captures the fully assembled model *before* the objective is freed. This is checked independently of the final reward and one-time receipt.
5. Across both maps there must be exactly two finished vehicles, and the number of visual installation checks must equal the number of physically deposited Repair materials (at least 20).

Expected native log: `TWR_SMOKE_PASS110_REPAIR_VEHICLE_OK`. `tests/test_pass110_repair_vehicle_assembly.py` adds static source/CI safety checks. Windows CI for the Pass110 branch additionally requires the old Pass109, Pass108, Pass105, Pass104, Pass103, navigation, infected, escort, save and source-exclusion smoke steps.

## Still not done

This adds truthful, playable **approximate** visual feedback for Repair, not exact original truck geometry, real car interaction physics, completion cinematic, pixel-verified matching or full fifteen-wave human survival. The next fidelity iteration should capture both variants in actual rendered Windows gameplay at close/medium distance, audit their orientation against recovered original screenshots, and compare the repair prop silhouette/HUD overlap on every playable map.

A Windows `.exe` is compiled by CI for tests, but its current public artifact upload gates deliberately remain disabled; there is no downloadable owner-private release from this pass until separately packaged.

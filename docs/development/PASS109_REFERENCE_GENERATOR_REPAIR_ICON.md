# Pass 109 — Reference-guided generator assembly and Repair objective icon

**Parent:** [Pass108 — native first-person carry ADS/frustum acceptance](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass108-native-carry-ads-frustum), whose [Windows CI run 38352406878](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38352406878) completed successfully. This is an incremental rendering pass, not an architecture rewrite.

## Actual supplied visual references (owner-held, not published)

- `twr_pass108_references/Generator-Market.png`: dark interior, beige rectangular industrial generator, black low skid/base, vented front service panel, compact red/green indicator, dark top exhausts and surrounding equipment. **The screenshot depicts a generator in an environment; it does not prove that a carried Load generator has the exact same source mesh or dimensions.**
- `twr_pass108_references/RepairObjective.png`: off-white circular icon with dark outer rim, diagonal open-end wrench and a ring at the bottom of the handle.
- `twr_pass108_references/50Cal-Hip.png`, `50Cal-ADS.png` and `WeaponHUD.png` are retained as *future* weapon/HUD comparison references. This pass does not falsely claim to reproduce those weapon pictures.

These original reference image bytes are **not** committed to Git, bundled in a public build, or used as an unverified resource ID. All geometry and icon strokes are locally authored approximations.

## Implemented render-only changes

1. `ObjectiveMaterialVisual106.Create("Generator", ...)` now builds a 23-piece primitive industrial generator instead of the earlier five-part cuboid. It uses a pale casing, black skid/base and feet, front service door/seam, control console, display, red/green status indicators, five intake vents, lifting rail and supports, twin cylindrical exhaust pipes and top caps. The existing material name, E-key acquisition/release, item carry slot, mission reward authority, world placement and first-person cleanup are unchanged.
2. The new `Pass109RepairObjectiveBadge` renders the supplied Repair icon's circular/wrench visual language with native Godot `Control._Draw` shapes. There is no texture download or external asset dependency. `GameplayHud` anchors the 42px badge alongside the objective panel; mouse clicks pass through it.
3. Crucially, the badge is driven by the **actual active Repair objective actors** in `GameplayRoot`, NOT by whichever one of 1–2 objective status callbacks happened to run last. It appears when a Repair mission spawns and clears when that actor completes or the wave changes. Other objectives may continue updating the text without hiding an active Repair badge.

## Verification / acceptance

- `tests/test_pass109_reference_visuals.py` enforces component names, absence of new colliders/external textures, active-objective HUD authority, and an exported native Windows acceptance marker.
- `--smoke-pass109` uses the existing real first-person W/E Pass103/104 production objective harness, not teleported inventory grants. Ranch and Manor Wave1 Load **world generators** must contain each expected real render component; the first actually picked-up Load material must become the same correctly structured *camera-held* generator. Both Wave2 Repair missions must display then clear their actual active Repair HUD icon. Every material must still be carried and deposited once, and previous Pass108 genuine RMB ADS transitions plus 4 viewport full-mesh checks per pickup still run.
- The native gate expects **two validated generator world models, two camera-held generators, two real Repair-badge appearances, two badge removals, four RMB stows, at least 160 viewport/item tests and at least forty normal physical pickups/deposits**. CI also retains all previous longer map, stair, infected, escort, persistence and owner-private-exclusion regressions.
- The public GitHub workflow continues to build Windows in temporary CI, with release artifact upload deliberately disabled. A private complete Windows distribution has not been delivered.

## Fidelity boundary

The new geometry uses screenshot-guided primitive proportions and colors, not original Roblox triangle meshes, materials, CSG, lightmaps, model rigs or exact measurements. CI evaluates node geometry and gameplay state in **headless** exported Windows execution, not rendered-pixel appearance. The Repair icon is a new procedurally drawn counterpart rather than the uploaded image.

The next graphical phase must collect F12 screenshots on a real Windows display, compare image pairs at identical camera positions/FOV and weapon, refine the scaled prop silhouette and HUD positioning, and benchmark draw calls/frame times. The surviving owner archives do not establish full 1:1 map geometry for the remaining nine maps.

## Repository

- [Pass109 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass109-reference-generator-repair-icon)
- [Pass109 Windows CI checks](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass109-reference-generator-repair-icon)

CI status should be checked against the **latest source commit**. Source existence is not a native compile or graphical approval claim.

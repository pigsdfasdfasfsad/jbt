# Pass 108 — Whole-mesh carry view framing and actual RMB ADS preservation

**Baseline:** [Pass 107 camera presentation](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass107-carry-aim-safe-presentation), which placed Pass106 objective props at a lower-left viewport-aware hip anchor and stowed them while aiming. No item authority, collision or original asset data is changed here.

## Implemented gameplay presentation

The Pass107 location audit considered only the *root* transform, so a tall/cylindrical generator, propane tank, wheel, toolbox or large repair jerry can could potentially reach outside the viewport even if the root was safe. Pass108 adds `Pass108CarryFrustum.Measure`. It transforms **all eight AABB corners of each real direct child MeshInstance3D** from the model's camera-local basis and measures their projected horizontal/vertical fractions at the actual weapon camera FOV and viewport aspect. A model only passes `EntirePropSafe` if the entire primitive assembly sits on screen, away from the crosshair region and screen borders. For narrow windows `Fit` reduces visual scale in bounded steps from 0.40 to no smaller than 0.34; there is no world-item, collider or interaction change.

The actual `ObjectiveRuntime` applies this check to the genuine `Pass106CarriedObjectiveMaterial` model after each successful E press and during each rendering frame. `Pass108HeldSilhouetteSafe` is exposed as read-only native acceptance telemetry. The original on-ground source-name material mesh is not resized or moved by this camera-only code.

## Actual first-person RMB check

Headless Windows runners cannot acquire a cursor, and older smoke tests did not exercise ADS input. Only when the executable is **explicitly launched with `--smoke-pass108`**, the player's normal UpdateAim accepts a parsed **right mouse button** event without requiring OS cursor capture. Everything else — equip restrictions, FOV, weapon aim, the item carrying/interaction budget and player simulation — uses production logic. The normal user game has unchanged cursor-capture requirements.

`Pass103PhysicalLoadLifecycleAcceptance` is reused for the real Wave1 Load and Wave2 Repair mission on both Ranch and Manor. On the **first successfully picked-up item** in each family/map combination, the new test:

1. Verifies a complete camera-framed carried mesh exists after normal physical W/E interaction.
2. Sends an actual RMB press; verifies the player begins aiming and **only the prop visual** hides, with the carried item name, pending item count and deposit count unchanged.
3. Sends RMB release and checks the unchanged item model reappears entirely within the hip-camera frame.
4. Walks back to the real depot and deposits with E, then continues the full original mission, reward, replay/persistence and subsequent wave checks.

The test requires **four real aim stows** (Load/Repair × Ranch/Manor) and **at least forty** genuinely physically collected/cleared material cycles. Expected success marker: `TWR_SMOKE_PASS108_CARRY_ADS_FRUSTUM_OK`. Existing Pass78/95/96/98/103/104/105/106 controls continue running in Windows CI, along with the source/private-asset exclusion gate. The separate Python assertions are in `tests/test_pass108_carry_ads_frustum.py`.

## Verification boundary

This is an **analytical mesh-frustum and Godot headless input test**, not a graphical screenshot/pixel comparison. It can catch offscreen primitive corners, center-reticle intrusion, missed aim-stow behavior and material-authority regressions. It cannot prove the item *looks* like the original Roblox prop, that gun meshes/sight lenses never visually overlap, that shadows and textures are correct, or that a human can survive fifteen real-time waves. Future graphical screenshot reference checks and authentic mesh/texture recovery remain required. No public Windows executable is uploaded: source project policy keeps compiled binaries and owner-private content outside GitHub release artifacts.

## Build/report links

- [Pass108 source](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass108-native-carry-ads-frustum)
- [Pass108 Windows CI runs](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass108-native-carry-ads-frustum)
- Pass106 verified baseline: [Windows run 38348938021](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38348938021)

At initial commit, Pass108 CI was running. Do not describe this branch as native-verified without checking the exact Actions run conclusion and matchable emitted markers.

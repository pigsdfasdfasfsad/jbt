# Pass 113 — Ground-contact settlement for animated infected corpses

**Parent:** [Pass112 articulated death pose](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass112-articulated-infected-death-pose), [fully green Windows gameplay/regression workflow 38422702922](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38422702922).

## Player-visible result

Pass111/112 preserved the visible infected's existing 3D mesh and materials and animated four R6 shoulder/hip groups on death. But the dead actor was tilted in place and translated down only ~0.16m, causing the approximate body to hover over the floor or intersect solid surfaces when the victim's actual world centre stood above the ground.

Pass113 makes that **same render-only corpse land against nearby physical terrain**:

1. At death the existing real `InfectedAgent.Died` event snapshots the real, *death-frame posed* infected mesh tree. It still does not duplicate collision/AI/ragdoll scripts or modify kill rewards.
2. In the next **physics tick**, the `Pass113CorpseGroundContact` helper fires exactly **one** downward ray from 0.20m above the victim centre to 4.2m below it, using world collision layer 1. It accepts only a real `StaticBody3D` with upward ground normal Y at least **0.58**; moving infected, players, triggers, wall faces and missing terrain cannot be used as a fake floor. The accepted world hit's height and normal are retained for read-only diagnostics.
3. During the existing 0.62s body fall, eight corners of each visible mesh AABB are transformed through their **current animated source-pivot pose**. This observes feet, rotated arms, heads, and any recovered infected accessories rather than assuming all zombies have a standard height. Nonfinite/badly oversized positions outside an 8m radius are ignored.
4. The minimal vertical body correction needed to place its lowest visible point **0.035m above the real floor** is clamped to ±2.75m. Correction is blended smoothly through the last ~58% of the fall. At the end, the measured clearance error is expected to be at most 0.045m in flat native acceptance.
5. If terrain is absent, too far below, too steep, dynamic or invalid, the old safe animation runs without any made-up ground, teleporting, hidden colliders or reward changes. In the final 0.7s of the existing lifetime the previous cleanup/short fade behaviour remains unchanged.

This is **a contact-stabilized visual pose**, not per-bone physical ragdoll, realistic impact forces, source Roblox body constraints or exact animation reconstruction. It measures the local hit beneath the corpse, not a multipoint body-plane and slope mesh solver. A moving platform is intentionally ignored.

## Hard limits and authority

- Existing **16 active corpses**, **64 mesh leaves/corpse**, **5.5-second lifetime**, **70m kill-distance cap**, dying/wave cleanup, visibility and collision budget remain unchanged.
- One terrain raycast per corpse in `_PhysicsProcess`; the few visual mesh-corner transforms run only during settling or the 5.5s visible lifetime. This is bounded to a maximum 64 meshes/corpse.
- No new `StaticBody3D`, `CollisionShape3D`, `RigidBody3D`, `CharacterBody3D`, `Area3D`, `NavigationAgent3D` or physics layers are created.
- No damage, loot, credits, XP, wave timers, source asset packs, private meshes/textures, gun behaviour or pathfinding logic is changed. The new static query **reads** world terrain collisions only.

## Native Windows acceptance

`--smoke-pass113` extends the exact [Pass70](https://github.com/pigsdfasdfasfsad/jbt/blob/twr-pass113-ground-contact-corpse-settling/src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs) isolated exported-Windows first-person test. The real player fires the Glock with its normal camera ray, cartridge and cooldown, actually kills the physically placed infected, receives its real XP/credits once, and reloads with the ordinary R key. The original synthetic flat *physics platform* stands in for a real floor in CI — **this is test geometry, not an original Laboratory source platform**.

The native gate expects a captured 8–64 mesh noncolliding corpse with all four animated R6 limb groups; the subsequent physics tick must find the nearby static floor. After the normal reload, a visible correction of at least 2cm and at most 2.75m should have occurred, the ground normal must be upward/walkable, and the corpse's visible lower-mesh corner must have a floor clearance error at most **4.5cm**. Then the original Wave1-to-Wave2 and body-clearing sequence must succeed.

The command outputs **four independent markers in the same actual run**:

- `TWR_SMOKE_PASS113_CORPSE_GROUND_OK` — measured ground normal, correction and contact error.
- `TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK` — source R6 limb groups moving independently.
- `TWR_SMOKE_PASS111_INFECTED_CORPSE_OK` — non-colliding bounded temporary corpse.
- `TWR_SMOKE_PASS70_LIVE_COMBAT_OK` — actual Glock shot, ammo, XP/credits, reload, wave.

The Windows CI PowerShell check parses the numeric contact measurements and rejects any nonzero exit, invalid ground normal or insufficient/oversized correction. A temporary **synthetic** source weapon/aim hierarchy is created with existing `make_pass68_aimcf_fixture.py` and deleted in `finally`; synthetic fixture parsing is restricted to explicit smoke modes. All historic Pass101–112, ten-map, escort, save/progression, and owner-source privacy tests remain on the branch.

## Files and progress

- `src/Twr.Godot/Scripts/Pass113CorpseGroundContact.cs` — fixed-budget solid-floor probe and mesh support points.
- `src/Twr.Godot/Scripts/Pass111FallenInfected.cs` — real one-time physics probe, smoothly applied source-pose contact correction, measured landing telemetry.
- `src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs`, `Bootstrap.cs` — native real kill, reload and grounded-landing assertions.
- `tests/test_pass113_corpse_ground_contact.py` — source safety, ground measurement and native CI contracts.
- `.github/workflows/windows-build.yml` — dedicated Windows acceptance while retaining preceding pass regressions.

[Pass113 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass113-ground-contact-corpse-settling) · [Pass113 CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass113-ground-contact-corpse-settling)

**Verification boundary:** Source commits and headless geometry tests do not establish screenshot-level Roblox fidelity, true limb physics, slope-accurate contact, or full 15-wave manual survival. Do not call this pass native-verified until its **exact newest Windows source commit** has passed the new native gate and the full regression workflow. The compiled public download artifact is still deliberately disabled to avoid distributing owner-private assets.

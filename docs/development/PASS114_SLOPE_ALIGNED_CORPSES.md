# Pass 114 — Sloped-terrain fall alignment and support-plane contact

**Verified parent:** [Pass113 ground-contact corpse](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass113-ground-contact-corpse-settling), whose dedicated exported-Windows Glock/floor native acceptance passed in [Actions 38443944987](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38443944987). Pass113's complete long regression must be verified separately.

## Visible feature implemented

Pass113 settled a short-lived infected body on the **horizontal Y-height** of the terrain under the victim. This works on flat ground, but on ramps and hills a tipped corpse could look as if its limbs penetrated the uphill portion or hung above the downhill part: the old solver considered only the minimum **world Y** corner, not the terrain's actual surface plane.

Pass114 adjusts that precise shortcoming, still using only the **existing visual snapshots from the genuine live infected actor**:

1. The Pass113 one-time physics ray is retained. A result is valid only from real `StaticBody3D` geometry (not the player, moving infected, trigger, decoration or wall) with walkable-normal Y at least 0.58. The normal is used as the slope reference. Neither the infected physics actor nor the terrain collider is changed.
2. `Pass114CorpseSlopeSupport.TiltToNormal` gently aligns the fallen body's orientation toward the measured collision surface, leaving its original live orientation and Pass112 arm/leg pivot animation intact. The transition is spread across the fall, the tilt cannot exceed **20°**, and normals within **3°** of vertical are ignored to avoid distracting animation on slightly imperfect map floors.
3. For an actual slope, the support solver transforms **all eight bounding corners of every visible source mesh** (including source accessories and animated limbs) into world coordinates, computes signed distance from the genuine collision plane, and derives a minimum bounded vertical translation to maintain **3.5cm** plane clearance.
4. That correction is blended toward the last half of the ordinary 0.62s fall. The strict **±2.75m maximum** remains, and the solver reports whether the final support-plane clearance error is under **4.5cm**.
5. Flat, missing or invalid floor information uses the previous verified Pass113 method, retaining its first-person and old game-map behaviour. The 16-body/64-visible-mesh budget, 5.5s lifetime, no-collider property, death and wave cleanup remain exactly as before.

This is **source-rig-compatible 3D posing**, not original Roblox Motor6D or BallSocket physics; no authentic skeletal joint constraint data, original exact model sizes or registered impact forces are claimed. A single physics-ray local plane cannot guarantee correct resting contact with terrain that changes steepness across a body. No private asset geometry or original screenshot bytes are committed.

## Headless Windows acceptance

The new `--smoke-pass114` **does not fake a death**. It reuses Pass70's authentic first-person Glock left-click, actual camera physics ray into a live Wave1 infected, one spent cartridge, exactly-once credit/XP reward, R-key reload, and Wave1-to-Wave2 transition. It adds one temporary **synthetic static inclined platform, 16° around the world Z axis**, centered below the real shot victim. This is exclusively a test of a real collision shape; normal game terrain, floor and player positions remain untouched.

On completing the real reload, the corpse must still:
- Be a single 8–64-part actual-mesh, noncolliding body with four working source-name R6 arm/leg pivots.
- Probe a slope with `normal_y` between **0.90 and 0.99** (16° incline gives about 0.961); apply **0.14–0.349 radians** of surface-dependent pose tilt.
- Have its actual **lowest visible mesh corner** within **0.045m of the inclined collision plane** at the end of the fall, retaining the previous bounded contact correction and no original source data.
- Produce the same four previous native success markers `TWR_SMOKE_PASS70_LIVE_COMBAT_OK`, `TWR_SMOKE_PASS111_INFECTED_CORPSE_OK`, `TWR_SMOKE_PASS112_ARTICULATED_CORPSE_OK`, `TWR_SMOKE_PASS113_CORPSE_GROUND_OK`; and additionally `TWR_SMOKE_PASS114_SLOPE_CORPSE_OK` with formatted numeric measurements.

The real exported-Windows Godot/.NET acceptance fails on nonzero process exit, absent markers, overly horizontal/upside-down terrain, excessive tilt or support gap. All preexisting 10-map material, Escort, gun and zombie, multiwave gameplay, source-privacy and regression gates remain enabled. A temporary synthetic gun/aim fixture is created only inside CI and removed in `finally`; synthetic original-style rigs remain disallowed on an ordinary user game launch.

## Changes

- `src/Twr.Godot/Scripts/Pass114CorpseSlopeSupport.cs` — bounded terrain normal tilt and dynamically measured source mesh-to-plane distance
- `src/Twr.Godot/Scripts/Pass111FallenInfected.cs` — smoothly apply the surface orientation and plane-based contact, preserve Pass113 fallback
- `src/Twr.Godot/Scripts/Pass70LiveCombatAcceptance.cs` and `Bootstrap.cs` — actual weapon/shooting/reload on isolated 16° static test ramp
- `tests/test_pass114_slope_corpse.py` — code/CI/authority regression checks
- `.github/workflows/windows-build.yml` — pass-specific native slope gate and inherited all-pass tests

[Pass114 development branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass114-slope-aligned-corpse-settling) · [Pass114 Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass114-slope-aligned-corpse-settling)

## Verified native Windows checkpoint

The new **dedicated Pass114 inclined-floor acceptance passed** in [Windows Actions run 38446320183](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38446320183), built with gameplay and testing code commit `d5e14be5b97a998e673adb0f33561176abe02961`. The same job also passed Python content/domain tests, the Godot/.NET exported Windows executable build and the Pass111 actual Glock kill, Pass112 four-limb fall and Pass113 flat-ground test before reaching Pass114. The Pass114 native test explicitly required a physical 16° slope, true rigid terrain normal, measured 0.14–0.35 radians of pose alignment and final body-to-plane error no greater than 0.045m; its PowerShell native gate concluded successfully.

Historical multi-map and long-running survival/navigation regressions were still executing at the time of this note. Thus the **dedicated Pass114 native gate is verified**, but this does not in itself establish the conclusion of the entire Windows workflow or source screenshot fidelity.

**Status boundary:** A source commit is not a verified build. The dedicated Pass114 native test and entire long-running CI suite must complete before calling the pass fully Windows-verified. Source-based headless checks do **not** prove rendered-pixel fidelity or a real human 15-wave victory. No distributable `.exe` is currently attached; owner-private assets are excluded from public releases.

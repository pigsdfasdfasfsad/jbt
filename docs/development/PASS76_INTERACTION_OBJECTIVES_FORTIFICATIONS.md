# Pass 76 — Interaction, Objectives, Supply Drops and Fortification Physics

**Branch:** `twr-pass76-interaction-objective-fortification`  
**Verified parent:** [Pass75 native weapon combat CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38206918852).

## Implementation

1. **Physical E-key pickup resolution.** `Pass76InteractionSafety` raycasts from the real player's camera to a pickup within 2.5m, using solid world collision including activated source-server layers. `PickupActor` registers in a runtime pickup group; only the nearest unobstructed item responds to one E press. Stable instance-ID tie-breaking prevents all eight closely-spaced helicopter drops from disappearing simultaneously.
2. **Load / Repair material handling.** `ObjectiveRuntime.TickFillInteract` now uses the same physical wall visibility for grabbing a 10/12-item world material and for depositing at the base. Remaining objective domain logic, reward authority and source-recovered item counts are unchanged.
3. **Fortification building safety.** `Pass76PlacementSafety` performs a real downward Godot floor ray, walkable normal validation, and a standing free-space `BoxShape3D` query at the desired hammer placement. The existing authoritative inventory `ConsumeItem` occurs *only after* the placement is valid. Hammer mode and swung placement stop on player death. Appropriate errors are displayed when a position lacks a valid floor or intersects solid geometry.
4. **50 Cal hit detection and death teardown.** `FortificationActor` now raycasts against infected collision layer 2, normal world layer 1 and optional source-server wall colliders rather than silently firing through zombies using the default layer-1 ray. Existing bullet penetration/headshot/heavy-hitter rules are preserved. Mounted operators dismount and firing stops on death.
5. **Clap Bomb / Jack balance and real walls.** Recovered original script data specifies 420 damage / 25 **stud** radius for Clap Bomb, 375 damage / 35 **stud** radius for Jack. These are converted with the common `RobloxUnits.Distance` factor before the existing perk multiplier, avoiding a roughly 3.57x inflated blast radius. Bomb triggers, damaged infected and tankers now use world line-of-sight via Pass75 blast occlusion; close Barbed Wire cannot damage through a wall.
6. **Physical supply pallet landing.** A descending `SupplyDropRuntime` ray samples actual walkable map collision and excludes the source player, landing 23cm above contact to avoid embedding the pallet in the floor. Missing geometry falls back to the existing configurable `GroundY` value. Its landed callback fires once and still drives the original four regular + four fortification supply-slot spawning from `GameplayRoot`.

## Exported Windows native acceptance — verified

A new `--smoke-pass76` test uses production Ranch/Manor `GameplayRoot` scenes and a new isolated offline profile. Ranch covers E-key pickup contention across multiple world items; occlusion at a synthetic wall; actual Load material pickup/deposit; invalid hammer placements that cannot consume an item; valid authoritative Clap Bomb placement and a blocked/visible radius-correct explosion with normal rewards. Manor covers mounting and firing a genuine 50 Cal actor into an infected collision body, credit rewards, physical supply pallet landing on an elevated collider, one-shot landing callback and forced turret dismount after player death.

This scenario deliberately uses synthetic raised platform and blocker fixtures, **not** recovered retail map collision. It is **not** a full human-driven 15-wave game, source-original item art, or all-map objective parity test. No owner-original archives or saved profiles are included in public CI.

**Verified:** [successful GitHub Actions Windows run 38208475688](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38208475688), built from gameplay commit `4c0ef8223a581bd91156a235edfd18c760d23ff8`.

- **622 Python regression tests passed**, with one inherited XML parsing FutureWarning.
- Godot/.NET standalone Windows executable compiled with **zero errors**.
- Ranch `TWR_PASS76_RANCH_INTERACTIONS_OK`: single press one pickup, solid-wall pickup refusal, successful next-press pickup, unobstructed Load material pickup and 1/10 deposit, blocked hammer placement with original inventory retained, successful grounded placement, solid-wall-blocked Clap Bomb, visible-infected kill and credit award, and out-of-stud-range tanker preservation.
- Manor `TWR_PASS76_MANOR_EQUIPMENT_OK`: actual mounted 50 Cal first-click bullet hitting a physics infected body and awarding credits, physical pallet landing on a raised solid floor with exactly one callback, and automatic turret dismount on fatal damage.
- Final `TWR_SMOKE_PASS76_OBJECTIVE_FORTIFICATION_OK` marker plus the owner-private content exclusion gate passed. CI uses only reconstructed public fallback content and temporary synthetic test geometry.

### Failures resolved during the implementation

1. Earlier native Ranch acceptance found that nearest-item ordering alone allowed the second pickup to collect during **the same held E press** after the first actor queued deletion. A per-player press budget, reset by the living player physics tick, fixed it.
2. Hammer input on headless/native Windows required an immediate first-click path in the existing production `_UnhandledInput` method. Valid floor placement then succeeded and authoritative inventory decreased exactly once.
3. Mounted 50 Cal originally relied entirely on held-mouse polling, resulting in zero shots during the native test. It now accepts discrete LMB through the same production weapon method and shares its existing cooldown to avoid duplicate shots.

### Unverified fidelity

These are functional engine fixes and synthetic-map interaction regressions, not recovery of the original Roblox models, full objective set progression on every retail map, authentic supply aircraft animation or a human-controlled complete 15-wave match. The workflow does not publish owner-private original content.

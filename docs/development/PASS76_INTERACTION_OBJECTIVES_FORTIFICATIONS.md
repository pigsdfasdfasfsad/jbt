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

## Exported Windows native acceptance (pending)

A new `--smoke-pass76` test uses production Ranch/Manor `GameplayRoot` scenes and a new isolated offline profile. Ranch covers E-key pickup contention across multiple world items; occlusion at a synthetic wall; actual Load material pickup/deposit; invalid hammer placements that cannot consume an item; valid authoritative Clap Bomb placement and a blocked/visible radius-correct explosion with normal rewards. Manor covers mounting and firing a genuine 50 Cal actor into an infected collision body, credit rewards, physical supply pallet landing on an elevated collider, one-shot landing callback and forced turret dismount after player death.

This scenario deliberately uses synthetic raised platform and blocker fixtures, **not** recovered retail map collision. It is **not** a full human-driven 15-wave game, source-original item art, or all-map objective parity test. No owner-original archives or saved profiles are included in public CI.

**Verification:** pending actual C# Windows compile, Python regressions, native map markers, and owner-private asset exclusion.

# Pass 78 — Reconstructed Manor Three-Flight Bidirectional Physics Traversal

**Branch:** `twr-pass78-manor-bidirectional-stairs`  
**Verified parent:** [Pass77 Windows gameplay CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38212138620) — 627 regressions and native Front Hall player/infected ascent.

## Implementation

**1. Downstairs fallback navigation.** The Pass73/77 collision-sampled graph now offers a guarded lower-floor pursuit branch restricted to fallback Manor and its three reconstructed collision-ramp records. A descending route is permitted only when the player is physically downstairs, the infected is above, and a specific flight passes seven physical floor-normal/height samples along the actual Godot ramp plus wall-free sight lines at both staircase endpoints. The new route returns foot-height `UpperFeet` and 12 descending sampled ramp sections, finishing at the downstairs target. It uses the nearest downhill section when an infected is already on a ramp, preventing 0.75-second replans from sending the zombie backwards uphill.

**2. Correct downstairs chase and contact.** `InfectedAgent` previously stopped moving in melee range when directly above a ground-floor player, and skipped a downhill waypoint once within 1.58m in XY. Pass78 treats floors separately and requires 0.35m planar/0.18m vertical alignment on descending flight waypoints before progressing. Ordinary `MoveAndSlide` and existing Godot collision rules remain authoritative, with no scripted jump or position teleport.

**3. All three flights have native gameplay acceptance.** `Pass78ManorBidirectionalStairAcceptance` creates an ordinary reconstructed Manor `GameplayRoot`, verifies `FrontHallUpper`, `WestWingUpper` and `EastWingUpper`, then traverses each using W to physically climb, S to physically descend, a real infected to climb to a balcony-targeted living player, and the SAME infected to return down to a relocated downstairs player. Twelve independent per-flight markers and one final marker are mandatory. The test uses an isolated profile and no private owner-original assets; it does not assign any infected position or velocity during movement.

**4. Fidelity boundary.** The recovered exact retail Manor stair meshes and true collision layout are not present in this public fallback branch. Real source map geometry remains owner-private and deliberately unmodified. The test covers only all three reconstructed stair runs under controlled navigation targets, not arbitrary upstairs routes across the complete building, 15 human-played waves, or a user-ready private-asset Windows executable.

## Verified Windows acceptance

**Verified:** [GitHub Actions run 38213401718](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38213401718) from gameplay commit `936e1fb1b615acf80ffd5a33aee9915e760ccb48`.

- **632 Python regressions passed** (one inherited XML parser FutureWarning).
- Godot/.NET exported Windows build passed with **0 compiler errors**.
- **West Wing:** player up 2.78m, player down 2.69m, infected up 2.70m, infected down 2.55m.
- **East Wing:** player up 2.78m, player down 2.69m, infected up 2.70m, infected down 2.39m.
- **Front Hall:** player up 2.65m, player down 2.55m, infected up 2.62m, infected down 2.50m.
- All twelve markers `TWR_PASS78_PLAYER_UP_OK`, `TWR_PASS78_PLAYER_DOWN_OK`, `TWR_PASS78_INFECTED_UP_OK` and `TWR_PASS78_INFECTED_DOWN_OK` appeared with the matching three distinct stair names. The final `TWR_SMOKE_PASS78_BIDIRECTIONAL_STAIRS_OK` marker and owner-private asset exclusion also passed.
- Existing scripted ten-map `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` is **domain simulation**, not a full player-controlled survival run.

**Scope:** Production reconstructed Manor actors and genuine Godot physics, but the geometry is still APPROXIMATE. No proof of exact original retail stair positions/parts, arbitrary upstairs route planning, other-map stair traversal or a distributable complete offline EXE is asserted.

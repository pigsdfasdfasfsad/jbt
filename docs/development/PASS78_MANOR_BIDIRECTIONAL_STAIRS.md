# Pass 78 — Reconstructed Manor Three-Flight Bidirectional Physics Traversal

**Branch:** `twr-pass78-manor-bidirectional-stairs`  
**Verified parent:** [Pass77 Windows gameplay CI](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38212138620) — 627 regressions and native Front Hall player/infected ascent.

## Implementation

**1. Downstairs fallback navigation.** The Pass73/77 collision-sampled graph now offers a guarded lower-floor pursuit branch restricted to fallback Manor and its three reconstructed collision-ramp records. A descending route is permitted only when the player is physically downstairs, the infected is above, and a specific flight passes seven physical floor-normal/height samples along the actual Godot ramp plus wall-free sight lines at both staircase endpoints. The new route returns foot-height `UpperFeet` and 12 descending sampled ramp sections, finishing at the downstairs target. It uses the nearest downhill section when an infected is already on a ramp, preventing 0.75-second replans from sending the zombie backwards uphill.

**2. Correct downstairs chase and contact.** `InfectedAgent` previously stopped moving in melee range when directly above a ground-floor player, and skipped a downhill waypoint once within 1.58m in XY. Pass78 treats floors separately and requires 0.35m planar/0.18m vertical alignment on descending flight waypoints before progressing. Ordinary `MoveAndSlide` and existing Godot collision rules remain authoritative, with no scripted jump or position teleport.

**3. All three flights have native gameplay acceptance.** `Pass78ManorBidirectionalStairAcceptance` creates an ordinary reconstructed Manor `GameplayRoot`, verifies `FrontHallUpper`, `WestWingUpper` and `EastWingUpper`, then traverses each using W to physically climb, S to physically descend, a real infected to climb to a balcony-targeted living player, and the SAME infected to return down to a relocated downstairs player. Twelve independent per-flight markers and one final marker are mandatory. The test uses an isolated profile and no private owner-original assets; it does not assign any infected position or velocity during movement.

**4. Fidelity boundary.** The recovered exact retail Manor stair meshes and true collision layout are not present in this public fallback branch. Real source map geometry remains owner-private and deliberately unmodified. The test covers only all three reconstructed stair runs under controlled navigation targets, not arbitrary upstairs routes across the complete building, 15 human-played waves, or a user-ready private-asset Windows executable.

**Status:** implemented; full Python/Windows/native acceptance pending.

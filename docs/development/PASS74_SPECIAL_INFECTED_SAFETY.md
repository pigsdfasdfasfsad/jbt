# Pass 74 — Special Infected Attack Safety and Native Combat Validation

**Source branch:** `twr-pass74-special-infected-safety`  
**Verified baseline:** [Pass73 exported Windows build](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38202837479).

## Implemented changes

- **Burster direct blast wall occlusion.** Previously `SpawnBursterHazard` checked only radial distance, so its instant 27.5-damage blast could hit players on the far side of a solid wall despite its spore cloud respecting collision. `Pass74SpecialInfectedSafety.CanHitPlayerWithBlast` checks world collision rays (including optional source server walls), player exclusion and original 20-stud radius converted to Godot metres. Gas mask still prevents the recurring gas cloud, **not** the direct blast.
- **Supported Bolter jump landing.** Before launching its existing fixed-direction physics leap, Bolter samples a real walkable floor at the target's feet using a world physics ray, with bounded contact-height tolerance and exclusion of attacker/target collision bodies. Unsupported airborne/void landings are rejected with a short retry delay while ordinary steering continues. The leap uses range-bounded 0.24–0.55 second travel time at its original reconstructed 28m/s planar speed to reduce close-range overshoot, retains the existing 1.0-second recovery and physics collision, and records starts, contacts and invalid landing rejections for tests.
- **Inactive/dead special actors stop acting.** A fatal enemy no longer issues attacks in the defer-free frame. Bloater volley attempts are counted for diagnostics.
- **Native Windows acceptance on two actual map gameplay scenes.** Ranch physics checks real Bolter launch/contact behind/removing synthetic walls and each of the eight catalogued infected types' native `ApplySlow` resistance. It verifies Riot melee resistance and Bloater fire vulnerability. Manor checks Burster direct blast blocked/unblocked by solid walls, cloud gas immunity after activating a mask, Bloater's solid-wall firing gate, a swept SporeProjectile hitting a wall without penetrating to the player, and a visible Bloater projectile harming the player. Domain health and bonuses remain production authoritative.
- A separate `--smoke-pass74` profile saves to `user://pass74-special-infected-smoke-profile.json`, never the owner's normal `profile.json`. CI exports the executable and requires distinct Ranch/Manor plus final native markers and owner-private content exclusion.

## Accuracy and safety limits

These are **reconstruction safety corrections**, not proof of the original game's exact attack trajectories or timing. The synthetic test floor/walls are not playable original Manor/Ranch geometry. The eight variant multipliers are supported by recovered source metadata; reconstructed creature models/rigging, special animations, retail all-map navigation, multiplayer and complete 15-wave manual runs remain unfinished.

**Current status:** implemented; Python and exported Windows Godot validation pending.

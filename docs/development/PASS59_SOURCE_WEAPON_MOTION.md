# Pass 59 — Original Weapon Fire Timing and Sprint Translation

Branch: `twr-pass59-source-weapon-motion`
Base: verified Pass58 branch `twr-pass58-original-weapon-silhouettes`

This pass uses the existing normalized owner-derived weapon records in
`Content/weapons/entries/*.json`. No new private assets, network fetch, or
Roblox services are needed.

## Implemented
- Original `animations.Fire` and `animations.AimFire` first-step weapon
  translation and outward/return timing, per weapon, when represented by
  bounded literal `CFrame.new(x,y,z)` source expressions.
- Aiming uses the **AimFire** profile rather than the hip-fire profile.
- Original `Stats.SprintCF` literal translation is blended into the Godot
  first-person weapon position while moving/sprinting.
- Invalid, missing, or dynamic Luau expressions use existing approximation.
  No scripts are evaluated or downloaded.
- The viewmodel's `Fire(bool aiming)` argument affects rendering only.
  Shot damage, player aim direction, projectile range, recoil authority,
  ammo, credits, purchases and progression are unchanged.

## Exact representative source verification
The source normalized Glock 17 record specifies:
- Fire: `CFrame.new(0,0,0.1)` over 0.075 seconds, return over 0.15s.
- AimFire: `CFrame.new(0,0,0.05)` over the same two intervals.
- Converted under the existing 0.28-meter-per-stud engine scale:
  hip 0.028 meters; aimed 0.014 meters.

The exported Windows `--smoke-pass59` acceptance check asserts these
values in actual C# runtime parsing, observes the original-viewmodel
position after a shot, and checks that it resets.

## Important remaining limitations
Source Fire rotations, dynamic AimCF sight positioning, source actor-arm
poses, 3D original MeshPart triangles, original held weapon animations,
sprinting angular transforms, and player-controlled 15-wave validation
are **not** implemented by this pass. The source sprint literal is a
translation-only approximation. Do not claim 1:1 weapon animation parity.

## Validation
New regression: `tests/test_pass59_source_weapon_motion.py`.
Windows CI branch now includes a dedicated native exported-game smoke.
CI retains public artifact upload disabled and original asset exclusion checks.
Status of the native build must be read from the branch's actual Actions run;
a committed test is not itself a passing test.

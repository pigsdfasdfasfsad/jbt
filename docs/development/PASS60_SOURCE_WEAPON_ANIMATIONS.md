# Pass 60 — Source-Normalized First-Person Weapon Animation

## Baseline and evidence

Extends Pass59's source firing travel and sprint translation, after repairing
three Pass59 regressions that still expected the legacy no-argument fire
method and a removed approximation comment. The source is the inert,
already checked-in `content/weapons/entries/*.json` normalization of user
supplied original Luau model/module animation records.

Example source fields for **Glock 17**:
- `animations.Fire.Sequence`: 0.075 seconds to
  `CFrame.new(0,0,0.1) * CFrame.Angles(0.174532925...,0,0)`, then 0.15s
  to `CFrame.new()`.
- `animations.AimFire.Sequence`: 0.05-stud travel and 0.08726646 rad
  pitch, same segment times.
- `stats.SprintCF`: original camera-local translation and two
  original rotation factors; `Sprint` and `UnSprint` supply blend times.
- `stats.GripCF` and multiple `animations.Reload.Sequence` `AV.Main`
  keyframes: original relative hold/move rotation matrices and positions.

## Implemented

1. `Pass60WeaponPoseMath` parses only numeric `CFrame.new()`,
   `CFrame.new(x,y,z)`, `CFrame.new(x,y,z,r00..r22)`, chained
   `CFrame.Angles(x,y,z)`, or the exact symbol
   `module.Stats.GripCF` **only when a prior verified source grip exists**.
   Numbers are finite and bounded, matrices are checked and
   orthonormalized, input length/factor count are capped. No Luau
   callbacks, HTTP, arbitrary expressions, reflection, dynamic evaluation,
   or remote data loading are involved.
2. `Pass60WeaponAnimationLibrary` loads per-gun target fire and aim-fire
   translations **and rotations**, source sprint position/rotation, and
   source reload `AV.Main.To` poses, with source per-keyframe timing.
3. Source reload main motion is applied as a transform **relative to
   the recovered GripCF**, and the original reload sequence is normalized
   to the existing gameplay-authoritative reload duration. The original
   exact source event timestamps are not claimed as gameplay timings.
4. Slerp-interpolated CFrame animation is composited with existing tiny
   idle sway and first-person gun view. Source recoil X-angle and
   full 3-vector shot travel supersede the previous generic kick for
   supported weapons. Distinct hip and aim-fire poses are preserved.
5. Sprint interpolates toward the original camera-local SprintCF basis
   as well as its translation; camera AimCF remains explicitly approximate.
6. Existing original MeshPart/OBJ precedence, source model poses, private
   skins, ammo, damage, movement speed, player recoil authority,
   perk timing, shop/save data and 15-wave gameplay rules are unchanged.
   Unsupported/dynamic/malformed animation expressions fall back safely.

## Automated acceptance

- Existing 525+ Python regressions with three updated interface/comment
  assertions, plus new `tests/test_pass60_source_animations.py`.
- Existing Pass59 exported Windows headless smoke is rerun on the new branch.
- New `--smoke-pass60` verifies source original Glock 17 rotations,
  correct 0.028m versus 0.014m travel at frame peak, sprint transform,
  nonempty source reload pose progression, pose reset and rejection of
  unsafe/nonliteral expressions in a native Godot executable.
- CI original private-asset exclusion remains enabled; artifact upload
  remains disabled.

## Remaining limitations

Original left/right hand and finger poses, magazine detachment/handoff,
tube-shell insert events, pump/slide/breech `AV.Reload` tracks,
actor animation and IK, actual original MeshPart triangle streams,
texture UVs, dynamic `AimCF` construction, easing-curve exact
matching and other script events still need reconstruction and real
screenshot-based validation. This is a **source-guided visual animation
pass**, not full 1:1 first-person fidelity or proof of game completion.

## Verified October 11, 2026 Windows checkpoint

**Successful Windows CI run:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38124413516

**Code revision tested:** `ca08e427a09822cb711327fd71d2cb5a9c958e79`

- **534 Python regressions passed**, one inherited Pass34 FutureWarning.
- Godot .NET Windows build/export succeeded: **0 errors**, one existing
  Pass36 `CS0108` warning.
- Baseline domain simulation: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150`
  (automated states, not a full player-controlled combat playthrough).
- Pass59 regression: `TWR_SMOKE_PASS59_SOURCE_MOTION_OK`, confirming
  Glock 17 hip 0.028m, aimed 0.014m and source firing timings.
- Pass60 native Windows: `TWR_SMOKE_PASS60_ANIMATION_OK` with
  `source_matrix_and_angles=true`,
  `hip_translation_and_rotation=true`,
  `aim_translation_and_rotation=true`,
  `original_sprint_position_and_rotation=true`,
  `original_reload_keyframes=true`,
  `reload_duration_authority_unchanged=true`,
  `invalid_source_expressions_blocked=true`.
- Owner-private map, meshes, skins and synthetic fixtures exclusion
  passed; GitHub public EXE upload remains disabled.

This verifies **source-data animation code paths under exported Windows
headless smoke**, not original mesh restoration, screenshot-level
1:1 fidelity, 10-map player-controlled acceptance, or a privately
delivered executable.

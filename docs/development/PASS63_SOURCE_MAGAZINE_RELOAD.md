# Pass 63 — Original Magazine Removal / Reinsertion, Source-Aware Gun Viewmodels

## Baseline

Continues the verified Pass62 Windows checkpoint without changing the
architecture, original owner packs or inventory/damage authority.

The original normalized weapon records in
`content/weapons/entries/*.json` include
`animations.Reload.Sequence[*].ModelMotor.Mag`, including the
`MotorHierarchy = ["Mag","Handle","MagMotor"]` location, source
absolute CFrame poses, optional explicit `Start`, `Lerp`, `Ease`,
and motor-tween `Time` overrides. The original `Stats.Rels.MagRel`
is a **dynamic Luau expression** based on the original tool's nested
`Mag.Handle` relative to the weapon `Handle`. It is NOT executed.

Pass62's SHA-verified owner-only source hierarchy is sufficient to
compute this specific original handle reference. Without that owner
hierarchy (or with an unsupported source expression), the
pre-existing fallback magazine animation remains active.

## Implemented

- `Pass63SourceMagazineReload.cs`: restricted data-only parser for
  `module.Stats.Rels.MagRel`, optionally multiplied by pure numeric
  `CFrame.new(...)`/`CFrame.Angles(...)`, plus bounded absolute
  numeric source CFrames. Dynamic expressions and unknown module
  lookups are rejected. The Roblox +Z-back source coordinate and
  original 3×3 basis are reflected into Godot -Z-forward coordinates.
- Source motor cues maintain original step ordering, zero-time
  instantaneous mag transitions, source `Lerp` flags, `Start`
  overrides and separately authored motor `Time` values.
  `Linear`, `SlowDown`, and `SpeedUp` have bounded interpolation
  approximations (not exact Roblox TweenService curve matching).
- Source cue elapsed time is normalized to the **existing** authoritative
  weapon reload duration. Ammo exchange timing, reserve count, perks,
  credits, progression, damage, and round state are unchanged.
- `Pass62SourceWeaponHierarchy.Binding.ResolveSourcePose` converts
  an absolute source motor target relative to the weapon Handle or a
  relative MagRel expression anchored to the original recovered Mag
  Handle. The resultant rigid transform applies to **every visible
  source Mag child**, preserving their relative spacing. All parts
  return to their rest transforms after reload.
- The static source hierarchy pack may contain nested magazine internals;
  Pass63 currently animates the top-level `Mag` group as one assembly.
  Magazine ejection, independent dropped magazines, shell-by-shell
  shotguns, and independent hand IK are not yet implemented.
- Any missing original owner pack falls back to the previous visible
  magazine-drop proxy. The source pack is never embedded into public
  GitHub builds.

## Source examples

| Weapon | Original Mag cues | Distinct source behavior |
|---|---:|---|
| Glock 17 | 4 | Magazine extraction, instantaneous handoff, replacement, return to `MagRel` |
| AK-47 | 5 | Original relative downward release, absolute intermediate hand poses, insertion |
| M4A1 | 4 | 0.2/0.3-stud relative drops and explicit return |
| P90 | 8 | Top magazine lifts, multi-axis rotations and reinsertion |
| Mossberg 500 | 3 | Original `Mag` motor hand/round-loading pose progression |

These are original **script keyframes** rather than claims of exact
character-hand animations.

## Verification

- `tests/test_pass63_magazine_reload.py` covers source records,
  strict expression and group bindings, original snapshot keyframe
  counts, owner-safe fixture generation and cleanup, native-test
  wiring, and the retained private upload guard.
- Exported Windows `--smoke-pass63` creates two entirely fabricated
  source weapon assemblies with two independently visible Mag parts,
  authored-style Handle helpers, and source hierarchy metadata.
  It verifies both magazine parts move and rotate together during a
  genuine Glock source reload, preserves interpart spacing and
  resets their original transforms.
- Pass59–62 exported-game smokes and ten-map scripted progression
  smoke are repeated; private packs are deleted before the final
  asset exclusion scan. Public downloadable CI builds remain disabled.

## Remaining requirements for faithful TWR

Correct original Motor6D C0/C1 joint offsets, complete 3D mesh and
texture binaries, hand/arm/finger animations, real shell/bolt/magazine
physics, dynamic AimCF, complete map restoration, ten full
player-controlled 15-wave acceptance runs, and a privately packaged
Windows game with authorized source assets remain unfinished.

## Status

Source code committed. Do not mark this pass native-Windows verified
until its own Pass63 full CI run succeeds.

# Pass 66 — Camera-Anchored First-Person Arms and Bounded Two-Bone Elbow Motion

**Branch:** `twr-pass66-articulated-source-arms`
**Base:** verified Pass65 source-weld first-person hands, Windows run
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38168111351

## Original source facts and strict fidelity distinction

The owner-supplied `TestPlace/TestPlace.rbxlx` includes
`ReplicatedStorage/Models/Supply Drop/Characters/Survivor1`
with a `Torso` measuring **2 × 2 × 1 Roblox studs**, and
`Left Arm` / `Right Arm` Parts each measuring
**1 × 2 × 1 Roblox studs**. These are original R6-shaped survivor
NPC arms, not evidence that the actual first-person player used the
same arm geometry, materials, or source Motor6D angles.

No owner-original standalone first-person avatar arm/hand model or
Motor6D shoulder, elbow, or wrist C0/C1 joints were recovered.
The following is an explicitly **procedural reconstruction** using
the approximate original NPC arm length only. It does not claim
pixel-perfect TWR arms.

## Changes to live first-person viewmodels

1. `Pass66ArmKinematics.cs` implements a finite, deterministic
   camera-space two-bone elbow solver. Its upper + forearm nominal
   length is **2 Roblox studs** using `RobloxUnits.MetersPerStud`.
   Both segments are 1 stud nominal, with an explicit maximum
   **1.65× reach** during unusually long authored hand motions.
   Unrealistically far hand positions are clamped and the condition
   can be inspected; NaNs, degenerate segments, and reflection
   basis issues are prevented.
2. Each source weapon with Pass65 grip CFrames displays
   **two procedural upper arms, two forearms, two elbow joints,
   and two shoulder joints** (8 meshes) in addition to the
   pre-existing 14 glove proxy meshes.
   Sleeves are reconstructed dark fabric/cuff materials; no
   remote mesh or image assets are represented as original.
3. The arm root `ArmRig66` is **a sibling of**
   the `WeaponBody` scene, and so stays camera-anchored even
   while the weapon recoils or reloads. For each frame, the
   wrist endpoints are obtained from:
   `WeaponBody.Transform * SourceHandWeld.Transform * wristCuffOffset`.
   The solver updates the elbow and upper/forearm segment orientation.
4. The system inherits actual source `LeftWeld`/`RightWeld`
   reload/equip/cycle target motion from Pass65, including
   parent motor group movement from Pass62–64. The left forearm
   follows the Glock magazine and Mossberg pump; the right
   forearm follows the Mosin bolt handle.
5. A completed animation returns the whole arm chain to rest.
   Weapon changes remove the old camera arm rig and create exactly
   one new rig. Arm proxies are hidden in static armory scenes;
   the pre-existing cosmetic material effects still run there.
6. Every new component is visual only. Damage, ammo, reload authority,
   firing cadence, economy, zombie spawning, map layouts and
   session progression are unchanged.

## Native Windows acceptance

- `tests/test_pass66_articulated_arms.py` checks the solver
  limits, camera-space separation, bounded geometry, native
  test wiring, and privacy protections.
- New exported-Windows `--smoke-pass66` validates finite
  two-bone geometry, nominal source NPC arm scale, stable shoulder
  anchors, Glock reload elbow motion, Mossberg pump support-arm
  motion, Mosin bolt operator elbow motion, end-of-clip rest resets,
  static armory hiding and weapon switch cleanup.
- The native smoke uses only fabricated gun assemblies and synthetic
  source hierarchy packs, created temporarily and deleted before
  the owner-private data exclusion step. It does not ship, expose,
  or embed the actual owner archive.
- All prior Pass59–65 exported Windows native weapon smokes and
  the scripted domain simulation `maps=10 waves=150` rerun.
  That simulation is *not* ten manually played 15-wave matches.

## Remaining fidelity limitations

The avatar shoulder anchors and upper/forearm polygons are not
original. Reaching past the bounded arm solver can leave a visible
wrist gap; the reach-clamp flag exposes that approximation rather
than synthesizing impossible original joint positions. Original
avatar Motor6D joint C0/C1, shoulder/elbow animation curves,
character materials/textures and original CFrame camera sway
also remain unavailable.

Still required for 1:1 completion: missing original MeshPart/CSG
triangle and UV/texture binaries, dynamic source AimCF, complete
ten-map decoration/collision, manual player-controlled full
15-wave acceptance, screenshot parity, owner-only asset
integration, and a privately distributable Windows executable.

## Verification

Pending final Pass66 exported-Windows CI success. Do not treat
commits alone as a verified native Windows result.

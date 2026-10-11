# Pass 61 — Original Equipped Weapon, Pistol Slide and Shotgun Pump Motion

## Baseline and scope

Pass61 continues native Windows-verified Pass60. Its evidence comes from
previously normalized original TWR weapon ModuleScript animation records in
`content/weapons/entries/*.json`; no network or Roblox runtime is required.

The original supplied `scripts.zip` confirms that an author's
`ModelMotor` keyframes control named `SlideSliding` and `Pump` motors,
and that `Equip` uses `AV.Main` poses. Pass61 uses these **literal
source values**, not fabricated timing or travel distances.

## Implementation

1. `Pass61SourceWeaponHandling` loads `Equip.Sequence[*].AV.Main.To`
   as transform keyframes relative to `Stats.GripCF`. It reuses the
   strict Pass60 CFrame parser, interpolates rotation via quaternions and
   time-normalizes the visual equip sequence to the existing
   gameplay-authoritative `EquipSeconds`.
2. Per-weapon `Sliding.Sequence[*].ModelMotor` and
   `Reload.Sequence[*].ModelMotor` recover eligible
   `SlideSliding`/`Pump` translations when `MotorHierarchy`
   is an exact `[Slide, Handle, SlideMotor]` or
   `[Pump, Handle, PumpMotor]` and the `To` expression is exactly
   `module.Stats.Rels.<Motor>Rel`, optionally followed by bounded,
   literal numeric CFrame transformations.
3. The existing Godot `WeaponViewModelRuntime` now moves its actual
   matching slide/pump MeshInstance3D parts while firing or reloading,
   restoring their original rest coordinates when the source clip ends.
   Previous generic slide movement remains the fallback for unsupported
   weapon records. No additional proprietary mesh binaries are assumed.
4. The first-person equip event triggers its source pose clip without
   extending the time before the player may shoot.
5. The source original part list does not yet retain motor model
   hierarchy and the source `Rels` object is dynamic Luau. Consequently
   the offsets are applied to **known named visual parts** in their
   normalized local axes; exact Roblox Motor6D pivots, every custom
   subassembly, and original 1:1 animation easing are not claimed.
6. Unsupported or malicious expressions, missing source relationships,
   unrecognized motor group names and oversized/nonfinite numbers
   are refused and fall back to previous visuals; no Luau execution,
   networking, gameplay authority or player economy changes.

## Source-confirmed example values

- Glock 17 `Sliding`: 0.225 studs = **0.063 meters**
  backward slide travel, with source segment durations 0.05s
  and 0.075s.
- Mossberg 500 `Sliding`: 0.3 studs = **0.084 meters**
  pump travel, with separate 0.2-stud slide movement. Its
  `Reload` also carries eligible pump and slide motor keyframes.
- The exact gun equip positions and rotations are read from each
  original `Equip` source record and measured relative to `GripCF`.

## Validation

- Added `tests/test_pass61_source_weapon_cycling.py`.
- Exported Windows `--smoke-pass61` validates original Glock
  equip/slide animation and Mossberg pump motion in actual Godot viewmodels,
  including visual MeshInstance displacement and reset.
- Prior Pass59/60 native tests rerun, along with the base ten-map wave
  domain simulation, content contract tests and private-source exclusion.
- Public EXE uploads remain disabled; source packs and missing
  mesh triangle bytes remain outside public Git.

## Remaining

Full slide/bolt/handgun subassembly hierarchy recovery, original
magazine Motor6D poses, shell insertion/ejection, character hand
IK, exact dynamic AimCF, original external mesh triangles, all ten maps'
visual and collision fidelity, real 15-wave human-controlled playtests,
and a private Windows release with authorized owner assets remain incomplete.

## Verified October 11, 2026 Windows checkpoint

**Successful Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38124940483

**Tested code revision:** `360769f1581c65d7fb8c1902edba1f5b1f3c0d47`

- **538 Python regressions passed**, one inherited Pass34 XML FutureWarning.
- Native Godot/.NET Windows build/export passed: **0 errors** and one
  pre-existing Pass36 `CS0108` warning.
- Previous exported Windows Pass59 and Pass60 source weapon motion
  smoke tests passed unchanged.
- New `TWR_SMOKE_PASS61_CYCLING_OK` confirms source equip transforms,
  original Glock 17 slide travel **0.063 m**, original Mossberg 500
  pump travel **0.084 m**, original reload pump keyframes, actual
  Godot MeshInstance3D displacements, return-to-rest, and rejection
  of unsupported dynamic source motor expressions.
- `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passed as **domain-level
  scripted progression**, not 10 complete player-controlled matches.
- Original owner-private and synthetic assets were absent from public
  Windows output, with GitHub EXE artifact uploads still disabled.

**Not verified by this test:** full original Motor6D hierarchy positions,
  all weapon animations, user-interactive gameplay, screenshot-level
  visual matching, or a privately downloadable game executable.

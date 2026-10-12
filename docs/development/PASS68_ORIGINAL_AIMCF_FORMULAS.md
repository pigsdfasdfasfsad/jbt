# Pass 68 — Recover Original AimCF Algebra and Source-Specific Sight Offsets

**Base:** verified Pass67 source AimPart sight alignment.
**Branch:** `twr-pass68-original-aimcf`.

## Original source, complete family inventory

The owner supplied `scripts.zip` has **73** weapon-specific
`module.Stats.AimCF = ...` assignments. The exact original expressions
were inspected offline without running Roblox/Luau. Only **distilled**
family labels and numeric parameters, not original scripts or private
models, were committed as
`content/weapons/aim_cf_families68.json`.

| Family | Original entries | Source matrix order |
|---|---:|---|
| AimPart-relative Handle with camera offsets **inside** | 38 | `AimPart^-1 * Handle * GripCF^-1 * B` |
| AimPart-relative Handle with camera offsets **after** | 9 | `(AimPart^-1 * Handle * GripCF^-1) * B` |
| GripCF inverse **before** inverse relative sight | 24 | `GripCF^-1 * (Handle^-1 * AimPart)^-1 * B` |
| SPAS-12 custom parenthesization | 1 | `AimPart * T(-0.6)^-1 * Handle * GripCF^-1 * B` |
| Flamethrower constant | 1 | `CFrame.new(-0.25,0.25,0)` |

Here `B = CFrame.new(0,-0.5,0) * CFrame.new(0.25000000000000006,0.4330127018922193,0)`.
All formula products preserve the source multiplication order and
are reflected into Godot's -Z-forward coordinate system.

Source-specific modifications (Roblox studs, not metres):

- **MP7:** original sight carries a **+0.45 Z** marker shift before
  the inverse sight-relative Handle operation.
- **SPAS-12:** original expression uses
  `AimPart.CFrame * CFrame.new(0,0,-0.6):toObjectSpace(...)`;
  because Lua `:` binds to `CFrame.new(...)` before `*`, this
  equals the special parenthesization above. This is not silently
  rewritten as the more intuitive inverse of `AimPart * offset`.
- **Winchester Model 70 and Mosin Nagant:** **-0.2 Z**
  right-multiplied *after* the basic AimCF.
- **Obrez Mosin:** **-0.1 Z** post-multiplied.
- **Flamethrower:** absolute numeric literal with no AimPart formula.

For the regular inside/outside families, post-multiplication
reassociation produces the same transform numerically. The
distinct families are retained to record the actual original
expression syntax. Inverse-sight family order can differ for
rotated/noncommuting CFrames.

## Engine implementation

- `Pass68OriginalAimFormulas.cs` loads all 73 exact **inert**
  source family categories and their authoritative original
  normalized per-weapon `GripCF` numeric strings.
  CFrame matrix products, inverses, optional marker shifts and
  post-shifts use Godot `Transform3D` and
  `RobloxUnits.MetersPerStud`. Bounded finite transforms and
  positive determinant checks reject malicious/corrupt data.
  **No Lua, require(), eval, external dynamic code or network
  access is invoked.**
- `OriginalWeaponSourceRuntime.TryGetSourceFormulaFrames`
  consumes the separately SHA-gated owner-only weapon pack
  (temporarily fabricated only in explicit native test mode)
  to extract the invisible original `AimPart` and
  **primary Tool Handle at source origin**. Nested magazine/pump/
  bolt `Handle` instances are not mistaken for the main
  Tool Handle; ambiguous/inconsistent roots are rejected.
  Unavailable original source packs or missing `AimPart`
  keep the previous Pass67 approximate visual ADS path.
- `WeaponViewModelRuntime.TryGetOriginalAimCF68` exposes the
  **source-algebra** resulting CFrame for tests/inspection,
  separate from the visually displayed Godot transform.
  The latter applies a bounded **relative correction** between
  the per-weapon exact AimCF and the common original formula,
  atop Pass67's camera-axis sight-marker centering. The final
  screen-space sight is recentered on the aiming axis and a
  finite **0.12–1.20m** visual eye-relief range is enforced. On the
  actual owner SPAS-12 model, the raw special-case formula would
  otherwise exceed that range; only the **visible camera correction**
  is clamped, while the independently inspectable original AimCF
  matrix retains its exact source-derived result.
  This deliberate calibration is necessary because the actual
  Roblox player camera, avatar, Motor6D C0/C1, and weapon
  equipment joint frames are still missing; blindly using the
  mathematically correct original AimCF as the Godot
  `WeaponViewModel` root would not produce true original
  camera placement.
- All original Pass67 `Aim`/`UnAim` cues, approximate easing,
  aim permission/FOV authority, source left/right hand animation,
  camera-anchored procedural arms, firing, recoil, damage, ammo,
  zombies, map waves and credits remain unchanged.

## Verification

- `tests/test_pass68_original_aimcf.py` validates all 73
  original-family distilled records and special-case values,
  their normalized inert `Aim` and `GripCF` dependencies,
  strict numeric matrix algebra, owner-private source
  separation, and native regression hooks.
- New exported Windows `--smoke-pass68` evaluates **noncommuting**
  synthetic rotations and translations against independent
  matrix products for each of the five formula families.
  It checks MP7 marker +0.45, SPAS-12 special -0.6
  parenthesization, Winchester -0.2 and Flamethrower numeric
  CFrame. In addition, fabricated original-shaped
  Glock/Mossberg/MP7/SPAS-12/Winchester guns supply
  invisible `AimPart`/primary `Handle` pairs, and the
  runtime must compute the original formula and produce
  centered visual sights for each. The smoke independently confirms
  **SPAS-12 bounded eye relief** and **MP7 source-specific sight
  depth movement**, in addition to the full numeric source CFrame.
- `tools/maps/make_pass68_aimcf_fixture.py` creates **only**
  fabricated assemblies and hierarchy metadata. It refuses
  to overwrite any original pack and refuses to delete
  unfamiliar files. The Windows workflow creates it solely
  during Pass68 smoke and cleans it before private-asset
  verification, which is preserved from all previous passes.
  CI still disables public EXE and owner-asset upload.
- Existing exported Windows weapon smokes Pass59–67
  and the scripted `maps=10 waves=150` progression run.
  That simulation is **not** ten genuine player-controlled
  15-wave survival playthroughs.

## Not done / limits

The **source AimCF algebra is recoverable**, but the exact original
*rendered* player ADS pose is not yet provable. Missing are source
avatar/equip Motor6D joints, actual player-side camera transforms,
real reticle/optic render layers and textures, exact Roblox
TweenService easing semantics, original external MeshPart triangle
and UV binaries, fully collision/decorated ten maps, manual
full-match gameplay, screenshot parity, and an owner-private
packaged Windows EXE.

The game is still **not** a complete 1:1 copy.

## Acceptance status

Implementation committed. Native Pass68 remains **unverified**
until its full CI run, including new Windows smoke and privacy
gate, completes successfully.

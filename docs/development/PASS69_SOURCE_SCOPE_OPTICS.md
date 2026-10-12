# Pass 69 — Original Scope Apertures, Live FOV Sensitivity, and Reconstructed Reticles

**Base:** verified Pass68 original AimCF algebra and source sight-centering.
**Development branch:** `twr-pass69-source-scope-optics`.

## Recovered original evidence

The original user-supplied `scripts.zip` weapon ModuleScripts retain precisely
**seven** `Stats.ScopeDiameter` values (studs), and **six**
`Stats.ReduceSensitivity = true` flags. The AUG is deliberately the
one original lens-masked weapon without reduced sensitivity.

| Weapon | Original ScopeDiameter (studs) | Reduce mouse sensitivity |
|---|---:|---|
| AUG | 0.106 | No |
| Barrett M82A1 | 0.205 | Yes |
| M110 SASS | 0.188 | Yes |
| M14 | 0.179 | Yes |
| SVD | 0.210 | Yes |
| VSS | 0.210 | Yes |
| Winchester Model 70 | 0.184 | Yes |

This data exists already as inert, normalized original
`content/weapons/entries/*.json`; no owner source file or original
copyrighted source code was committed.

The separately SHA-verified original Pass47 owner weapon pack also
has original source-part CFrames for each scope-lens part:
`ReticleGlass` (AUG, M110, SVD, VSS);
`ScopeReticle` (Barrett);
and `ScopeGlass` (M14, Winchester 70). The source models may also
contain `ReflectiveGlass`, but the primary original reticle/lens
part takes precedence.

**The original scope glass MeshParts' remote triangle/UV/texture bytes
are still unavailable.** Six of the seven `ScopeDiameter` values
do not equal the original lens mesh size. We preserve the **authored
diameter** and original source **position** separately rather than
pretending a Model's physical Size.X is the original retail mask size.

## Implemented

1. `Pass69SourceScope.cs` loads all seven source weapon diameters and
   sensitivity flags directly from the normalized original weapon
   records. Invalid numbers, unknown formats and unrelated weapons
   never activate a scope.
2. Original intended sensitivity behavior is reconstructed as
   `baseMouseSensitivity * liveCameraFov / hipCameraFov` **only** for
   six source-flagged weapons. Aiming sensitivity therefore ramps
   with the actual interpolating camera FOV rather than snapping to
   target FOV. The AUG retains unchanged sensitivity.
   The offline Godot game's authoritative hip FOV is **60°**;
   an earlier user-recovered reference client uses a default
   of 70° — this is an explicit local-client difference.
3. `OriginalWeaponSourceRuntime.TryGetScopeLensLocal` returns an
   unambiguous source glass/lens part center only from the same
   previously SHA-gated owner-original weapon assembly.
   `WeaponViewModelRuntime` converts that local point into
   the **current** viewmodel world transform, including weapon ADS
   and source-authored motion. Missing/uninstalled owner assets
   return a clear unavailable condition; no remote fetch occurs.
4. `Pass69SourceScope.TryProjectLens` uses the Godot camera's genuine
   world-to-screen perspective `UnprojectPosition` of a source
   physical left/right lens width. The lens hole diameter uses
   the exact original **ScopeDiameter**, not lens mesh dimensions.
   A bounded physically derived center-screen fallback at the
   Pass67 reconstructed 0.43m eye-relief position remains available
   when the original glass part is missing.
5. `Pass69ScopeMask` draws a dark opaque outside-the-lens field
   with a 100-band circle cutout, lens rim, and procedural crosshair.
   This is a clearly marked **approximate original 2D mask and
   optic reticle**: the original source texture, scope-mask shader
   and exact retail screen-space algorithm were not recovered.
   It is hidden during hipfire, item use, hammer mode, mounted
   mode, missing scopes and invalid camera geometry.
6. `FirstPersonPlayer` creates the first-person scope layer,
   tracks camera-FOV transition opacity, hides and restores the
   mask on weapon switching, and multiplies mouse input by the
   source-derived sensitivity factor. `GameplayHud` suppresses
   the ordinary `+` hipfire crosshair only when the new scope
   overlay is actually visible, preventing two overlapping
   aiming symbols.
7. No server/domain weapon or ammunition changes, damage changes,
   weapon accuracy changes, authoritative FOV modifications,
   projectile-ray changes or map modifications were made.

## Validation strategy

- `tests/test_pass69_source_scopes.py` exhaustively matches the seven
  checked-in original source diameters/sensitivity flags, asserts
  bounded projection/sensitivity formulas, source-owner SHA gates,
  player/HUD integration, and owner-safe deterministic fixture
  creation/cleanup.
- `tools/maps/make_pass69_scope_fixture.py` creates **12** fictional
  source-shaped assemblies with seven fabricated scope glass markers
  and a synthetic hierarchy pack. The exact original owner
  source packs are never overwritten or deleted.
- Exported Windows `--smoke-pass69` validates source diameter and
  live-FOV sensitivity including AUG exception, perspective lens
  size change with FOV and depth, all seven synthetic recovered
  marker names, projected camera axis, invalid lens rejection,
  mask view-model creation and teardown/fallback. Pass59–68
  native smokes and the private-asset exclusion checks are retained.
- CI's `maps=10 waves=150` is a **scripted state progression
  simulation**, not ten complete player-controlled matches.
- Public release artifact uploads are deliberately disabled to
  avoid exposing owner-supplied original models or meshes.

## Remaining original parity blockers

Original 2D scope mask shader and reticle image/texture overlays,
remote scope MeshPart triangles/UVs, verified actual lens-to-avatar
camera offset/scope magnification semantics, original Roblox player
skeleton and Motor6D joints, decorated/collision-faithful all-ten-map
reconstruction, genuine end-to-end playable match tests, reference
screenshots of every weapon's optic, and final owner-private
offline Windows EXE packaging.

**Status:** committed; native Windows acceptance must succeed before
this pass is marked verified. No 1:1 completion claim.

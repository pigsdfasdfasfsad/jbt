# Pass 69 — Source-Sized Optics and Reconstructed First-Person Reticles

**Base:** verified Pass68 `twr-pass68-original-aimcf`  
**Branch:** `twr-pass69-source-optical-reticles`

## Source evidence

The recovered, inert weapon `ModuleScript` stats contain
`ScopeDiameter` for **seven** guns and `ScopeRadius` for
**CBJ-MS**. These are authoritative original authored Roblox-stud
aperture dimensions, not inferred from screenshots.

| Weapon | Original aperture field | Original full diameter, studs | Original aim FOV |
|---|---|---:|---:|
| AUG | ScopeDiameter | 0.106 | 40° |
| Barrett M82A1 | ScopeDiameter | 0.205 | 10° |
| CBJ-MS | ScopeRadius=0.0555 | 0.111 | 60° |
| M110 SASS | ScopeDiameter | 0.188 | 10° |
| M14 | ScopeDiameter | 0.179 | 10° |
| SVD | ScopeDiameter | 0.210 | 10° |
| VSS | ScopeDiameter | 0.210 | 10° |
| Winchester Model 70 | ScopeDiameter | 0.184 | 10° |

The user's private `images.zip` also contains original-game
`*-ADS.png` reference stills for these eight guns. Their names are
retained **only for review provenance**; no photograph, screenshot
pixel or original texture is copied into the public GitHub build.

## Code changes

1. `Pass69SourceScopeOptics.cs` is a new procedural **screen-space
   overlay**, built using Godot Control drawing APIs. It uses six
   reconstructed reticle styles: compact cross, reflex red dot,
   circle cross, duplex, mil-dot and eastern post.
   The profile/style associations are approximations guided by the
   supplied ADS screenshots. They are not actual recovered original
   Roblox scope textures or scope glass simulation.
2. The projected circular lens radius is computed from the **original
   aperture diameter in studs**, `RobloxUnits.MetersPerStud`,
   current camera FOV, viewport dimensions, and geometric eye relief.
   Projection clamps reject invalid/NaN FOV, screen sizes and depth;
   the rendered scope must remain finite/readable.
   CBJ-MS's original **radius** field is explicitly doubled to
   diameter before projection.
3. An original-derived `AimPart` sight depth is used when a
   SHA-verified source weapon model is available. Otherwise Pass67's
   **approximated 0.43m** eye relief remains in effect. The
   overlay is hidden during unsupported equipment, weapon changes,
   unscoped pistols, throwables, mounted/hammer modes, lost aim
   permission and after death.
4. Pass67/68 aiming remain the **only** authority for the viewmodel
   pose and original source-`AimCF` algebra. Existing weapon
   damage, bullet direction, recoil, item perks, ammo and FOV are
   unaffected by the new overlay.
5. `GameplayHud` hides the generic central **+** only while a
   scoped reticle is genuinely displayed; the usual crosshair
   returns when exiting ADS. This is render-only and does not
   alter hit testing or the HUD's wave/health/ammo data.

## Windows acceptance

- `tests/test_pass69_source_optics.py` validates all eight
  source-authored diameters/FOVs, original CBJ-MS radius,
  six procedural styles, finite geometric projection,
  safe ADS/crosshair wiring and private-asset exclusions.
- `--smoke-pass69` runs inside the **exported Windows Godot game**,
  validating source profile counts and values, physically
  projected radius changes, CBJ-MS reflex behavior, optic
  visibility resets, camera FOV/eye-relief changes, HUD
  crosshair hiding/restoration and Pass68 Winchester AimCF
  compatibility with a fabricated owner-style original
  AimPart/Handle test pack. Original owner assets are never
  committed. Synthetic fixture cleanup is required by CI.
- Previous Pass59–68 exported-game source weapon smokes and the
  ten-map `150-wave` domain progression simulation remain on.
  The latter is a state-machine test, not ten player-controlled
  completed 15-wave games.
- Public Windows EXE and owner-private mesh/script/image artifact
  publishing remain disabled in GitHub Actions.

## Remaining fidelity limitations

These are reconstructed reticles, not source-native bitmap/mesh
textures or original postprocessing. Original optic zoom effects,
accurate lens glass distortion, black-out shape, pupil alignment,
scope sway, exact scope-to-camera depth and occlusion, user avatar
and weapon Motor6D attachments, actual source Tween easing and
player-perspective screenshot comparison remain unfinished.
Additionally, many external weapon MeshPart triangle/UV binaries,
full decorated/collision-faithful ten maps, manual 15-wave match
acceptance and a privately deliverable Windows executable remain
outstanding.

**Pass69 is a visual fidelity increment, not proof that the complete
game is a 1:1 offline recreation.**

## Verification status

Implementation and CI wiring committed. Mark the pass **verified**
only after the entire exported Windows run completes successfully.

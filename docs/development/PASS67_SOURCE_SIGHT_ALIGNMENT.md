# Pass 67 — SHA-Bound Source Sight Marker Alignment (AimPart)

**Base:** verified Pass66 `twr-pass66-articulated-source-arms`  
**Branch:** `twr-pass67-source-sight-alignment`

## Source evidence and fidelity distinction

The original tool modules in the user's supplied `scripts.zip` assign
`module.Stats.AimCF` at runtime using source
`ReplicatedStorage.Models.Tools[weapon].AimPart.CFrame`,
`Handle.CFrame` and `GripCF`. The formula varies slightly by
weapon, including a reversed inverse order and the SPAS-12's special
0.6-stud sight offset. **AimCF itself is not serialized as a numeric
`stats.AimCF` entry** in the normalized public weapon JSON.

The separately verified, owner-only Pass47 original source assembly
does retain the invisible original `AimPart` CFrame:

- **73 out of 100** original/alias Tool model entries contain exactly
  one source `AimPart`; 27 absent entries are mostly melee tools,
  explosives and special equipment.
- A matching original tool `Handle` is required; ambiguous or
  missing `AimPart` is never guessed.
- The same alignment is applied to the marker as the Pass47 visible
  model importer uses for its source barrel orientation.
- Source original body geometry, UVs, scope reticles and external
  MeshPart triangles are not newly recovered in this pass.

## Implemented

1. `OriginalWeaponSourceRuntime.TryGetSourceSightLocal` reads the
   invisible original Tool `AimPart` positional/rotational CFrame
   from the already SHA-gated private Pass47 assembly pack. Synthetic
   fixtures are accepted **only** by a dedicated native smoke flag.
   This is a read-only source marker, no original Lua execution.
2. `Pass67SourceAimAlignment` parses inert original
   `animations.Aim`/`UnAim` keyframes, recognizing the exact
   `module.Stats.AimCF` animation target and source return
   `CFrame.new()`. For the Glock, M4A1, AK-47, Mossberg and
   other standard weapons, the original visual Aim/UnAim cue
   durations are **0.50/0.25 seconds** and the original easing
   selectors are **SlowDown2/SlowDown**. The engine implements
   cubic/quadratic ease-out *approximations*, not proven Roblox
   easing parity.
3. A weapon with a recoverable source sight gets a camera-local
   target parent transform that places the **actual imported
   AimPart** on the reticle centerline, **0.43 metres**
   forward of the camera. The marker is not rendered; the original
   gun mesh/proxies move with it. This eye relief is an explicit
   local reconstruction value, **not** a recovered Roblox exact
   eye-camera value. Nontrivial original AimPart bases are checked
   but visual weapon forward orientation stays correctly facing
   the Godot camera rather than arbitrarily reversing based on
   a 180-degree invisible helper.
4. `FirstPersonPlayer.UpdateAim` now prefers that verified
   source sight correction when present, using source visual
   transition cues. Without owner pack/AimPart, the prior
   approximate center/height visual aim behavior is retained.
   Existing `AimSeconds`, perk gates, camera FOV and shotgun/bullet
   ray direction authority remain in the player; the new easing
   never executes source scripts or changes damage.
5. Since the parent viewmodel moves in ADS, the Pass66 arm root
   now counter-transforms the root and computes its wrists in
   **camera coordinates** each frame. Consequently shoulder
   pivots remain camera-anchored while the hands follow the
   source magazine/pump/bolt reload motions and centered sights.
6. Weapon switches, throwables and hammer/equip transitions
   reset the source ADS visual blend. Unsupported aim source
   geometry leaves the earlier viewmodel fallback intact.

## Native Windows verification

- `tests/test_pass67_source_sights.py` checks exact original
  normalized aim and unaim cue values, absent dynamic AimCF
  numeric serialization, strict source-marker provenance,
  FOV/recoil/ammo authority preservation, synthetic fixture
  creation/removal and private-asset exclusion.
- `--smoke-pass67` in the **exported Windows executable** uses
  the fake Pass64 source weapon/hierarchy fixture. Glock 17 and
  Mossberg 500 invisible AimParts must align on camera -Z axis,
  whereas fake Mosin without AimPart must decline precise ADS
  and retain the fallback. It also checks original normalized
  profile timings and camera-offset record, easing, absurd input
  refusal, camera-anchored shoulders, and restored hip origin.
- Existing exported Windows tests Pass59–66, domain `maps=10
  waves=150` scripted progression, and private-asset exclusion
  all remain enabled in the GitHub workflow. The progression
  simulation **does not imply complete manual gameplay acceptance**.
- No owner source pack, original script archive or publicly
  downloadable Windows EXE is committed or uploaded.

## Remaining

Still missing: exact original dynamic `AimCF` formula evaluation for
each known variation (including the SPAS-12 optic shift), exact
Roblox first-person camera offsets and easing curves, original
true optics/reticle meshes and textures, first-person screenshot
matching, remote gun mesh binaries, avatar Motor6D rig joints,
fully faithful ten-map visuals and physics, 10 real full
15-wave playthroughs, and a privately distributed offline EXE.

The geometric sight-center placement is demonstrably grounded
in original Tool AimPart positions but **is not yet 1:1 sight
animation or scope/optic fidelity**.

## Verified Windows checkpoint — October 11, 2026 (US Central)

**Successful native Windows CI:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38191379736

**Tested code SHA:** `41b5057771b378851167ae455b4ec66a3936556c`.

- **569 Python regression tests passed**, with one inherited Pass34
  XML FutureWarning. The Windows Godot/.NET export succeeded with
  **0 errors** and the one pre-existing Pass36 `CS0108` compiler warning.
- Native `TWR_SMOKE_PASS67_SOURCE_SIGHT_OK` validated original
  Glock 17 Aim 0.5s / UnAim 0.25s and source easing, strict finite
  geometric alignment, an invisible synthetic owner-style `AimPart`
  in the Glock and Mossberg, camera-axis sight centering at the
  explicitly approximated **0.43m eye relief**, camera-stable
  procedural shoulders during sight movement, a source-unsupported
  Mosin fixture using fallback, and refusal of nonfinite or
  impossible sight poses.
- Exported Windows Pass59–66 native weapon smokes passed unchanged.
  `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` passed as a scripted
  progression simulation, **not a manually played ten-map game**.
- The synthetic owner-style test packs were removed before the
  original source/private-asset exclusion check; it passed. Public
  Windows EXE artifact uploads remain disabled.

**This verifies the new native engine visual alignment code.** It does
not verify owner-original meshes, exact original dynamic `AimCF`
algebra, scope optics, screenshot parity or a complete player-played
15-wave match.

# Pass 62 — Original Nested Weapon Subassemblies and Source Handle-Centered Motion

## Goal and verified source evidence

This pass continues Pass61's Glock slide and Mossberg pump animations,
replacing single-part-name approximations with whole source-model group
transformations wherever an owner-supplied, SHA-verified sidecar is installed.

The original `TestPlace/TestPlace.rbxlx` supplies an actual authored
`ReplicatedStorage/Models/Tools` hierarchy. The Pass47 importer recovered
its Part CFrames and meshes, but flattened every original nested
`Slide`, `Mag`, `Pump`, `Bolt`, `Charge` and other Model into direct
Godot children. That flattened import lost grouped-part membership.

**New extraction from the owner archive:**
- 98 original weapon models and **1,484** original tool parts.
- **78** weapon models with unambiguous recovered nested groups.
- **259** nested groups with uniquely identifiable original `Handle`
  CFrames; ambiguous, missing or duplicate Handle origins are skipped.
- Every group preserves an ordered Model ancestry path, verified
  Pass47-compatible source part indices, the original nested Handle
  position relative to Pass47's first Handle, and original 3×3 orientation.
- **No original Motor6D C0/C1 joints were present or recovered from
  these place tool assemblies.** A source nested Model Handle provides
  a grounded group coordinate system, not proof of the exact Roblox
  Motor6D joint pivot. Nor are missing remote MeshPart triangles included.

The Pass62 private sidecar remains **optional**; the existing original
source assembly pack and its SHA never change.

## Implementation

- `tools/maps/extract_source_weapon_hierarchy62.py` streams the owner's
  `TestPlace.zip` original RBXLX, enforces its historical original source
  SHA256, and emits `SourceWeaponMotorHierarchy62.json.gz` plus an
  audit manifest. It runs only locally, never in public Windows CI against
  the owner data. Deterministic gzip `mtime=0` makes audit repeatable.
- `Pass62SourceWeaponHierarchy.cs` accepts that sidecar only if it matches
  the exact SHA256 and the existing, separately verified Pass47 original
  source assembly SHA. It bounds compressed and decompressed sizes, the
  number of models, source part indices, and numeric transform components;
  synthetic packs are allowed solely for `--smoke-pass62`.
- The Pass47 source assembler binds **visible** MeshInstance3D nodes using
  their unchanged authored source part index, not guessed names.
  Invisible `Handle`, `Pos`, and `AimPart` helpers still influence
  indices and pivots but never become visible bounding cubes.
- For moving `Slide` and `Pump` groups, the Pass61 source-motor
  translations are now composed around each recovered original Model
  Handle transform. All visible children of the group move together in
  group-local axes and reset from captured original transforms.
  Without the owner sidecar, the previous Pass61 viewmodel animation
  remains available. Recoil damage, ammunition authority, fire rates,
  reload timers, skin ownership, credits, map logic, and progression
  are unchanged.
- Other recovered groups, including `Mag`, `Charge`, `Bolt` and nested
  barrel/round models, are **preserved as data but not yet all animated**.
  Dynamic relocations and absolute motor targets require a later pass
  rather than inventing original Motor6D joints.

## Owner-only installation

The owner ZIP generated with Pass62 includes
`Content/Weapons/SourceWeaponMotorHierarchy62.json.gz`. Place this file
beside the existing verified `Content/Weapons/SourceWeaponModels.json.gz`
relative to the Pass62-or-later game executable. No Roblox or internet is
needed for local reading. This is not a complete new playable Windows EXE.

**Original source SHA256:**
`272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`

**Unmodified Pass47 source assembly pack SHA256:**
`fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7`

**New Pass62 hierarchy sidecar SHA256:**
`505678dcbad987d28233753a359d189f520e927f10cfffc6e16f01381406b130`

## Native Windows acceptance

- `tests/test_pass62_source_weapon_hierarchy.py` exercises a fabricated
  nested original-style XML scene, verifies exact part indices/pivots,
  deterministic extraction, strict owner provenance, and fixture cleanup.
- A new exported-Windows `--smoke-pass62` uses wholly **synthetic**
  source packs created immediately before the test and removed afterward.
- The synthetic test places a rotated, off-axis Handle in each `Slide`
  and `Pump` Model and verifies that BOTH visible child parts move in the
  Handle's local direction, return to rest, and are not mistaken for
  original source data.
- Windows CI reruns existing Pass59–61 native acceptance, legacy map/wave
  state simulation, and the owner-private pack exclusion scan. Public
  uploads for owner packs and EXEs remain disabled.

## Remaining fidelity limits

This does **not** recover Roblox Motor6D Part0/Part1 or C0/C1, dynamic
magazine handoff, individual hand/arm IK, source easing curves, source
weapon triangle meshes, accurate 3D UV/texture maps, original dynamic
AimCF sights, or visual/full gameplay parity for all ten original maps.
The private hierarchy sidecar alone has no effect without the owner's
historically verified original weapon assembly pack. Do not call the
game a 1:1 completed remake based on this pass.

## Verified Windows checkpoint — October 11, 2026

**Successful native Windows CI:**
https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38125821580

**Tested source-code SHA:** `ee79449559052acf5897aa0bc9ba1ed60032b00d`.

- **543 Python regressions passed** with one inherited Pass34 FutureWarning.
- Godot/.NET Windows export passed: **zero errors**, one pre-existing
  Pass36 `CS0108` compiler warning.
- Previous Pass59, Pass60 and Pass61 native source-weapon animation smokes
  passed on the exported Windows EXE.
- New `TWR_SMOKE_PASS62_SOURCE_HIERARCHY_OK`: SHA-gated synthetic-only
  sidecar, original-index group mapping, offset and rotated Handle pivots,
  **two** distinct Glock slide child meshes and **two** distinct Mossberg
  pump child meshes moved, exact reset to source rest positions, no
  fabricated remote mesh bytes, and no combat-authority mutation.
- CI created exactly two synthetic temporary test packs and confirmed
  their cleanup. The owner-private pack exclusion step passed.
- `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` remains a **scripted
  state simulation** rather than full user-controlled 15-wave matches.

**Separate owner ZIP:** includes both the recreated historically
verified Pass47 `SourceWeaponModels.json.gz` (original SHA:
`fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7`)
and the new Pass62 SHA-verified hierarchy pack. Both are owner-only
artifacts, not embedded into the repository or publicly uploaded via
Windows Actions.

The build and smoke tests prove software paths, not source-triangle
recovery, original animation joint fidelity, a complete playable
Windows distribution, or full 10-map 1:1 parity.

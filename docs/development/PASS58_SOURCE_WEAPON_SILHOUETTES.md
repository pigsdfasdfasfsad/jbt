# Pass 58 - Source-Guided Weapon Silhouettes and Original Geometry Audit

## Baseline

Continues the verified Pass57 offline Windows Godot build. Pass57 fixed numeric `rbxassetid://` resolution for locally provided OBJ meshes and recreated native Part spheres, cylinders and wedges. Pass58 improves **actual 3D first-person and Armory weapon render geometry** for named, dimension-grounded missing custom meshes. It does NOT reconstruct the original Roblox triangle streams from thumbnails.

## Authenticated user-source inventory

The uploaded `TestPlace.zip` and original SourceWeaponModels.json.gz private sidecar identify **98 authored tool models plus 2 aliases**, 1,484 original source parts, and **1,137 distinct source MeshId references**. The owner pack SHA256 is `fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7`; original TestPlace.rbxlx SHA256 is `272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

Our deterministic audit of the 98 distinct source models (excluding alias duplication) classified their original part records as follows:

| Original geometry category | Part count |
|---|---:|
| Total original part records | 1,484 |
| Invisible source helpers/markers | 548 |
| Visible original parts | 936 |
| Recognizable source-guided silhouette opportunities | **390** |
| Remaining conservative/unrecognized MeshPart bounds | **525** |
| Visible native Part/Union records | 21 |

The **390** recognized parts break down into 140 chassis/receiver profiles, 78 tubular shapes, 60 grips, 50 magazines, 43 stocks, 10 trigger guards and 9 optic-glass shapes. These counts represent **eligible visible shapes**, not recovered meshes. The original source archives contain **zero** of the 1,137 referenced custom triangle streams. Without separately installed OBJ meshes, 525 visible MeshParts still need conservative bounding proxies.

The reproducible owner-only audit is `tools/maps/audit_weapon_details58.py`, which checks pinned source SHA256s and refuses to overwrite earlier reports.

## Code changes

1. New `Pass58WeaponDetailGeometry` uses strict source class/name matches and original Size aspect ratios to construct normalized procedural prism contours for magazines, stocks, grips, chassis, trigger guards and optic disks. The silhouettes have actual triangle surfaces, normals and UVs. The trigger guard is an open ring rather than a filled box. Generated profiles are cached and limited to at most 2,000 vertices.
2. Authentic locally installed owner OBJ meshes **always take precedence**. The renderer uses these fallbacks only when a given original source MeshId cannot be resolved; the geometry still uses its original part-relative Roblox CFrame, Size, color, transparency and skin material assignments.
3. The prior carefully constrained Pass57 circular-tube substitutes and true native Part geometry remain as fallbacks; unknown names, implausible aspect ratios and nonfinite/unbounded dimensions are refused.
4. The first-person `WeaponViewModelRuntime` now tracks constructed source-guided profiles and optic disks independently from locally resolved original triangle meshes, tube proxies, native primitives and plain bounds.
5. The 3D Armory/Lobby display exposes **RECONSTRUCTED SILHOUETTES** and **OPTIC DISKS** counts and continues to identify unprovided original MeshIds. It never labels these reconstructed meshes as original source triangles or a 1:1 match.
6. No damage, credits, weapons stats, ammunition, purchases, waves, AI or map-collision code is changed.

## Native Windows acceptance

The Pass58 exported Windows Godot smoke (`--smoke-pass58`) uses a wholly fabricated 13-record source model with **12 visible parts**. It tests:

- One handcrafted, local, fake OBJ mesh loaded by source MeshId (correctly taking precedence over any reconstructed profile)
- Five named source-guided profiles: Receiver, Magazine, Stock, PistolGrip and TriggerGuard
- One thin reticle/optic glass disk, one narrow barrel tube, one anonymous fallback bounds proxy
- Three native source primitives (sphere, cylinder and wedge); invisible helper does not render
- Mesh surface vertex counts, cached profile reuse, strict finite source size guards
- Actual 3D Armory/Lobby preview counts, isolated offline save/credits, and cleaned scene on starting Manor

The native test fixture never contains owner source meshes and is removed before the CI private-asset audit. An accepted Pass58 run requires all Python regressions passing, a zero-error Godot/.NET Windows build/export, `TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK` and exclusion of source assets from the public build.

## Asset scope and unfinished fidelity

The private Pass57 kit still contains the original source tool transform metadata and the skin/gallery reference packs. No new original triangle binaries were recovered in Pass58; the supplied .zip files contain .rbxlx source references but not the actual 3D MeshPart byte streams. This pass is a targeted improvement in geometry and on-screen silhouette legibility, **not** a pixel-perfect recreation of original weapon geometry.

Unfinished work: recover authorized original mesh assets when available, source-specific animated UV materials, first-person pose/animation parity, complete per-map indoor detail, genuine long-form player-controlled ten-map gameplay validation, and privately distribute the Windows EXE with all authorized owner assets.


## Verified October 11, 2026 Windows checkpoint

**Successful test run:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38117871532

**Code revision tested:** `74349aec4c3ad5be3160c05429a3446312875c3b`

- **525 Python regressions passed** (1 existing Pass34 FutureWarning), against unchanged original tool and game authority contracts.
- Godot/.NET Windows build/export succeeded, **0 errors**, with one inherited Pass36 CS0108 warning.
- Baseline native simulation: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150`; this does not certify real player-controlled wave combat.
- Pass58 native exported Windows smoke:
  `TWR_SMOKE_PASS58_DETAIL_GEOMETRY_OK synthetic_only=true visible=12 local_obj=1 missing_mesh_ids=8 detail_profiles=5 lens_disk=1 barrel_proxy=1 unclassified_bounds=1 native_primitives=3 trigger_guard_open=true source_pose_preserved=true profile_mesh_cached=true invalid_bounds_rejected=true armory_fidelity_counters=true credits_unchanged=true original_mesh_triangles_not_claimed=true`.
- Original/synthetic asset exclusion passed after fake lobby/tool/OBJ cleanup; no public executable was uploaded.
- The owner-only TWR_Pass58_Private_Weapon_Silhouettes_Kit.zip passed full ZIP integrity checks. SHA256:
  `c39b8ceab70727fcd60c8cf430903d509fcae3f05c2fc6d4e0399e6349ec3054`.
- Its audit measures 390 source-derived geometry eligibility opportunities and 525 remaining unclassified bounds without asserting original triangle recovery. The **native CI uses fake data only**, not the actual recovered owner source OBJ streams, which are still missing.

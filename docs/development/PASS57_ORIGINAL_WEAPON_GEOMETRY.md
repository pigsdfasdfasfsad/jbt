# Pass 57 - Original Tool Geometry Resolution, Native Shape Fidelity and Honest Proxy Diagnostics

## Baseline

Continues native-Windows-verified Pass56. The aim is to improve the actual first-person weapon and original five-slot 3D Armory views, not merely add another static image. No original Roblox cloud mesh download or network connection is needed at play time.

## Original source evidence

Uploaded owner TestPlace.zip contains TestPlace/TestPlace.rbxlx, SHA256:

272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2

The source ReplicatedStorage/Models/Tools hierarchy has:

| Original source field | Count |
|---|---:|
| Distinct original tool models | 98 |
| Original tool part records | 1,484 |
| MeshPart objects | 1,282 |
| Native Roblox Part objects | 186 |
| UnionOperation objects | 16 |
| Distinct referenced remote mesh asset IDs | 1,137 |
| MeshPart triangle bytes recovered from TestPlace | **0** |

The existing Pass47 exporter creates a verified sidecar with **100 model names** (98 original models + 2 source-name aliases) and **1,499 total sidecar part entries** including alias duplication. That sidecar is independently provided in an owner-only Pass57 installation kit. It preserves all 91 active catalog weapons with original relative CFrames, dimensions, colors and metadata. These are source-authored relative assembly transforms, **not** full original MeshPart geometry.

Owner pack: Content/Weapons/SourceWeaponModels.json.gz
Expected SHA256: fa63bcfb3fc69c69c1066aa280372bdc8e2e093d61684c9c252c75c43d7080e7

## Pass57 code changes

1. **Fix live local mesh and texture resolution.** OfflineAssetResolver now accepts only source-literal rbxassetid:// followed by 1-20 ASCII decimal characters **or** a bare decimal asset ID. It canonicalizes both to the numeric filename under Content/Assets/Meshes/<id>.obj or Content/Assets/Textures/<id>.png. It explicitly rejects HTTP, absolute paths, traversal and malformed IDs. Existing private OBJ safeguards are retained (32 MiB per file, at most 300,000 triangles, bounded finite coordinates and valid vertex/UV indices). The prior code accepted only bare digits, so it could not resolve most authored source MeshIds directly.
2. **Replace some bounding blocks with real native shapes.** Original authored Roblox native ball Parts are spherical; original Cylinder Parts generate closed circular geometry along Roblox X; WedgeParts use the existing unit wedge surface. For missing source MeshParts with strictly recognized names such as Barrel, Tube, Muzzle or Suppressor **and** sufficiently slender, approximately round authored bounds, a 24-sided closed cylinder mesh replaces the generic bounding cube. A receiver, stock, magazine, UnionOperation or non-tubular unknown shape **stays** a conservative box. No original remote triangle meshes are claimed as recovered.
3. **Keep native tool hierarchy and appearance.** All generated geometry is unit sized, then uses the existing owner relative CFrame, CFrame-basis correction, and authored Size. Part colors, source Roblox materials and locally installed texture IDs continue to work. Fully transparent helper Parts remain invisible. A local mesh resource always takes precedence over a procedural substitute.
4. **Surface exact provenance.** OriginalWeaponSourceRuntime and WeaponViewModelRuntime track locally loaded meshes, recognizable circular proxies, conservative bounding proxies and original native primitives. The 3D Armory preview shows each category separately along with how many authored source mesh IDs remain unavailable.
5. **No gameplay authority changes.** The source rendering stays inside the existing viewmodel. There are no modifications to player inventory, weapon damage, firing/recoil simulations, credit balances, waves, NPC hitboxes, or map collision.

## Native Windows verification

The new --smoke-pass57 exported Windows test uses **synthetic** JSON tool source records plus exactly one handcrafted tetrahedron OBJ named 555570001.obj. The fixture includes 9 source part records (8 visible) designed to exercise:

- One actual locally loaded OBJ (authored rbxassetid:// ID canonicalization)
- Two unresolved mesh asset IDs, one safely tube-shaped barrel proxy and one conservative receiver bounds
- One original sphere Part, one cylinder Part, one block Part, and one WedgePart
- One conservative UnionOperation proxy and one source-invisible Handle
- Unsafe ID/path rejection, deterministic tube geometry and cache reuse
- Accurate native 3D Armory diagnostic counts, unchanged player credits/skins, and no source renderer leaking into an active Manor match

Synthetic owner-like inputs are cleaned before the public privacy exclusion audit. GitHub Actions artifact uploads remain disabled and original source packs stay outside Git.

## Owner-only install

A separate owner-private ZIP was prepared: TWR_Pass57_Private_Geometry_and_Visual_Kit.zip. It includes the verified Pass47 original weapon assembly sidecar, a geometry audit manifest and the Pass54-56 reference/skin packs. Extract its Content directory beside the offline Windows EXE.

The kit **does not contain the original 1,137 remote mesh triangle assets**. Additional authorized, unit-normalized OBJ files can be installed by numeric asset ID under Content/Assets/Meshes. Those real local meshes then supersede the Pass57 shape proxies automatically. Nothing in Pass57 requires Roblox or internet access at runtime.

## Fidelity work still open

The largest remaining visual gaps include 1,137 custom remote mesh references, many original animated textures, UnionOperation geometry, exact original weapon animation timing and first-person pose, complete map interior geometry/surface appearance, original UI details, extended real player-controlled 15-wave sessions, and a private downloadable Windows EXE built with owner assets. A successful native smoke would verify **code behavior with fabricated assets**, not 1:1 visual or whole-game completion.


## Verified Pass57 Windows checkpoint — October 11, 2026

**Tested commit:** `1b948edb1133315838515b05e5bb5bb824bc4ec6`.

**Successful CI run:** https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38115844881

- **518 Python tests passed**, with one previously existing Pass34 FutureWarning.
- Godot/.NET Windows EXE build/export **succeeded, zero errors**, with one pre-existing Pass36 `CS0108` compiler warning.
- Base simulation: `TWR_SMOKE_COMPLETE_OK maps=10 waves=150` — domain-level wave progression only, not a human-played completion certificate.
- Native Pass57: `TWR_SMOKE_PASS57_GEOMETRY_OK synthetic_only=true source_visible_parts=8 local_obj_meshes=1 unresolved_source_mesh_ids=2 circular_mesh_proxies=1 conservative_bounds=2 native_primitive_parts=4 wedge_mesh=true cylinder_mesh=true canonical_rbxassetid_url=true path_traversal_blocked=true armory_provenance_shown=true player_economy_unchanged=true owner_mesh_triangles_not_claimed=true`.
- CI's owner-private/synthetic asset exclusion audit passed after source-lobby, weapon-assembly and hand-authored OBJ fixtures were removed. Public Windows executable upload actions remain disabled.
- Separate owner-only geometry and reference kit ZIP passed independent archive-integrity checking and includes the source-derived 98-model tool transforms (100 names including two aliases), source weapon metadata, and the Pass54–56 private visual packs. It contains **no** custom remote mesh triangle bytes. Native CI tested a hand-authored tetrahedron OBJ, **not** a genuine remote Roblox mesh.

**Result:** Verified functionality for optional local mesh resolution and source-based primitive approximation. Visual or gameplay-wide 1:1 completion is NOT established.

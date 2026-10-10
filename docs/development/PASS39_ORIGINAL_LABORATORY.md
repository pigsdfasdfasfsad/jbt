# Pass 39 — Original Loaded Laboratory Reconstruction and Layered Source Map

**Engineering basis:** owner-held `TestPlace/TestPlace.rbxlx` from `TestPlace.zip`, SHA-256 `272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`. This is the **loaded** `Workspace/Map` snapshot, not the reduced `ReplicatedStorage/CMaps/Laboratory/Map/Objects` LOD collection from Pass 36.

## What was recovered from original authored transforms

| Original Laboratory source component | Count |
| --- | ---: |
| Loaded visual geometry records (Part, WedgePart, MeshPart, UnionOperation) | **34,268** |
| Geometry with authored `CanCollide` | **16,613** |
| Source invisible CMaps Server Wall records | **1,517** |
| Enabled Point/Spot/SurfaceLight instances | **836** |
| Enemy spawn markers | **15** |
| Player spawn markers | **8** |
| Item spawn markers | **127** |
| Fortification spawn markers | **47** |

The exported private `Content/Maps/Laboratory.scene.jsonl.gz` contains the original source world positions, rotations, dimensions, colors, material token, transparency, collision flag and `Lighting` service metadata. The existing `LaboratorySourceLoader` automatically prefers that scene over the approximate Laboratory blockout when the private file is installed.

**Exact scene compressed SHA-256:** `35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74`.

The source snapshot contains MeshPart/UnionOperation **references but not the original custom triangle bytes**. When unavailable, their Godot rendered and physical bounding proxies remain approximations. Cloud texture, sky, clothing and original animation binaries were not recovered. Full original terrain SmoothGrid was not present in this TestPlace snapshot.

## Collision reconstruction

The owner-private `Content/Collision/Laboratory.col26.gz` is a deterministic, SHA-bound **TWRCOL26 v1** static physics cache: **18,130** source authored collision shapes over **180** spatial tiles, **217,204** triangles, including **4,501 explicit bounding-shape approximations**. It is pinned to the exact gzip scene bytes and rejected automatically if the source file differs. Box and wedge primitives derive geometry from the original transform matrix; missing custom meshes are not misrepresented as source triangles.

Collision tiles replace the worst-case per-object fallback collider hierarchy. This is a structural optimization, **not** proof of human traversability or original collision-group parity. The original place's actual collision-group matrix, door scripting, navigation surfaces and dynamic geometry remain incomplete.

## Original Laboratory floor plans

Three owner-private PNG views are generated directly from original coordinate bounds, not from a fictional reconstruction:

- **Lower:** source Y [-10, 16) studs
- **Main:** source Y [16, 32) studs
- **Upper:** source Y [32, +infinity) studs

Each shows the original server-wall outlines, physically collidable source primitives, original player/infected markers, pickup and fortification anchors. Color and legend indicate explicitly what is source geometry versus bounded proxy. The original source snapshot does **not** establish global true North, so image axes are labeled in Roblox source coordinate units instead.

The Godot class `Pass39LaboratoryFloorplan` requires the exact original scene SHA plus all three expected PNG SHA-256 digests before showing the floor plans during a regular Laboratory match.

**Controls:** `F4` to open/close; `Left`/`Right` to switch source height level while open. The overlay defaults OFF. It is unavailable on approximate fallback Laboratory and does not affect physics, entity simulation or saved progression.

The F10 and F11 diagnostics include exact-source verification status and the number of loaded floor plan layers.

## Private installation and playability

For owner-use place these files alongside the new Windows executable:

```text
Content/Maps/Laboratory.scene.jsonl.gz
Content/Collision/Laboratory.col26.gz
Content/MapPlans/Laboratory.sourceplan39-0.png
Content/MapPlans/Laboratory.sourceplan39-1.png
Content/MapPlans/Laboratory.sourceplan39-2.png
```

Previously installed Pass 37/38 audio and lighting private packs remain supported. The public GitHub branch includes **only code, synthetic fixture tests and this documentation**, never the original source scene XML, original source-world geometry packs, generated source-coordinate PNGs or private audio binaries.

## Verification distinction

GitHub Actions uses a **fabricated** 20-part Laboratory scene, three generated sample PNGs, and --smoke-pass39 inside the compiled **exported Windows executable**, checking F4 open/close and floor selection. No owner-private source is included in CI. The real private 34,268-item scene and 18,130-collider cache are validated structurally and SHA-checked offline, but **a real Windows graphical traversal/performance test with those private assets is still required**. In particular, this does not establish 15-wave playability, acceptable FPS, original custom mesh accuracy or ten-map full source fidelity.

The other nine release maps require the separate owner-held `twr places` full loaded-map snapshots for complete geometry. Their existing fallbacks and Pass 36 optional source wall/prop fragments are preserved.


## Nine additional original CMaps source fragment sidecars included privately

The owner-side Pass 39 package also reconstructs the **existing Pass 36**
source wall, map-object and top-down-plan packs for the other nine maps,
using exactly the original source export schema. All **18 source JSONL.GZ
SHA-256 digests** and **nine PNG SHA-256 digests** were reproduced byte-for-byte
against the known compiled runtime constants.

- 6,920 positioned original invisible server-wall records, collision OFF by default
- 3,556 CMaps source object records, 305 available native Part/Wedge previews
- 3,251 original MeshPart/UnionOperation vertices still missing
- Nine original Server Wall footprint PNGs for F4
- F5 original native prop previews and F6 optional source-wall collision

These are **not** nine full recovered Workspace/Map scenes: the nine full
map geometries still depend on the missing original place snapshots, and
experimental source collisions on fallback blockouts may be unsafe. The
original Pass 36 loader is unchanged and retains all opt-in safeguards.
The real original source sidecars and map-plan PNGs are distributed privately
and never uploaded into public Git history or Windows CI artifacts.

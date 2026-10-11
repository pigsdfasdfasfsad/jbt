# Pass 44 — Native Roblox Part/Wedge Spatial Streaming (Original Laboratory)

**Source owner file:** `TestPlace/TestPlace.rbxlx` from the privately supplied `TestPlace.zip`.

**Input:** `Content/Maps/Laboratory.scene.jsonl.gz` (Pass 39 original-positioned source pack), exact SHA-256 `35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74`.

**Output:** `Content/Geometry/Laboratory.native28.gz` (TWRINS28 v1 cache), SHA-256 `9484e8507d71ae84bf33b1a66a3431e2c250d5ccdf1af825712fd5bc5524b3c3`.

## What changed

An earlier Godot class, `Pass28PrimitiveStreamer`, already supported bounded, source-SHA-verified native-Part MultiMesh streaming. However, the **owner Windows Pass 43 ZIP had no `Content/Geometry/Laboratory.native28.gz` file**, so this feature was dormant in practice.

Pass 44 generates the missing private cache deterministically from the available source geometry records. The exporter preserves the source CFrame rotation, position, local size, material token, color, alpha and CastShadow flag. It converts Roblox source studs to Godot metres (0.28) and reflects Z once, exactly as `LaboratorySourceLoader` does.

| Original Laboratory render record | Count |
| --- | ---: |
| Total source visual geometry records | 34,268 |
| Eligible visible native Part records | **18,180** |
| Eligible visible native WedgePart records | **33** |
| Total real native instances streamed | **18,213** |
| Spatial grid tiles (96 source studs) | **47** |
| Grouped MultiMesh draw batches | **787** |
| Custom MeshPart/CSG and other non-native records | 15,600 |
| SpecialMesh cases kept in legacy renderer | **337** |
| Invisible source native Parts excluded from drawing | **118** |

The original collision source for those parts is **unchanged**. The owner-side Pass 26 cache still uses the original 18,130 collision volumes. None of the missing custom triangle binaries are fabricated or recovered by this pass.

Before Pass 44 the source native parts were batched by material across the entire map, with no original-coordinate spatial subdivision. Pass 44's cache uses the existing `Pass28PrimitiveStreamer` with per-batch world-space bounding boxes, 145-metre draw radius and 166-metre hide hysteresis. It only refreshes on meaningful player movement. This **should reduce off-screen rendering**, but **does not prove higher measured FPS** on the user's hardware.

## Critical bug fixed

The old `LaboratorySourceLoader` checked `specialMeshType != "5"` before suppressing original Part rendering when an instancing pack existed. It would therefore silently suppress `specialMeshType=6` special shapes even when the cache did **not** include them. Pass 44 now keeps **all** special-mesh Parts in their previous fallback renderer and suppresses only eligible native brick/wedge parts represented by the new cache. Original physical collision remains preserved regardless of render eligibility.

If the owner file is missing, corrupt, oversized or linked to a different source-scene SHA, `Pass28PrimitiveStreamer` rejects it and the original legacy visual renderer is used. Native streaming is never required for successful game startup.

## Reproducible private generation

Run the following locally, from the repository root, using the user's legitimately held source pack:

```powershell
python tools/validation/build_source_primitives44.py --scene Content/Maps/Laboratory.scene.jsonl.gz --out Content/Geometry/Laboratory.native28.gz
```

The generated `LABORATORY_NATIVE_RENDER_MANIFEST44.json` records byte hashes and counts. It can be compared to the owner ZIP's private version. The exporter accepts `--synthetic` only for synthetic CI scenes, never original owner packs. Public GitHub history contains **only the exporter, runtime, tests, documentation and synthetic fixture generator**—not the original source cache.

## Testing

`tests/test_pass44_native_source_streaming.py` checks deterministic cache generation and independently decodes the exact packed binary format, verifying source SHA, transforms, exclusion categories, and cleanup.

Windows CI exports an actual Godot/.NET Windows executable, installs a fully **fabricated** 20-part Laboratory scene and a synthetic source-bound native stream:

- 18 visible ordinary native Parts are packed.
- One `specialMeshType=6` Part remains in the legacy renderer.
- One invisible Part has no visible rendered instance.
- Native parts in both near and distant regions are grouped into different source tiles.
- The exported game verifies the correct native instance counts, batch counts, fallback special geometry, unchanged collision body and dynamic tile visibility after moving the player.
- Synthetic scene and `native28.gz` files are removed before uploading the public executable build.

**Remaining acceptance gate:** launch the owner-built private Windows ZIP with the full 34,268-record scene. Run an actual 15-wave, player-controlled Laboratory match, record F10/F11 frame-time distributions before/after the cache, and photograph or screenshot walls, floors and mesh proxies for visual QA. No native Windows synthetic fixture establishes original scene FPS or custom mesh accuracy.

## Installation

The owner-only Pass 44 ZIP includes the compiled offline executable and a `Content/Geometry` directory containing the original owner-side `Laboratory.native28.gz` and manifest. These sit alongside the existing Pass 39 source scene, collision cache and Pass 42 navigation graph. All previous original map and game content remains intact. There is no internet, Roblox installation or Roblox Studio requirement.

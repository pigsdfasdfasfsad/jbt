# Pass 45 — Spatial Culling for Remaining Laboratory Mesh/CSG Proxies

**Branch:** `twr-pass45-lab-proxy-visibility`  
**Base:** verified Pass 44 original Laboratory native Part streamer.

## Why this pass exists

Pass 44 provided source-bound `Laboratory.native28.gz`, enabling distance
culling of 18,213 ordinary original-positioned Parts and Wedges. The other
rendered geometry was still produced by `LaboratorySourceLoader` in
**map-wide MultiMesh batches**, so a single object near the camera often
kept other geographically distant objects in the same render batch active.

From the private 34,268-record Laboratory scene:

| Original source record class | Count |
| --- | ---: |
| Drawn native Part/Wedge instances streamed by Pass 44 | 18,213 |
| Remaining visible source MeshPart/CSG/SpecialMesh proxy records | **15,678** |
| Original geometry invisible, not drawn | 377 |
| Total source geometry records | **34,268** |

**No original missing custom triangle data has been recovered.** The
15,678 records still use prior proxy meshes, source-sized geometry bounds,
offline-prepared meshes when available, or source-specific special-mesh
fallbacks. Positions, rotations, transparency, material codes, color and
collision remain as the existing recovered source specified.

## Implementation

`LaboratorySourceLoader` now groups the fallback source visual records by
**256-stud original X/Z tiles and source appearance**, rather than by
appearance alone across the entire map. Related tile groups share
`Mesh`/`StandardMaterial3D` resources to avoid duplicating them.

`Pass45FallbackProxyStreamer` holds those source visual batches, with
world-space AABBs computed from each object's complete actual transformed
`Mesh.GetAabb()`. These are not tile-centre approximations. They include
large rotated beams and author-specified `SpecialMesh.Scale` and
`SpecialMesh.Offset` transforms, and allow loaded legitimate custom
Mesh resources with differing local bounds.

The runtime uses a 145-metre initial/draw radius, 166-metre hide radius
during movement (hysteresis), a 0.32s refresh interval, and a 5-metre
player displacement trigger. This affects only visibility of static source
proxy meshes, never physics, navigation, player inventories, enemy spawns,
collision or source-object scripts.

**Safety/fallback:** This grouping is active only for the full recovered
Laboratory map when its Pass44 native Part pack was verified and loaded.
Without that pack, the previous appearance-only grouping remains. All
nine other release map fallbacks are unchanged.

## Original-source static audit

Independent calculations from the owner-held, SHA-bound
`Content/Maps/Laboratory.scene.jsonl.gz`, SHA-256
`35ba9ce77ef220448cdc087679afcc5ef727fa41f54f61dba25e886bc8c8ef74`,
find **669 appearance-and-tile batches** for these 15,678 fallback
visual records when original custom triangle binaries are unavailable.

At original Laboratory player spawn #1, with the **145m draw radius**:

| Static comparison | Before (world-wide grouping) | After (tile culling) |
| --- | ---: | ---: |
| Total batches | about 216 | about 669 |
| Batches active at source spawn #1 | about 202 | **455** |
| Fallback proxy instances in active batches | about 15,258 | **11,155** |
| Proxy instances in hidden batches | about 420 | **4,523** |

The more spatially precise renderer draws **more batches but fewer
off-screen proxy instances**. Total draw-call overhead can offset any GPU
fill-rate savings. These counts are computed from the recovered source
and proxy AABB envelopes; **they are not observed game FPS, GPU performance,
or proof that this renderer is faster**. Rendering of original custom
triangle meshes could alter exact bounds if more source media is recovered.
A private `LABORATORY_FALLBACK_STATIC_AUDIT45.json` provides all eight
source player-spawn calculations, including 145m and 166m visibility.

## Controls and owner A/B performance testing

**F1** during Laboratory gameplay toggles only proxy batch culling.
On by default. It does not change the native Pass44 renderer or collision
state. F10 now reports source proxy count, active/total proxy batches,
proxy instances visible, and current F1 mode. F11 records that information
alongside real sampled p50/p95/p99 frame times and existing jump metrics.

Recommended manual A/B protocol:

1. Launch the Pass45 owner Windows ZIP with its source scene, native28 and
   existing private collision/navigation/light packs in place.
2. Enter Laboratory, stand at a reproducible position. Press F10, record
   visible proxy/native batches, sample count and enemy count.
3. Keep the same resolution, graphics settings and view. After 45–60
   seconds in ordinary gameplay, press F11 and record F1 ON sample.
4. Press F1 to turn proxy culling OFF. Wait another 45–60 seconds at
   similar gameplay load and press F11 for the OFF sample.
5. Toggle F1 ON again and capture one further F11 report to reduce time
   trends. Compare p95/p99 frame time and infected counts; F11 archives
   timestamped reports under the Godot local user-data directory.
6. Repeat at original spawn, interior corridors and areas with visible
   huge far-mountain proxy geometry. Capture F12 frames for visual parity.

This is not a substitute for a real 15-wave player-controlled run, and
a 15-wave lifecycle smoke does not prove rendering under actual wave load.

## Tests and privacy

The public CI fixture is **entirely fabricated**: 18 native Parts,
one nonpacked SpecialMesh, one invisible Part and two synthetic near/far
MeshPart proxy records. The native exported Windows test checks:

- F1 culling ON/OFF/ON changes the real Godot renderer counts (2/3/2).
- Moving the player ~504m away swaps to only the far proxy batch.
- Rotation-aware Mesh bounds are not falsely culled.
- Original source collision remains installed.
- The retained Pass44 source ordinary native Part stream still loads.
- p50/p95/p99 game sampling remains available.

All fabricated and owner-private source scene, original collision,
navigation, source images and native cache data are excluded from GitHub
Actions build artifacts. Source code, tests and this documentation may
remain public. The source-private all-in-one Windows ZIP must remain
owner-controlled.

## Remaining gaps

There are still nine missing complete original-world maps, absent custom
MeshPart/UnionOperation triangle binaries, original terrain/audio/media
differences, incomplete real-source enemy traversal, and no measured
player-controlled 15-wave match performance results. This pass is a
bounded rendering optimization and diagnostic A/B experiment, not a claim
of a complete 1:1 replica.

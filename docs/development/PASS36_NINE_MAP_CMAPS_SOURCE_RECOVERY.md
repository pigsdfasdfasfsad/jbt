# Pass 36 — Original CMaps Server Wall and Map Object Source Fragments

**Windows verification:** GitHub Actions run [38076152654](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38076152654), 408 Python tests, Godot/.NET compile, Windows export, F4/F5/F6 native smoke. Owner-private source packs are distributed separately alongside the verified executable, NOT in public GitHub.

## Original source recovery

Source: owner-held `TestPlace/TestPlace.rbxlx` SHA-256 `272c478460c32bd332b7314eea0b69c08d0d78a34bfbfa1f796d6f059976f0d2`.

Nine maps have **6,920 original invisible Server Wall records** with authored sizes, rotations and positions; **6,866** are native Part/Wedge collisions and **54** District MeshParts use labeled oriented-box collision proxies (original mesh vertices missing). The same nine maps supply **3,556 CMaps Map Objects** with their original transforms and source metadata. Of those, **305** are directly renderable native Part objects; the remaining **3,251** custom MeshPart/UnionOperation geometries are absent from the owner-held source and are not falsely displayed as recovered mesh triangles.

Per-map Server Wall counts: Ranch 1266, Mill 1055, Bypass 768, Cabin 871, Cargo 321, District 346, Expressway 274, Prison 1234, Manor 785.

Previously recovered Laboratory 1,517 source Server Wall records are already included in its actual scene pack and are not duplicated.

## Implementation

`tools/validation/recover_cmaps_source_fragments36.py` deterministically exports SHA-checked compressed wall and object records. `tools/validation/draw_source_map_plans36.py` generates nine bird's-eye PNG source footprints. `Pass36SourceFragmentsRuntime.cs` checks the original source and packaged SHA before creating geometry. `Pass36MapPlanOverlay.cs` verifies the PNG SHA and provides an in-game map viewer.

**F4** shows source Server Wall outlines. **F5** previews only native source geometry (missing custom meshes are omitted), default OFF. **F6** toggles Server Wall collision on isolated source layer 16, seen by players, infected, bullets and spore physics, default OFF. F7/F8 independent special walls and F9 Lab accessibility are unchanged. F10/F11 display/export inventory and toggle statuses.

## Why the new fragments are off by default

The nine non-Laboratory maps still use *approximate blockout maps* with unverified alignment to original coordinates and player/infected spawn locations. The source CMaps Server Walls are **invisible collision geometry**, NOT the complete original visual maps, terrain or custom meshes. Turning these barriers on at their true coordinates without the full map could disrupt gameplay. The optional source previews make actual recovered evidence inspectable while preserving existing gameplay defaults.

## Windows validation

CI uses entirely fabricated one-wall/one-native-prop/one-missing-CSG Manor source packs and a tiny synthetic PNG. The exported Windows executable checks F4/F5/F6 state, layer separation, physics contact, native source display and missing-mesh omission; its temporary fixtures are deleted before the public artifact uploads. Private source fragments do not enter the GitHub build.

Outstanding: complete geometry and terrain for nine maps, missing original meshes and collision-group matrix, verified original exterior entrances, player-controlled 15-wave full-match testing and real FPS measurements with all owner-derived source assets enabled.

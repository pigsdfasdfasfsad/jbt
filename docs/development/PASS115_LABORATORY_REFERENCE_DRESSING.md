# Pass 115 — Laboratory source-screenshot-guided 3D environment dressing

**Baseline:** [Pass114 Windows CI run 38446320183](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38446320183) completed successfully, including its dedicated native corpse-on-slope test and the wider gameplay regressions.

## Why this pass changes the priority

The existing Ten-Map blockouts had functioning character motion, collision, infected spawns, objectives, and wave testing, but many interiors were still **featureless, uniformly colored boxes**. Continuing tiny animation refinements before rebuilding recognizable rooms would not meaningfully advance the requested 1:1 map appearance. This pass therefore focuses on one of the original game's most distinctive maps, Laboratory, and uses owner reference screenshots to give its offline placeholder environments identifiable landmark objects.

**Reference evidence (available only in the owner's Project archive):**
`images.zip` includes `Laboratory-ReceptionDesk.png`, `Laboratory-ServerRoom.png`, `Laboratory-ResearchRoom.png`, `Laboratory-ExaminationRoom.png`, `Laboratory-ParkingGarage.png`, `Laboratory-FacilityEntrance.png`, and `Laboratory-Cafeteria.png`. They show a wide pale lobby counter with dark ALYTH corporate lettering, cool fluorescent ceiling fixtures, blue scanning machinery, racks and walkways in a server facility, research benches and cabinetry, an examination room, a sparse parking structure, and rows of institutional cafeteria seating. The previously recovered Laboratory map dossier also describes a makeshift emergency ward containing beds and screens.

The screenshots establish **the existence and general appearance of the landmark categories**, not exact original world-space coordinates, mesh triangles, dimensions, collisions, lighting values, or prop counts. Every new object is labelled **approximate**. The screenshot files and their original bytes are **not committed, copied into public content or downloaded at runtime**.

## Actual implemented scene changes

`src/Twr.Godot/Scripts/Pass115LaboratoryDressing.cs` builds **seven** named, render-only clusters as children of a single `Pass115LaboratoryDressing` node:

| Cluster | Source-guided render content |
|---|---|
| `ReceptionDressing` | Multi-section pale reception counter, dark desktop monitors, ALYTH sign, blue-lit door scanner, entrance benches and cold ceiling lights |
| `ServerDressing` | Six two-row-by-three-bank dark rack units, instrument indicator lights, overhead rail cues and cold fluorescent lamps |
| `ResearchDressing` | Six metal/white counter workstations, screens, cabinets and overhead fixtures |
| `ExaminationDressing` | Three examination cots with mattresses/frames, wall cabinets and handles |
| `GarageDressing` | Two parked automotive silhouettes with roof/window/tire clusters, structural-looking pillars and fluorescent fixtures |
| `WardDressing` | Four emergency cots with supports and pillows, canvas-like privacy partitions and rails |
| `CafeteriaDressing` | Four dark tables, paired bench seats and sparse potted greenery |

The scene is original, locally authored primitive Godot art. Every added prop is a `MeshInstance3D`, `Label3D`, or `OmniLight3D` under a named `Node3D`. No extra `StaticBody3D`, collision shape, weapon hitbox, pathfinding obstruction, objective anchor, pickup, spawn marker, hazard or gameplay trigger is introduced. There is a hard cap of **260** mesh parts and at most 10 small local lights in this pass.

**Critical owner-source protection:** `GameplayRoot` first calls `LaboratorySourceLoader.TryBuild`. `Pass115LaboratoryDressing.AddToFallback(root)` is called **inside `MapBlockoutBuilder.BuildLaboratory` only**. If an owner-private source Laboratory scene is installed and loads successfully, `MapBlockoutBuilder` is not invoked. The new approximate objects therefore cannot cover up or change original imported Laboratory geometry. All nine other game maps are untouched.

## Verification

The static source test `tests/test_pass115_laboratory_reference_dressing.py` asserts all seven named areas, principal props, ALYTH lettering, render-only objects, original-map precedence, unchanged first-person Laboratory spawn, old-gameplay gates and zero private screenshot assets.

The new native `--smoke-pass115` runs the **real existing Pass72 10-map lifecycle** in the exported Windows Godot executable, not a fake scene: one real map gameplay root at a time, actual grounded first-person player, real pickup actors, infected waves 1 and 2, domain survival rewards, test-only match results, scene disposal and lobby return.

For Laboratory, the native checker requires exactly seven known landmark nodes, at least **140** total render-mesh components but no more than **260**, at least **8** real local lights, original-geometry metadata marked *false*, no collision objects and at least 12 mesh components per landmark. For the other **nine** maps, this Lab-only dressing node must be **absent**. The dedicated native marker `TWR_SMOKE_PASS115_LAB_DRESSING_OK` requires all ten maps to finish without changing the original `TWR_SMOKE_PASS72_TEN_MAP_ENTRY_OK` success conditions. Every older Pass70–114 map, infected, weapon, Load/Repair, Escort, save and original-asset leakage regression remains in Windows CI.

## Windows native checkpoint

The first two Pass115 exported-Windows source commits passed the **dedicated Pass115 seven-zone / ten-map native acceptance** in [Actions 38449900951](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38449900951) and [Actions 38449959826](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38449959826), including the older Pass111–114 live weapon and corpse checks. That is native executable gameplay evidence, not screenshots of the Godot scene.

A later source-only visibility adjustment moves emergency-ward beds above the historical raised slab and the cafeteria tables beside, not inside, the older bridge proxy. Those source and Python assertions are in [latest Pass115 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass115-laboratory-reference-dressing); confirm its exact-commit conclusion separately before describing those latest tweaks as native-verified. A green dedicated stage is not proof that the entire historical regression suite finished.

## Important fidelity limits and next map milestone

This is **recognizable landmark set dressing**, not a recovered 1:1 Laboratory. The original source screenshot relative layouts cannot safely be converted into actual coordinates without original `.rbxlx` world geometry. The approximate placement in the fallback shell does not reproduce original complex staircase access, exact bridges, vents, level topology, dynamic sound, medical equipment animations, lighting probes or asset textures. As a deliberate first compatibility step the dressings do not collide; furniture is visually present but currently not a physical obstacle.

Follow-up work should prioritize authoritative recovery of raw original Laboratory geometry; placing true 3D meshes/materials and collisions into each room; preserving the validated 10-map navigation; capturing exact-camera 1080p gameplay screenshots against these owner-provided original frames; and documenting a geometry/visual deviation ledger before expanding this system to the other nine maps.

**Validation status:** A committed file is not a fully validated game. The latest `Pass115` Windows workflow must pass the dedicated ten-map test and its larger regression suite. Headless geometry checks do not certify visual pixel parity. No compiled owner-private `.exe` is publicly distributed.

[Pass115 source branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass115-laboratory-reference-dressing) · [Live Windows acceptance](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass115-laboratory-reference-dressing)

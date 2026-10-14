# Pass 116 — Laboratory floor finishes, industrial textures, and emissive fixtures

**Verified parent:** [Pass115 Windows run 38450462709](https://github.com/pigsdfasdfasfsad/jbt/actions/runs/38450462709) passed its full Windows and native game regression matrix. The owner-provided seven-view Laboratory reference montage makes three remaining differences apparent: clinical floor surfaces are almost entirely untextured, industrial zones lack repeating texture, and illuminated monitoring equipment remains flat-colored. This pass remedies those gaps in the **source-absent Laboratory fallback only**.

## Real implementation

`src/Twr.Godot/Scripts/Pass116LaboratorySurfaceMaterials.cs` builds small, **deterministic 64×64 textures entirely in memory** from authored color palettes and tile/grout patterns; no online content, Roblox texture IDs, screenshot bytes, asset archives, or original mesh/material exports are loaded or written:

| Generated finish | Visual application |
|---|---|
| `clinical` | Reception floor and its west/east walls |
| `research` | Research-room floor and west/east walls |
| `industrial` | Server room and parking garage floors/ceilings |
| `vinyl` | Visual hospital-ward raised slab |
| `acoustic` | Reception and research-room ceiling panels |

For each existing fallback `StaticBody3D` room piece, the pass finds its already-existing **single MeshInstance3D child** and changes **only `MaterialOverride`**. The static body's `CollisionShape3D` and all world geometry, entity positions, physics layers, movement routes and objective actors are unchanged. Material UV repeat factors are chosen separately for each room dimension. Exactly **five** floor/decorative slabs, **four** roofs and **four** walls are treated.

The pass also adjusts the existing Pass115 visual-only props to use **emission-enabled** materials: small server-rack green/blue status LEDs, the entrance blue terminal/indicator, the reception monitor, and ten ceiling fluorescent diffusers. This creates visible glow from actual meshes rather than merely brightening point lights. The existing limited, shadow-free `OmniLight3D` fixtures are retained.

Finally, **ten thin, render-only parking-lane and bay stripes** sit just above the existing parking garage floor. They have no collision geometry and are not claimed to reproduce original parking-bay numbers or dimensions.

### Hard constraints

- `Pass116LaboratorySurfaceMaterials.AddToFallback` runs only within `MapBlockoutBuilder.BuildLaboratory`; **owner-original Laboratory source always takes precedence** through `LaboratorySourceLoader.TryBuild`.
- No additional `StaticBody3D`, `CollisionShape3D`, `RigidBody3D`, `Area3D` or navigation is created. The existing 10-map spawn points, physics, wave timers, inventory, reward authority and original private asset contracts are unchanged.
- Exactly five small in-RAM textures. These represent an **inferred look**, not recovered retail asset pixels. Screenshots and the original Roblox skybox/source material bytes stay private and outside public GitHub.
- Lights and texture materials are intentionally bounded; this pass does not claim performance parity or support for a full source lighting bake.

## Native verification contract

The new `--smoke-pass116` extends the established **real exported-Windows ten-map gameplay acceptance**, which sequentially launches each actual `GameplayRoot`, creates a grounded first-person player and active pick-ups, spawns infected in both Wave 1 and Wave 2, validates rewards, saves test-only results and returns to the lobby.

On **Laboratory only**, native acceptance checks:
- One source-safe `Pass116LaboratorySurfaceMaterials` renderer.
- Exactly **five** 64×64 patterns and **13** repainted room meshes (5 floors, 4 ceilings, 4 walls).
- At least **28**, at most **48**, emission-enabled source-guided light/status meshes.
- Exactly **ten** thin garage lane/bay decals.
- No added physics colliders and preserved `CollisionShape3D` under each tested existing static floor.
- The actual floor mesh has a live `StandardMaterial3D` override holding the expected `ImageTexture` in memory.
- No Pass116 node present on the other nine maps.
- Exact original material, screenshot-byte and collision-modification metadata marked **false**.

The Windows gate demands native `TWR_SMOKE_PASS116_LAB_SURFACES_OK`, preserved Pass115 landmark marker and preserved Pass72 ten-map Wave2 lifecycle marker from **the same executable**. It retains all previously proven Pass70–115 mouse/weapon, objectives, infected, navigation, save and private-source-leak gates.

`tests/test_pass116_laboratory_surface_materials.py` enforces the code/budget/scene/privacy contracts independently of compiled native acceptance.

## Fidelity limits / next pass

This improves **recognizable screenshot-derived surface categories**. It is not a 1:1 import of original tiled diffuse/normal/roughness maps, Roblox PBR shading, realistic light bounce, fixed prop transforms, accurate 3D silhouettes or source-authenticated original Laboratory geometry. Godot headless CI cannot establish rendered screenshot similarity. For exact visual comparison, capture local actual-game views with F12 using `FidelityCaptureRuntime` and compare against the owner's private originals. Do **not** publish those original PNG frames or generated source-asset streams.

**Status:** Source and native CI must be evaluated at the exact latest commit before marking Pass116 verified. No public download artifact containing private assets is enabled.

[Pass116 branch](https://github.com/pigsdfasdfasfsad/jbt/tree/twr-pass116-laboratory-surface-materials) · [Pass116 Windows CI](https://github.com/pigsdfasdfasfsad/jbt/actions/workflows/windows-build.yml?query=branch%3Atwr-pass116-laboratory-surface-materials)

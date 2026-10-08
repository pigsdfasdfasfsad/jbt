# Expressway reconstruction dossier

![Expressway reconstruction outline](../assets/maps/expressway-outline.png)

![Expressway supplied reference board](../assets/reference-boards/expressway-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Cloudy
- **World location:** Indianapolis, Indiana, USA
- **Supported objective families:** Load, Repair, Radio, Unpack, Damage, Escort
- **Collectibles / easter-egg references:** Spiffo plush

## What the reconstruction must preserve

*Source: Expressway.wiki*

- **Layout character:** a **highway bridge extension** with abandoned vehicles packed together along it.
- **Landmarks:** a **medical screening zone** shaped as a bottleneck formed by precast concrete panels
  secured with sandbags, with two abandoned military humvees nearby; inside are crates, wooden pallets
  and other objects, partially divided by a fence and a few road blocks. Outside the zone, behind the
  concrete walls and fence: **two ramps**, two *Alyth* patrol vehicles, a black SUV, and a **helicopter
  mounted with a Browning M2 machine gun**.
- **Infected spawns:** both the front side of the bridge, behind the 18-wheeler beyond the map's
  boundaries, **and** behind the medical screening zone, **using the ramps to vault over the concrete panels**.
- **Map-specific mechanics / details:**
  - The right side of the bridge has a long stretch where infected line up.
  - Long-distance kills on this map grant the **"long range" kill bonus**.
  - **Spawn history:** Expressway previously had infected spawning on the front side only. When spawn-zone
    changes were first implemented, on the **24th of December 2021**, no explanation was given and the map
    was reverted to its original state the next day for unknown reasons; after changes were made again on
    the **16th of January 2022** it was officially stated that one of the driving issues was complaints
    about the **lack of teamwork**. (The page's own Change History dates the re-add to the
    **January 17, 2022** patch — see §Source Notes.)
  - On Classic and Hardcore, items spawn more slowly, so high-capacity weapons (M60, MG 42) are advised;
    Juggernaut Infected arrive **at both ends of the map**.

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- No named locations derived from filenames; use source hierarchy extraction.

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (6)

- `Card-expressway.png`
- `Expressway.png`
- `ExpresswayIcon.png`
- `ExpresswayPreview.png`
- `ExpresswayPreview2.png`
- `Vote-expressway.png`

## Build sequence

- [ ] Freeze/hash source map and associated evidence.
- [ ] Extract hierarchy, transforms, dimensions, materials, collision and named anchors.
- [ ] Generate a machine-readable instance inventory.
- [ ] Generate top-down bounds/outlines from extracted geometry.
- [ ] Build engine-neutral asset mapping and resolve external dependencies.
- [ ] Reconstruct static scene in Godot.
- [ ] Add collision/navigation and validate traversal.
- [ ] Add spawn, item and objective anchors.
- [ ] Recreate lighting, atmosphere and audio zones.
- [ ] Run visual/fidelity comparisons against supplied captures.
- [ ] Run gameplay acceptance tests and record known deviations.

## Completion evidence expected

A map is not considered reconstructed merely because it renders. Completion requires a source manifest, normalized scene data, top-down diagram, instance/asset inventory, collision/nav validation, spawn/objective validation, comparison captures, automated tests where possible, and a written deviation log.


## Implemented source-guided visual reconstruction (2026-10-08)

Expressway now uses `ExpresswaySceneBuilder.Build` rather than the original
monochrome road-and-box blockout. This is **not** a recovered original full
Expressway scene; it is a hand-authored geometric approximation grounded in
the six supplied Expressway map-reference images and this dossier.

The new scene constructs a 138-metre elevated bridge deck and visible
support pylons, lower freeway and city backdrop, asphalt and lane markings,
parapets, curved street lamps, abandoned civilian cars/pickups/trucks,
two military Humvees, an abandoned semi, checkpoint concrete barriers,
sandbags, road cones, chainlink fencing, stop signs, medical shelters,
pallets/crates, patrol SUV and a stationary helicopter. A cloudy procedural
sky and local light emitters replace the old flat grey presentation.
Colliders are assigned to the main deck, barricades, vehicles, fences
and other physical obstacles.

The new Expressway scene-loading smoke test executes in the exported Windows
executable and checks that the major geometry categories instantiate. **This
does not verify accurate original dimensions, textures, real navigation
connectivity, performance, or visual similarity.** Static layout/spawn points
remain approximations until original Expressway place/asset data is available.

Remaining visual work: actual source mesh/texture/CSG files, complete original
baked vehicle placements, detailed vehicle geometry, authentic atmospheric
skybox and lighting, more road surface wear, patrol markings, original
helicopter and medical camp assets, hands-on full-wave gameplay testing,
and matched-camera comparisons to the private screenshots.

# Manor reconstruction dossier

![Manor reconstruction outline](../assets/maps/manor-outline.png)

![Manor supplied reference board](../assets/reference-boards/manor-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Stormy Night
- **World location:** Eastern France
- **Supported objective families:** Load, Repair, Radio, Unpack, Escort
- **Collectibles / easter-egg references:** Spiffo plush; wooden statue

## What the reconstruction must preserve

*Source: Manor.wiki*

- **Layout character:** a **two-story French gothic manor** in a dark, stormy atmosphere; spacious both
  outdoors and indoors.
- **Landmarks:** guest rooms, offices and libraries on both floors; a **lengthy central courtyard**
  connecting the front and back entrance hallways, with doors on the sides of the courtyard path leading
  to both corridors; a garage; front entrance staircase and west staircase; front and back hallways;
  west and east corridors; a grand dining room and a dining room. Outside: expansive grass with
  occasional footpaths, tall metal fences bordering the map and **two entrance gates at the front**.
- **Infected spawns:** all infected spawn outside the borders and **hop over the gates and fences**.
- **Map-specific mechanics / details:**
  - The courtyard has **two main chokepoints**, but limited item spawns at later waves.
  - The upstairs office windows work as a shooting floor for elevated, high-magnification fire.
  - Outside running is viable (no obstacles) but **there are no item spawns outside**.
  - Front stairs force infected into one linear path — high-penetration weapons (M60 cited) are
    recommended — but this can lure infected up the stairs near the office and library.
  - Manor was originally planned to have a **rainy skybox with lightning effects**; never added, reason unknown.

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- Assets
- Build
- Build2
- Layout
- Layout2
- Statue Manor

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (28)

- `Card-manor.png`
- `Manor.PNG`
- `ManorAssets.png`
- `ManorBuild.png`
- `ManorBuild2.png`
- `ManorIcon.png`
- `ManorLayout.png`
- `ManorLayout2.png`
- `ManorPreview.png`
- `ManorPreview11.png`
- `ManorPreview12.png`
- `ManorPreview13.png`
- `ManorPreview14.png`
- `ManorPreview15.png`
- `ManorPreview16.png`
- `ManorPreview17.png`
- `ManorPreview18.png`
- `ManorPreview2.png`
- `ManorPreview3.png`
- `ManorPreview4.png`
- `ManorPreview5.png`
- `ManorPreview7.png`
- `ManorPreview8.png`
- `ManorPreview9.png`
- `ManorThumbnail.png`
- `Spiffo_on_Manor.PNG`
- `Statue-Manor.png`
- `Vote-manor.png`

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

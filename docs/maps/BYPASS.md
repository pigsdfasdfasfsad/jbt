# Bypass reconstruction dossier

![Bypass reconstruction outline](../assets/maps/bypass-outline.png)

![Bypass supplied reference board](../assets/reference-boards/bypass-reference-board.jpg)

## Release identity

- **Release mode:** Regular, 15 waves
- **Environment / skybox:** Cloudy Sunset
- **World location:** Chapel County, USA
- **Supported objective families:** Load, Repair, Radio, Unpack, Damage, Escort
- **Collectibles / easter-egg references:** Spiffo plush; 1 Infection Examination Form; police radio chatter

## What the reconstruction must preserve

*Source: Bypass.wiki*

- **Layout character:** centred around a road heading towards a city, bracketed by an **arch bridge**
  at one end (leading to a tunnel) and a **suspension bridge** at the other.
- **Landmarks:** small plaza with a parking lot; a *Foxoil* gas station; **two medical screening
  zones** with barricades and signs blocking/redirecting traffic on both bridges; abandoned vehicles
  along the road, parking lot, suspension bridge and behind the map boundary on the arch bridge;
  medical and military tents, with a concentration of tents opposite the parking lot under a steep
  hill; civilian barracks next to the gas station; utility corridor, transformer room and generator
  room (a row of three large generators) inside the tunnel. Background: vast sparse hills and
  skyscrapers.
  Interiors documented in the gallery: *CRISP CUTS*, *Marshal OUTDOOR SUPPLY*, *Tasty Toast All Day
  Breakfast* (plus its kitchen), *MEAN 'N CLEAN LAUNDROMAT*, a convenience store and its counter back end.
- **Infected spawns:** around the edge of many cliffs behind the plaza and civilian barracks; inside
  the tunnel; the road and hills underneath the arch bridge; and a medical screening zone on the
  suspension bridge.
- **Map-specific mechanics / details:**
  - The **tunnel on Bypass directly leads to the tunnel on Mill** — the two maps are connected.
  - Unclear voices of people chattering over the police radio can be heard on the police cruiser
    located on the arch bridge.
  - Turtling: the civilian barracks (limited entry points, various item drops) and the tunnel's
    generator/water-boiler room (limited chokepoints, various item spawns; item spawn times are slow,
    so ammunition can become unsustainable, and it degrades badly on Classic and Hardcore).

## Engine scaffold

Each map is reconstructed as five independently verifiable layers:

1. **Static art** — terrain, structures, props, decals, signs, vegetation and background dressing.
2. **Collision + navigation** — player collision, infected barriers, pathfinding modifiers, traversal blockers and drop-offs.
3. **Gameplay anchors** — player starts, infected spawns, item boxes, objective anchors, fortification positions and helicopter/supply-drop support points where applicable.
4. **Environment** — sky, fog/atmosphere, color grading, lights, local/environment sounds and weather presentation.
5. **Map metadata** — map name, voting card/icon, objective support, mode restrictions and reconstruction validation data.

## Named locations visible in supplied reference material

- Barrack
- Barrack2
- ConvenienceStore
- CrispCuts
- GeneratorRoom
- Laundromat
- Marshal
- StoreBack
- TastyToast
- TastyToastKitchen
- TentBack
- TransformerRoom
- UtilityTunnel
- 1 0
- 1
- 2 0
- 2
- 3
- Finished Tanker Bypass

This list is a **reference-asset inventory**, not a claim that every map instance has already been extracted. The source-map conversion pass remains the authority for the final instance-by-instance inventory.

## Reference image files indexed (31)

- `Bypass-Barrack.png`
- `Bypass-Barrack2.png`
- `Bypass-ConvenienceStore.png`
- `Bypass-CrispCuts.png`
- `Bypass-GeneratorRoom.png`
- `Bypass-Laundromat.png`
- `Bypass-Marshal.png`
- `Bypass-StoreBack.png`
- `Bypass-TastyToast.png`
- `Bypass-TastyToastKitchen.png`
- `Bypass-TentBack.png`
- `Bypass-TransformerRoom.png`
- `Bypass-UtilityTunnel.png`
- `Bypass1-0.png`
- `Bypass1.png`
- `Bypass2-0.png`
- `Bypass2.png`
- `Bypass3.png`
- `BypassIcon.png`
- `BypassPreview.jpg`
- `BypassPreview2.jpg`
- `BypassPreview3.jpg`
- `BypassThumbnail.png`
- `Bypass_Note.PNG`
- `Card-bypass.png`
- `Finished_Tanker_Bypass.png`
- `Mossberg_Bypass.png`
- `Pistol_Bypass.png`
- `Pistol_Bypass_2.png`
- `Spiffo_Bypassv3.png`
- `Vote-bypass.png`

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
